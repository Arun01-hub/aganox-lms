# Run the Angular dev server.
# Usage: .\scripts\run-web.ps1 [-SkipPrereqCheck] [-SkipInstall]

param(
    [switch]$SkipPrereqCheck,
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$webRoot = Join-Path $repoRoot "src\frontend\aganox-lms-web"

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode RunWeb -Strict
}

Write-Host ""
Write-Host "=== Starting Angular dev server ===" -ForegroundColor Cyan
Write-Host "  UI: http://localhost:4200 (proxies /api to https://localhost:7277)" -ForegroundColor Gray
Write-Host ""

Push-Location $webRoot
try {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    if (-not $SkipInstall -and -not (Test-Path "node_modules")) {
        cmd /c "npm install"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    cmd /c "npm start"
    exit $LASTEXITCODE
}
finally {
    $ErrorActionPreference = $prevEap
    Pop-Location
}
