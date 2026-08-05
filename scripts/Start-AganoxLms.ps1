# Starts Aganox LMS for installed / published deployments.
# Creates appsettings.Local.json (JWT key) on first run and waits for the API before opening the browser.

$ErrorActionPreference = "Stop"

$installDir = $PSScriptRoot
Set-Location $installDir

$exePath = Join-Path $installDir "Aganox.Lms.Api.exe"
$localSettings = Join-Path $installDir "appsettings.Local.json"
$logDir = Join-Path $installDir "logs"
$appUrl = "http://localhost:8080"

if (-not (Test-Path $exePath)) {
    Write-Host "Aganox.Lms.Api.exe not found in $installDir" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

if (-not (Test-Path $localSettings)) {
    Write-Host "First run - creating appsettings.Local.json with a secure JWT key..." -ForegroundColor Cyan
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $key = [Convert]::ToBase64String($bytes)
    $json = @{ Jwt = @{ Key = $key } } | ConvertTo-Json -Depth 3 -Compress
    [System.IO.File]::WriteAllText($localSettings, $json, (New-Object System.Text.UTF8Encoding $false))
}

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:8080"

Write-Host ""
Write-Host "=== Aganox LMS ===" -ForegroundColor Cyan
Write-Host "Starting server at $appUrl ..."
Write-Host "Keep this window open while using the application."
Write-Host ""

$proc = Start-Process -FilePath $exePath -WorkingDirectory $installDir -PassThru

$ready = $false
for ($i = 0; $i -lt 90; $i++) {
    if ($proc.HasExited) {
        Write-Host ""
        Write-Host "Server exited unexpectedly (code $($proc.ExitCode))." -ForegroundColor Red
        if (Test-Path $logDir) {
            $latest = Get-ChildItem $logDir -Filter "aganox-lms-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($latest) {
                Write-Host "Latest log: $($latest.FullName)" -ForegroundColor Yellow
                Get-Content $latest.FullName -Tail 15 | ForEach-Object { Write-Host $_ }
            }
        }
        Read-Host "Press Enter to exit"
        exit $(if ($null -ne $proc.ExitCode) { $proc.ExitCode } else { 1 })
    }

    try {
        $response = Invoke-WebRequest -Uri "$appUrl/health" -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            $ready = $true
            break
        }
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if ($ready) {
    Write-Host "Server ready. Opening $appUrl" -ForegroundColor Green
    Start-Process $appUrl
}
else {
    Write-Host "Server did not respond within 90 seconds. Check logs in $logDir" -ForegroundColor Yellow
}

Wait-Process -Id $proc.Id
