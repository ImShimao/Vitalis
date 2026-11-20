---
id: JOURNAL-02
type: journal
priorite: P0
statut: fait
etape: 2
tags: [journal, wmi, inventaire]
cree: 2026-11-20
---

# Étape 2 : inventaire matériel

Branche : `etape-02-inventaire-materiel`. Tâche : [[T-003-inventaire-materiel]]. Fonction : [[inventaire-materiel]]. Décision : [[ADR-004-wmi-system-management]].

## Ce qui est fait

- Service `InventaireMateriel` : huit familles lues en parallèle, chacune dans son propre bloc qui ne peut pas faire tomber les autres.
- Source `ISourceWmi` ; implémentation réelle `WmiSource` (System.Management) ; tests avec `FauxSourceWmi`.
- Modèles `RapportMateriel` avec un état `Disponible`, `Partielle` ou `Indisponible` par section, et une raison en texte.
- Conversions : décodage UTF-16 (écrans), dates CIM (partie date), entiers, textes, total de mémoire.
- Le réseau utilise `MSFT_NetAdapter` (filtre `HardwareInterface = TRUE`) : `Win32_NetworkAdapter` est déprécié, vérifié dans la doc Microsoft.
- Note `04-Fonctions/inventaire-materiel.md`, ADR-004, tâche T-003.
- Page « Matériel » non modifiée : l'affichage des données est prévu à une étape ultérieure.

## Vérifications des sources

Classes vérifiées dans la documentation Microsoft Learn (2026-11-20) : `Win32_Processor`, `Win32_BaseBoard`, `Win32_BIOS`, `Win32_PhysicalMemory`, `Win32_VideoController`, `WmiMonitorID`, `MSFT_NetAdapter`, `Win32_SoundDevice`. Détail des pages dans la note de fonction.

## Résultats des tests

- `dotnet build Vitalis.sln -c Release` : 0 erreur, 0 avertissement.
- `dotnet test Vitalis.sln -c Release` : 36 réussis, 0 échec. Dont 14 nouveaux tests simulés (inventaire et conversions) et 1 lecture WMI réelle.

## Problèmes rencontrés

1. **Build bloqué** : l'application lancée sur l'écran secondaire verrouillait `Vitalis.Core.dll`. Fermée pour compiler ; relancée ensuite.
2. **Dates décalées** : `ManagementDateTimeConverter` applique un fuseau horaire, ce qui décalait les dates de BIOS et de pilote. Remplacé par une lecture directe de la partie AAAAMMJJ, sans heure.
3. **Accès à une clé inexistante** : remplacé par `GetValueOrDefault` dans le décodage des écrans.

## Non testé

- Affichage des données dans la page « Matériel » : hors périmètre de cette étape.
- Machine sans une classe WMI réelle (VM) : le cas est simulé dans les tests, pas sur une vraie machine.
- AdapterRAM au-delà de 4 Gio : limite documentée, non reproduite.
- Données de la machine de test : la lecture réelle vérifie seulement la présence du processeur et de la mémoire.

## Ce qui reste

- Valider l'étape 2 (validation de l'utilisateur), puis commiter.
- Étape 3 : santé des disques (MSFT_PhysicalDisk, élévation demandée à cette étape).
