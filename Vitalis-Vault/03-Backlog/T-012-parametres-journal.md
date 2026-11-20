---
id: T-012
type: tache
priorite: P1
statut: fait
etape: 11
tags: [parametres, journal, planification, notifications]
cree: 2026-11-19
---

# T-012 : paramètres et journal d'erreurs

## Contexte et but

Ajouter le thème, l'analyse planifiée (Planificateur de tâches), les notifications, les logs locaux et le mode diagnostic.

## Critères d'acceptation

- [x] Thème réglable dans les paramètres
- [x] Analyse planifiée créée dans le Planificateur de tâches, droits demandés expliqués
- [x] Notifications Windows
- [x] Logs locaux uniquement, rotation quotidienne, niveaux Info, Warning, Error
- [x] Mode diagnostic : ce qui n'est jamais enregistré est écrit dans `04-Fonctions/journal-erreurs.md`
- [x] `04-Fonctions/parametres.md` et `04-Fonctions/journal-erreurs.md` créés

## Fichiers de code concernés

- `src/Vitalis.Core/Journal/`, `src/Vitalis.Core/Planification/`
- `tests/Vitalis.Core.Tests/Journal/`

## Risques

- Question ouverte : Serilog est dans la pile mais aucune étape ne le met en place. À décider avant l'étape 1 (voir [[etape-00]]).
- Les logs ne doivent jamais contenir de données personnelles.

## Liens

- Fonctions : `parametres`, `journal-erreurs` (notes à créer à l'étape 11)
- Roadmap : [[02-Roadmap]]
