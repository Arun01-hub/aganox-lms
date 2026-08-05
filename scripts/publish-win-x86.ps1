# Publish self-contained x86 package.
# Usage: .\scripts\publish-win-x86.ps1 [-Configuration Release]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipFrontend,
    [switch]$SkipPrereqCheck
)

& (Join-Path $PSScriptRoot "publish-win.ps1") `
    -Runtime win-x86 `
    -Configuration $Configuration `
    -SkipFrontend:$SkipFrontend `
    -SkipPrereqCheck:$SkipPrereqCheck

exit $LASTEXITCODE
