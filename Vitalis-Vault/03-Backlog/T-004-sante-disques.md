---
id: T-004
type: tache
priorite: P1
statut: valide
etape: 3
tags: [disques, smart, elevation]
cree: 2026-11-19
---

# T-004 : santé des disques

## Contexte et but

Afficher le statut SMART, la température, l'usure et l'espace libre de chaque disque. L'élévation n'est demandée qu'à cette étape, avec un message explicatif.

## Critères d'acceptation

- [x] Lecture via `MSFT_PhysicalDisk` et `MSFT_StorageReliabilityCounter` (jointure par DeviceId, non vérifiée avec SMART réel)
- [x] Espace libre par lecteur, statut coloré doublé d'un texte
- [x] Message explicatif avant la demande d'élévation (boîte de dialogue, [[ADR-005-elevation-a-la-demande]])
- [x] Test avec données simulées : disque sans compteurs de fiabilité
- [x] `04-Fonctions/sante-disques.md` créé, avec limites selon le matériel

## Fichiers de code concernés

- `src/Vitalis.Core/Disques/`
- `tests/Vitalis.Core.Tests/Disques/`
- `04-Fonctions/sante-disques.md` (à créer)

## Risques

- Beaucoup de disques USB ou certains NVMe ne remontent pas SMART.
- L'élévation UAC peut être refusée : l'application doit continuer sans SMART.

## Liens

- Fonction : `sante-disques` (note à créer à l'étape 3)
- Roadmap : [[02-Roadmap]]
