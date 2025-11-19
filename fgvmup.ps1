<#
.SYNOPSIS
    fgvm Updater - Installation management tool for fgvm.

.DESCRIPTION
    Manages the installation and removal of fgvm.

.LINK
    https://github.com/patricktcoakley/fgvm

.PARAMETER Command
    The operation to perform: install, uninstall, or upgrade fgvm.

.PARAMETER Version
    The specific version to install (defaults to latest).

.PARAMETER Quiet
    Suppress all non-error output.

.PARAMETER Force
    Force a clean installation, removing existing fgvm files.

.EXAMPLE
    .\fgvmup.ps1 install
    # Installs the latest version of fgvm

.EXAMPLE
    .\fgvmup.ps1 install --version 0.1.6
    # Installs version 0.1.6 of fgvm

.EXAMPLE
    .\fgvmup.ps1 uninstall
    # Completely removes fgvm from the system, including all Godot installations

.NOTES
    Platform: Currently only supports Windows x86_64.
#>

#region Script Configuration
# Error handling behavior
$ErrorActionPreference = "Stop" # Immediately halt on errors

# Repository and versioning
$script:repo = "patricktcoakley/fgvm"
$script:version = "latest"

# Downloads
$script:maxRetries = 3

# Installation paths
$script:installPath = "$env:LOCALAPPDATA\fgvm"       # Main installation directory
$script:fgvmPath = "$env:USERPROFILE\fgvm"           # Godot versions directory

# Temporary file paths
$script:zipPath = "$env:TEMP\fgvm.zip"               # Downloaded archive
$script:checksumPath = "$env:TEMP\fgvm.sha256"       # Downloaded checksum
$script:updaterScript = "$env:TEMP\fgvmup_updater.ps1" # Self-updater script

# Cache management
$script:cacheDir = "$script:installPath\cache"       # Cache directory
$script:apiCacheFile = "$script:cacheDir\releases.json" # GitHub API response cache
$script:cacheExpiry = 3600                           # Cache lifetime in seconds (1 hour)

# Command line parameters
$script:command = ""                                 # Operation to perform
$script:quiet = $false                               # Suppress non-error output
$script:force = $false                               # Force clean installation

# Security configuration
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
#endregion
function Initialize-Environment {
    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw "Error: PowerShell 5 or later is required to use fgvmup."
    }

    # Check execution policy
    $allowedPolicy = @('Unrestricted', 'RemoteSigned', 'Bypass')
    if ((Get-ExecutionPolicy).ToString() -notin $allowedPolicy) {
        throw "Error: PowerShell requires execution policy in [$($allowedPolicy -join ", ")] to run fgvmup."
    }
}

function Get-Arch {
    try {
        $a = [System.Reflection.Assembly]::LoadWithPartialName("System.Runtime.InteropServices.RuntimeInformation")
        $t = $a.GetType("System.Runtime.InteropServices.RuntimeInformation")
        $p = $t.GetProperty("OSArchitecture")
        
        switch ($p.GetValue($null).ToString()) {
            "X64" { return "win-x64" }
            # TODO: support ARM Windows? "Arm64" { return "win-arm64" }
            default { throw "Unsupported architecture" }
        }
    } 
    catch {
        # Fallback for older PowerShell
        if ([System.Environment]::Is64BitOperatingSystem) {
            return "win-x64"
        }
        else {
            throw "Unsupported architecture: 32-bit Windows is not supported."
        }
    }
}

function New-TempDir {
    $parent = [System.IO.Path]::GetTempPath()
    $name = [System.Guid]::NewGuid().ToString()
    $tempDir = Join-Path $parent $name
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    return $tempDir
}

function Show-Usage {
    Write-Output "Usage: fgvmup COMMAND [OPTIONS]"
    Write-Output "Commands: install [--quiet] [--version VERSION] [--force], uninstall, upgrade"
    Write-Output "Options:"
    Write-Output "  --version VERSION    Specify version to install"
    Write-Output "  --quiet              Suppress output"
    Write-Output "  --force              Force clean installation" 
    exit 1
}

