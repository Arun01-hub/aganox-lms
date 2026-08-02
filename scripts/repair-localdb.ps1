# Recreate a broken SQL Server LocalDB instance (MSSQLLocalDB).
# Usage: .\scripts\repair-localdb.ps1

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
    Write-Host "sqllocaldb not found. Install SQL Server Express with LocalDB." -ForegroundColor Red
    exit 1
}

Write-Host "Recreating LocalDB instance MSSQLLocalDB..." -ForegroundColor Cyan

sqllocaldb stop MSSQLLocalDB 2>$null
sqllocaldb delete MSSQLLocalDB 2>$null
sqllocaldb create MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

sqllocaldb start MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
sqllocaldb info MSSQLLocalDB
Write-Host ""
Write-Host "LocalDB repair completed." -ForegroundColor Green
