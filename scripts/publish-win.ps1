# Self-contained publish for Windows (x64 or x86).
# Usage: .\scripts\publish-win.ps1 [-Runtime win-x64|win-x86] [-Configuration Release] [-SkipFrontend] [-SkipPrereqCheck]

param(
    [ValidateSet("win-x64", "win-x86")]
    [string]$Runtime = "win-x64",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipFrontend,
    [switch]$SkipPrereqCheck
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$apiProject = Join-Path $repoRoot "src\backend\Aganox.Lms.Api\Aganox.Lms.Api.csproj"
$wwwroot = Join-Path $repoRoot "src\backend\Aganox.Lms.Api\wwwroot"
$frontendBrowser = Join-Path $repoRoot "src\frontend\aganox-lms-web\dist\aganox-lms-web\browser"
$outDir = Join-Path $repoRoot "artifacts\publish\$Runtime"

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode Build -Strict
}

if (-not $SkipFrontend) {
    & (Join-Path $PSScriptRoot "build-frontend.ps1") -Configuration production -SkipPrereqCheck
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if (-not (Test-Path $frontendBrowser)) {
    Write-Host "Frontend build output not found at $frontendBrowser" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Publishing Aganox LMS ($Runtime, $Configuration) ===" -ForegroundColor Cyan

if (Test-Path $wwwroot) {
    Remove-Item $wwwroot -Recurse -Force
}
New-Item -ItemType Directory -Path $wwwroot -Force | Out-Null
Copy-Item -Path (Join-Path $frontendBrowser "*") -Destination $wwwroot -Recurse -Force

if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

dotnet publish $apiProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$prodSettings = Join-Path $outDir "appsettings.Production.json"
if (Test-Path $prodSettings) {
    Copy-Item $prodSettings (Join-Path $outDir "appsettings.Production.json.example") -Force
}

$launcherPs1 = Join-Path $outDir "Start-AganoxLms.ps1"
Copy-Item -Path (Join-Path $PSScriptRoot "Start-AganoxLms.ps1") -Destination $launcherPs1 -Force

$launcher = Join-Path $outDir "Start-AganoxLms.cmd"
@(
    "@echo off",
    "cd /d `"%~dp0`"",
    "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"%~dp0Start-AganoxLms.ps1`""
) | Set-Content -Path $launcher -Encoding ASCII

Write-Host ""
Write-Host "Publish completed: $outDir" -ForegroundColor Green
Write-Host "Run: $launcher" -ForegroundColor Gray
