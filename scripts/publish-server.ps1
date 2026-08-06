# Self-contained server publish for Windows x64 (installer input).
# Usage: .\scripts\publish-server.ps1 [-Configuration Release] [-SkipPrereqCheck]

param(
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
$outDir = Join-Path $repoRoot "artifacts\publish\server-x64"
$installerTools = Join-Path $repoRoot "src\installer\tools"
$installerTemplates = Join-Path $repoRoot "src\installer\templates"

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
Write-Host "=== Publishing Aganox LMS server package (win-x64, $Configuration) ===" -ForegroundColor Cyan

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
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$prodSettings = Join-Path $outDir "appsettings.Production.json"
if (Test-Path $prodSettings) {
    Copy-Item $prodSettings (Join-Path $outDir "appsettings.Production.json.example") -Force
    Remove-Item $prodSettings -Force
}

$toolsDest = Join-Path $outDir "tools"
$templatesDest = Join-Path $outDir "templates"
New-Item -ItemType Directory -Path $toolsDest -Force | Out-Null
New-Item -ItemType Directory -Path $templatesDest -Force | Out-Null
Copy-Item -Path (Join-Path $installerTools "*") -Destination $toolsDest -Force
Copy-Item -Path (Join-Path $installerTemplates "*") -Destination $templatesDest -Force

$requiredTools = @(
    "Configure-Server.ps1",
    "Install-Service.ps1",
    "Install-SqlExpress.ps1",
    "Uninstall-Service.ps1"
)
foreach ($script in $requiredTools) {
    $path = Join-Path $toolsDest $script
    if (-not (Test-Path $path)) {
        Write-Host "Installer tool missing from publish output: $path" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Server publish completed: $outDir" -ForegroundColor Green
