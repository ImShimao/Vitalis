---
id: T-007
type: tache
priorite: P1
statut: fait
etape: 6
tags: [rapport, html, pdf, vie-privee]
cree: 2026-11-19
---

# T-007 : rapport HTML et PDF

## Contexte et but

Exporter l'analyse en HTML puis en PDF. Le rapport ne contient jamais de données personnelles : nom d'utilisateur, adresse MAC, numéro de série masqué.

## Critères d'acceptation

- [x] Export HTML complet (matériel, disques, mises à jour, score)
- [x] Export PDF (Edge sans affichage, [[ADR-009-pdf-edge]]) — rendu non vérifié visuellement
- [x] Test : aucun nom d'utilisateur, aucune adresse MAC, aucun numéro de série (numéro de série jamais lu)
- [x] Structure du rapport décrite dans `04-Fonctions/rapport.md`

## Fichiers de code concernés

- `src/Vitalis.Core/Rapport/`
- `tests/Vitalis.Core.Tests/Rapport/`

## Risques

- Choix de la bibliothèque PDF : vérifier sa licence avant usage.
- Une donnée personnelle peut se glisser dans un champ texte libre : filtrer à la source.

## Liens

- Fonction : `rapport` (note à créer à l'étape 6)
- Roadmap : [[02-Roadmap]]
