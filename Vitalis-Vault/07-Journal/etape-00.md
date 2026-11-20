---
id: JOURNAL-00
type: journal
priorite: P0
statut: fait
etape: 0
tags: [journal]
cree: 2026-11-19
---

# Étape 0 : mise en place

Branche Git : `etape-00-mise-en-place`. Tâche : [[T-001-mise-en-place]].

## Ce qui a été fait

- Coffre `Vitalis-Vault` créé à côté du dépôt de code `Vitalis/`, avec la structure demandée.
- Dépôt Git initialisé dans `Vitalis/`.
- Solution `Vitalis.sln` : projet `Vitalis.Core` (bibliothèque) et `Vitalis.Core.Tests` (xUnit + FluentAssertions 7.0.0).
- `global.json` (SDK 8.0.428), `Directory.Build.props` (cible `net8.0-windows10.0.19041.0`).
- CI GitHub Actions `.github/workflows/ci.yml` (build Release x64 + tests, résultats TRX publiés).
- [[ADR-001-winui3]], [[01-Vision]], [[02-Roadmap]], modèles dans `_Modeles/`.
- Tâches T-001 à T-013 créées dans `03-Backlog/`, une par étape de la roadmap.

## Problèmes rencontrés

- Le SDK .NET 8 n'était pas installé (seul le runtime l'était). Installé avec `winget install Microsoft.DotNet.SDK.8` (version 8.0.428).
- Premier essai de cible `net8.0-windows10.0.19045.0` : erreur NETSDK1140 à la compilation. Remplacée par `net8.0-windows10.0.19041.0`, qui compile.
- Obsidian n'est pas installé sur cette machine : le coffre n'a pas été ouvert dans l'application.

## Tests lancés et résultats

- `dotnet build Vitalis.sln -c Release` : 0 avertissement, 0 erreur.
- `dotnet test Vitalis.sln -c Release` : 2 réussis, 0 échec (`AppInfoTests`).
- Vérification des liens internes du coffre (script PowerShell sur les liens internes) : 0 lien cassé, 22 notes.

## Non testé et pourquoi

- **CI GitHub Actions** : le dépôt n'a pas de remote, donc la CI n'a pas tourné.
- **Ouverture du coffre dans Obsidian** : Obsidian absent ; les liens sont vérifiés par script.
- **Windows 10 22H2** : aucune VM disponible ; compatibilité non vérifiée.

## Ce qui reste

- Créer le dépôt distant et vérifier la CI.
- Ouvrir le coffre dans Obsidian et installer Dataview si on veut la requête.
- Valider l'étape 0 avant de commencer [[T-002-shell-interface]].
