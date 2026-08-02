# Run the API (HTTPS profile by default).
# Usage: .\scripts\run-api.ps1 [-Profile https|http] [-SkipPrereqCheck] [-SkipBuild]

param(
    [ValidateSet("https", "http")]
    [string]$Profile = "https",
    [switch]$SkipPrereqCheck,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$apiProject = Join-Path $repoRoot "src\backend\Aganox.Lms.Api"

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode RunApi -Strict
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "build-backend.ps1") -SkipPrereqCheck
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host ""
Write-Host "=== Starting API (profile: $Profile) ===" -ForegroundColor Cyan
Write-Host "  Swagger: https://localhost:7277/swagger (HTTPS profile)" -ForegroundColor Gray
Write-Host "  Health:  https://localhost:7277/health" -ForegroundColor Gray
Write-Host ""

Push-Location $apiProject
try {
    dotnet run --launch-profile $Profile
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
