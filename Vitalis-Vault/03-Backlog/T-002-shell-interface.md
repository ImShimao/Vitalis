---
id: T-002
type: tache
priorite: P0
statut: fait
etape: 1
tags: [interface, winui, navigation]
cree: 2026-11-19
---

# T-002 : shell de l'interface

## Contexte et but

Créer la fenêtre WinUI 3 avec Mica, une NavigationView et les 10 pages vides, avec thème clair par défaut et sombre automatique. But : toutes les pages s'ouvrent, et la navigation fonctionne au clavier.

## Critères d'acceptation

- [x] Fenêtre WinUI 3 qui démarre (vérifiée : titre « Vitalis », processus vivant)
- [x] Mica sur Windows 11 : vérifié (contenu RGB 32 avec Mica, 0 sans)
- [x] Menu avec les 10 pages (boutons, pas NavigationView : voir [[ADR-002-dix-pages]])
- [x] Les 10 pages s'ouvrent au clic : titre et étape corrects pour chacune
- [x] Thème clair et sombre suivi du système : vérifiés par capture ; barre de titre corrigée en sombre
- [x] Navigation clavier (Ctrl+1 à Ctrl+0) : vérifiée (codes de touche bruts)
- [x] Contraste WCAG AA vérifié sur les deux thèmes (test automatisé)
- [x] `04-Fonctions/` contient un fichier par page (10 fichiers)

## Fichiers de code concernés

- Nouveau projet `src/Vitalis.App/` (WinUI 3)
- `src/Vitalis.App/Views/` (10 pages), `src/Vitalis.App/ViewModels/`

## Risques

- Outillage WinUI 3 (charge de travail Visual Studio, SDK Windows) à vérifier.
- Mica disponible seulement sur Windows 11.
- Contraste à vérifier sur les deux thèmes.

## Liens

- Décision : [[ADR-001-winui3]]
- Fonction : pas encore de note (les fichiers `04-Fonctions/` sont créés à cette étape)
- Roadmap : [[02-Roadmap]]
