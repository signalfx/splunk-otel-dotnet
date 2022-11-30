#Requires -RunAsAdministrator

$ServiceLocatorVariable = "SPLUNK_OTEL_DOTNET_AUTO_INSTALL_DIR"

function Get-Current-InstallDir() {
    return [System.Environment]::GetEnvironmentVariable($ServiceLocatorVariable, [System.EnvironmentVariableTarget]::Machine)
}

function Get-CLIInstallDir-From-InstallDir([string]$InstallDir) {
    $dir = "OpenTelemetry .NET AutoInstrumentation"
    
    if ($InstallDir -eq "<auto>") {
        return (Join-Path $Env:ProgramFiles $dir)
    }
    elseif (Test-Path $InstallDir -IsValid) {
        return $InstallDir
    }

    throw "Invalid install directory provided '$InstallDir'"
}

function Get-Temp-Directory() {
    $temp = $env:TEMP

    if (-not (Test-Path $temp)) {
        New-Item -ItemType Directory -Force -Path $temp | Out-Null
    }

    return $temp
}

function Prepare-Install-Directory([string]$InstallDir) {
    if (Test-Path $InstallDir) {
        # Cleanup old directory
        Remove-Item -LiteralPath $InstallDir -Force -Recurse
    }

    New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
}

function Reset-IIS() {    
    Start-Process "iisreset.exe" -NoNewWindow -Wait
}

function Download-OpenTelemetry([string]$Version, [string]$Path) {
    $archive = "splunk-opentelemetry-dotnet-windows.zip"
    $dlUrl = "https://github.com/signalfx/splunk-otel-dotnet/releases/download/$Version/$archive"
    $dlPath = Join-Path $Path $archive

    Invoke-WebRequest -Uri $dlUrl -OutFile $dlPath

    return $dlPath
}

function Get-Environment-Variables-Table([string]$InstallDir, [string]$OTelServiceName) {
    $COR_PROFILER_PATH_32 = Join-Path $InstallDir "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"
    $COR_PROFILER_PATH_64 = Join-Path $InstallDir "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"
    $CORECLR_PROFILER_PATH_32 = Join-Path $InstallDir "/win-x86/OpenTelemetry.AutoInstrumentation.Native.dll"
    $CORECLR_PROFILER_PATH_64 = Join-Path $InstallDir "/win-x64/OpenTelemetry.AutoInstrumentation.Native.dll"

    $DOTNET_ADDITIONAL_DEPS = Join-Path $InstallDir "AdditionalDeps"
    $DOTNET_SHARED_STORE = Join-Path $InstallDir "store"
    $DOTNET_STARTUP_HOOKS = Join-Path $InstallDir "net/OpenTelemetry.AutoInstrumentation.StartupHook.dll"

    $OTEL_DOTNET_AUTO_HOME = $InstallDir
    $OTEL_DOTNET_AUTO_INTEGRATIONS_FILE = Join-Path $InstallDir "integrations.json"
    
    $vars = @{
        # .NET Framework
        "COR_ENABLE_PROFILING"                = "1";
        "COR_PROFILER"                        = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
        "COR_PROFILER_PATH_32"                = $COR_PROFILER_PATH_32;
        "COR_PROFILER_PATH_64"                = $COR_PROFILER_PATH_64;
        # .NET Core
        "CORECLR_ENABLE_PROFILING"            = "1";
        "CORECLR_PROFILER"                    = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
        "CORECLR_PROFILER_PATH_32"            = $CORECLR_PROFILER_PATH_32;
        "CORECLR_PROFILER_PATH_64"            = $CORECLR_PROFILER_PATH_64;
        # ASP.NET Core
        "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES" = "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper";
        # .NET Common
        "DOTNET_ADDITIONAL_DEPS"              = $DOTNET_ADDITIONAL_DEPS;
        "DOTNET_SHARED_STORE"                 = $DOTNET_SHARED_STORE;
        "DOTNET_STARTUP_HOOKS"                = $DOTNET_STARTUP_HOOKS;
        # OpenTelemetry
        "OTEL_DOTNET_AUTO_HOME"               = $OTEL_DOTNET_AUTO_HOME;
        "OTEL_DOTNET_AUTO_INTEGRATIONS_FILE"  = $OTEL_DOTNET_AUTO_INTEGRATIONS_FILE;
        "OTEL_DOTNET_AUTO_PLUGINS"            = "Splunk.OpenTelemetry.AutoInstrumentation.Plugin, Splunk.OpenTelemetry.AutoInstrumentation.Plugin";
    }

    if (-not [string]::IsNullOrWhiteSpace($OTelServiceName)) {
        $vars.Add("OTEL_SERVICE_NAME", $OTelServiceName)
    }

    return $vars
}

