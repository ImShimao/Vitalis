---
id: ACCUEIL
type: doc
statut: en-cours
cree: 2026-11-19
---

# Vitalis : tableau de bord

**Étape en cours :** Étape 0 : Mise en place (statut : Fait, en attente de votre validation, voir [[02-Roadmap]])

**Dernière décision :** [[ADR-001-winui3]] (WinUI 3)

**Code :** dépôt Git séparé, dossier `Vitalis/` à côté de ce coffre.

## Tâches P0 et P1 non faites

Requête Dataview (si le plugin Dataview est installé) :

```dataview
TABLE priorite, statut, etape
FROM "03-Backlog"
WHERE (priorite = "P0" OR priorite = "P1") AND statut != "fait" AND statut != "valide"
SORT etape ASC
```

Liste à la main (à tenir à jour, sans Dataview) :

- [[T-001-mise-en-place]] : P0, étape 0, fait (validation en attente)
- [[T-002-shell-interface]] : P0, étape 1
- [[T-003-inventaire-materiel]] : P0, étape 2
- [[T-004-sante-disques]] : P1, étape 3
- [[T-005-windows-update-logiciels]] : P1, étape 4
- [[T-006-tableau-de-bord]] : P1, étape 5
- [[T-007-rapport]] : P1, étape 6
- [[T-008-pilotes]] : P1, étape 7
- [[T-012-parametres-journal]] : P1, étape 11
- [[T-013-distribution]] : P1, étape 12

## Dernières décisions

- [[ADR-001-winui3]] : interface en WinUI 3 (2026-11-19)

## Liens utiles

- [[01-Vision]] : objectif, public, périmètre v1
- [[02-Roadmap]] : étapes et statuts
- [[matrice-compatibilite]] : machines testées
- [[etape-00]] : journal de l'étape 0
