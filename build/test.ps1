[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('chromium', 'firefox', 'chromium-unauthenticated', 'all')]
    [string] $Project = 'chromium',

    [Parameter()]
    [switch] $EnsureRunning,

    [Parameter()]
    [switch] $SkipBuild,

    [Parameter()]
    [switch] $OpenReport,

    [Parameter()]
    [switch] $Headed,

    [Parameter()]
    [string[]] $Filter
)

$ErrorActionPreference = 'Stop'
$BuildRoot = $PSScriptRoot
$RepoRoot = (Resolve-Path (Join-Path $BuildRoot '..')).Path
$TestRoot = Join-Path $RepoRoot 'tests/Aganox.Rental.UITests'

Import-Module (Join-Path $BuildRoot 'lib/Aganox.Build.psm1') -Force

function Test-AganoxHttpEndpoint {
    param(
        [Parameter(Mandatory)]
        [string] $Url
    )

    try {
        Invoke-WebRequest -Uri $Url -SkipCertificateCheck -TimeoutSec 5 | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

if ($EnsureRunning) {
    $needsStart = $false
    if (-not (Test-AganoxHttpEndpoint -Url 'http://localhost:4200')) { $needsStart = $true }
    if (-not (Test-AganoxHttpEndpoint -Url 'https://localhost:7036')) { $needsStart = $true }
    if (-not (Test-AganoxHttpEndpoint -Url 'https://localhost:7241/health')) { $needsStart = $true }

    if ($needsStart) {
        Write-Host 'Starting rental stack (identity-server, rental-api, rental-web)...' -ForegroundColor Cyan
        Invoke-AganoxRun -App @('identity-server', 'rental-api', 'rental-web') -SkipBuild:$SkipBuild -BuildRoot $BuildRoot
        Start-Sleep -Seconds 15
    }
    else {
        Write-Host 'Rental stack already reachable.' -ForegroundColor DarkGray
    }
}

if (-not (Test-Path (Join-Path $TestRoot '.env'))) {
    Copy-Item (Join-Path $TestRoot '.env.example') (Join-Path $TestRoot '.env')
    Write-Host "Created tests/Aganox.Rental.UITests/.env from .env.example" -ForegroundColor DarkGray
}

Push-Location $TestRoot
try {
    if (-not (Test-Path 'node_modules')) {
        Write-Host 'Installing UI test dependencies...' -ForegroundColor Cyan
        npm.cmd install
        npx playwright install chromium firefox
    }

    $npmArgs = @('run', 'test', '--')
    if ($Project -ne 'all') {
        $npmArgs += @('--project', $Project)
    }
    if ($Headed) {
        $npmArgs += '--headed'
    }
    if ($Filter) {
        $npmArgs += @('--grep', ($Filter -join '|'))
    }

    Write-Host "Running UI tests from $TestRoot" -ForegroundColor Cyan
    & npm.cmd @npmArgs
    $exitCode = $LASTEXITCODE

    Write-Host ''
    Write-Host 'Reports:' -ForegroundColor Green
    Write-Host "  Summary HTML : tests/Aganox.Rental.UITests/reports/summary.html"
    Write-Host "  Playwright   : tests/Aganox.Rental.UITests/reports/html/index.html"
    Write-Host "  JUnit XML    : tests/Aganox.Rental.UITests/reports/junit/results.xml"
    Write-Host "  JSON summary : tests/Aganox.Rental.UITests/reports/summary.json"

    if ($OpenReport -and (Test-Path 'reports/summary.html')) {
        Start-Process (Resolve-Path 'reports/summary.html').Path
    }

    exit $exitCode
}
finally {
    Pop-Location
}
