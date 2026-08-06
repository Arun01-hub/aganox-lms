# Build backend + frontend.
# Usage: .\scripts\build.ps1 [-Configuration Debug|Release] [-FrontendConfig development|production] [-SkipPrereqCheck]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("development", "production")]
    [string]$FrontendConfig = "production",
    [switch]$SkipPrereqCheck
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
. (Join-Path $scriptDir "lib\Prerequisites.ps1")

Write-Host "=== Aganox LMS — full build ===" -ForegroundColor Cyan

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode Build -Strict
}

$backendArgs = @("-Configuration", $Configuration, "-SkipPrereqCheck")
$frontendArgs = @("-Configuration", $FrontendConfig, "-SkipPrereqCheck")

& (Join-Path $scriptDir "build-backend.ps1") @backendArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $scriptDir "build-frontend.ps1") @frontendArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Full build completed successfully." -ForegroundColor Green
