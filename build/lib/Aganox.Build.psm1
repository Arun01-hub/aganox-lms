# Aganox build/run helpers. Import from build/*.ps1 entry points.

function Get-AganoxRepoRoot {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    return (Resolve-Path -LiteralPath (Join-Path $BuildRoot '..')).Path
}

function Get-AganoxBuildRoot {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    return (Resolve-Path -LiteralPath $BuildRoot).Path
}

function Get-AganoxConfigPath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    return Join-Path $BuildRoot 'config/apps.json'
}

function Get-AganoxLocalConfigPath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    return Join-Path $BuildRoot 'config/local.json'
}

function Get-AganoxStatePath {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    return Join-Path $BuildRoot '.state/running.json'
}

function Merge-AganoxHashtable {
    [CmdletBinding()]
    param(
        [Parameter()]
        [hashtable] $Base,

        [Parameter()]
        [hashtable] $Override
    )

    $result = @{}
    foreach ($key in $Base.Keys) {
        $result[$key] = $Base[$key]
    }

    foreach ($key in $Override.Keys) {
        $baseValue = $result[$key]
        $overrideValue = $Override[$key]

        if ($null -ne $baseValue -and $baseValue -is [hashtable] -and $overrideValue -is [hashtable]) {
            $result[$key] = Merge-AganoxHashtable -Base $baseValue -Override $overrideValue
        }
        else {
            $result[$key] = $overrideValue
        }
    }

    return $result
}

function ConvertTo-AganoxHashtable {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline = $true)]
        $InputObject
    )

    process {
        if ($null -eq $InputObject) {
            return $null
        }

        if ($InputObject -is [System.Collections.IDictionary]) {
            $table = @{}
            foreach ($key in $InputObject.Keys) {
                $table[$key] = ConvertTo-AganoxHashtable -InputObject $InputObject[$key]
            }
            return $table
        }

        if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
            $items = @()
            foreach ($item in $InputObject) {
                $items += ConvertTo-AganoxHashtable -InputObject $item
            }
            return ,$items
        }

        return $InputObject
    }
}

function Merge-AganoxApplication {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable] $BaseApp,

        [Parameter(Mandatory)]
        [hashtable] $OverrideApp
    )

    return Merge-AganoxHashtable -Base $BaseApp -Override $OverrideApp
}

function Get-AganoxConfig {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $configPath = Get-AganoxConfigPath -BuildRoot $BuildRoot
    if (-not (Test-Path -LiteralPath $configPath)) {
        throw "Application config not found: $configPath"
    }

    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json | ConvertTo-AganoxHashtable
    $localPath = Get-AganoxLocalConfigPath -BuildRoot $BuildRoot

    if (Test-Path -LiteralPath $localPath) {
        $localConfig = Get-Content -LiteralPath $localPath -Raw | ConvertFrom-Json | ConvertTo-AganoxHashtable

        if ($localConfig.defaults) {
            $config.defaults = Merge-AganoxHashtable -Base $config.defaults -Override $localConfig.defaults
        }

        if ($localConfig.applications) {
            $overrideById = @{}
            foreach ($app in $localConfig.applications) {
                $overrideById[$app.id] = $app
            }

            $mergedApps = @()
            foreach ($app in $config.applications) {
                if ($overrideById.ContainsKey($app.id)) {
                    $mergedApps += Merge-AganoxApplication -BaseApp $app -OverrideApp $overrideById[$app.id]
                }
                else {
                    $mergedApps += $app
                }
            }

            $config.applications = $mergedApps
        }
    }

    return [pscustomobject]@{
        Defaults = $config.defaults
        Applications = @($config.applications | ForEach-Object {
            [pscustomobject]$_
        })
    }
}

function Get-AganoxApplications {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [switch] $IncludeDisabled,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $config = Get-AganoxConfig -BuildRoot $BuildRoot
    $apps = $config.Applications

    if (-not $IncludeDisabled) {
        $apps = @($apps | Where-Object { $_.enabled -ne $false })
    }

    if ($App) {
        $requested = @($App)
        $apps = @($apps | Where-Object { $requested -contains $_.id })

        $missing = @($requested | Where-Object { $_ -notin ($apps | ForEach-Object { $_.id }) })
        if ($missing.Count -gt 0) {
            throw "Unknown application id(s): $($missing -join ', ')"
        }
    }

    return ,$apps
}