function Test-WindowsPlatform {
    # For PowerShell Core (6.0+)
    if (Test-Path Variable:IsWindows) {
        if (-not $IsWindows) {
            Write-LogMessage "Error: This script only supports Windows."
            Write-LogMessage "See: https://github.com/patricktcoakley/fgvm"
            exit 1
        }
    }
    # For Windows PowerShell (5.1 and below)
    else {
        if (-not $env:OS -or $env:OS -ne "Windows_NT") {
            Write-LogMessage "Error: This script only supports Windows."
            Write-LogMessage "See: https://github.com/patricktcoakley/fgvm"
            exit 1
        }
    }
}


function Read-Arguments {
    param (
        [string[]]$Arguments
    )

    if ($Arguments.Count -eq 0) {
        Show-Usage
    }

    $script:command = $Arguments[0].ToLower()
    
    for ($i = 1; $i -lt $Arguments.Count; $i++) {
        if ($Arguments[$i] -eq "--version" -and $i + 1 -lt $Arguments.Count) {
            $script:version = $Arguments[$i + 1]
            $i++
        }
        elseif ($Arguments[$i] -eq "--quiet") {
            $script:quiet = $true
        }
        elseif ($Arguments[$i] -eq "--force") {
            $script:force = $true
        }
    }
}

function Write-LogMessage {
    param([string]$Message)
    if (-not $script:quiet) { Write-Output $Message }
}

function Test-Dependencies {
    try { 
        $null = [System.IO.Compression.ZipFile] 
    }
    catch {
        try {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
        }
        catch {
            throw "Required dependency 'System.IO.Compression.FileSystem' not available."
        }
    }
}

function Install-Script {
    # Get path to Self
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not $scriptPath) {

        # Fallback if path null
        $scriptPath = $PSCommandPath
        if (-not $scriptPath) {
            throw "Error: Could not determine script path for installation."
        }
    }
    
    # Verify script file exists
    if (-not (Test-Path -Path $scriptPath)) {
        throw "Error: Script file not found at path: $scriptPath."
    }
    
    # Target path
    $targetPath = "$script:installPath\fgvmup.ps1"
    
    # Create a batch to wrap fgvmup.ps1
    $batchPath = "$script:installPath\fgvmup.cmd"
    $batchContent = @"
@echo off
powershell.exe -ExecutionPolicy Bypass -NoLogo -NoProfile -Command "& '%LOCALAPPDATA%\fgvm\fgvmup.ps1' %*"
"@
    Set-Content -Path $batchPath -Value $batchContent
    
    # Only copy the script if it doesn't exist yet or we're in force mode
    if (-not (Test-Path -Path $targetPath) -or $script:force) {
        # Don't try to copy if running from the target path
        if ($scriptPath -ne $targetPath) {
            Copy-Item -Path $scriptPath -Destination $targetPath -Force
            Write-LogMessage "Installed fgvmup."
        }
    }
}

function Get-InstalledVersion {
    try {
        $output = & "$script:installPath\fgvm.exe" --version 2>$null
        return $output.Trim()
    }
    catch {
        return ""
    }
}

function Get-LatestRelease {
    if (-not (Test-Path $script:cacheDir)) {
        New-Item -ItemType Directory -Path $script:cacheDir -Force | Out-Null
    }
    
    if (Test-Path $script:apiCacheFile) {
        $cacheAge = (Get-Date) - (Get-Item $script:apiCacheFile).LastWriteTime
        if ($cacheAge.TotalSeconds -lt $script:cacheExpiry) {
            try {
                $cached = Get-Content $script:apiCacheFile -Raw | ConvertFrom-Json
                if ($cached.tag_name) {
                    return $cached.tag_name
                }
            }
            catch {
                Write-LogMessage "Warning: Couldn't read cached release data, fetching fresh data."
            }
        }
    }
    
    # Have basic retry logic for failed attempts
    $attempt = 0
    while ($attempt -lt $script:maxRetries) {
        try {
            $url = "https://api.github.com/repos/$script:repo/releases/latest"
            $response = Invoke-RestMethod -Uri $url -UseBasicParsing
            
            # Cache the response to help mitigate API rate limiting
            $response | ConvertTo-Json -Depth 10 | Set-Content -Path $script:apiCacheFile
            
            return $response.tag_name
        }
        catch {
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode -eq 403) {
                throw "GitHub API rate limit exceeded. Please try again later."
            }
            
            $attempt++
            if ($attempt -ge $script:maxRetries) { 
                throw "Failed to get latest release after $script:maxRetries attempts: $_" 
            }
            
            $sleepTime = [Math]::Pow(2, $attempt)
            Write-LogMessage "Attempt $attempt failed, retrying in $sleepTime seconds..."
            Start-Sleep -Seconds $sleepTime
        }
    }
}

