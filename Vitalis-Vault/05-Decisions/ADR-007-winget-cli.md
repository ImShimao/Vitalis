---
id: ADR-007
type: adr
priorite: P1
statut: fait
etape: 4
tags: [winget, cli, dependance, risque]
cree: 2026-11-20
---

# ADR-007 : mises à jour de logiciels via la commande winget (lecture seule)

## Contexte

L'étape 4 doit lister les mises à jour de logiciels (winget). La spec demande l'API COM `Microsoft.Management.Deployment`, mais trois recherches ciblées dans la documentation Microsoft n'ont trouvé aucune page de cette API, et la spec interdit de l'utiliser sans vérification.

## Décision

Lancer la commande `winget upgrade` en processus enfant, en **lecture seule**, et lire son tableau texte. Le choix a été validé par l'utilisateur.

## Alternatives écartées

- **API COM WinGet** : non vérifiable dans la doc ; à reprendre si la doc est retrouvée.
- **PowerShell `Get-WinGetPackage`** : module non vérifié dans ce projet, et lancement d'un processus de toute façon.
- **Lire les fichiers de cache de winget** : non documenté, et risque de casse à chaque version.

## Conséquences

- Le format texte peut changer : les tests utilisent la sortie réelle de la version 1.29.380. Une autre version demandera de vérifier les tests.
- La lecture à partir de la droite tolère les noms avec espaces et l'identifiant collé au nom.
- Le processus winget est borné (3 minutes) ; l'échec de winget n'affecte pas Windows Update.
- Aucune option d'installation n'est utilisée : le code ne peut pas modifier le système.
- À reprendre : si la documentation de l'API COM WinGet est retrouvée, la remplacer par l'API (ADR à compléter).

Liens : [[logiciels-winget]], [[ADR-006-windows-update-com]].