function Setup-Windows-Service([string]$InstallDir, [string]$WindowsServiceName, [string]$OTelServiceName) {  
    $varsTable = Get-Environment-Variables-Table -InstallDir $InstallDir -OTelServiceName $OTelServiceName
    [string []] $varsList = ($varsTable.Keys | foreach-object { "$_=$($varsTable[$_])" }) # [string []] definition is required for WS2016
    $regPath = "HKLM:SYSTEM\CurrentControlSet\Services\"
    $regKey = Join-Path $regPath $WindowsServiceName
   
    if (Test-Path $regKey) {
        Set-ItemProperty $regKey -Name Environment -Value $varsList
    }
    else {
        throw "Invalid service '$WindowsServiceName'. Service does not exist."
    }
}

function Remove-Windows-Service([string]$WindowsServiceName) {
    [string[]] $filters = @(
        # .NET Framework
        "COR_ENABLE_PROFILING",
        "COR_PROFILER",
        "COR_PROFILER_PATH_32",
        "COR_PROFILER_PATH_64",
        # .NET Core
        "CORECLR_ENABLE_PROFILING",
        "CORECLR_PROFILER",
        "CORECLR_PROFILER_PATH_32",
        "CORECLR_PROFILER_PATH_64",
        # ASP.NET Core
        "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES",
        # .NET Common
        "DOTNET_ADDITIONAL_DEPS",
        "DOTNET_SHARED_STORE",
        "DOTNET_STARTUP_HOOKS",
        # OpenTelemetry
        "OTEL_DOTNET_"
    )

    $regPath = "HKLM:SYSTEM\CurrentControlSet\Services\"
    $regKey = Join-Path $regPath $WindowsServiceName
   
    if (Test-Path $regKey) {
        $values = Get-ItemPropertyValue $regKey -Name Environment
        $vars = Filter-Env-List -EnvValues $values -Filters $filters
        
        Set-ItemProperty $regKey -Name Environment -Value $vars
    }
    else {
        throw "Invalid service '$WindowsServiceName'. Service does not exist."
    }

    $remaining = Get-ItemPropertyValue $regKey -Name Environment
    if (-not $remaining) {
        Remove-ItemProperty $regKey -Name Environment
    }
}

function Filter-Env-List([string[]]$EnvValues, [string[]]$Filters) {
    $remaining = @()

    foreach ($value in $EnvValues) {
        $match = $false

        foreach ($filter in $Filters) {
            if ($value -clike "$($filter)*") {
                $match = $true
                break
            }
        }

        if (-not $match) {
            $remaining += $value
        }
    }

    return $remaining
}

<#
    .SYNOPSIS
    Installs Splunk Distribution of OpenTelemetry .NET.
    .PARAMETER InstallDir
    Default: <auto> - the default value is AppData
    Install path of the Splunk Distribution of OpenTelemetry .NET
    Possible values: <auto>, ProgramFiles, AppData, (Custom path)
#>
function Install-OpenTelemetryCore() {
    param(
        [string]$InstallDir = "<auto>"
    )

    $version = "v0.0.1-alpha.2"
    $installDir = Get-CLIInstallDir-From-InstallDir $InstallDir
    $tempDir = Get-Temp-Directory
    $dlPath = $null

    try {
        $dlPath = Download-OpenTelemetry $version $tempDir
        Prepare-Install-Directory $installDir

        # Extract files from zip
        Expand-Archive $dlPath $installDir -Force

        # OpenTelemetry DotNet service locator
        [System.Environment]::SetEnvironmentVariable($ServiceLocatorVariable, $installDir, [System.EnvironmentVariableTarget]::Machine)
    } 
    catch {
        $message = $_
        Write-Error "Could not setup Splunk Distribution of OpenTelemetry .NET. $message"
    } 
    finally {
        if ($dlPath) {
            # Cleanup
            Remove-Item $dlPath
        }
    }
}

<#
    .SYNOPSIS
    Uninstalls Splunk Distribution of OpenTelemetry .NET.
#>
function Uninstall-OpenTelemetryCore() {
    $installDir = Get-Current-InstallDir

    if (-not $installDir) {
        throw "OpenTelemetry Core is already removed."
    }

    Remove-Item -LiteralPath $installDir -Force -Recurse

    # Remove OTel service locator variable
    [System.Environment]::SetEnvironmentVariable($ServiceLocatorVariable, $null, [System.EnvironmentVariableTarget]::Machine)
}

<#
    .SYNOPSIS
    Setups environment variables to enable automatic instrumentation started from the current PowerShell session.
    .PARAMETER OTelServiceName
    Specifies OpenTelemetry service name to identify your service.
#>
function Register-OpenTelemetryForCurrentSession() {
    param(
        [Parameter(Mandatory = $true)]
        [string]$OTelServiceName
    )

    $installDir = Get-Current-InstallDir

    if (-not $installDir) {
        throw "OpenTelemetry Core must be setup first. Run 'Install-OpenTelemetryCore' to setup OpenTelemetry Core."
    }

    $varsTable = Get-Environment-Variables-Table -InstallDir $installDir -OTelServiceName $OTelServiceName

    foreach ($var in $varsTable.Keys) {
        Set-Item "env:$var" $varsTable[$var]
    }
}

