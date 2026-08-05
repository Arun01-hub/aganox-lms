# Publish self-contained x64 package.
# Usage: .\scripts\publish-win-x64.ps1 [-Configuration Release]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipFrontend,
    [switch]$SkipPrereqCheck
)

& (Join-Path $PSScriptRoot "publish-win.ps1") `
    -Runtime win-x64 `
    -Configuration $Configuration `
    -SkipFrontend:$SkipFrontend `
    -SkipPrereqCheck:$SkipPrereqCheck

exit $LASTEXITCODE
