# UltraDictate Windows Installer Script
param (
    [string]$InstallDir = "$env:LocalAppData\Programs\UltraDictate"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Installing UltraDictate for Windows..." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceExe = Join-Path $ScriptDir "dist\UltraDictate.exe"

if (-not (Test-Path $SourceExe)) {
    Write-Host "Binary not found in dist/. Building from source..." -ForegroundColor Yellow
    & (Join-Path $ScriptDir "build.bat")
}

if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

$ModelsDir = Join-Path $env:APPDATA "UltraDictate\models"
if (-not (Test-Path $ModelsDir)) {
    New-Item -ItemType Directory -Path $ModelsDir -Force | Out-Null
}

Copy-Item -Path $SourceExe -Destination (Join-Path $InstallDir "UltraDictate.exe") -Force

# Create Desktop Shortcut
$WshShell = New-Object -ComObject WScript.Shell
$ShortcutPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath("Desktop"), "UltraDictate.lnk")
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Join-Path $InstallDir "UltraDictate.exe"
$Shortcut.Description = "UltraDictate Speech Dictation"
$Shortcut.Save()

Write-Host "Installation complete! Installed at $InstallDir" -ForegroundColor Green
Write-Host "Shortcut created on Desktop. Launching UltraDictate..." -ForegroundColor Green
Start-Process (Join-Path $InstallDir "UltraDictate.exe")
