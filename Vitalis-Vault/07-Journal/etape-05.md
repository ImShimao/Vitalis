---
id: JOURNAL-05
type: journal
priorite: P1
statut: fait
etape: 5
tags: [journal, tableau-de-bord, score]
cree: 2026-11-20
---

# Étape 5 : tableau de bord

Branche : `etape-05-tableau-de-bord`. Tâche : [[T-006-tableau-de-bord]]. Fonction : [[tableau-de-bord]]. Décision : [[ADR-008-score]].

## Validation de l'étape 4

L'utilisateur a délégué la décision (« je te laisse la main ») : l'étape 4 est marquée validée par délégation, avec winget par ligne de commande (ADR-007).

## Ce qui est fait

- `Score` : trois composants (Disques 40, Mises à jour 35, Espace libre 25), composants indisponibles retirés du calcul.
- `TableauDeBordService` : quatre analyses en parallèle, cartes isolées, progression, annulation.
- Page « Tableau de bord » : score, bouton « Analyser maintenant », annulation, barre de progression, date de dernière analyse (fichier local), cartes.
- Page affichée par défaut au démarrage (premier élément du menu).

## Résultats des tests

- `dotnet test` : 95 réussis, 0 échec.
- Build : 0 erreur, 0 avertissement.
- Analyse réelle dans l'application : score 86 / 100, toutes les cartes remplies.

## Problèmes rencontrés et corrigés

1. **Composant « mises à jour » compté à 100 quand Windows Update est en erreur** : le service comptait « 0 mise en attente ». Corrigé : composant indéterminé. Trouvé par le test.
2. **Progression qui n'atteignait jamais 100 %** : divisée par 4 au lieu de 3 analyses. Corrigé. Trouvé par le test.
3. **Contrôle `UniformGrid` absent de WinUI 3** : le compilateur XAML échouait sans message clair. Trouvé par réduction du XAML ; remplacé par une pile verticale.
4. **Date de dernière analyse non rafraîchie** après l'analyse : corrigé.
5. **Carte « 34 en attente » incohérente avec le score** (qui ne compte que Windows) : la carte affiche maintenant « 4 Windows · 30 logiciels ».

## Non testé

- Annulation pendant la recherche Windows Update : l'annulation n'est testée qu'avant le lancement. Un appel COM en cours ne peut pas être interrompu (comportement documenté).
- Score sur une machine sans disque SMART ou sans mise à jour : cas simulés seulement.

## Ce qui reste

- Valider l'étape 5, et en particulier la formule du score (ADR-008).
