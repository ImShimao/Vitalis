---
id: T-011
type: tache
priorite: P2
statut: fait
etape: 10
tags: [temperatures, librehardwaremonitor, risque]
cree: 2026-11-19
---

# T-011 : santé live

## Contexte et but

Afficher températures et fréquences en temps réel avec LibreHardwareMonitor. Cette étape est la plus risquée (pilote noyau, antivirus, Vanguard). Elle démarre seulement si l'étape 9 est validée.

## Critères d'acceptation

- [x] Décision de dépendance (LibreHardwareMonitor, licence MPL-2.0) validée par écrit avec l'utilisateur
- [x] Risques pilote noyau, antivirus et Vanguard documentés dans `04-Fonctions/sante-live.md`
- [x] Mode dégradé si le pilote est bloqué
- [x] Test : capteur absent affiche « indisponible »

## Fichiers de code concernés

- `src/Vitalis.Core/SanteLive/`
- `tests/Vitalis.Core.Tests/SanteLive/`

## Risques

- Pilote noyau : peut être bloqué par Windows ou par un antivirus.
- Vanguard (anti-triche) peut bloquer ou réagir à l'accès matériel bas niveau.

## Liens

- Fonction : `sante-live` (note à créer à l'étape 10)
- Roadmap : [[02-Roadmap]]
