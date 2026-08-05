# Create a SQL Server .bak file using the configured connection string.
# Usage: .\scripts\backup-db.ps1 [-OutputFolder C:\Backups\AganoxLms]

param(
    [string]$OutputFolder = (Join-Path $env:USERPROFILE "Documents\AganoxLms\Backups")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$apiDir = Join-Path $repoRoot "src\backend\Aganox.Lms.Api"

if (-not (Test-Path $apiDir)) {
    Write-Host "API project not found." -ForegroundColor Red
    exit 1
}

New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null

Push-Location $apiDir
try {
    $connectionString = (Get-Content "appsettings.json" -Raw | ConvertFrom-Json).ConnectionStrings.DefaultConnection
    if (-not $connectionString) {
        throw "DefaultConnection not found in appsettings.json"
    }

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $databaseName = $builder.InitialCatalog
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = Join-Path $OutputFolder "$databaseName`_$timestamp.bak"

    $escapedPath = $backupPath.Replace("'", "''")
    $sql = "BACKUP DATABASE [$databaseName] TO DISK = N'$escapedPath' WITH FORMAT, INIT, NAME = N'Aganox LMS Backup';"

    Write-Host "Backing up [$databaseName] to $backupPath" -ForegroundColor Cyan
    sqlcmd -S $builder.DataSource -E -Q $sql
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd backup failed (exit $LASTEXITCODE). Ensure SQL Server LocalDB/Express is running."
    }

    Write-Host "Backup completed." -ForegroundColor Green
}
finally {
    Pop-Location
}
