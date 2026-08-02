# Build the .NET backend solution.
# Usage: .\scripts\build-backend.ps1 [-Configuration Release|Debug] [-SkipPrereqCheck]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipPrereqCheck
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$solution = Join-Path $repoRoot "src\backend\Aganox.Lms.sln"

if (-not $SkipPrereqCheck) {
    Invoke-PrerequisiteCheck -Mode Build -Strict
}

Write-Host ""
Write-Host "=== Building backend ($Configuration) ===" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "src\backend")
try {
    dotnet restore Aganox.Lms.sln
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet build Aganox.Lms.sln -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host ""
    Write-Host "Backend build succeeded." -ForegroundColor Green
}
finally {
    Pop-Location
}
