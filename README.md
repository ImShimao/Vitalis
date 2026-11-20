# Vitalis

Diagnostic complet du PC Windows, pilotes et mises à jour, dans une seule application en français.
L'esprit de DriversCloud pour les pilotes et les mises à jour, l'esprit de UserDiag pour le diagnostic.

## Fonctions

| Page | Ce qu'elle fait |
|---|---|
| Tableau de bord | Score sur 100, cartes de synthèse, analyse complète en une fois |
| Matériel | Processeur, mémoire, carte mère et BIOS, carte graphique, écrans, réseau, audio |
| Disques | Santé SMART, température, usure, espace libre |
| Mises à jour | Windows Update et logiciels (winget) : installation en un clic, après confirmation |
| Pilotes | Chaque pilote comparé au catalogue officiel Windows Update (installé → proposé), installation ; liens directs vers les pages pilotes de 30 fabricants (NVIDIA, AMD, Intel, MSI, ASUS…) |
| Démarrage | Programmes lancés au démarrage, désactivation réversible |
| Performances | Mesures processeur, mémoire, disque ; carte graphique par l'indice WinSAT ; historique |
| Santé live | Températures, charges, fréquences, ventilateurs en temps réel |
| Rapport | Export HTML ou PDF, sans donnée personnelle |
| Paramètres | Thème, analyse planifiée hebdomadaire, notifications, journal, mise à jour de Vitalis |

Rien n'est installé ni modifié sans confirmation. Vitalis se lance en administrateur (nécessaire pour SMART, les capteurs et les installations).

## Installation

Téléchargez `Vitalis-Setup-<version>.exe` (installeur) ou `Vitalis.exe` (portable, rien à installer) depuis la page [Releases](https://github.com/ImShimao/Vitalis/releases).

Configuration : Windows 10 22H2 ou Windows 11, x64. Les températures du processeur demandent le pilote PawnIO, que Vitalis propose d'installer.

## Construire

```powershell
dotnet test Vitalis.sln
powershell -ExecutionPolicy Bypass -File installeur\construire.ps1
```

Prérequis : SDK .NET 8, Inno Setup 6 pour l'installeur. Une étiquette `v*` poussée sur GitHub publie automatiquement l'exécutable et l'installeur.

## Documentation

- Coffre Obsidian : `Vitalis-Vault/` (vision, roadmap, décisions ADR, fonctions, changelogs).
- [Politique de confidentialité](docs/politique-confidentialite.md) : aucune donnée envoyée sur Internet.
- [Composants tiers et licences](docs/licences-tiers.md).
