[CmdletBinding()]
param(
    [Parameter()]
    [Alias('App')]
    [string[]] $Application,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration,

    [Parameter()]
    [switch] $List
)

$ErrorActionPreference = 'Stop'
$BuildRoot = $PSScriptRoot

Import-Module (Join-Path $BuildRoot 'lib/Aganox.Build.psm1') -Force

if ($List) {
    Get-AganoxApplicationList -BuildRoot $BuildRoot | Format-Table -AutoSize
    return
}

Invoke-AganoxBuild -App $Application -Configuration $Configuration -BuildRoot $BuildRoot
