[CmdletBinding()]
param(
    [switch]$Run
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSCommandPath
$srcDir = Join-Path $root "src"
$toolsDir = Join-Path $root "tools"
$assetsDir = Join-Path $root "assets"
$buildDir = Join-Path $root "build"
$distDir = Join-Path $root "dist"

$cscCandidates = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Cannot find the .NET Framework 4.x compiler (csc.exe)."
}

New-Item -ItemType Directory -Force -Path $assetsDir, $buildDir, $distDir | Out-Null

$iconPath = Join-Path $assetsDir "MemEase.ico"
$iconGenPath = Join-Path $buildDir "IconGen.exe"
$outExe = Join-Path $distDir "MemEase.exe"
$manifestPath = Join-Path $srcDir "app.manifest"

Write-Host "1/3  Building icon generator..."
$iconGenArgs = @(
    "/nologo",
    "/target:exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/out:$iconGenPath",
    "/reference:System.Drawing.dll",
    (Join-Path $toolsDir "IconGen.cs"),
    (Join-Path $srcDir "IconFactory.cs")
)
& $csc $iconGenArgs
if ($LASTEXITCODE -ne 0) { throw "Icon generator build failed." }

Write-Host "2/3  Generating application icon..."
& $iconGenPath $iconPath
if ($LASTEXITCODE -ne 0) { throw "Icon generation failed." }

Write-Host "3/3  Building MemEase..."
$sources = Get-ChildItem -Path $srcDir -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
$appArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/debug-",
    "/warn:4",
    "/codepage:65001",
    "/utf8output",
    "/win32icon:$iconPath",
    "/win32manifest:$manifestPath",
    "/out:$outExe",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll"
) + $sources

& $csc $appArgs
if ($LASTEXITCODE -ne 0) { throw "MemEase build failed." }

Copy-Item -Path (Join-Path $root "README.md") -Destination (Join-Path $distDir "README.md") -Force
Copy-Item -Path (Join-Path $root "LICENSE.txt") -Destination (Join-Path $distDir "LICENSE.txt") -Force
$distAssets = Join-Path $distDir "assets"
New-Item -ItemType Directory -Force -Path $distAssets | Out-Null
Copy-Item -Path (Join-Path $assetsDir "icon-256.png") -Destination (Join-Path $distAssets "icon-256.png") -Force

$zipPath = Join-Path $distDir "MemEase-1.0-win.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $distDir "MemEase.exe"), (Join-Path $distDir "README.md"), (Join-Path $distDir "LICENSE.txt"), $distAssets -DestinationPath $zipPath

$exe = Get-Item $outExe
$hash = (Get-FileHash -Algorithm SHA256 $outExe).Hash
Write-Host ""
Write-Host "Built: $outExe"
Write-Host ("Size: {0:N0} bytes" -f $exe.Length)
Write-Host "SHA256: $hash"
Write-Host "Zip: $zipPath"

if ($Run) {
    Start-Process -FilePath $outExe
}
