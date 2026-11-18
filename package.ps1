# Compile Smart Stations en Release et crée le zip prêt à envoyer sur Thunderstore.
#
# Depuis la racine du dépôt :
#   powershell -ExecutionPolicy Bypass -File package.ps1
# Résultat : release/SmartStations-<version>.zip

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = Join-Path $root "SmartStations\SmartStations.csproj"
$meta = Join-Path $root "Thunderstore"

# 1. Version : celle du manifest doit être la même que celle du mod (Plugin.cs)
$manifest = Get-Content (Join-Path $meta "manifest.json") -Raw | ConvertFrom-Json
$version = $manifest.version_number
$pluginSource = Get-Content (Join-Path $root "SmartStations\Plugin.cs") -Raw
if ($pluginSource -notmatch "ModVersion = `"$([regex]::Escape($version))`"") {
    throw "La version du manifest ($version) ne correspond pas à ModVersion dans Plugin.cs. Mets les deux à jour."
}

# 2. Compilation
Write-Host "Compilation de Smart Stations $version..." -ForegroundColor Cyan
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw "La compilation a échoué (voir les erreurs ci-dessus)." }

$dll = Join-Path $root "SmartStations\bin\Release\net462\SmartStations.dll"
if (-not (Test-Path $dll)) { throw "DLL introuvable : $dll" }

# 3. Dossier temporaire avec la structure attendue par Thunderstore / r2modman :
#    manifest.json, README.md, CHANGELOG.md, icon.png à la racine ; le mod dans plugins/
$release = Join-Path $root "release"
$stage = Join-Path $release "stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $stage "plugins") -Force | Out-Null

foreach ($f in "manifest.json", "README.md", "CHANGELOG.md", "icon.png") {
    Copy-Item (Join-Path $meta $f) $stage
}
Copy-Item $dll (Join-Path $stage "plugins")
Copy-Item (Join-Path $root "SmartStations\Translations") (Join-Path $stage "plugins") -Recurse

# 4. Zip (chemins avec « / » : Compress-Archive de Windows PowerShell met des « \ » que certains outils lisent mal)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path $release "SmartStations-$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $stageFull = (Resolve-Path $stage).Path
    Get-ChildItem $stageFull -Recurse -File | ForEach-Object {
        $entry = $_.FullName.Substring($stageFull.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entry) | Out-Null
    }
}
finally {
    $zip.Dispose()
}
Remove-Item $stage -Recurse -Force

Write-Host "Zip prêt : $zipPath" -ForegroundColor Green