function Test-ReleaseExists {
    param ([string]$Version)
    
    $attempt = 0
    while ($attempt -lt $script:maxRetries) {
        try {
            $url = "https://api.github.com/repos/$script:repo/releases/tags/$Version"
            $null = Invoke-RestMethod -Uri $url -UseBasicParsing
            return $true
        }
        catch {
            # Release doesn't exist, no need to retry
            if ($_.Exception.Response -and 
                ($_.Exception.Response.StatusCode -eq 404 -or $_.Exception.Response.StatusCode -eq 410)) {
                return $false
            }
            
            $attempt++
            if ($attempt -ge $script:maxRetries) { 
                return $false 
            }
            
            Start-Sleep -Seconds ([Math]::Pow(2, $attempt))
        }
    }
    
    return $false
}
function Update-UserPath {
    param (
        [string[]]$pathsToAdd = @(),
        [string[]]$pathsToRemove = @()
    )

    try {
        $registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
        $currentPath = Get-ItemProperty -Path $registryPath -Name Path -ErrorAction Stop | Select-Object -ExpandProperty Path
        $currentDirs = $currentPath -split ';' | Where-Object { $_ -and $_ -notmatch '^\s*$' }

        # Check if we need to make changes
        $needsUpdate = $false
        
        # Check for paths to remove
        if ($pathsToRemove.Count -gt 0) {
            $pathsBeingRemoved = $currentDirs | Where-Object { $_ -in $pathsToRemove }
            
            foreach ($removedPath in $pathsBeingRemoved) {
                Write-LogMessage "Removing $removedPath from PATH."
            }
            
            $originalCount = $currentDirs.Count
            $currentDirs = $currentDirs | Where-Object { $_ -notin $pathsToRemove }
            if ($currentDirs.Count -lt $originalCount) {
                $needsUpdate = $true
            }
        }

        # Add new paths if needed
        foreach ($path in $pathsToAdd) {
            if ($path -and $path -notin $currentDirs) {
                Write-LogMessage "Adding $path to PATH."
                $currentDirs += $path
                $needsUpdate = $true
            }
        }

        # Only update if changes were made
        if ($needsUpdate) {
            $newPath = $currentDirs -join ';'
            Set-ItemProperty -Path $registryPath -Name Path -Value $newPath -Type ExpandString
            
            # Update PATH in the current session
            $env:Path = $newPath

            # Use P/Invoke instead of manually updating the PATH env in PS
            if (-not ('WinAPI.User32' -as [Type])) {
                $signature = @'
[DllImport("user32.dll", SetLastError=true, CharSet=CharSet.Auto)]
public static extern IntPtr SendMessageTimeout(
    IntPtr hWnd, 
    uint Msg, 
    UIntPtr wParam, 
    string lParam, 
    uint fuFlags, 
    uint uTimeout, 
    out UIntPtr lpdwResult
);
'@
                Add-Type -MemberDefinition $signature -Name 'User32' -Namespace 'WinAPI' -PassThru | Out-Null
            }

            [UIntPtr]$result = [UIntPtr]::Zero
            $hwndBroadcast = [IntPtr]::new(0xffff)  # HWND_BROADCAST
            $WM_SETTINGCHANGE = 0x1A
            $SMTO_ABORTIFHUNG = 0x0002
            
            # Broadcast change
            $ret = [WinAPI.User32]::SendMessageTimeout(
                $hwndBroadcast,
                $WM_SETTINGCHANGE,
                [UIntPtr]::Zero,
                "Environment",
                $SMTO_ABORTIFHUNG,
                5000,
                [ref]$result
            )
            
            if ($ret -eq 0) {
                Write-LogMessage "Warning: Failed to broadcast PATH change notification"
            }
            else {
                Write-LogMessage "PATH updated successfully"
            }
        }
    }
    catch {
        throw "Failed to update PATH: $_"
    }
}

