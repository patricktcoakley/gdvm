<#
.SYNOPSIS
    One-line installer for gdvmup and gdvm

.DESCRIPTION
    Provides a simple way to download and install gdvmup and gdvm.

.LINK
    https://github.com/patricktcoakley/gdvm

.EXAMPLE
    irm https://raw.githubusercontent.com/patricktcoakley/gdvm/main/installer.ps1 | iex
    # Downloads and installs the latest version of gdvmup
    
.NOTES
    Platform: Currently only supports Windows x86_64.
    Requires PowerShell 5.0 or later.
#>

$tempFile = Join-Path $env:TEMP ([System.Guid]::NewGuid().ToString() + ".ps1")

try {
    # Download the main script
    $scriptUrl = "https://raw.githubusercontent.com/patricktcoakley/gdvm/main/gdvmup.ps1"
    Write-Host "Downloading gdvmup installer from $scriptUrl."
    Invoke-WebRequest -Uri $scriptUrl -OutFile $tempFile -UseBasicParsing
    
    # Execute with install command
    Write-Host "Installing gdvmup..."
    & $tempFile install
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "Installation failed with exit code $LASTEXITCODE." -ForegroundColor Red
        exit $LASTEXITCODE 
    }
    else {
        Write-Host "Installation completed successfully." -ForegroundColor Green
    }
}
catch {
    Write-Host "Error during installation: $_." -ForegroundColor Red
    exit 1
}
finally {
    # Cleanup
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
}