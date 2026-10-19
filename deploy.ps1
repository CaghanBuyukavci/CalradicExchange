<#
.SYNOPSIS
  Runs Core tests, builds the game module and copies it to <game>\Modules\CalradicExchange.
.PARAMETER GameFolder
  Bannerlord install folder. Defaults to $env:BANNERLORD_GAME_DIR, then the path in CLAUDE.md.
.PARAMETER SkipTests
  Skip the Core test run.
#>
param(
    [string]$GameFolder = $(if ($env:BANNERLORD_GAME_DIR) { $env:BANNERLORD_GAME_DIR } else { 'G:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord' }),
    [string]$Configuration = 'Release',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$moduleId = 'CalradicExchange'
$target = Join-Path $GameFolder "Modules\$moduleId"
$binTarget = Join-Path $target 'bin\Win64_Shipping_Client'

if (-not (Test-Path (Join-Path $GameFolder 'bin\Win64_Shipping_Client\Bannerlord.exe'))) {
    throw "Bannerlord not found at '$GameFolder'."
}

if (-not $SkipTests) {
    Write-Host '== Core tests' -ForegroundColor Cyan
    dotnet test (Join-Path $repo 'src\CalradicExchange.Core.Tests') -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed; not deploying.' }
}

Write-Host '== Build game module' -ForegroundColor Cyan
dotnet build (Join-Path $repo 'src\CalradicExchange') -c $Configuration --nologo "-p:GameFolder=$GameFolder"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

# The output folder has moved between bin\x64\... and bin\... before; take the freshest build so a stale
# folder can never be deployed again.
$out = @("src\CalradicExchange\bin\x64\$Configuration\net472", "src\CalradicExchange\bin\$Configuration\net472") |
    ForEach-Object { Join-Path $repo $_ } |
    Where-Object { Test-Path (Join-Path $_ "$moduleId.dll") } |
    Sort-Object { (Get-Item (Join-Path $_ "$moduleId.dll")).LastWriteTime } -Descending |
    Select-Object -First 1
if (-not $out) { throw 'Build output not found.' }
$built = (Get-Item (Join-Path $out "$moduleId.dll")).LastWriteTime
if ($built -lt (Get-Date).AddMinutes(-10)) { throw "Newest build output is old ($built); the build did not write where deploy expects." }
Write-Host "Using build output $out ($built)"

Write-Host "== Deploy to $target" -ForegroundColor Cyan
New-Item -ItemType Directory -Force $binTarget | Out-Null
try {
    # Only our own assemblies. Game DLLs and Newtonsoft.Json come from the game (ADR-012).
    foreach ($name in @("$moduleId.dll", "$moduleId.pdb", "$moduleId.Core.dll", "$moduleId.Core.pdb")) {
        $src = Join-Path $out $name
        if (Test-Path $src) { Copy-Item $src $binTarget -Force }
    }
}
catch [System.IO.IOException] {
    throw "Could not overwrite the DLLs (sharing violation). Close Bannerlord and run deploy again. $_"
}
foreach ($name in @("$moduleId.dll", "$moduleId.Core.dll")) {
    $a = (Get-FileHash (Join-Path $out $name)).Hash
    $b = (Get-FileHash (Join-Path $binTarget $name)).Hash
    if ($a -ne $b) { throw "$name in the game folder differs from the build output after copying." }
}

Copy-Item (Join-Path $repo 'SubModule.xml') $target -Force

$moduleDataTarget = Join-Path $target 'ModuleData'
if (Test-Path $moduleDataTarget) { Remove-Item $moduleDataTarget -Recurse -Force }
Copy-Item (Join-Path $repo 'ModuleData') $moduleDataTarget -Recurse -Force

$guiSource = Join-Path $repo 'GUI'
$guiTarget = Join-Path $target 'GUI'
if (Test-Path $guiTarget) { Remove-Item $guiTarget -Recurse -Force }
if ((Test-Path $guiSource) -and (Get-ChildItem $guiSource -Recurse -File | Where-Object { $_.Name -ne '.gitkeep' })) {
    Copy-Item $guiSource $guiTarget -Recurse -Force
}

# Runtime-loaded textures (ADR-023). Texture.LoadTextureFromPath reads PNGs with red and blue swapped, so we
# write a pre-swapped copy into the game folder and keep the original in Assets\Textures.
Add-Type -AssemblyName System.Drawing
$texSource = Join-Path $repo 'Assets\Textures'
$texTarget = Join-Path $guiTarget 'Textures'
New-Item -ItemType Directory -Force $texTarget | Out-Null
foreach ($png in Get-ChildItem $texSource -Filter *.png) {
    $src = [System.Drawing.Bitmap]::FromFile($png.FullName)
    $bmp = New-Object System.Drawing.Bitmap($src.Width, $src.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    # Color matrix rows are input R, G, B, A, offset; columns are output R, G, B, A.
    $swap = New-Object System.Drawing.Imaging.ColorMatrix(,[single[][]]@(
        [single[]]@(0, 0, 1, 0, 0), [single[]]@(0, 1, 0, 0, 0), [single[]]@(1, 0, 0, 0, 0),
        [single[]]@(0, 0, 0, 1, 0), [single[]]@(0, 0, 0, 0, 1)))
    $attrs = New-Object System.Drawing.Imaging.ImageAttributes
    $attrs.SetColorMatrix($swap)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0, 0, $src.Width, $src.Height)), 0, 0, $src.Width, $src.Height, [System.Drawing.GraphicsUnit]::Pixel, $attrs)
    $g.Dispose(); $attrs.Dispose(); $src.Dispose()
    $bmp.Save((Join-Path $texTarget $png.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Texture $($png.Name) written with red/blue swapped"
}

Write-Host "Deployed $moduleId to $target" -ForegroundColor Green
