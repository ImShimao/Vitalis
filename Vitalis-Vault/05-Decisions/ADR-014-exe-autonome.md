---
id: ADR-014
type: adr
priorite: P1
statut: fait
etape: 12
tags: [distribution, publication, icone]
cree: 2026-11-20
---

# ADR-014 : exécutable autonome Vitalis.exe

## Contexte

La publication `dotnet publish` produisait une application qui plantait au démarrage (fichiers XAML compilés et `.pri` absents). La distribution passait par un zip du dossier de build, qui demandait d'installer .NET 8 et le Windows App Runtime.

## Décision

- `dotnet publish src/Vitalis.App -c Release -p:Autonome=true` produit **un seul fichier, `Vitalis.exe`** (83 Mo compressé), copié à la racine du dépôt (ignoré par Git).
- Il embarque .NET 8 et le Windows App SDK (`SelfContained`, `WindowsAppSDKSelfContained`, `PublishSingleFile`, compression). Rien à installer sur le PC cible.
- `EnableMsixTooling=true` dans ce mode : c'est ce qui fait produire le fichier `.pri` à la publication. Le bug de publication est corrigé.
- En mode autonome, `Program.cs` n'appelle pas `Bootstrap.Initialize` (le runtime est embarqué, constante `VITALIS_AUTONOME`).
- Nom d'assembly `Vitalis` ; icône `Assets/Vitalis.ico` (pouls blanc sur dégradé vert-bleu), appliquée à l'exe, à la fenêtre et à la barre des tâches.

## Conséquences

- Vérifié sur cette machine : l'exe démarre, les pages s'affichent, l'icône apparaît.
- Non vérifié : un PC sans .NET ni Windows App Runtime, Windows 10. L'exe n'est pas signé : SmartScreen avertira au premier lancement.
- Le premier démarrage est un peu plus lent (décompression).

Liens : [[ADR-003-portable]], [[ADR-013-refonte-interface]], [[T-013-distribution]].
