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

Write-Host "=== Aganox LMS — full build ===" -ForegroundColor Cyan

$backendArgs = @("-Configuration", $Configuration)
$frontendArgs = @("-Configuration", $FrontendConfig)
if ($SkipPrereqCheck) {
    $backendArgs += "-SkipPrereqCheck"
    $frontendArgs += "-SkipPrereqCheck"
}

& (Join-Path $scriptDir "build-backend.ps1") @backendArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $scriptDir "build-frontend.ps1") @frontendArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Full build completed successfully." -ForegroundColor Green
