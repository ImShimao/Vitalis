---
id: ADR-001
type: adr
priorite: P0
statut: fait
etape: 0
tags: [interface, winui, decision]
cree: 2026-11-19
---

# ADR-001 : interface en WinUI 3 (Windows App SDK)

## Contexte

Vitalis est une application de bureau Windows 10 22H2 et Windows 11, x64. Elle doit ressembler à une application Windows moderne : Mica sur Windows 11, thème clair et sombre automatique, navigation clavier, NavigationView. La pile retenue est C# / .NET 8 avec MVVM (CommunityToolkit.Mvvm).

## Décision

Interface en **WinUI 3** via le **Windows App SDK**, avec packaging **MSIX** et une version portable `.exe`.

## Alternatives écartées

- **UWP** : plateforme en fin de développement côté Microsoft ; pas de voie d'avenir.
- **WPF** : mature et bien documenté, mais pas de Mica ni de NavigationView natifs au même niveau ; apparence moins moderne sur Windows 11.
- **WinForms** : hors de question pour une interface moderne.
- **.NET MAUI (Windows)** : une seule base de code multiplateforme, inutile ici ; le rendu Windows reste moins proche de la plateforme.

## Conséquences

- Le projet cible `net8.0-windows10.0.19041.0` (voir `Directory.Build.props`). Le support réel de Windows 10 22H2 reste à vérifier sur une VM (voir [[matrice-compatibilite]]).
- Le template WinUI 3 demande Visual Studio avec la charge de travail « Développement Desktop .NET » et le SDK Windows. Il faudra le vérifier à l'étape 1.
- Les tests unitaires ne dépendent pas de WinUI : le cœur (`Vitalis.Core`) reste testable en console.
- Le packaging MSIX impose une identité d'application et une signature à la distribution (étape 12).
- ARM64 est hors périmètre v1.

## Sources à vérifier avant usage (étape 1)

- Documentation Windows App SDK sur learn.microsoft.com (prérequis, versions minimales de Windows).
- Note : cette décision n'a pas été vérifiée en ligne lors de l'étape 0 ; les versions exactes du Windows App SDK restent à fixer à l'étape 1.

Liens : [[01-Vision]], [[T-002-shell-interface]], [[T-001-mise-en-place]].
