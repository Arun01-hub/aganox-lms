[CmdletBinding()]
param(
    [Parameter()]
    [Alias('App')]
    [string[]] $Application,

    [Parameter()]
    [switch] $Status
)

$ErrorActionPreference = 'Stop'
$BuildRoot = $PSScriptRoot

Import-Module (Join-Path $BuildRoot 'lib/Aganox.Build.psm1') -Force

if ($Status) {
    $running = Get-AganoxRunningState -BuildRoot $BuildRoot
    if ($running.Count -eq 0) {
        Write-Host 'No tracked Aganox applications are running.' -ForegroundColor Yellow
        return
    }

    $rows = foreach ($entry in $running) {
        $process = Get-Process -Id $entry.processId -ErrorAction SilentlyContinue
        $alive = if ($process) { 'running' } else { 'stopped' }
        [pscustomobject]@{
            Id = $entry.id
            Name = $entry.name
            ProcessId = $entry.processId
            Status = $alive
            StartedAt = $entry.startedAt
            Command = $entry.command
        }
    }

    $rows | Format-Table -AutoSize

    return
}

Stop-AganoxApplications -App $Application -BuildRoot $BuildRoot
