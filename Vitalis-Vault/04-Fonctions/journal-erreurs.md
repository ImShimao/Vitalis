---
id: F-journal-erreurs
type: fonction
priorite: P2
statut: partiel
etape: 11
tags: [fonction, journal, logs, diagnostic]
cree: 2026-11-20
---

# Journal d'erreurs

## Emplacement

`%LOCALAPPDATA%\Vitalis\journaux\vitalis-AAAAMMJJ.log` (un fichier par jour, 14 jours conservés).

## Niveaux

- Information : démarrage, changements de préférences.
- Avertissement : à ajouter dans les fonctions qui le demandent.
- Erreur : exceptions non gérées (journalisées avec le type d'exception).
- Débogage : uniquement en mode diagnostic.

## Ce qui n'est jamais enregistré

- Nom d'utilisateur, nom de la machine, chemins de profil (remplacés par `[utilisateur]`).
- Adresses MAC, numéros de série, contenu des fichiers analysés.
- Message complet d'une exception (il peut contenir un chemin) : seul son type est écrit.

## Tests

- `JournalVitalisTests` : masquage, création du fichier.

Liens : [[parametres]].
