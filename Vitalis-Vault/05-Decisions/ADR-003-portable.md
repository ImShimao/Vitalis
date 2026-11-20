---
id: ADR-003
type: adr
priorite: P0
statut: fait
etape: 1
tags: [packaging, portable, winui, runtime]
cree: 2026-11-20
---

# ADR-003 : version portable non empaquetée pendant l'étape 1

## Contexte

Le modèle WinUI officiel crée un projet empaqueté (MSIX), qui demande une identité d'application et une signature. La distribution MSIX est prévue à l'étape 12, et une version portable `.exe` est demandée pour la v1.

## Décision

Le projet `Vitalis.App` est **non empaqueté** (`WindowsPackageType=None`), cible `win-x64`, framework-dépendant, avec initialisation explicite du runtime dans `Program.cs` (`Bootstrap.Initialize(0x00010008)`, Windows App SDK 1.8).

## Alternatives écartées

- **MSIX dès l'étape 1** : ajoute une identité, un certificat et des outils de packaging avant qu'il y ait une fonction à tester.
- **Autonome (`WindowsAppSDKSelfContained`)** : testé, sortie `0xC0000242`. Pas retenu pour l'instant.

## Conséquences

- La machine doit avoir le **Windows App Runtime 1.8** installé. Sans lui, l'application ne démarre pas.
- Le paquet NuGet `Microsoft.WindowsAppSDK` doit correspondre au runtime installé : la version `1.8.260921001` plantait au démarrage, `1.8.251106002` fonctionne sur la machine de test.
- À trancher à l'étape 12 : version autonome pour la version portable (plus grosse, sans prérequis), ou runtime demandé à l'installation.

Liens : [[ADR-001-winui3]], [[ADR-002-dix-pages]], [[T-013-distribution]].
