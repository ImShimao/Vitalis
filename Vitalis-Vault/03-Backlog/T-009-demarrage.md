---
id: T-009
type: tache
priorite: P2
statut: a-faire
etape: 8
tags: [demarrage, registre, reversible]
cree: 2026-11-19
---

# T-009 : programmes au démarrage

## Contexte et but

Lire les programmes qui démarrent avec Windows et permettre de les désactiver de façon réversible, après confirmation et avec sauvegarde de l'état précédent.

## Critères d'acceptation

- [ ] Lecture des emplacements de registre Run et RunOnce et des dossiers Démarrage
- [ ] Désactivation réversible, confirmation demandée
- [ ] État précédent sauvegardé avant toute modification
- [ ] Test : désactiver puis rétablir restitue l'état d'origine
- [ ] `04-Fonctions/demarrage.md` créé, avec emplacements listés

## Fichiers de code concernés

- `src/Vitalis.Core/Demarrage/`
- `tests/Vitalis.Core.Tests/Demarrage/`

## Risques

- Une désactivation mal faite peut empêcher un logiciel de démarrer : toujours réversible.
- Ne pas toucher aux services système à cette étape.

## Liens

- Fonction : `demarrage` (note à créer à l'étape 8)
- Roadmap : [[02-Roadmap]]
