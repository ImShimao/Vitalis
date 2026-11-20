---
id: T-008
type: tache
priorite: P1
statut: a-faire
etape: 7
tags: [pilotes, liens-officiels]
cree: 2026-11-19
---

# T-008 : pilotes

## Contexte et but

Lister les pilotes, signaler ceux de plus de 2 ans, et proposer les liens officiels par fabricant. Chaque URL est vérifiée et sa date de vérification est notée.

## Critères d'acceptation

- [ ] Liste des pilotes avec date et version
- [ ] Règle des 2 ans appliquée et expliquée
- [ ] Table des URL par fabricant, chacune vérifiée
- [ ] Date de vérification notée pour chaque URL
- [ ] `04-Fonctions/pilotes.md` créé

## Fichiers de code concernés

- `src/Vitalis.Core/Pilotes/`
- `tests/Vitalis.Core.Tests/Pilotes/`

## Risques

- Les URL fabricants changent : prévoir une date de vérification et une procédure de contrôle.
- Ne jamais installer de pilote automatiquement.

## Liens

- Fonction : `pilotes` (note à créer à l'étape 7)
- Roadmap : [[02-Roadmap]]
