<#
.SYNOPSIS
    One-line installer for fgvmup and fgvm

.DESCRIPTION
    Provides a simple way to download and install fgvmup and fgvm.

.LINK
    https://github.com/patricktcoakley/fgvm

.EXAMPLE
    irm https://raw.githubusercontent.com/patricktcoakley/fgvm/main/installer.ps1 | iex
    # Downloads and installs the latest version of fgvmup
    
.NOTES
    Platform: Currently only supports Windows x86_64.
    Requires PowerShell 5.0 or later.
#>

$tempFile = Join-Path $env:TEMP ([System.Guid]::NewGuid().ToString() + ".ps1")

try {
    # Download the main script
    $scriptUrl = "https://raw.githubusercontent.com/patricktcoakley/fgvm/main/fgvmup.ps1"
    Write-Host "Downloading fgvmup installer from $scriptUrl."
    Invoke-WebRequest -Uri $scriptUrl -OutFile $tempFile -UseBasicParsing
    
    # Execute with install command
    Write-Host "Installing fgvmup..."
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