[CmdletBinding()]
param(
    [Parameter()]
    [Alias('App')]
    [string[]] $Application,

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration,

    [Parameter()]
    [switch] $SkipBuild,

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

Invoke-AganoxRestart -App $Application -SkipBuild:$SkipBuild -Configuration $Configuration -BuildRoot $BuildRoot
