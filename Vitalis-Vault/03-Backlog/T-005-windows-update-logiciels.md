---
id: T-005
type: tache
priorite: P1
statut: fait
etape: 4
tags: [windows-update, winget, lecture-seule]
cree: 2026-11-19
---

# T-005 : Windows Update et logiciels

## Contexte et but

Lister les mises à jour Windows et pilotes disponibles (API Windows Update Agent, COM) et les mises à jour d'applications (API WinGet, COM). Lecture seule : aucune installation à cette étape.

## Critères d'acceptation

- [x] Liste des mises à jour Windows en attente et date de la dernière installation (vérifié sur la machine : 4 mises en attente)
- [x] Liste des mises à jour d'applications (winget, commande `winget upgrade` ; API COM non vérifiée, voir [[ADR-007-winget-cli]]) : 30 logiciels listés sur la machine de test
- [x] Gestion des erreurs : API COM en échec → section indisponible avec raison
- [x] Aucune installation déclenchée (lecture seule)
- [x] `04-Fonctions/windows-update.md` créé ; `logiciels-winget.md` créé comme « non réalisé »

## Fichiers de code concernés

- `src/Vitalis.Core/Updates/`
- `tests/Vitalis.Core.Tests/Updates/`

## Risques

- WinGet n'est pas installé partout : prévoir le message « indisponible ».
- L'API WUA peut être lente et doit tourner hors du thread d'interface.
- Les interfaces COM et leurs identifiants doivent être vérifiés dans la documentation officielle avant usage.

## Liens

- Fonctions : `windows-update`, `logiciels-winget` (notes à créer à l'étape 4)
- Roadmap : [[02-Roadmap]]
