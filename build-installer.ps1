[CmdletBinding()]
param(
    [switch]$SkipPortable,
    [switch]$Run
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSCommandPath
$distDir = Join-Path $root "dist"
$installerScript = Join-Path $root "installer\MemEase.iss"
$wizardScript = Join-Path $root "installer\make-artwork.ps1"
$iconPath = Join-Path $root "assets\MemEase.ico"
$outputExe = Join-Path $distDir "MemEase-Setup-1.0.exe"

if (-not (Test-Path $installerScript)) {
    throw "Cannot find installer script: $installerScript"
}

if (-not $SkipPortable) {
    Write-Host "1/4  Building portable executable..."
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "build.ps1")
    if ($LASTEXITCODE -ne 0) { throw "Portable build failed." }
}
else {
    Write-Host "1/4  Skipping portable build."
}

if (-not (Test-Path (Join-Path $distDir "MemEase.exe"))) {
    throw "dist\MemEase.exe is missing. Run build.ps1 first."
}

Write-Host "2/4  Generating installer wizard artwork..."
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $wizardScript -IconPath $iconPath
if ($LASTEXITCODE -ne 0) { throw "Wizard artwork generation failed." }

$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
    "C:\Program Files\Inno Setup 7\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Cannot find Inno Setup 6 (ISCC.exe). Install it with: winget install -e --id JRSoftware.InnoSetup"
}

Write-Host "3/4  Compiling installer with Inno Setup..."
& $iscc "/Qp" $installerScript
if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }
if (-not (Test-Path $outputExe)) { throw "Installer was not created: $outputExe" }

Write-Host "4/4  Done."
$installer = Get-Item $outputExe
$portable = Get-Item (Join-Path $distDir "MemEase.exe")
$installerHash = (Get-FileHash $outputExe -Algorithm SHA256).Hash
$portableHash = (Get-FileHash $portable.FullName -Algorithm SHA256).Hash

Write-Host ""
Write-Host "Installer: $outputExe"
Write-Host ("Installer size: {0:N0} bytes" -f $installer.Length)
Write-Host "Installer SHA256: $installerHash"
Write-Host ""
Write-Host "Portable: $($portable.FullName)"
Write-Host ("Portable size: {0:N0} bytes" -f $portable.Length)
Write-Host "Portable SHA256: $portableHash"

if ($Run) {
    Start-Process -FilePath $outputExe
}
