---
id: T-003
type: tache
priorite: P0
statut: valide
etape: 2
tags: [inventaire, wmi, cim]
cree: 2026-11-19
---

# T-003 : inventaire matériel

## Contexte et but

Lire le matériel via WMI/CIM : CPU, carte mère, BIOS, RAM, GPU et pilote GPU, écrans, réseau, audio. Chaque champ absent doit afficher « indisponible », jamais une valeur inventée.

## Critères d'acceptation

- [x] Service WMI avec lecture des 8 familles de matériel (`src/Vitalis.Core/Materiel/`)
- [x] « Indisponible » géré pour chaque section et chaque classe ; valeur absente reste null
- [x] Test : machine complète simulée
- [x] Test : machine où une classe WMI manque
- [x] Source de chaque classe notée dans `04-Fonctions/inventaire-materiel.md` avec sa date de vérification (2026-11-20)
- [x] Lecture réelle sur la machine de test : processeur et mémoire lus

## Fichiers de code concernés

- `src/Vitalis.Core/Materiel/` (service et modèles)
- `tests/Vitalis.Core.Tests/Materiel/` (données simulées)
- `04-Fonctions/inventaire-materiel.md` (à créer à cette étape)

## Risques

- Certaines classes WMI absentes sur les VM ou pilotes anciens.
- Lectures WMI lentes : prévoir l'asynchrone.
- Les classes et champs doivent être vérifiés dans la documentation officielle Microsoft avant usage.

## Liens

- Fonction : `inventaire-materiel` (note à créer à l'étape 2)
- Roadmap : [[02-Roadmap]]
