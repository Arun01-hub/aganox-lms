# Creates ProgramData config, tests SQL connectivity, installs and starts the Windows Service.
# Called by the Inno Setup installer (interactive or silent).

param(
    [Parameter(Mandatory = $true)]
    [string]$InstallDir,

    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$SqlDatabase = "AganoxLms",
    [int]$Port = 8080,
    [string]$ServiceAccount = "LocalSystem",
    [switch]$CreateDatabase,
    [switch]$SkipServiceInstall,
    [switch]$SkipHealthCheck,
    [string]$LogFile = ""
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message)
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $line
    if ($LogFile) {
        Add-Content -Path $LogFile -Value $line -Encoding UTF8
    }
}

function New-SecureJwtKey {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

function Test-SqlConnection {
    param(
        [string]$ConnectionString,
        [string]$Label
    )

    Write-Log "Testing SQL connection ($Label)..."
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        Write-Log "SQL connection OK ($Label)."
        return $true
    }
    finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
    }
}

function Resolve-SqlLoginName {
    param([string]$Account)

    switch ($Account.Trim().ToLowerInvariant()) {
        "localsystem" { return "NT AUTHORITY\SYSTEM" }
        "networkservice" { return "NT AUTHORITY\NETWORK SERVICE" }
        "localservice" { return "NT AUTHORITY\LOCAL SERVICE" }
        default { return $Account.Trim() }
    }
}

function Invoke-SqlNonQuery {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $CommandText
        $command.ExecuteNonQuery() | Out-Null
    }
    finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
    }
}

function Ensure-Database {
    param(
        [string]$MasterConnectionString,
        [string]$DatabaseName
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $MasterConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = "SELECT DB_ID(@name)"
        $null = $command.Parameters.AddWithValue("@name", $DatabaseName)
        $dbId = $command.ExecuteScalar()
        if ($dbId -ne [DBNull]::Value -and $null -ne $dbId) {
            Write-Log "Database '$DatabaseName' already exists."
            return
        }

        Write-Log "Creating database '$DatabaseName'..."
        $create = $connection.CreateCommand()
        $escaped = $DatabaseName.Replace("]", "]]")
        $create.CommandText = "CREATE DATABASE [$escaped]"
        $create.ExecuteNonQuery() | Out-Null
        Write-Log "Database '$DatabaseName' created."
    }
    finally {
        if ($connection.State -eq 'Open') {
            $connection.Close()
        }
    }
}

function Grant-ServiceAccountSqlAccess {
    param(
        [string]$MasterConnectionString,
        [string]$DatabaseName,
        [string]$Account
    )

    $loginName = Resolve-SqlLoginName -Account $Account
    $escapedLogin = $loginName.Replace("]", "]]")
    $escapedDb = $DatabaseName.Replace("]", "]]")

    Write-Log "Granting SQL access to service account '$loginName' on database '$DatabaseName'..."

    $loginSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$escapedLogin')
    CREATE LOGIN [$escapedLogin] FROM WINDOWS;
"@

    $databaseSql = @"
USE [$escapedDb];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$escapedLogin')
    CREATE USER [$escapedLogin] FOR LOGIN [$escapedLogin];
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members drm
    JOIN sys.database_principals role_principal ON drm.role_principal_id = role_principal.principal_id
    JOIN sys.database_principals member ON drm.member_principal_id = member.principal_id
    WHERE role_principal.name = N'db_owner' AND member.name = N'$escapedLogin')
    ALTER ROLE db_owner ADD MEMBER [$escapedLogin];
"@

    Invoke-SqlNonQuery -ConnectionString $MasterConnectionString -CommandText $loginSql
    Invoke-SqlNonQuery -ConnectionString $MasterConnectionString -CommandText $databaseSql
    Write-Log "SQL access granted to '$loginName'."
}

$programDataRoot = Join-Path $env:ProgramData "Aganox\LMS"
$configPath = Join-Path $programDataRoot "appsettings.Production.json"
$logsDir = Join-Path $programDataRoot "logs"

Write-Log "Aganox LMS server configuration starting."
Write-Log "Install directory: $InstallDir"
Write-Log "ProgramData root: $programDataRoot"

if (-not (Test-Path (Join-Path $InstallDir "Aganox.Lms.Api.exe"))) {
    throw "Aganox.Lms.Api.exe not found in $InstallDir"
}

New-Item -ItemType Directory -Path $programDataRoot -Force | Out-Null
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

$jwtKey = New-SecureJwtKey
$masterConnection = "Server=$SqlServer;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=15"
$appConnection = "Server=$SqlServer;Database=$SqlDatabase;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=15"

Test-SqlConnection -ConnectionString $masterConnection -Label "master"

if ($CreateDatabase) {
    Ensure-Database -MasterConnectionString $masterConnection -DatabaseName $SqlDatabase
}

Grant-ServiceAccountSqlAccess `
    -MasterConnectionString $masterConnection `
    -DatabaseName $SqlDatabase `
    -Account $ServiceAccount

Test-SqlConnection -ConnectionString $appConnection -Label $SqlDatabase

$config = [ordered]@{
    Urls = "http://+:$Port"
    ConnectionStrings = [ordered]@{
        DefaultConnection = "Server=$SqlServer;Database=$SqlDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
    }
    Jwt = [ordered]@{
        Issuer            = "Aganox.Lms"
        Audience          = "Aganox.Lms.Clients"
        Key               = $jwtKey
        AccessTokenMinutes = 60
    }
    Cors = [ordered]@{
        AllowedOrigins = @()
    }
    Serilog = [ordered]@{
        MinimumLevel = [ordered]@{
            Default = "Warning"
            Override = [ordered]@{
                Microsoft = "Warning"
                "Microsoft.Hosting.Lifetime" = "Information"
            }
        }
    }
    AllowedHosts = "*"
}

$config | ConvertTo-Json -Depth 6 | Set-Content -Path $configPath -Encoding UTF8
Write-Log "Wrote production config: $configPath"

if (-not $SkipServiceInstall) {
    $installService = Join-Path $InstallDir "tools\Install-Service.ps1"
    if (-not (Test-Path $installService)) {
        throw "Install-Service.ps1 not found: $installService"
    }

    & $installService `
        -InstallDir $InstallDir `
        -Port $Port `
        -ServiceAccount $ServiceAccount `
        -LogFile $LogFile

    if ($LASTEXITCODE -ne 0) {
        throw "Install-Service.ps1 failed with exit code $LASTEXITCODE"
    }
}

if (-not $SkipHealthCheck) {
    $healthUrl = "http://localhost:$Port/health"
    Write-Log "Waiting for $healthUrl ..."
    $ready = $false
    for ($i = 0; $i -lt 120; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $ready) {
        throw "Health check failed: $healthUrl did not respond within 120 seconds."
    }

    Write-Log "Health check OK: $healthUrl"
}

Write-Log "Server configuration completed successfully."
