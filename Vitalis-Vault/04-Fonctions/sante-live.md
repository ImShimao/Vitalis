---
id: F-sante-live
type: fonction
priorite: P2
statut: fait
etape: 10
tags: [fonction, page, capteurs]
cree: 2026-11-20
---

# Santé live

## Couverture

Températures, charges, fréquences, ventilateurs et consommation, mis à jour toutes les deux secondes, avec bouton Pause. Une carte par composant (température principale, utilisation, badge Normal / Chaud / Trop chaud), puis le détail de chaque capteur. Code : `src/Vitalis.Core/SanteLive/CapteursLive.cs`, page `src/Vitalis.App/Views/SanteLivePage.xaml`. Décision : [[ADR-016-sante-live-librehardwaremonitor]].

## Seuils (°C, attention / critique)

| Composant | Attention | Critique |
|---|---|---|
| Processeur | 85 | 95 |
| Carte graphique | 80 | 90 |
| Stockage | 55 | 65 |
| Autres | 70 | 85 |

## Valeurs non lues

Valeur NaN ou 0 °C renvoyée par LibreHardwareMonitor = capteur non lu, affiché « n/d ». Aucune valeur inventée.

## Pilote PawnIO

Températures du processeur seulement avec PawnIO. Absent : message d'explication et bouton « Installer PawnIO » (winget, après confirmation). Risques : anti-triches et Intégrité de la mémoire.

## Tests

`CapteursLiveTests` : seuils, choix de la température et de la charge principales, capteur non lu, lecture réelle sans exception.

Liens : [[T-011-sante-live]].