function Get-AganoxApplicationsById {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $applications = Get-AganoxApplications -IncludeDisabled -BuildRoot $BuildRoot
    $byId = @{}
    foreach ($application in $applications) {
        $byId[$application.id] = $application
    }

    return $byId
}

function Get-AganoxDependencyIds {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $AppId,

        [Parameter(Mandatory)]
        [hashtable] $ApplicationsById
    )

    if (-not $ApplicationsById.ContainsKey($AppId)) {
        throw "Unknown application '$AppId'."
    }

    $collected = New-Object 'System.Collections.Generic.HashSet[string]'

    function Visit([string] $CurrentId) {
        $application = $ApplicationsById[$CurrentId]
        if (-not $application) {
            throw "Unknown application '$CurrentId'."
        }

        foreach ($dependencyId in @($application.dependsOn)) {
            if ([string]::IsNullOrWhiteSpace($dependencyId)) {
                continue
            }

            if ($collected.Add($dependencyId)) {
                Visit -CurrentId $dependencyId
            }
        }
    }

    Visit -CurrentId $AppId
    return @($collected | ForEach-Object { [string]$_ })
}

function Get-AganoxApplicationsWithDependencies {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $applicationsById = Get-AganoxApplicationsById -BuildRoot $BuildRoot
    $selected = if ($App) {
        @(Get-AganoxApplications -App $App -BuildRoot $BuildRoot)
    }
    else {
        @(Get-AganoxApplications -BuildRoot $BuildRoot)
    }

    $closureIds = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($application in $selected) {
        [void]$closureIds.Add([string]$application.id)
        $dependencyIds = @(Get-AganoxDependencyIds -AppId ([string]$application.id) -ApplicationsById $applicationsById)
        foreach ($dependencyId in $dependencyIds) {
            if ($null -eq $dependencyId) { continue }
            [void]$closureIds.Add([string]$dependencyId)
        }
    }

    $closureApps = @(
        $applicationsById.Values |
            Where-Object { $_.enabled -ne $false -and $closureIds.Contains($_.id) }
    )

    return @(Resolve-AganoxApplicationOrder -Applications $closureApps)
}

function Test-AganoxApplicationRunning {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $AppId,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $entry = @(Get-AganoxRunningState -BuildRoot $BuildRoot | Where-Object { $_.id -eq $AppId }) | Select-Object -First 1
    if (-not $entry) {
        return $false
    }

    return $null -ne (Get-Process -Id $entry.processId -ErrorAction SilentlyContinue)
}

function Resolve-AganoxApplicationOrder {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object[]] $Applications
    )

    $byId = @{}
    foreach ($app in $Applications) {
        $byId[$app.id] = $app
    }

    $visited = @{}
    $sorted = New-Object System.Collections.Generic.List[object]

    function Visit([string] $Id) {
        if ($visited.ContainsKey($Id)) {
            return
        }

        $visited[$Id] = $true
        $app = $byId[$Id]
        if (-not $app) {
            throw "Dependency '$Id' is not included in the selected applications."
        }

        foreach ($dependencyId in @($app.dependsOn)) {
            if ($dependencyId) {
                Visit -Id $dependencyId
            }
        }

        $sorted.Add($app) | Out-Null
    }

    foreach ($app in $Applications) {
        Visit -Id $app.id
    }

    return @($sorted.ToArray())
}

function Get-AganoxApplicationPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter()]
        [string] $RepoRoot = (Get-AganoxRepoRoot -BuildRoot $PSScriptRoot)
    )

    $path = Join-Path $RepoRoot $Application.path
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Application path not found for '$($Application.id)': $path"
    }

    return (Resolve-Path -LiteralPath $path).Path
}

function Invoke-AganoxHook {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter(Mandatory)]
        [ValidateSet('preBuild', 'postBuild', 'preRun', 'postRun')]
        [string] $HookName,

        [Parameter()]
        [string] $RepoRoot = (Get-AganoxRepoRoot -BuildRoot $PSScriptRoot),

        [Parameter()]
        [hashtable] $Context = @{}
    )

    if (-not $Application.hooks) {
        return
    }

    $hookPathRelative = $Application.hooks.$HookName
    if ([string]::IsNullOrWhiteSpace($hookPathRelative)) {
        return
    }

    $hookPath = Join-Path $RepoRoot $hookPathRelative
    if (-not (Test-Path -LiteralPath $hookPath)) {
        throw "Hook script not found for '$($Application.id).$HookName': $hookPath"
    }

    Write-Host "  Hook: $HookName -> $hookPathRelative" -ForegroundColor DarkGray
    & $hookPath @Context
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Hook '$HookName' failed for '$($Application.id)' with exit code $LASTEXITCODE"
    }
}

