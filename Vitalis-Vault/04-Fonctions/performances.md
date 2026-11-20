---
id: F-performances
type: fonction
priorite: P2
statut: fait
etape: 9
tags: [fonction, performances, benchmark, historique]
cree: 2026-11-20
---

# Performances

## Couverture

Trois benchmarks mesurés (processeur, mémoire vive, disque), un GPU non mesuré, un historique des analyses et la comparaison avec la précédente. Code : `src/Vitalis.Core/Performances/`, page `src/Vitalis.App/Views/PerformancesPage.xaml`. Tâche : [[T-010-performances]].

## Méthodes (chaque résultat les affiche)

| Composant | Méthode | Limites |
|---|---|---|
| Processeur | Crible d'Ératosthène jusqu'à 2 000 000, médiane de 3 exécutions, un seul cœur. Score = 1000 / durée (ms). | Un seul cœur : ne reflète ni le multi-thread ni les vraies charges. |
| Mémoire vive | Copie de 256 Mio (Buffer.BlockCopy), médiane de 3 exécutions. | Mesure la copie du processus : ni latence ni configuration des barrettes. |
| Disque | Écriture (WriteThrough) puis lecture de 256 Mio séquentiels, dans le dossier temporaire ; fichier supprimé ensuite. | Le cache Windows peut influencer la lecture ; pas d'accès aléatoires. |
| Carte graphique | **Non mesurée.** Aucune méthode simple et documentée retenue : pas de score inventé. | À définir (API graphique, durée, charge). |

## Historique et comparaison

- Chaque analyse est ajoutée à `%LOCALAPPDATA%\Vitalis\performances.json`.
- La variation (en %) est calculée entre l'analyse précédente et la nouvelle, composant par composant. Un composant absent d'une des deux analyses n'a pas de variation.
- Les benchmarks ne sont pas des scores officiels : ils servent à comparer deux analyses sur la même machine.

## Tests associés

- `BenchmarksTests` : crible (10, 100, 1), médiane impaire et paire, CPU avec méthode, GPU non mesuré sans score inventé, disque avec suppression du fichier temporaire, variation, historique.
- Réel : les trois benchmarks ont tourné dans l'application (processeur, mémoire, disque) ; première analyse, donc pas de comparaison.

## Limites connues

- Pas de comparaison avec des machines de référence : la variation ne signale que l'écart avec la machine elle-même, dans les mêmes conditions.
- Les mesures varient selon la charge de la machine pendant le test.

Liens : [[T-010-performances]].

## Carte graphique et indice Windows (0.2.0)

Après les trois mesures, Vitalis lance « winsat formal » (évaluation officielle de Windows, 1 à 3 minutes, administrateur), puis lit `Win32_WinSAT` : score 3D (D3DScore) et bureau (GraphicsScore) pour la carte graphique, et une carte « Indice Windows (WinSAT) » avec les cinq notes. Différence expliquée : WinSAT note de 1,0 à 9,9 (plafond atteint par les cartes récentes), Vitalis mesure une durée ou un débit brut, comparable entre deux analyses du même PC. Sans évaluation (état 3) : « Non mesuré », aucun score inventé. Tests : `BenchmarksTests.WinSat_*`.
