# Shared prerequisite checks for Aganox LMS build/run scripts.

function Write-PrereqHeader {
    Write-Host ""
    Write-Host "=== Aganox LMS - prerequisite check ===" -ForegroundColor Cyan
}

function Test-CommandExists {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-DotNetSdkVersion {
    if (-not (Test-CommandExists "dotnet")) { return $null }
    $output = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return [version]$output
}

function Test-DotNet8Sdk {
    param([ref]$Issues, [ref]$Warnings)

    if (-not (Test-CommandExists "dotnet")) {
        $Issues.Value += "dotnet CLI not found. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
        return $false
    }

    $version = Get-DotNetSdkVersion
    if ($null -eq $version) {
        $Issues.Value += "Unable to determine .NET SDK version."
        return $false
    }

    if ($version.Major -lt 8) {
        $Issues.Value += ".NET SDK 8.0+ required (found $($version.ToString()))."
        return $false
    }

    if ($version.Major -gt 8) {
        $Warnings.Value += ".NET SDK $($version.ToString()) detected (project targets net10.0, SDK 10.x recommended)."
    }

    Write-Host "  [OK] .NET SDK $($version.ToString())" -ForegroundColor Green
    return $true
}

function Test-NodeJs {
    param(
        [ref]$Issues,
        [ref]$Warnings,
        [int]$MinimumMajor = 22
    )

    if (-not (Test-CommandExists "node")) {
        $Issues.Value += "Node.js not found. Install Node.js 22+ LTS: https://nodejs.org/"
        return $false
    }

    $nodeVersionRaw = (cmd /c "node --version 2>nul") -replace '^v', '' -replace '\r',''
    if (-not $nodeVersionRaw) {
        $Issues.Value += "Unable to determine Node.js version."
        return $false
    }

    $nodeVersion = [version]$nodeVersionRaw
    if ($nodeVersion.Major -lt $MinimumMajor) {
        $Issues.Value += "Node.js $MinimumMajor+ required (found v$nodeVersionRaw). PATH may be pointing at an older install."
        return $false
    }

    if ($nodeVersion.Major -eq 22 -and $nodeVersion -lt [version]"22.22.3") {
        $Issues.Value += "Angular CLI 22 requires Node.js 22.22.3+ (found v$nodeVersionRaw)."
        return $false
    }

    if ($nodeVersion.Major -eq 24 -and $nodeVersion -lt [version]"24.15.0") {
        $Issues.Value += "Angular CLI 22 requires Node.js 24.15.0+ (found v$nodeVersionRaw)."
        return $false
    }

    if ($nodeVersion.Major -ge 23 -and $nodeVersion.Major -lt 24) {
        $Warnings.Value += "Node.js v$nodeVersionRaw is not in an Angular CLI 22 supported line (use 22.22+, 24.15+, or 26+)."
    }

    if ($nodeVersion.Major -ge 25 -and $nodeVersion.Major -lt 26) {
        $Warnings.Value += "Node.js v$nodeVersionRaw is not in an Angular CLI 22 supported line (use 22.22+, 24.15+, or 26+)."
    }

    $npmVersion = (cmd /c "npm --version 2>nul") -replace '\r',''
    Write-Host "  [OK] Node.js v$nodeVersionRaw (npm $npmVersion)" -ForegroundColor Green
    return $true
}

function Test-VcRedist {
    param(
        [ref]$Issues,
        [ref]$Warnings,
        [switch]$Required
    )

    $keys = @(
        "HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"
    )

    foreach ($key in $keys) {
        $installed = Get-ItemProperty -Path $key -Name Installed -ErrorAction SilentlyContinue
        if ($installed -and $installed.Installed -eq 1) {
            $version = (Get-ItemProperty -Path $key -Name Version -ErrorAction SilentlyContinue).Version
            Write-Host "  [OK] Visual C++ Redistributable x64 ($version)" -ForegroundColor Green
            return $true
        }
    }

    $message = "Visual C++ 2015-2022 Redistributable (x64) not found. Required for SQL Server LocalDB."
    if ($Required) {
        $Issues.Value += $message
    } else {
        $Warnings.Value += $message
        Write-Host "  [WARN] Visual C++ Redistributable x64 missing" -ForegroundColor Yellow
    }
    return $false
}

function Test-SqlLocalDb {
    param(
        [ref]$Issues,
        [ref]$Warnings,
        [switch]$Required
    )

    if (-not (Test-CommandExists "sqllocaldb")) {
        if ($Required) {
            $Issues.Value += "SQL Server LocalDB (sqllocaldb) not found. Install SQL Server Express / LocalDB."
        } else {
            $Warnings.Value += "sqllocaldb not found. API needs SQL Server LocalDB, Express, or a custom connection string."
        }
        return $false
    }

    $info = & sqllocaldb info MSSQLLocalDB 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0 -or $info -match "does not exist|not found") {
        $Warnings.Value += "LocalDB instance MSSQLLocalDB not found. Run scripts\repair-localdb.ps1 or create it manually."
        Write-Host "  [WARN] LocalDB instance MSSQLLocalDB missing" -ForegroundColor Yellow
        return $false
    }

    if ($info -match "State:\s+Running") {
        Write-Host "  [OK] LocalDB MSSQLLocalDB (Running)" -ForegroundColor Green
    } elseif ($info -match "State:\s+Stopped") {
        Write-Host "  [OK] LocalDB MSSQLLocalDB (Stopped - will start on first API run)" -ForegroundColor Green
    } else {
        Write-Host "  [OK] LocalDB MSSQLLocalDB installed" -ForegroundColor Green
    }

    return $true
}

