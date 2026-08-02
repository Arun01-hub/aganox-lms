# Check build/run prerequisites for Aganox LMS.
# Usage: .\scripts\check-prerequisites.ps1 [-Mode Build|RunApi|RunWeb|All]

param(
    [ValidateSet("Build", "RunApi", "RunWeb", "All")]
    [string]$Mode = "All"
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

Invoke-PrerequisiteCheck -Mode $Mode -Strict
