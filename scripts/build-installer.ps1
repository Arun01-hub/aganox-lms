# Build the Windows enterprise server installer (Inno Setup) from a fresh server publish.
# Requires Inno Setup 6+ (ISCC.exe on PATH or at default install location).
# Usage: .\scripts\build-installer.ps1 [-Configuration Release]

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipPrereqCheck
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Prerequisites.ps1")

$repoRoot = Get-RepoRoot
$publishDir = Join-Path $repoRoot "artifacts\publish\server-x64"
$installerScript = Join-Path $repoRoot "src\installer\Aganox.Lms.iss"
$outputDir = Join-Path $repoRoot "artifacts\installer"
$prereqDir = Join-Path $repoRoot "src\installer\prerequisites"

function Ensure-SqlExpressBootstrapper {
    param([string]$TargetDir)

    $exePath = Join-Path $TargetDir "SQL2022-SSEI-Expr.exe"
    if (Test-Path $exePath) {
        $existing = Get-Item $exePath
        if ($existing.Length -gt 1MB) {
            return
        }
        Remove-Item $exePath -Force
    }

    Write-Host ""
    Write-Host "=== Downloading SQL Server 2022 Express bootstrapper ===" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $url = "https://download.microsoft.com/download/5/1/4/5145fe04-4d30-4b85-b0d1-39533663a2f1/SQL2022-SSEI-Expr.exe"
    Invoke-WebRequest -Uri $url -OutFile $exePath -UseBasicParsing

    if (-not (Test-Path $exePath) -or (Get-Item $exePath).Length -lt 1MB) {
        throw "SQL2022-SSEI-Expr.exe download failed or file is too small. Check network access and retry."
    }

    Write-Host "Saved SQL Express bootstrapper to $exePath" -ForegroundColor Green
}

& (Join-Path $PSScriptRoot "publish-server.ps1") -Configuration $Configuration -SkipPrereqCheck:$SkipPrereqCheck
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$requiredPublishFiles = @(
    (Join-Path $publishDir "tools\Install-SqlExpress.ps1"),
    (Join-Path $publishDir "tools\Configure-Server.ps1"),
    (Join-Path $publishDir "tools\Install-Service.ps1"),
    (Join-Path $publishDir "tools\Uninstall-Service.ps1")
)
foreach ($path in $requiredPublishFiles) {
    if (-not (Test-Path $path)) {
        Write-Host "Publish output is missing required installer file: $path" -ForegroundColor Red
        Write-Host "Re-run publish-server.ps1 and ensure src\installer\tools is copied into a tools\ folder." -ForegroundColor Yellow
        exit 1
    }
}

function Get-InnoSetupCompiler {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    return $null
}

$iscc = Get-InnoSetupCompiler

if (-not $iscc) {
    Write-Host "Inno Setup compiler (ISCC.exe) not found. Install from https://jrsoftware.org/isinfo.php" -ForegroundColor Red
    Write-Host "Publish output is ready at: $publishDir" -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
Ensure-SqlExpressBootstrapper -TargetDir $prereqDir

Write-Host ""
Write-Host "=== Building installer ===" -ForegroundColor Cyan
& $iscc "/DPublishDir=$publishDir" "/O$outputDir" $installerScript
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Installer built in $outputDir" -ForegroundColor Green
