---
id: ADR-002
type: adr
priorite: P0
statut: fait
etape: 1
tags: [interface, pages, navigation]
cree: 2026-11-20
---

# ADR-002 : les dix pages de la v1 et le menu de boutons

## Contexte

La spec demande dix pages vides. La roadmap décrit onze sujets : Tableau de bord, Inventaire, Disques, Windows Update, Logiciels, Rapport, Pilotes, Démarrage, Performances, Santé live, Paramètres et journal d'erreurs.

## Décision

1. **Dix pages** : Windows Update et Logiciels sont regroupés dans « Mises à jour ». Le journal d'erreurs est rangé dans « Paramètres ». Les fichiers `windows-update.md`, `logiciels-winget.md` et `journal-erreurs.md` restent des notes de fonction, pas des pages.
2. **Une page générique** (`PagePlaceholder`) pour l'étape 1. Chaque page aura sa propre classe quand son étape commencera.
3. **Menu latéral en boutons** (`Button` dans un `StackPanel`) à la place du `NavigationView`. Raison : le `NavigationView` faisait planter l'application au démarrage. La cause réelle était l'absence de `XamlControlsResources` dans `App.xaml`, ajouté ensuite. Le `NavigationView` n'a pas été réessayé avec ce correctif.
4. **Raccourcis** : Ctrl+1 à Ctrl+9 pour les neuf premières pages, Ctrl+0 pour la dixième.

## Alternatives écartées

- Onze pages : écarte la limite de dix de la spec sans raison technique.
- Une classe de page par fonction dès l'étape 1 : travail inutile avant que la fonction existe.

## Conséquences

- Le titre et l'étape de chaque page viennent de `CataloguePages` (`Vitalis.Core`), testés.
- Le menu actuel n'a pas la sélection visuelle standard de WinUI : la page active est en gras.
- À reconsidérer : revenir au `NavigationView` une fois le correctif `XamlControlsResources` vérifié.

Liens : [[ADR-001-winui3]], [[ADR-003-portable]], [[T-002-shell-interface]].
