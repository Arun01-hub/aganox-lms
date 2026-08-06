# Stops and removes the AganoxLmsApi Windows Service.

param(
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

$serviceName = "AganoxLmsApi"

function Write-Log {
    param([string]$Message)
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) {
        Add-Content -Path $LogFile -Value $line -Encoding UTF8
    }
}

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $existing) {
    Write-Log "Service '$serviceName' is not installed."
    exit 0
}

Write-Log "Stopping service '$serviceName'..."
if ($existing.Status -eq 'Running') {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
}

Write-Log "Removing service '$serviceName'..."
sc.exe delete $serviceName | Out-Null
Write-Log "Service '$serviceName' removed."