function Invoke-AganoxDotnetBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory)]
        [object] $Defaults,

        [Parameter()]
        [string] $Configuration
    )

    $configuration = if ($Configuration) {
        $Configuration
    }
    elseif ($Application.build.configuration) {
        $Application.build.configuration
    }
    else {
        $Defaults.dotnetConfiguration
    }

    $arguments = @('build', '--configuration', $configuration)
    if ($Application.build.project) {
        $arguments += $Application.build.project
    }

    Write-Host "  dotnet $($arguments -join ' ')" -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed for '$($Application.id)'"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-AganoxNpmInstall {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $WorkingDirectory
    )

    if (-not (Test-Path -LiteralPath (Join-Path $WorkingDirectory 'node_modules'))) {
        Write-Host '  npm install' -ForegroundColor DarkGray
        Push-Location $WorkingDirectory
        try {
            & npm install
            if ($LASTEXITCODE -ne 0) {
                throw 'npm install failed'
            }
        }
        finally {
            Pop-Location
        }
    }
}

function Invoke-AganoxNpmBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory)]
        [object] $Defaults
    )

    if ($Defaults.npmInstallOnBuild) {
        Invoke-AganoxNpmInstall -WorkingDirectory $WorkingDirectory
    }

    $scriptName = if ($Application.build.script) { $Application.build.script } else { 'build' }
    Write-Host "  npm run $scriptName" -ForegroundColor DarkGray
    Push-Location $WorkingDirectory
    try {
        & npm run $scriptName
        if ($LASTEXITCODE -ne 0) {
            throw "npm run $scriptName failed"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-AganoxScriptBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter(Mandatory)]
        [string] $WorkingDirectory,

        [Parameter()]
        [string] $RepoRoot = (Get-AganoxRepoRoot -BuildRoot $PSScriptRoot)
    )

    $scriptPath = if ($Application.build.script) {
        Join-Path $RepoRoot $Application.build.script
    }
    else {
        Join-Path $WorkingDirectory 'build.ps1'
    }

    if (-not (Test-Path -LiteralPath $scriptPath)) {
        throw "Build script not found for '$($Application.id)': $scriptPath"
    }

    Write-Host "  script $scriptPath" -ForegroundColor DarkGray
    & $scriptPath -WorkingDirectory $WorkingDirectory -Application $Application -Mode 'build'
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Build script failed for '$($Application.id)'"
    }
}

function Invoke-AganoxApplicationBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter()]
        [object] $Defaults = (Get-AganoxConfig -BuildRoot $PSScriptRoot).Defaults,

        [Parameter()]
        [string] $Configuration,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $repoRoot = Get-AganoxRepoRoot -BuildRoot $BuildRoot
    $workingDirectory = Get-AganoxApplicationPath -Application $Application -RepoRoot $repoRoot

    Write-Host "Building $($Application.name) ($($Application.id))" -ForegroundColor Cyan
    Write-Host "  Path: $workingDirectory" -ForegroundColor DarkGray

    Invoke-AganoxHook -Application $Application -HookName 'preBuild' -RepoRoot $repoRoot -Context @{
        WorkingDirectory = $workingDirectory
        Application = $Application
    }

    switch ($Application.type) {
        'dotnet' {
            Invoke-AganoxDotnetBuild -Application $Application -WorkingDirectory $workingDirectory -Defaults $Defaults -Configuration $Configuration
        }
        'npm' {
            Invoke-AganoxNpmBuild -Application $Application -WorkingDirectory $workingDirectory -Defaults $Defaults
        }
        'script' {
            Invoke-AganoxScriptBuild -Application $Application -WorkingDirectory $workingDirectory -RepoRoot $repoRoot
        }
        default {
            throw "Unsupported application type '$($Application.type)' for '$($Application.id)'"
        }
    }

    Invoke-AganoxHook -Application $Application -HookName 'postBuild' -RepoRoot $repoRoot -Context @{
        WorkingDirectory = $workingDirectory
        Application = $Application
    }

    Write-Host "Built $($Application.name)" -ForegroundColor Green
}