<#
    .SYNOPSIS
    Setups IIS environment variables to enable automatic instrumentation.
    Performs IIS reset after registration.
#>
function Register-OpenTelemetryForIIS() {
    $installDir = Get-Current-InstallDir

    if (-not $installDir) {
        throw "OpenTelemetry Core must be setup first. Run 'Install-OpenTelemetryCore' to setup OpenTelemetry Core."
    }

    Setup-Windows-Service -InstallDir $installDir -WindowsServiceName "W3SVC"
    Setup-Windows-Service -InstallDir $installDir -WindowsServiceName "WAS"

    Reset-IIS
}

<#
    .SYNOPSIS
    Setups specific Windows service environment variables to enable automatic instrumentation.
    Performs service restart after registration.
    .PARAMETER WindowsServiceName
    Actual Windows service name in registry.
    .PARAMETER OTelServiceName
    Specifies OpenTelemetry service name to identify your service.
#>
function Register-OpenTelemetryForWindowsService() {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WindowsServiceName,
        [Parameter(Mandatory = $true)]
        [string]$OTelServiceName
    )

    $installDir = Get-Current-InstallDir

    if (-not $installDir) {
        throw "OpenTelemetry Core must be setup first. Run 'Install-OpenTelemetryCore' to setup OpenTelemetry Core."
    }

    Setup-Windows-Service -InstallDir $installDir -WindowsServiceName $WindowsServiceName -OTelServiceName $OTelServiceName
    Restart-Service -Name $WindowsServiceName -Force
}

<#
    .SYNOPSIS
    Removes environment variables to disable automatic instrumentation started from the current PowerShell session.
#>
function Unregister-OpenTelemetryForCurrentSession() {
    # .NET Framework
    $env:COR_ENABLE_PROFILING = $null
    $env:COR_PROFILER = $null
    $env:COR_PROFILER_PATH_32 = $null
    $env:COR_PROFILER_PATH_64 = $null

    # .NET Core
    $env:CORECLR_ENABLE_PROFILING = $null
    $env:CORECLR_PROFILER = $null
    $env:CORECLR_PROFILER_PATH_32 = $null
    $env:CORECLR_PROFILER_PATH_64 = $null

    # ASP.NET Core
    $env:ASPNETCORE_HOSTINGSTARTUPASSEMBLIES = $null

    # .NET Common
    $env:DOTNET_ADDITIONAL_DEPS = $null
    $env:DOTNET_SHARED_STORE = $null
    $env:DOTNET_STARTUP_HOOKS = $null

    # OpenTelemetry
    Get-ChildItem env: | Where-Object { $_.Name -like "OTEL_DOTNET_*" } | ForEach-Object { Set-Item "env:$($_.Name)" $null }

    # Splunk
    Get-ChildItem env: | Where-Object { $_.Name -like "SPLUNK_*" } | ForEach-Object { Set-Item "env:$($_.Name)" $null }
}

<#
    .SYNOPSIS
    Removes IIS environment variables to disable automatic instrumentation.
    Performs IIS reset after removal.
#>
function Unregister-OpenTelemetryForIIS() {
    Remove-Windows-Service -WindowsServiceName "W3SVC"
    Remove-Windows-Service -WindowsServiceName "WAS"
    Reset-IIS
}

<#
    .SYNOPSIS
    Removes specific Windows service environment variables to disable automatic instrumentation.
    Performs service restart after removal.
    .PARAMETER WindowsServiceName
    Actual Windows service Name in registry.
#>
function Unregister-OpenTelemetryForWindowsService() {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WindowsServiceName
    )  

    Remove-Windows-Service -WindowsServiceName $WindowsServiceName
    Restart-Service -Name $WindowsServiceName -Force
}

<#
    .SYNOPSIS
    Locates Splunk Distribution of OpenTelemetry .NET's install path. 
#>
function Get-OpenTelemetryInstallDirectory() {
    $installDir = Get-Current-InstallDir

    if ($installDir) {
        return $installDir
    }

    Write-Warning "Splunk Distribution of OpenTelemetry .NET is not installed."
}

Export-ModuleMember -Function Install-OpenTelemetryCore
Export-ModuleMember -Function Register-OpenTelemetryForIIS
Export-ModuleMember -Function Register-OpenTelemetryForWindowsService
Export-ModuleMember -Function Register-OpenTelemetryForCurrentSession
Export-ModuleMember -Function Uninstall-OpenTelemetryCore
Export-ModuleMember -Function Unregister-OpenTelemetryForIIS
Export-ModuleMember -Function Unregister-OpenTelemetryForWindowsService
Export-ModuleMember -Function Unregister-OpenTelemetryForCurrentSession
Export-ModuleMember -Function Get-OpenTelemetryInstallDirectory
