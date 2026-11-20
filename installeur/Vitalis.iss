; Installeur Vitalis (Inno Setup 6).
; Préalable : dotnet publish src/Vitalis.App -c Release -p:Autonome=true, puis copie de Vitalis.exe à la racine du dépôt.
; Compilation : ISCC.exe installeur\Vitalis.iss  →  installeur\Sortie\Vitalis-Setup-<version>.exe
; Ou simplement : powershell -File installeur\construire.ps1

#define Nom "Vitalis"
#ifndef Version
  #define Version "0.2.0"
#endif
#define Exe "Vitalis.exe"
#define Racine ".."

[Setup]
; AppId fixe : ne jamais le changer, il relie les mises à jour et la désinstallation.
AppId={{E91A7522-6503-4A6B-9C25-4E0FD14AE6FB}
AppName={#Nom}
AppVersion={#Version}
AppVerName={#Nom} {#Version}
AppPublisher=Vitalis
AppPublisherURL=https://github.com/ImShimao/Vitalis
AppSupportURL=https://github.com/ImShimao/Vitalis/issues
AppUpdatesURL=https://github.com/ImShimao/Vitalis/releases
DefaultDirName={autopf}\{#Nom}
DefaultGroupName={#Nom}
DisableProgramGroupPage=yes
; Vitalis s'exécute en administrateur (ADR-015) : l'installation se fait pour tout le PC.
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir=Sortie
OutputBaseFilename=Vitalis-Setup-{#Version}
SetupIconFile={#Racine}\src\Vitalis.App\Assets\Vitalis.ico
UninstallDisplayIcon={app}\{#Exe}
UninstallDisplayName={#Nom} {#Version}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
LicenseFile={#Racine}\docs\licences-tiers.md
InfoBeforeFile={#Racine}\docs\politique-confidentialite.md
CloseApplications=yes
VersionInfoVersion={#Version}
VersionInfoDescription=Installeur de {#Nom}

[Languages]
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "bureau"; Description: "Créer une icône sur le Bureau"; GroupDescription: "Raccourcis :"

[Files]
Source: "{#Racine}\{#Exe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Racine}\docs\licences-tiers.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Racine}\docs\politique-confidentialite.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#Nom}"; Filename: "{app}\{#Exe}"
Name: "{group}\Désinstaller {#Nom}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#Nom}"; Filename: "{app}\{#Exe}"; Tasks: bureau

[Run]
Filename: "{app}\{#Exe}"; Description: "Lancer {#Nom}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
; Supprime l'analyse planifiée si elle a été activée (sans erreur si elle n'existe pas).
Filename: "{sys}\schtasks.exe"; Parameters: "/Delete /TN ""Vitalis - analyse hebdomadaire"" /F"; Flags: runhidden; RunOnceId: "SupprimerTache"

[UninstallDelete]
; Les préférences, journaux et historiques (%LOCALAPPDATA%\Vitalis) et les rapports (Documents\Vitalis) restent : ce sont les données de l'utilisateur.
Type: dirifempty; Name: "{app}"