function Resolve-AganoxProcessStartInfo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable] $Command
    )

    $filePath = $Command.FilePath
    $argumentList = @($Command.ArgumentList)

    switch ($filePath) {
        'npm' {
            $npmCmd = Get-Command npm.cmd -ErrorAction SilentlyContinue
            if ($npmCmd) {
                $filePath = $npmCmd.Source
            }
            else {
                $filePath = $env:ComSpec
                $argumentList = @('/c', 'npm') + $argumentList
            }
        }
        'dotnet' {
            $dotnetCmd = Get-Command dotnet.exe -ErrorAction SilentlyContinue
            if (-not $dotnetCmd) {
                $dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
            }

            if ($dotnetCmd) {
                $filePath = $dotnetCmd.Source
            }
        }
        default {
            if ($filePath -notmatch '\.(exe|cmd|bat|com)$') {
                $resolved = Get-Command $filePath -ErrorAction SilentlyContinue
                if ($resolved -and $resolved.Source -match '\.(exe|cmd|bat|com)$') {
                    $filePath = $resolved.Source
                }
            }
        }
    }

    return @{
        FilePath = $filePath
        ArgumentList = $argumentList
    }
}

function Get-AganoxRunCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter(Mandatory)]
        [object] $Defaults
    )

    switch ($Application.type) {
        'dotnet' {
            $launchProfile = if ($Application.run.launchProfile) {
                $Application.run.launchProfile
            }
            else {
                $Defaults.dotnetLaunchProfile
            }

            return @{
                FilePath = 'dotnet'
                ArgumentList = @('run', '--launch-profile', $launchProfile)
            }
        }
        'npm' {
            $scriptName = if ($Application.run.script) { $Application.run.script } else { 'start' }
            return @{
                FilePath = 'npm'
                ArgumentList = @('run', $scriptName)
            }
        }
        'script' {
            $scriptPath = if ($Application.run.script) {
                if ([System.IO.Path]::IsPathRooted($Application.run.script)) {
                    $Application.run.script
                }
                else {
                    Join-Path (Get-AganoxRepoRoot -BuildRoot $PSScriptRoot) $Application.run.script
                }
            }
            else {
                Join-Path $Application.path 'run.ps1'
            }

            $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { 'pwsh' } else { 'powershell' }

            return @{
                FilePath = $shell
                ArgumentList = @('-NoProfile', '-File', $scriptPath, '-Mode', 'run')
            }
        }
        default {
            throw "Unsupported application type '$($Application.type)' for '$($Application.id)'"
        }
    }
}

function Get-AganoxRunningState {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $statePath = Get-AganoxStatePath -BuildRoot $BuildRoot
    if (-not (Test-Path -LiteralPath $statePath)) {
        return @()
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    return @($state)
}

function Save-AganoxRunningState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object[]] $Entries,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $statePath = Get-AganoxStatePath -BuildRoot $BuildRoot
    $stateDirectory = Split-Path -Parent $statePath
    if (-not (Test-Path -LiteralPath $stateDirectory)) {
        New-Item -ItemType Directory -Path $stateDirectory | Out-Null
    }

    $Entries | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8
}

function Clear-AganoxRunningState {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $statePath = Get-AganoxStatePath -BuildRoot $BuildRoot
    if (Test-Path -LiteralPath $statePath) {
        Remove-Item -LiteralPath $statePath -Force
    }
}