function Test-DevHttpsCert {
    param([ref]$Warnings)

    if (-not (Test-CommandExists "dotnet")) { return $false }

    $certs = & dotnet dev-certs https --check --trust 2>&1 | Out-String
    if ($certs -match "A trusted certificate was found|already trusted") {
        Write-Host "  [OK] HTTPS dev certificate trusted" -ForegroundColor Green
        return $true
    }

    $Warnings.Value += "HTTPS dev certificate may not be trusted. Run: dotnet dev-certs https --trust"
    Write-Host "  [WARN] HTTPS dev certificate not trusted" -ForegroundColor Yellow
    return $false
}

function Invoke-PrerequisiteCheck {
    param(
        [ValidateSet("Build", "RunApi", "RunWeb", "All")]
        [string]$Mode = "All",
        [switch]$Strict
    )

    Write-PrereqHeader

    $issues = [System.Collections.Generic.List[string]]::new()
    $warnings = [System.Collections.Generic.List[string]]::new()

    $needsBackend = $Mode -in @("Build", "RunApi", "All")
    $needsFrontend = $Mode -in @("Build", "RunWeb", "All")

    if ($needsBackend) {
        Write-Host "Backend:" -ForegroundColor White
        [void](Test-DotNet8Sdk -Issues ([ref]$issues) -Warnings ([ref]$warnings))
        if ($Mode -in @("RunApi", "All")) {
            [void](Test-VcRedist -Issues ([ref]$issues) -Warnings ([ref]$warnings))
            [void](Test-SqlLocalDb -Issues ([ref]$issues) -Warnings ([ref]$warnings))
            [void](Test-DevHttpsCert -Warnings ([ref]$warnings))
        }
    }

    if ($needsFrontend) {
        Write-Host "Frontend:" -ForegroundColor White
        [void](Test-NodeJs -Issues ([ref]$issues) -Warnings ([ref]$warnings))
    }

    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings:" -ForegroundColor Yellow
        foreach ($w in $warnings) { Write-Host "  - $w" -ForegroundColor Yellow }
    }

    if ($issues.Count -gt 0) {
        Write-Host ""
        Write-Host "Missing prerequisites:" -ForegroundColor Red
        foreach ($i in $issues) { Write-Host "  - $i" -ForegroundColor Red }
        if ($Strict) { exit 1 }
        Write-Host ""
        Write-Host "Prerequisite check failed." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "All required prerequisites satisfied." -ForegroundColor Green
    return $true
}

function Get-RepoRoot {
    $root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    return $root.Path
}
