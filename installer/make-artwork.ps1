param(
    [string]$IconPath,
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
if (-not $IconPath) { $IconPath = Join-Path $root "assets\icon-256.png" }
if (-not $OutputDir) { $OutputDir = Join-Path $PSScriptRoot "assets" }
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Use the PNG frame from the ICO so the transparent corners survive. Extracting
# a 256px icon through the shell API produces opaque black corners.
if (-not (Test-Path $IconPath)) {
    $IconPath = Join-Path $root "assets\MemEase.ico"
}
if ([System.IO.Path]::GetExtension($IconPath) -eq ".ico") {
    $bytes = [System.IO.File]::ReadAllBytes($IconPath)
    $count = [BitConverter]::ToUInt16($bytes, 4)
    $pngPath = Join-Path $OutputDir "icon-256.png"
    for ($i = 0; $i -lt $count; $i++) {
        $offset = 6 + 16 * $i
        if ($bytes[$offset] -eq 0) {
            $length = [BitConverter]::ToUInt32($bytes, $offset + 8)
            $dataOffset = [BitConverter]::ToUInt32($bytes, $offset + 12)
            [System.IO.File]::WriteAllBytes($pngPath, $bytes[$dataOffset..($dataOffset + $length - 1)])
            $IconPath = $pngPath
            break
        }
    }
}

$icon = [System.Drawing.Image]::FromFile((Resolve-Path $IconPath).Path)

# Large wizard artwork: 640x1224 (164:314 aspect, enough for high-DPI setups).
$width = 640
$height = 1224
$bitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$rectangle = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
$gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rectangle,
    [System.Drawing.Color]::FromArgb(9, 58, 53),
    [System.Drawing.Color]::FromArgb(15, 118, 110),
    55.0)
$graphics.FillRectangle($gradient, $rectangle)
$gradient.Dispose()

$decor = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(18, 255, 255, 255))
$graphics.FillEllipse($decor, 420, -80, 420, 420)
$graphics.FillEllipse($decor, -120, 940, 380, 380)
$decor.Dispose()

$line = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(40, 255, 255, 255), 2)
$graphics.DrawLine($line, 48, 48, 592, 48)
$line.Dispose()

$graphics.DrawImage($icon, 160, 96, 320, 320)

$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$brandFont = New-Object System.Drawing.Font(
    "Segoe UI Semibold", 64, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.DrawString("MemEase", $brandFont, $white, (New-Object System.Drawing.RectangleF(0, 448, $width, 100)), $format)
$brandFont.Dispose()

$taglineFont = New-Object System.Drawing.Font(
    "Microsoft YaHei UI", 30, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$mint = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(190, 225, 255, 248))
$graphics.DrawString("轻量内存托盘工具", $taglineFont, $mint, (New-Object System.Drawing.RectangleF(0, 566, $width, 50)), $format)
$taglineFont.Dispose()

$divider = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, 255, 255, 255), 2)
$graphics.DrawLine($divider, 250, 650, 390, 650)
$divider.Dispose()

$featureFont = New-Object System.Drawing.Font(
    "Microsoft YaHei UI", 25, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$soft = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
$features = @(
    "托盘常驻，无主界面",
    "手动释放 / 阈值自动释放",
    "不结束进程，无需管理员"
)
$y = 708
foreach ($feature in $features) {
    $graphics.DrawString($feature, $featureFont, $soft, (New-Object System.Drawing.RectangleF(0, $y, $width, 42)), $format)
    $y += 54
}
$featureFont.Dispose()

$versionFont = New-Object System.Drawing.Font(
    "Segoe UI", 22, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$dim = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(130, 255, 255, 255))
$graphics.DrawString("v1.0.0", $versionFont, $dim, (New-Object System.Drawing.RectangleF(0, 1148, $width, 40)), $format)
$versionFont.Dispose()

$graphics.Dispose()
$largePath = Join-Path $OutputDir "wizard-large.png"
$bitmap.Save($largePath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

# Small wizard artwork: transparent 256x256, shown at 58x58 and up.
$smallBitmap = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$smallGraphics = [System.Drawing.Graphics]::FromImage($smallBitmap)
$smallGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$smallGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$smallGraphics.Clear([System.Drawing.Color]::Transparent)
$smallGraphics.DrawImage($icon, 16, 16, 224, 224)
$smallGraphics.Dispose()
$smallPath = Join-Path $OutputDir "wizard-small.png"
$smallBitmap.Save($smallPath, [System.Drawing.Imaging.ImageFormat]::Png)
$smallBitmap.Dispose()

$icon.Dispose()
$white.Dispose()
$mint.Dispose()
$soft.Dispose()
$dim.Dispose()
$format.Dispose()

Get-Item $largePath, $smallPath | Select-Object Name, Length