function Start-AganoxApplication {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object] $Application,

        [Parameter()]
        [object] $Defaults = (Get-AganoxConfig -BuildRoot $PSScriptRoot).Defaults,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $repoRoot = Get-AganoxRepoRoot -BuildRoot $BuildRoot
    $workingDirectory = Get-AganoxApplicationPath -Application $Application -RepoRoot $repoRoot

    Write-Host "Starting $($Application.name) ($($Application.id))" -ForegroundColor Cyan
    Write-Host "  Path: $workingDirectory" -ForegroundColor DarkGray

    Invoke-AganoxHook -Application $Application -HookName 'preRun' -RepoRoot $repoRoot -Context @{
        WorkingDirectory = $workingDirectory
        Application = $Application
    }

    $command = Get-AganoxRunCommand -Application $Application -Defaults $Defaults
    $startInfo = Resolve-AganoxProcessStartInfo -Command $command
    $argumentString = ($startInfo.ArgumentList | ForEach-Object {
        if ($_ -match '\s') { "`"$_`"" } else { $_ }
    }) -join ' '

    Write-Host "  $($startInfo.FilePath) $argumentString" -ForegroundColor DarkGray

    $envBlock = @{}
    if ($Application.env) {
        foreach ($key in $Application.env.Keys) {
            $envBlock[$key] = $Application.env[$key]
        }
    }

    $startParams = @{
        FilePath = $startInfo.FilePath
        ArgumentList = $startInfo.ArgumentList
        WorkingDirectory = $workingDirectory
        WindowStyle = 'Minimized'
        PassThru = $true
    }

    if ($envBlock.Count -gt 0) {
        $startParams['Environment'] = $envBlock
    }

    $process = Start-Process @startParams
    Start-Sleep -Seconds 1

    if ($process.HasExited) {
        throw "Application '$($Application.id)' exited immediately with code $($process.ExitCode)"
    }

    if ($Application.urls) {
        foreach ($url in $Application.urls) {
            Write-Host "  URL: $url" -ForegroundColor DarkCyan
        }
    }

    Invoke-AganoxHook -Application $Application -HookName 'postRun' -RepoRoot $repoRoot -Context @{
        WorkingDirectory = $workingDirectory
        Application = $Application
        ProcessId = $process.Id
    }

    return [pscustomobject]@{
        id = $Application.id
        name = $Application.name
        processId = $process.Id
        startedAt = (Get-Date).ToString('o')
        workingDirectory = $workingDirectory
        command = "$($startInfo.FilePath) $argumentString"
        urls = @($Application.urls)
    }
}

function Stop-AganoxApplications {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $running = Get-AganoxRunningState -BuildRoot $BuildRoot
    if ($running.Count -eq 0) {
        Write-Host 'No tracked Aganox applications are running.' -ForegroundColor Yellow
        return
    }

    if ($App) {
        $running = @($running | Where-Object { $App -contains $_.id })
    }

    $remaining = @()
    foreach ($entry in (Get-AganoxRunningState -BuildRoot $BuildRoot)) {
        if ($App -and ($App -notcontains $entry.id)) {
            $remaining += $entry
            continue
        }

        $process = Get-Process -Id $entry.processId -ErrorAction SilentlyContinue
        if ($process) {
            Write-Host "Stopping $($entry.name) (PID $($entry.processId))" -ForegroundColor Cyan
            Stop-Process -Id $entry.processId -Force -ErrorAction SilentlyContinue
        }
        else {
            Write-Host "$($entry.name) is not running (PID $($entry.processId))" -ForegroundColor DarkGray
        }
    }

    if ($App) {
        Save-AganoxRunningState -Entries $remaining -BuildRoot $BuildRoot
    }
    else {
        Clear-AganoxRunningState -BuildRoot $BuildRoot
    }

    Write-Host 'Stop complete.' -ForegroundColor Green
}

function Invoke-AganoxBuild {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [string] $Configuration,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $config = Get-AganoxConfig -BuildRoot $BuildRoot
    $applications = if ($App) {
        @(Get-AganoxApplications -App $App -BuildRoot $BuildRoot)
    }
    else {
        @(Get-AganoxApplications -BuildRoot $BuildRoot)
    }

    foreach ($application in $applications) {
        Invoke-AganoxApplicationBuild -Application $application -Defaults $config.Defaults -Configuration $Configuration -BuildRoot $BuildRoot
    }
}

function Invoke-AganoxRun {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [switch] $SkipBuild,

        [Parameter()]
        [string] $Configuration,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $config = Get-AganoxConfig -BuildRoot $BuildRoot
    $applications = Get-AganoxApplicationsWithDependencies -App $App -BuildRoot $BuildRoot

    if (-not $SkipBuild) {
        Invoke-AganoxBuild -App $App -Configuration $Configuration -BuildRoot $BuildRoot
    }

    $existing = @()
    $statePath = Get-AganoxStatePath -BuildRoot $BuildRoot
    if (Test-Path -LiteralPath $statePath) {
        $existing = Get-AganoxRunningState -BuildRoot $BuildRoot
    }

    $started = @()
    $startupDelay = [int]$config.Defaults.startupDelaySeconds

    foreach ($application in $applications) {
        $entry = Start-AganoxApplication -Application $application -Defaults $config.Defaults -BuildRoot $BuildRoot
        $started += $entry

        if ($startupDelay -gt 0 -and $application -ne $applications[-1]) {
            Write-Host "  Waiting ${startupDelay}s for dependencies..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $startupDelay
        }
    }

    $merged = @($existing + $started)
    Save-AganoxRunningState -Entries $merged -BuildRoot $BuildRoot

    Write-Host ''
    Write-Host 'Applications started:' -ForegroundColor Green
    foreach ($entry in $started) {
        Write-Host "  - $($entry.name) (PID $($entry.processId))"
        foreach ($url in $entry.urls) {
            Write-Host "      $url" -ForegroundColor DarkCyan
        }
    }

    Write-Host ''
    Write-Host 'Use ./build/stop.ps1 to stop tracked processes.' -ForegroundColor DarkGray
}

function Invoke-AganoxRestart {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]] $App,

        [Parameter()]
        [switch] $SkipBuild,

        [Parameter()]
        [string] $Configuration,

        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $config = Get-AganoxConfig -BuildRoot $BuildRoot
    $restartApps = if ($App) {
        @(Get-AganoxApplications -App $App -BuildRoot $BuildRoot)
    }
    else {
        @(Get-AganoxApplications -BuildRoot $BuildRoot)
    }

    $restartIds = @($restartApps | ForEach-Object { $_.id })
    if ($restartIds.Count -eq 0) {
        Write-Host 'No applications selected to restart.' -ForegroundColor Yellow
        return
    }

    Write-Host "Restarting: $($restartIds -join ', ')" -ForegroundColor Cyan

    if ($App) {
        Stop-AganoxApplications -App $restartIds -BuildRoot $BuildRoot
    }
    else {
        Stop-AganoxApplications -BuildRoot $BuildRoot
    }

    if (-not $SkipBuild) {
        foreach ($application in $restartApps) {
            Invoke-AganoxApplicationBuild -Application $application -Defaults $config.Defaults -Configuration $Configuration -BuildRoot $BuildRoot
        }
    }

    Write-Verbose 'Resolving start order with dependencies...'
    $orderedStart = @(Get-AganoxApplicationsWithDependencies -App $restartIds -BuildRoot $BuildRoot)

    $state = @(Get-AganoxRunningState -BuildRoot $BuildRoot)
    $started = @()
    $startupDelay = [int]$config.Defaults.startupDelaySeconds

    foreach ($application in $orderedStart) {
        $mustStart = $restartIds -contains $application.id
        $alreadyRunning = Test-AganoxApplicationRunning -AppId ([string]$application.id) -BuildRoot $BuildRoot

        if (-not $mustStart -and $alreadyRunning) {
            Write-Host "Dependency $($application.name) already running - skipping start" -ForegroundColor DarkGray
            continue
        }

        $entry = Start-AganoxApplication -Application $application -Defaults $config.Defaults -BuildRoot $BuildRoot
        $started += $entry
        $state = @($state | Where-Object { $_.id -ne $entry.id }) + $entry

        if ($startupDelay -gt 0 -and $application -ne $orderedStart[-1]) {
            Write-Host "  Waiting ${startupDelay}s for dependencies..." -ForegroundColor DarkGray
            Start-Sleep -Seconds $startupDelay
        }
    }

    Save-AganoxRunningState -Entries $state -BuildRoot $BuildRoot

    Write-Host ''
    if ($started.Count -gt 0) {
        Write-Host 'Applications restarted/started:' -ForegroundColor Green
        foreach ($entry in $started) {
            Write-Host "  - $($entry.name) (PID $($entry.processId))"
            foreach ($url in $entry.urls) {
                Write-Host "      $url" -ForegroundColor DarkCyan
            }
        }
    }
    else {
        Write-Host 'No applications were started.' -ForegroundColor Yellow
    }
}

function Get-AganoxApplicationList {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string] $BuildRoot = $PSScriptRoot
    )

    $config = Get-AganoxConfig -BuildRoot $BuildRoot
    foreach ($application in $config.Applications) {
        $status = if ($application.enabled -eq $false) { 'disabled' } else { 'enabled' }
        [pscustomobject]@{
            Id = $application.id
            Name = $application.name
            Type = $application.type
            Status = $status
            DependsOn = ($application.dependsOn -join ', ')
            Urls = ($application.urls -join ', ')
        }
    }
}

Export-ModuleMember -Function @(
    'Get-AganoxRepoRoot',
    'Get-AganoxBuildRoot',
    'Get-AganoxConfig',
    'Get-AganoxApplications',
    'Resolve-AganoxApplicationOrder',
    'Invoke-AganoxBuild',
    'Invoke-AganoxRun',
    'Invoke-AganoxRestart',
    'Stop-AganoxApplications',
    'Get-AganoxApplicationList',
    'Get-AganoxRunningState'
)
