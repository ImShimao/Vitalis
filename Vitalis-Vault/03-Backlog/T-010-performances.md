---
id: T-010
type: tache
priorite: P2
statut: a-faire
etape: 9
tags: [performances, benchmark, historique]
cree: 2026-11-19
---

# T-010 : performances

## Contexte et but

Mesurer CPU, RAM, disque et GPU, garder un historique et comparer les analyses. Chaque résultat indique la méthode utilisée.

## Critères d'acceptation

- [ ] Benchmarks CPU, RAM, disque et GPU
- [ ] Méthode, durée et limites affichées avec chaque résultat
- [ ] Historique des analyses et comparaison entre deux analyses
- [ ] Score WinSAT comparé aux autres méthodes, avec la différence expliquée
- [ ] `04-Fonctions/performances.md` créé

## Fichiers de code concernés

- `src/Vitalis.Core/Performances/`
- `tests/Vitalis.Core.Tests/Performances/`

## Risques

- Les résultats varient selon la charge de la machine : répéter les mesures.
- Un benchmark long bloque l'utilisateur : prévoir l'annulation.

## Liens

- Fonction : `performances` (note à créer à l'étape 9)
- Roadmap : [[02-Roadmap]]
