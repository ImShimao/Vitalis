---
id: ADR-004
type: adr
priorite: P0
statut: fait
etape: 2
tags: [wmi, inventaire, dependance]
cree: 2026-11-20
---

# ADR-004 : lecture WMI via System.Management

## Contexte

L'étape 2 lit le matériel (CPU, carte mère, BIOS, RAM, GPU, écrans, réseau, audio). La spec demande un service « WMI/CIM ». Il faut choisir l'API .NET.

## Décision

Utiliser **System.Management** (`ManagementObjectSearcher`, paquet NuGet `System.Management` 8.0.0), derrière une interface `ISourceWmi`. Une requête par classe, avec la liste explicite des propriétés.

## Alternatives écartées

- **Microsoft.Management.Infrastructure (CIM)** : API moderne et plus rapide, mais plus de code pour un même résultat ici, et moins d'exemples pour les classes `root\wmi`. Non retenue pour cette étape ; reste possible si les performances le demandent.
- **Commande `wmic` ou PowerShell lancée en processus** : dépréciée (wmic), coûteuse à lancer, et plus difficile à tester.
- **Lecture directe du registre** : ne couvre pas les classes WMI utiles (écrans, GPU, BIOS).

## Conséquences

- Les requêtes passent par `ISourceWmi`, donc les tests utilisent des données simulées sans machine réelle.
- Une seule vraie lecture (`MachineReelle_...`) vérifie que WMI répond sur la machine de test.
- `Win32_NetworkAdapter` est déprécié : on utilise `MSFT_NetAdapter` (vérifié dans la doc Microsoft).
- Le paquet `System.Management` reste à surveiller : ses versions 8 et plus sont à vérifier avant mise à jour.

Liens : [[T-003-inventaire-materiel]], [[inventaire-materiel]], [[ADR-001-winui3]].
