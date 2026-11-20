---
id: F-tableau-de-bord
type: fonction
priorite: P1
statut: fait
etape: 5
tags: [fonction, tableau-de-bord, score]
cree: 2026-11-20
---

# Tableau de bord

## Couverture

Score global sur 100, cartes de synthèse (Matériel, Disques, Espace libre, Mises à jour), bouton « Analyser maintenant », date de la dernière analyse, progression et annulation. Code : `src/Vitalis.Core/TableauDeBord/`, page `src/Vitalis.App/Views/TableauDeBordPage.xaml`. Tâche : [[T-006-tableau-de-bord]]. Décision : [[ADR-008-score]].

## Formule du score

| Composant | Poids | Valeur (sur 100) |
|---|---|---|
| Disques | 40 | Moyenne par disque : Sain 100, Attention 50, Critique 0. Disques de santé inconnue exclus. |
| Mises à jour | 35 | 100 − 10 × mises **Windows** en attente, − 20 si aucune installation Windows depuis plus de 30 jours. Plancher 0. |
| Espace libre | 25 | Moyenne par lecteur : Sain 100, Attention 60, Critique 20. |

- Score global = moyenne pondérée des composants **connus**. Un composant indisponible n'est pas compté : son poids est réparti sur les autres, le score reste sur 100.
- Si aucun composant n'est connu : « Score global : indisponible ».
- Les **logiciels** (winget) ne sont pas dans le score : ils sont affichés sur la carte Mises à jour, à part. Choix à valider.
- Le matériel n'a pas de valeur de score : il est affiché sans note (son rôle est informatif).

## Cartes

- **Matériel** : premier processeur, mémoire totale. État : celui de la section processeur.
- **Disques** : nombre de disques, pire état. Un disque sans compteur SMART reste compté pour sa santé (HealthStatus).
- **Espace libre** : lecteur le plus chargé. Une carte sans lecteur lisible est « indisponible ».
- **Mises à jour** : nombre de mises Windows et de logiciels, dernière installation Windows.

Chaque carte est isolée : une erreur ne bloque ni les autres cartes ni le score.

## Analyse

- Les quatre analyses (matériel, disques, mises à jour) tournent en parallèle.
- La progression est signalée à chaque analyse terminée (33, 66, 100 %).
- L'annulation est possible. Les appels COM de Windows Update ne s'interrompent pas en cours d'appel : l'annulation attend la fin de l'appel.
- La date de dernière analyse est enregistrée dans `%LOCALAPPDATA%\Vitalis\derniere-analyse.txt` et affichée à l'ouverture.

## Résultat sur la machine de développement (2026-11-20)

- Score : 86 / 100.
- Matériel : Intel Core i7-10700KF, 16 Gio.
- Disques : 5, pire état Sain.
- Espace libre : lecteur le plus chargé à 72 % libre.
- Mises à jour : 4 Windows, 30 logiciels ; dernière installation Windows le 20 novembre 2026.

## Tests associés

- `ScoreTests` : formules des trois composants, bornes (0 et 100), composants indéterminés, pondération avec un composant manquant.
- `TableauDeBordServiceTests` : quatre cartes quand tout va bien, carte en erreur sans effet sur les autres, composant « mises à jour » indéterminé quand Windows Update est en erreur, progression croissante jusqu'à 100 %, annulation.
- Réel : analyse complète lancée dans l'application (86 / 100, toutes les cartes remplies).

Liens : [[ADR-008-score]], [[inventaire-materiel]], [[sante-disques]], [[windows-update]], [[logiciels-winget]].
