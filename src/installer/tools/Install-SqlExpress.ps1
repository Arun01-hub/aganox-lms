# Installs SQL Server 2022 Express (SQLEXPRESS instance) when no suitable instance exists.
# Called by the Inno Setup installer.

param(
    [Parameter(Mandatory = $true)]
    [string]$BootstrapperPath,

    [string]$InstanceName = "SQLEXPRESS",
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

function Test-SqlInstanceExists {
    param([string]$Name)

    $key = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    if (Test-Path $key) {
        $value = Get-ItemProperty -Path $key -Name $Name -ErrorAction SilentlyContinue
        if ($null -ne $value.$Name) {
            return $true
        }
    }

    $key32 = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Microsoft SQL Server\Instance Names\SQL"
    if (Test-Path $key32) {
        $value = Get-ItemProperty -Path $key32 -Name $Name -ErrorAction SilentlyContinue
        if ($null -ne $value.$Name) {
            return $true
        }
    }

    return $false
}

function Wait-SqlService {
    param(
        [string]$Name,
        [int]$TimeoutSeconds = 600
    )

    $serviceName = if ($Name -eq "MSSQLSERVER") { "MSSQLSERVER" } else { "MSSQL`$$Name" }
    Write-Log "Waiting for SQL service '$serviceName' to start..."

    for ($i = 0; $i -lt $TimeoutSeconds; $i += 5) {
        $svc = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($svc -and $svc.Status -eq "Running") {
            Write-Log "SQL service '$serviceName' is running."
            return
        }
        Start-Sleep -Seconds 5
    }

    throw "SQL service '$serviceName' did not start within $TimeoutSeconds seconds."
}

Write-Log "SQL Server Express install starting (instance: $InstanceName)."

if (-not (Test-Path $BootstrapperPath)) {
    throw "SQL Express bootstrapper not found: $BootstrapperPath"
}

if (Test-SqlInstanceExists -Name $InstanceName) {
    Write-Log "SQL instance '$InstanceName' already exists; skipping Express setup."
    exit 0
}

if ($InstanceName -ne "SQLEXPRESS") {
    throw "The SQL2022-SSEI-Expr bootstrapper only supports the default SQLEXPRESS instance name."
}

Write-Log "Running bootstrapper: $BootstrapperPath"
Write-Log "Internet access is required; the bootstrapper downloads engine files during setup."

# SQL2022-SSEI-Expr.exe is a small web bootstrapper. It only accepts a limited set of
# parameters (/ACTION, /QUIET, /IACCEPTSQLSERVERLICENSETERMS, /ENU, etc.). Passing
# setup.exe options such as /FEATURES or /INSTANCENAME causes immediate exit code -1.
$arguments = @(
    "/ACTION=Install",
    "/QUIET",
    "/IACCEPTSQLSERVERLICENSETERMS",
    "/ENU",
    "/HIDEPROGRESSBAR"
)

$proc = Start-Process -FilePath $BootstrapperPath -ArgumentList $arguments -PassThru -Wait
Write-Log "Bootstrapper exit code: $($proc.ExitCode)"

$sqlSetupLogRoot = Join-Path ${env:ProgramFiles} "Microsoft SQL Server\160\Setup Bootstrap\Log"
if (Test-Path $sqlSetupLogRoot) {
    $latestSetupLog = Get-ChildItem -Path $sqlSetupLogRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($latestSetupLog) {
        Write-Log "SQL Server setup logs: $($latestSetupLog.FullName)"
    }
}

if ($proc.ExitCode -ne 0 -and $proc.ExitCode -ne 3010) {
    $hint = switch ($proc.ExitCode) {
        -1 { "The bootstrapper rejected the install command or could not download setup files. Verify internet access and that no other SQL setup is running." }
        default { "See the latest folder under $sqlSetupLogRoot" }
    }
    throw "SQL Server Express setup failed with exit code $($proc.ExitCode). $hint"
}

if (-not (Test-SqlInstanceExists -Name $InstanceName)) {
    throw "SQL Server Express setup completed but instance '$InstanceName' was not registered."
}

Wait-SqlService -Name $InstanceName
Write-Log "SQL Server Express instance '$InstanceName' is ready."
