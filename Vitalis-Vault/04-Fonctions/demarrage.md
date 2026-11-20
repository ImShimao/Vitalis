---
id: F-demarrage
type: fonction
priorite: P2
statut: fait
etape: 8
tags: [fonction, demarrage, registre, reversible]
cree: 2026-11-20
---

# Démarrage

## Couverture

Liste les programmes lancés avec Windows et permet de les désactiver de façon **réversible**, après confirmation. Code : `src/Vitalis.Core/Demarrage/`, page `src/Vitalis.App/Views/DemarragePage.xaml`. Tâche : [[T-009-demarrage]]. Décision : [[ADR-011-demarrage-run]].

## Emplacements lus

- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (lecture et écriture).
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` (lecture seule : l'écriture demande les droits administrateur et n'est jamais faite).
- Chemin vérifié dans la doc Microsoft « Run and RunOnce registry keys » (consultée le 2026-11-20).

Non lus : dossiers Démarrage, RunOnce, tâches planifiées. À ajouter si nécessaire.

## Désactivation réversible

1. Confirmation explicite (boîte de dialogue : ce qui change, ce qui ne change pas).
2. La commande exacte est sauvegardée dans `%LOCALAPPDATA%\Vitalis\demarrage-desactive.json`.
3. La valeur est retirée de la clé Run. Le programme n'est pas supprimé du disque.
4. « Réactiver » réécrit la commande sauvegardée à l'identique, puis retire la sauvegarde.

Sans confirmation, la désactivation est refusée (vérifié par test).

## Pourquoi pas StartupApproved

Windows garde l'état « activé / désactivé » dans `StartupApproved\Run`, mais le format binaire n'est pas documenté par Microsoft (seulement observé par la communauté). Plutôt que de l'écrire à l'aveugle, Vitalis retire la valeur de la clé Run documentée.

## Cas « indisponible »

- Clé absente : liste vide, sans erreur.
- Erreur de lecture : texte d'erreur sur la page.

## Tests associés

- `DemarrageServiceTests` (simulés) : refus sans confirmation, sauvegarde et retrait, restauration à l'identique, pas de doublon, lecture.
- Réel : liste lue sur la machine de développement (18 programmes). **Aucune désactivation réelle n'a été faite** : elle modifierait le démarrage de l'utilisateur sans son accord.

Liens : [[ADR-011-demarrage-run]].
