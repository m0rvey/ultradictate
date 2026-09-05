@echo off
setlocal enabledelayedexpansion

echo ==============================================
echo Building UltraDictate for Windows (x64 Release Standalone)
echo ==============================================

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo Error: .NET 8 SDK is not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/
    exit /b 1
)

taskkill /F /IM UltraDictate.exe >nul 2>nul
timeout /t 1 /nobreak >nul 2>nul

cd /d "%~dp0\UltraDictate.Windows"
dotnet restore
if %ERRORLEVEL% neq 0 exit /b 1

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "%~dp0\dist"
if %ERRORLEVEL% neq 0 exit /b 1

del "%~dp0dist\*.pdb" >nul 2>nul

echo.
echo ==============================================
echo Build successful! Standalone executable created at:
echo %~dp0dist\UltraDictate.exe
echo ==============================================