function Add-fgvmToPath {
    try {
        $binPath = Join-Path $script:fgvmPath "bin"
        
        # Add paths to PATH
        Update-UserPath -pathsToAdd @($script:installPath, $binPath)
    }
    catch {
        throw "Failed to add fgvm to PATH: $_"
    }
}

function Uninstall-fgvm {
    try {
        $binPath = Join-Path $script:fgvmPath "bin"

        Update-UserPath -pathsToRemove @($script:installPath, $binPath)
    }
    catch {
        throw "Failed to remove fgvm from PATH: $_."
    }

    if (Test-Path $script:installPath) {
        try {
            Remove-Item -Recurse -Force $script:installPath
            Write-LogMessage "Removed fgvm installation."
        }
        catch {
            throw "Unable to remove fgvm 1: $_."
        }
    }

    # Remove Godot installs only on uninstall, not on force install
    if ($script:command -eq "uninstall" -and (Test-Path $script:fgvmPath)) {
        try {
            Remove-Item -Recurse -Force $script:fgvmPath
            Write-LogMessage "Removed fgvm Godot installations."
        }
        catch {
            throw "Unable to remove fgvm Godot installations: $_."
        }
    }


    Write-LogMessage "Successfully uninstalled fgvm."
}

function Invoke-FileDownload {
    param([string]$Url, [string]$OutputFile)
    
    if ($script:quiet) {
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($Url, $OutputFile)
    }
    else {
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $Url -OutFile $OutputFile -UseBasicParsing
    }
}

function Get-ReleaseChecksum {
    param([string]$Version)
    
    $url = "https://github.com/$script:repo/releases/download/$Version/fgvm-win-x64.zip.sha256"
    Invoke-FileDownload -Url $url -OutputFile $script:checksumPath
    
    if (Test-Path $script:checksumPath) {
        $content = Get-Content $script:checksumPath -Raw
        if ([string]::IsNullOrWhiteSpace($content)) {
            throw "Empty checksum file"
        }

        # Extract just the hash
        $hash = $content -split '\s+' | Select-Object -First 1
        return $hash.Trim()
    }
    
    throw "Checksum file not found"
}

function Test-Checksum {
    param([string]$File, [string]$Expected)
    
    if (-not $Expected) {
        Write-LogMessage "Error: No checksum provided for verification"
        return $false
    }
    
    $hash = (Get-FileHash $File -Algorithm SHA256).Hash
    if ($hash -eq $Expected) {
        return $true
    }
    else {
        Write-LogMessage "Error: Checksum verification failed"
        Write-LogMessage "Expected: $Expected"
        Write-LogMessage "Actual:   $hash"
        return $false
    }
}

