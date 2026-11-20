---
id: T-006
type: tache
priorite: P1
statut: fait
etape: 5
tags: [tableau-de-bord, score, asynchrone]
cree: 2026-11-19
---

# T-006 : tableau de bord

## Contexte et but

Afficher un score global sur 100 et des cartes de synthèse, avec un bouton « Analyser maintenant », la date de dernière analyse et une analyse asynchrone avec progression et annulation. Une carte en erreur ne doit jamais bloquer les autres.

## Critères d'acceptation

- [x] Score global sur 100 et formule documentée avec les poids de chaque composant ([[ADR-008-score]])
- [x] Analyse asynchrone avec barre de progression et annulation (annulation : attend la fin d'un appel COM)
- [x] Date de dernière analyse affichée
- [x] Test : une carte en erreur, les autres s'affichent
- [x] `04-Fonctions/tableau-de-bord.md` créé

## Fichiers de code concernés

- `src/Vitalis.App/ViewModels/TableauDeBordViewModel.cs` (à créer)
- `src/Vitalis.Core/Score/`
- `tests/Vitalis.Core.Tests/Score/`

## Risques

- Poids du score arbitraires : à expliquer dans la note de fonction.
- Annulation : ne pas laisser un service en cours de lecture WMI.

## Liens

- Fonction : `tableau-de-bord` (note à créer à l'étape 5)
- Roadmap : [[02-Roadmap]]
