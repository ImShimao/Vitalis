---
id: T-001
type: tache
priorite: P0
statut: valide
etape: 0
tags: [mise-en-place, git, ci, obsidian]
cree: 2026-11-19
---

# T-001 : mise en place

## Contexte et but

Poser le dépôt Git, la solution .NET, la CI et le coffre Obsidian avant tout code fonctionnel. But : `dotnet build` et `dotnet test` passent, et le coffre n'a pas de lien cassé.

## Critères d'acceptation

- [x] Dépôt Git initialisé dans `Vitalis/`, travail sur la branche `etape-00-mise-en-place`
- [x] Solution `Vitalis.sln` avec `Vitalis.Core` et `Vitalis.Core.Tests`
- [x] `dotnet build` passe (0 erreur, 0 avertissement)
- [x] `dotnet test` passe (2 réussis)
- [x] Coffre `Vitalis-Vault` créé à côté du dépôt, avec la structure demandée
- [x] `ADR-001-winui3`, `01-Vision`, `02-Roadmap` et modèles rédigés
- [x] Liens internes du coffre vérifiés par script
- [ ] CI GitHub Actions verte (pas encore de dépôt distant)
- [ ] Coffre ouvert dans Obsidian sans lien cassé (Obsidian non installé)

## Fichiers de code concernés

- `Vitalis.sln`, `global.json`, `Directory.Build.props`
- `src/Vitalis.Core/` (`Vitalis.Core.csproj`, `AppInfo.cs`)
- `tests/Vitalis.Core.Tests/` (`Vitalis.Core.Tests.csproj`, `AppInfoTests.cs`)
- `.github/workflows/ci.yml`

## Risques

- Le SDK .NET 8 n'était pas installé sur la machine ; installé pendant l'étape.
- La cible `net8.0-windows10.0.19045.0` échoue ; la cible `19041.0` est utilisée.
- La CI n'a pas tourné faute de dépôt distant.
- FluentAssertions 8 et plus passent sous licence commerciale : version 7.0.0 épinglée.

## Liens

- Décision : [[ADR-001-winui3]]
- Journal : [[etape-00]]
- Roadmap : [[02-Roadmap]]
