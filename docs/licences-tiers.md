# Composants tiers utilisés par Vitalis

Vitalis 0.2.0 inclut les composants suivants, sans modification de leur code.

| Composant | Version | Licence | Source |
|---|---|---|---|
| .NET Runtime | 8.0 | MIT | https://github.com/dotnet/runtime |
| Windows App SDK | 1.8.251106002 | MIT | https://github.com/microsoft/WindowsAppSDK |
| CommunityToolkit.Mvvm | 8.4.0 | MIT | https://github.com/CommunityToolkit/dotnet |
| LibreHardwareMonitorLib | 0.9.6 | MPL-2.0 | https://github.com/LibreHardwareMonitor/LibreHardwareMonitor |
| HidSharp (dépendance de LibreHardwareMonitorLib) | 2.6.4 | Apache-2.0 | https://www.zer7.com/software/hidsharp |
| DiskInfoToolkit, RAMSPDToolkit-NDD (dépendances de LibreHardwareMonitorLib) | 1.1.2, 1.4.2 | MPL-2.0 | voir la page NuGet de chaque paquet |
| Serilog, Serilog.Sinks.File | 4.3.1, 6.0.0 | Apache-2.0 | https://github.com/serilog/serilog |
| System.Management | 10.0.2 | MIT | https://github.com/dotnet/runtime |

## MPL-2.0 (LibreHardwareMonitorLib et ses dépendances)

Le code source de ces bibliothèques est disponible aux adresses ci-dessus. Vitalis les utilise telles quelles ; aucun fichier sous MPL-2.0 n'a été modifié. Texte de la licence : https://www.mozilla.org/MPL/2.0/

## Logiciels installés à la demande (non inclus)

- **PawnIO** (pilote pour les capteurs du processeur), installé par winget seulement si vous le demandez : https://pawnio.eu
