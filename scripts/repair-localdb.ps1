# Recreate a broken SQL Server LocalDB instance (MSSQLLocalDB) and reconcile AganoxLms dev files.
# Usage: .\scripts\repair-localdb.ps1 [-ResetAganoxDatabase]

param(
    [switch]$ResetAganoxDatabase
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
    Write-Host "sqllocaldb not found. Install SQL Server Express with LocalDB." -ForegroundColor Red
    exit 1
}

function Get-AganoxLocalDbFilePaths {
    return @{
        Mdf = Join-Path $env:USERPROFILE "AganoxLms.mdf"
        Ldf = Join-Path $env:USERPROFILE "AganoxLms_log.ldf"
    }
}

function Test-AganoxDatabaseRegistered {
    $query = "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'AganoxLms';"
    $count = sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $query -h -1 -W 2>$null
    return ($count -match '^\s*1\s*$')
}

function Move-OrphanedAganoxFiles {
    param([string]$Reason)

    $paths = Get-AganoxLocalDbFilePaths
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $moved = @()

    foreach ($path in @($paths.Mdf, $paths.Ldf)) {
        if (-not (Test-Path $path)) { continue }
        $backup = "$path.$stamp.bak"
        Move-Item $path $backup -Force
        $moved += $backup
    }

    if ($moved.Count -gt 0) {
        Write-Host ""
        Write-Host "Moved orphaned AganoxLms database files ($Reason):" -ForegroundColor Yellow
        foreach ($file in $moved) { Write-Host "  $file" -ForegroundColor Yellow }
        Write-Host "The API will recreate AganoxLms on next startup." -ForegroundColor Yellow
    }
}

Write-Host "Recreating LocalDB instance MSSQLLocalDB..." -ForegroundColor Cyan

$prevEap = $ErrorActionPreference
$ErrorActionPreference = "Continue"
sqllocaldb stop MSSQLLocalDB 2>$null
sqllocaldb delete MSSQLLocalDB 2>$null
$ErrorActionPreference = $prevEap

sqllocaldb create MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

sqllocaldb start MSSQLLocalDB
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($ResetAganoxDatabase) {
    Move-OrphanedAganoxFiles -Reason "manual reset requested"
} else {
    $paths = Get-AganoxLocalDbFilePaths
    $filesExist = (Test-Path $paths.Mdf) -or (Test-Path $paths.Ldf)
    $registered = Test-AganoxDatabaseRegistered

    if ($filesExist -and -not $registered) {
        Write-Host ""
        Write-Host "Orphaned AganoxLms files found but database is not registered in LocalDB." -ForegroundColor Yellow
        Write-Host "Attempting to attach existing database files..." -ForegroundColor Cyan

        if ((Test-Path $paths.Mdf) -and (Test-Path $paths.Ldf)) {
            $attachSql = @"
CREATE DATABASE AganoxLms ON
(FILENAME = N'$($paths.Mdf)'),
(FILENAME = N'$($paths.Ldf)')
FOR ATTACH;
"@
            $attachResult = sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $attachSql 2>&1 | Out-String
            if ($LASTEXITCODE -ne 0 -or $attachResult -match "Msg [0-9]+") {
                Write-Host "Attach failed (often due to SQL version mismatch after LocalDB reinstall)." -ForegroundColor Yellow
                Move-OrphanedAganoxFiles -Reason "attach failed; incompatible or orphaned files"
            } else {
                Write-Host "Attached existing AganoxLms database." -ForegroundColor Green
            }
        } else {
            Move-OrphanedAganoxFiles -Reason "incomplete file pair"
        }
    }
}

Write-Host ""
sqllocaldb info MSSQLLocalDB
Write-Host ""
Write-Host "LocalDB repair completed." -ForegroundColor Green
