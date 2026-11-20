---
id: F-sante-disques
type: fonction
priorite: P1
statut: fait
etape: 3
tags: [fonction, disques, smart, elevation]
cree: 2026-11-20
---

# Santé des disques et espace libre

## Couverture

Liste les disques physiques (type, taille, statut de santé) et, si les droits le permettent, leurs compteurs SMART (température, usure, heures de marche, erreurs de lecture). Affiche aussi l'espace libre de chaque lecteur fixe, avec un statut coloré doublé d'un texte. Code : `src/Vitalis.Core/Disques/`, page `src/Vitalis.App/Views/DisquesPage.xaml`. Tâche : [[T-004-sante-disques]]. Décisions : [[ADR-005-elevation-a-la-demande]].

## Sources (vérifiées dans la documentation Microsoft le 2026-11-20)

| Donnée | Classe | Espace de noms | Documentation |
|---|---|---|---|
| Disque physique, santé, type | `MSFT_PhysicalDisk` | `root\Microsoft\Windows\Storage` | windows-hardware/drivers/storage/msft-physicaldisk |
| Compteurs SMART | `MSFT_StorageReliabilityCounter` | `root\Microsoft\Windows\Storage` | windows-hardware/drivers/storage/msft-storagereliabilitycounter |
| Lien disque ↔ compteurs | `MSFT_PhysicalDiskToStorageReliabilityCounter` | `root\Microsoft\Windows\Storage` | windows-hardware/drivers/storage/msft-physicaldisktostoragereliabilitycounter |
| Espace libre | `Win32_LogicalDisk` (`DriveType = 3`) | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-logicaldisk |

## Champs lus

- **MSFT_PhysicalDisk** : DeviceId, FriendlyName, MediaType (3 = HDD, 4 = SSD, 5 = SCM, sinon inconnu), HealthStatus (0 = sain, 1 = attention, 2 = critique, 5 = inconnu), Size.
- **MSFT_StorageReliabilityCounter** : DeviceId, Temperature (°C), TemperatureMax, Wear (%), PowerOnHours (sur 16 bits, peut déborder sur les vieux disques), ReadErrorsTotal.
- **Win32_LogicalDisk** : DeviceID, VolumeName, Size, FreeSpace (octets).

Non lu : `OperationalStatus`, dont la table de valeurs n'a pas été vérifiée pour cette classe.

## Jointure

**Constat sur la machine de développement (en administrateur, 2026-11-20)** : Windows ne renvoie **aucun** compteur `MSFT_StorageReliabilityCounter` et **aucun** lien d'association, pour les 5 disques (SSD, HDD et USB). Ce n'est pas un problème de jointure : les compteurs ne sont pas fournis par le pilote ou le matériel. L'application l'affiche comme tel. Conséquence : sur cette machine, la température, l'usure et les heures de marche ne peuvent pas être lues par cette voie.

**Jointure corrigée** (dans l'ordre) :
1. Par l'association `MSFT_PhysicalDiskToStorageReliabilityCounter` : on relie le disque et le compteur par le GUID final de leur `ObjectId`.
2. À défaut, par DeviceId (ancien comportement, gardé en secours).

Un journal de diagnostic (`%LOCALAPPDATA%\Vitalis\diagnostic-smart.txt`) n'est écrit qu'en administrateur : il contient les DeviceId, les GUID et les noms de modèle, aucun numéro de série. **Non encore vérifié** : la relance en administrateur du nouveau build est à faire pour lire ce journal.

## Statuts

- **Disque** : `HealthStatus` du disque (Sain, Attention, Critique, Inconnu). Affichage : « ✔ Sain », « ⚠ Attention », « ✖ Critique », « ? Inconnu ».
- **Lecteur**, selon l'espace libre en pourcentage (décision de l'étape 3) :
  - sain : 20 % libre ou plus ;
  - attention : entre 10 % (inclus) et 20 % ;
  - critique : moins de 10 %, ou taille inconnue.

Les seuils sont des constantes (`AnalyseDisques.SeuilAttention`, `SeuilCritique`), testées aux bornes.

## Droits et élévation

- Sans droits administrateur, les disques et l'espace libre sont lus normalement. Seuls les compteurs SMART sont absents, et la page l'indique (« Compteurs SMART non lisibles … »).
- La relance en administrateur n'a lieu qu'après un clic sur le bouton, puis une confirmation dans une boîte de dialogue qui explique ce qui change (rien n'est modifié sur le disque) ; Windows demande ensuite l'élévation (UAC).
- Si l'utilisateur refuse l'UAC, la page reste en place et affiche « Relance annulée ».

## Cas « indisponible » et absence de donnée

- Classe `MSFT_PhysicalDisk` absente ou erreur : section disques « indisponible » avec la raison ; l'espace libre reste affiché.
- Aucun disque ou lecteur renvoyé : « indisponible » avec la raison.
- Compteur sans disque correspondant, ou compteurs refusés : SMART « non lu », jamais inventé.
- Taille de lecteur absente : statut « critique », pourcentage non affiché.

## Limites connues

- Beaucoup de disques USB ne remontent pas SMART, ni même un type de média (« type inconnu » sur le disque USB testé).
- `PowerOnHours` est sur 16 bits : valeur fausse au-delà de 65 535 heures (environ 7,5 ans de marche).
- La santé `HealthStatus` peut être « sain » alors que le cluster ou le pool a un problème (doc Microsoft sur Storage Spaces Direct).

## Tests associés

- Simulés (`AnalyseDisquesTests`) : disques complets (statuts, SMART), compteurs refusés (disques conservés), compteur sans correspondance, classe absente, filtre `DriveType = 3`, pourcentages et seuils, mapping HealthStatus et MediaType, annulation.
- Réel (`AnalyseDisquesReelTests`) : les lecteurs fixes sont lus sur la machine de test. Vérifié sur la machine de développement : 5 disques physiques et 5 lecteurs fixes lus, SMART refusé sans administrateur.

Liens : [[ADR-005-elevation-a-la-demande]], [[ADR-002-dix-pages]].
