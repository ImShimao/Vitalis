---
id: F-inventaire-materiel
type: fonction
priorite: P0
statut: fait
etape: 2
tags: [fonction, wmi, inventaire]
cree: 2026-11-20
---

# Inventaire matériel

## Couverture

Lit le matériel de la machine : processeur, carte mère, BIOS, mémoire, cartes graphiques (dont pilote), écrans, adaptateurs réseau matériels, périphériques audio. Code : `src/Vitalis.Core/Materiel/`. Tâche : [[T-003-inventaire-materiel]]. Décision : [[ADR-004-wmi-system-management]].

## Sources (vérifiées dans la documentation Microsoft le 2026-11-20)

| Famille | Classe | Espace de noms | Documentation |
|---|---|---|---|
| Processeur | `Win32_Processor` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-processor |
| Carte mère | `Win32_BaseBoard` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-baseboard |
| BIOS | `Win32_BIOS` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-bios |
| Mémoire | `Win32_PhysicalMemory` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-physicalmemory |
| Cartes graphiques | `Win32_VideoController` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-videocontroller |
| Écrans | `WmiMonitorID` | `root\wmi` | learn.microsoft.com, wmicoreprov/wmimonitorid |
| Réseau | `MSFT_NetAdapter` | `root\StandardCimv2` | learn.microsoft.com, fwp/wmi/netadaptercimprov/msft-netadapter |
| Audio | `Win32_SoundDevice` | `root\CIMV2` | learn.microsoft.com, cimwin32prov/win32-sounddevice |

Note : `Win32_NetworkAdapter` est déprécié par Microsoft (cimwin32prov/win32-networkadapter) ; il n'est pas utilisé.

## Champs lus

- **Processeur** : Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors (threads), MaxClockSpeed (MHz), L2CacheSize et L3CacheSize (Ko).
- **Carte mère** : Manufacturer, Product, Version. Le numéro de série n'est pas lu.
- **BIOS** : Manufacturer, SMBIOSBIOSVersion, ReleaseDate (partie date uniquement).
- **Mémoire** (une entrée par barrette) : DeviceLocator, Capacity (octets), Speed (MHz), Manufacturer. Total = somme des capacités connues (`MemoireTotaleOctets`).
- **Cartes graphiques** : Name, AdapterRAM, DriverVersion, DriverDate (partie date), résolution et fréquence courantes.
- **Écrans** : ManufacturerName (code à 3 lettres, ex. `DEL`) et UserFriendlyName (nom commercial), décodés depuis les tableaux UTF-16.
- **Réseau** : Name et InterfaceDescription, filtre `HardwareInterface = TRUE`. L'adresse MAC n'est pas lue.
- **Audio** : Name, Manufacturer, Status.

## Entrées, sorties

- Entrée : `ISourceWmi` (lecture réelle : `WmiSource` ; tests : `FauxSourceWmi`), jeton d'annulation optionnel.
- Sortie : `RapportMateriel`, huit sections. Chaque section a un état `Disponible`, `Partielle` ou `Indisponible`, une liste d'éléments et une raison en texte.

## Cas « indisponible » et absence de donnée

- **Classe absente ou erreur WMI** : la section passe à `Indisponible`, avec la raison (classe et type d'erreur). Les autres sections ne sont pas touchées.
- **Aucune ligne renvoyée** : `Indisponible`, raison « Aucun élément renvoyé ».
- **Champ essentiel absent** (nom du processeur, version du BIOS...) : `Partielle`. La valeur reste `null`, elle n'est jamais inventée.
- **Date mal formée** : `null`.

## Limites connues

- **AdapterRAM (`uint32`)** : la valeur est plafonnée à 4 Gio. Au-delà, la mémoire d'une carte récente est fausse. Un message Q&A Microsoft confirme ce plafond ; ce n'est pas une garantie de la doc officielle. Une correction par le registre (classe Display) est possible plus tard.
- **Win32_VideoController** : non fiable pour les pilotes non WDDM (doc Microsoft).
- **Win32_Processor** : `NumberOfLogicalProcessors` ne compte que les threads activés ; comparer avec `NumberOfCores` pour savoir si l'Hyper-Threading est actif.
- **Mémoire** : `Capacity` est plus fiable que `TotalPhysicalMemory` (doc Microsoft) : la mémoire réservée par le BIOS n'apparaît pas dans la somme de `TotalPhysicalMemory`.
- **Écrans** : les codes fabricant sont courts (ex. `DEL`, `ACR`) ; le nom commercial vient de UserFriendlyName, quand il est présent.
- **Durée** : chaque classe est une requête WMI séparée, exécutées en parallèle.

## Tests associés

- `MachineComplete_ToutesLesSectionsSontDisponibles` : données simulées complètes.
- `MachineComplete_ValeursCorrectementConverties` : valeurs, dates, total de mémoire, décodage des écrans.
- `Reseau_NeLitQueLesAdaptateursMateriels` : filtre `HardwareInterface = TRUE` bien envoyé.
- `ClasseWmiManquante_SeuleLaSectionConcerneeEstIndisponible` : `Win32_SoundDevice` absent.
- `DonneeEssentielleAbsente_EtatPartiel_SansInventer` : nom du processeur absent.
- `AucuneLigne_EtatIndisponibleAvecRaison` ; `AnnulationDemandee_LanceOperationCanceled`.
- `ConversionsTests` : décodage UTF-16, dates CIM, entiers, textes, total de mémoire.
- `MachineReelle_LeProcesseurEtLaMemoireSontLus` : lecture WMI réelle sur la machine de test (Windows uniquement).

Liens : [[ADR-004-wmi-system-management]], [[ADR-002-dix-pages]] (la page « Matériel » n'affiche pas encore ces données : étape à venir).
