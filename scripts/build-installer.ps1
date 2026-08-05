# Build the Windows installer (Inno Setup) from a fresh x64 publish.
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
$publishDir = Join-Path $repoRoot "artifacts\publish\win-x64"
$installerScript = Join-Path $repoRoot "src\installer\Aganox.Lms.iss"
$outputDir = Join-Path $repoRoot "artifacts\installer"
$localDbPrereqDir = Join-Path $repoRoot "src\installer\prerequisites"

function Ensure-VcRedistInstaller {
    param([string]$TargetDir)

    $exePath = Join-Path $TargetDir "vc_redist.x64.exe"
    if (Test-Path $exePath) {
        $existing = Get-Item $exePath
        if ($existing.Length -gt 1MB) {
            return
        }
        Remove-Item $exePath -Force
    }

    Write-Host ""
    Write-Host "=== Downloading Visual C++ Redistributable prerequisite ===" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $url = "https://aka.ms/vs/17/release/vc_redist.x64.exe"
    Invoke-WebRequest -Uri $url -OutFile $exePath -UseBasicParsing

    if (-not (Test-Path $exePath) -or (Get-Item $exePath).Length -lt 1MB) {
        throw "vc_redist.x64.exe download failed or file is too small. Check network access and retry."
    }

    Write-Host "Saved Visual C++ Redistributable to $exePath" -ForegroundColor Green
}

function Ensure-SqlLocalDbInstaller {
    param([string]$TargetDir)

    $msiPath = Join-Path $TargetDir "SqlLocalDB.msi"
    if (Test-Path $msiPath) {
        $existing = Get-Item $msiPath
        if ($existing.Length -gt 1MB) {
            return
        }
        Remove-Item $msiPath -Force
    }

    Write-Host ""
    Write-Host "=== Downloading SQL Server LocalDB prerequisite ===" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

    $bootstrapperUrl = "https://download.microsoft.com/download/5/1/4/5145fe04-4d30-4b85-b0d1-39533663a2f1/SQL2022-SSEI-Expr.exe"
    $bootstrapper = Join-Path $env:TEMP "SQL2022-SSEI-Expr-$([guid]::NewGuid().ToString('n')).exe"

    try {
        Invoke-WebRequest -Uri $bootstrapperUrl -OutFile $bootstrapper -UseBasicParsing
        $proc = Start-Process -FilePath $bootstrapper -ArgumentList @(
            "/ACTION=Download",
            "/MEDIATYPE=LocalDB",
            "/MEDIAPATH=$TargetDir",
            "/QUIET"
        ) -PassThru -Wait
        if ($proc.ExitCode -ne 0) {
            throw "SQL Server Express bootstrapper exited with code $($proc.ExitCode)."
        }

        $found = $null
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            $found = Get-ChildItem -Path $TargetDir -Recurse -Filter "SqlLocalDB.msi" -ErrorAction SilentlyContinue |
                Where-Object { $_.Length -gt 1MB } |
                Sort-Object Length -Descending |
                Select-Object -First 1
            if ($found) {
                break
            }
            Start-Sleep -Seconds 2
        }

        if (-not $found) {
            throw "SqlLocalDB.msi was not downloaded. Check network access and retry."
        }

        if ($found.FullName -ne $msiPath) {
            Move-Item -Path $found.FullName -Destination $msiPath -Force
        }

        Get-ChildItem -Path $TargetDir -Directory -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

        Write-Host "Saved LocalDB installer to $msiPath" -ForegroundColor Green
    }
    finally {
        Remove-Item $bootstrapper -Force -ErrorAction SilentlyContinue
    }
}

& (Join-Path $PSScriptRoot "publish-win-x64.ps1") -Configuration $Configuration -SkipPrereqCheck:$SkipPrereqCheck
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

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
Ensure-VcRedistInstaller -TargetDir $localDbPrereqDir
Ensure-SqlLocalDbInstaller -TargetDir $localDbPrereqDir

Write-Host ""
Write-Host "=== Building installer ===" -ForegroundColor Cyan
& $iscc "/DPublishDir=$publishDir" "/O$outputDir" $installerScript
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Installer built in $outputDir" -ForegroundColor Green
