# Construit Vitalis.exe (autonome) puis l'installeur Vitalis-Setup-<version>.exe.
# Usage : powershell -ExecutionPolicy Bypass -File installeur\construire.ps1
$ErrorActionPreference = "Stop"
$racine = Split-Path $PSScriptRoot -Parent
Set-Location $racine

$version = (Select-String -Path "src\Vitalis.Core\AppInfo.cs" -Pattern 'Version = "([^"]+)"').Matches[0].Groups[1].Value

dotnet publish src\Vitalis.App -c Release -p:Autonome=true
if ($LASTEXITCODE -ne 0) { throw "Publication échouée." }
Copy-Item "src\Vitalis.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\Vitalis.exe" "$racine\Vitalis.exe" -Force

$iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 introuvable (winget install JRSoftware.InnoSetup)." }

& $iscc "/DVersion=$version" "installeur\Vitalis.iss"
if ($LASTEXITCODE -ne 0) { throw "Compilation de l'installeur échouée." }
Get-Item "installeur\Sortie\Vitalis-Setup-$version.exe"
