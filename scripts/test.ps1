# Run .NET unit tests.
# Usage: .\scripts\test.ps1 [-Configuration Debug|Release] [-SkipPrereqCheck]

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
Write-Host "=== Running tests ===" -ForegroundColor Cyan
Push-Location (Join-Path $repoRoot "src\backend")
try {
    dotnet test Aganox.Lms.sln -c $Configuration --no-restore
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
