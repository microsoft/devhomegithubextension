# Completely removes DevHome and cleans state.

# Clean Widget State and remove extensions.
& "$PSScriptRoot\CleanWidgets.ps1"

# Remove Devhome.
Write-Host "Removing DevHome..."
Get-Appxpackage *Microsoft.Windows.DevHome* | Remove-AppxPackage

Write-Host "DevHome and related packages and state has been removed."