function Install-fgvm {
    param([string]$Version)
    try {
        # Check environment before proceeding
        Initialize-Environment
        
        # Determine architecture
        $arch = Get-Arch
        $url = "https://github.com/$script:repo/releases/download/$Version/fgvm-$arch.zip"
        
        # Create installation directory
        if (-not (Test-Path $script:installPath)) {
            New-Item -ItemType Directory -Path $script:installPath -Force | Out-Null
        }

        # Ensure fgvmPath exists
        if (-not (Test-Path -Path $script:fgvmPath)) {
            New-Item -ItemType Directory -Path $script:fgvmPath -Force | Out-Null
        }
        
        # Check if already installed, unless force flag is specified
        if (-not $script:force) {
            $installedVersion = Get-InstalledVersion
            if ($installedVersion -eq $Version) {
                Write-LogMessage "fgvm version $Version is already installed."
                return
            }
        }
        else {
            # Remove existing installation
            Write-LogMessage "Forcing clean installation of fgvm version $Version"
            if (Test-Path $script:installPath) {
                Write-LogMessage "Removing existing installation..."
                Remove-Item -Path "$script:installPath\*" -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
        
        # Use temp directory for downloads
        $tempDir = New-TempDir
        $tempZipPath = Join-Path $tempDir "fgvm.zip"
        $tempChecksumPath = Join-Path $tempDir "fgvm.sha256"
        
        # Download and verify
        Write-LogMessage "Downloading fgvm version $Version..."
        Invoke-FileDownload -Url $url -OutputFile $tempZipPath
            
        $checksumUrl = "$url.sha256"
        Invoke-FileDownload -Url $checksumUrl -OutputFile $tempChecksumPath
            
        $expectedHash = Get-Content $tempChecksumPath -Raw | ForEach-Object { $_ -split '\s+' | Select-Object -First 1 }
        if (-not $expectedHash) {
            throw "Empty checksum file"
        }
            
        if (-not (Test-Checksum -File $tempZipPath -Expected $expectedHash)) {
            Write-LogMessage "Error: Checksum verification failed - aborting installation"
            throw "Checksum verification failed"
        }
            
        # Extract files
        Write-LogMessage "Extracting files..."
        Expand-Archive -Path $tempZipPath -DestinationPath $script:installPath -Force
            
        # Update PATH if needed
        Add-fgvmToPath
            
        # Always copy the script in case of updates or fixes
        Install-Script

        Write-LogMessage "Successfully installed fgvmup with fgvm version $Version"
    }
    catch {
        Write-LogMessage "Error: Installation failed - $_"
        Remove-InstallationFiles -IsError
        throw
    }
    finally {
        # Clean up temp directory
        if (Test-Path $tempDir) {
            Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
        }
    }
}

function Start-Installation {
    try {
        if ($script:version -eq "latest") {
            $script:version = Get-LatestRelease
            Write-LogMessage "Latest version: $script:version"
        }
        else {
            $script:version = "v$script:version"
            if (-not (Test-ReleaseExists -Version $script:version)) {
                throw "Error: Version $script:version does not exist"
            }
        }
        Install-fgvm -Version $script:version
    }
    catch {
        Write-LogMessage "Installation failed: $_"
        exit 1
    }
}

function Start-Upgrade {
    try {
        $script:version = Get-LatestRelease
        Write-LogMessage "Latest version: $script:version"
        Install-fgvm -Version $script:version
    }
    catch {
        Write-LogMessage "Upgrade failed: $_"
        exit 1
    }
}

function Remove-InstallationFiles {
    param(
        [switch]$IsError
    )
    # Remove temporary files
    if (Test-Path -Path $script:zipPath -ErrorAction SilentlyContinue) {
        Remove-Item -Path $script:zipPath -Force -ErrorAction SilentlyContinue
    }
    
    if (Test-Path -Path $script:checksumPath -ErrorAction SilentlyContinue) {
        Remove-Item -Path $script:checksumPath -Force -ErrorAction SilentlyContinue
    }

    Write-LogMessage "Cleanup complete"
}

function Start-fgvmProcess {
    param (
        [string[]]$Arguments
    )
    
    try {
        Test-WindowsPlatform
        Read-Arguments -Arguments $Arguments
        Test-Dependencies
        
        switch ($script:command) {
            "install" {
                Start-Installation
            }
            "uninstall" {
                Uninstall-fgvm
            }
            "upgrade" {
                Start-Upgrade
            }
            default {
                Show-Usage
            }
        }
    }
    catch {
        Write-LogMessage "Error: $_"
        exit 1
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    # Check if we're running remote execution
    $scriptPath = $MyInvocation.MyCommand.Path
    $isWebExecution = [string]::IsNullOrEmpty($scriptPath)
    
    if ($isWebExecution) {
        # Default to installation
        Start-fgvmProcess @("install")
    }
    else {
        Start-fgvmProcess $args
    }
}