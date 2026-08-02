# Build the Angular frontend.
# Usage: .\scripts\build-frontend.ps1 [-Configuration development|production] [-SkipPrereqCheck] [-SkipInstall]

param(
    [ValidateSet("development", "production")]
    [string]$Configuration = "production",
    [switch]$SkipPrereqCheck,
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$webRoot = Join-Path $repoRoot "src\frontend\aganox-lms-web"

if (-not (Test-Path $webRoot)) {
    Write-Host "Frontend project not found at $webRoot" -ForegroundColor Red
    exit 1
}

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode Build -Strict
}

Write-Host ""
Write-Host "=== Building frontend ($Configuration) ===" -ForegroundColor Cyan
Push-Location $webRoot
try {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    if (-not $SkipInstall -or -not (Test-Path "node_modules")) {
        Write-Host "Installing npm packages..." -ForegroundColor Gray
        cmd /c "npm install"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if ($Configuration -eq "production") {
        cmd /c "npm run build"
    } else {
        cmd /c "npx ng build --configuration=development"
    }
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host ""
    Write-Host "Frontend build succeeded. Output: dist/aganox-lms-web" -ForegroundColor Green
}
finally {
    $ErrorActionPreference = $prevEap
    Pop-Location
}
