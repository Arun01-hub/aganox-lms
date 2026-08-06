# Installs or updates the AganoxLmsApi Windows Service.

param(
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,

    [int]$Port = 8080,
    [string]$ServiceAccount = "LocalSystem",
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

$serviceName = "AganoxLmsApi"
$displayName = "Aganox LMS API"
$exePath = (Resolve-Path (Join-Path $InstallDir "Aganox.Lms.Api.exe")).Path

function Write-Log {
    param([string]$Message)
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) {
        Add-Content -Path $LogFile -Value $line -Encoding UTF8
    }
}

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Log "Stopping existing service '$serviceName'..."
    if ($existing.Status -eq 'Running') {
        Stop-Service -Name $serviceName -Force -ErrorAction Stop
    }
    Write-Log "Removing existing service '$serviceName'..."
    & sc.exe delete $serviceName | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe delete failed with exit code $LASTEXITCODE"
    }
    Start-Sleep -Seconds 2
}

$binPath = "`"$exePath`""
Write-Log "Creating service '$serviceName' -> $exePath"
& sc.exe create $serviceName binPath= $binPath start= auto DisplayName= "$displayName" | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe create failed with exit code $LASTEXITCODE"
}

if ($ServiceAccount -and $ServiceAccount -ne "LocalSystem") {
    Write-Log "Setting service account: $ServiceAccount"
    & sc.exe config $serviceName obj= "$ServiceAccount" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe config obj failed with exit code $LASTEXITCODE"
    }
}

$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$serviceName"
New-ItemProperty -Path $regPath -Name Description -Value "Aganox Leave Management System API and web UI" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $regPath -Name Environment -PropertyType MultiString -Value @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:$Port"
) -Force | Out-Null

& sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Log "Starting service '$serviceName'..."
try {
    Start-Service -Name $serviceName -ErrorAction Stop
}
catch {
    $appLogDir = Join-Path $env:ProgramData "Aganox\LMS\logs"
    $latestLog = Get-ChildItem -Path $appLogDir -Filter "aganox-lms-*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latestLog) {
        Write-Log "Recent application log ($($latestLog.FullName)):"
        Get-Content -Path $latestLog.FullName -Tail 20 | ForEach-Object { Write-Log "  $_" }
    }

    throw
}

for ($i = 0; $i -lt 60; $i++) {
    $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq 'Running') {
        Write-Log "Service '$serviceName' is running."
        return
    }
    Start-Sleep -Seconds 1
}

throw "Service '$serviceName' did not reach Running state within 60 seconds."
