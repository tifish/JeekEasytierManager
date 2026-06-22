@echo off
setlocal

(fsutil dirty query %systemdrive% 1>nul 2>nul) || (echo Start-Process $env:ComSpec '/s /c "cd /d "%cd%" && "%~f0" %*"' -Verb RunAs -PassThru ^| Wait-Process> "%temp%\getadmin.ps1") && (powershell -ExecutionPolicy Bypass -File "%temp%\getadmin.ps1") && (exit /b)

echo Installing .NET runtime...
rem MagicOnion/Kestrel needs the ASP.NET Core runtime (it includes the base .NET runtime).
rem Install into the shared dotnet folder so the app host can locate it.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0dotnet-install.ps1" -Channel 10.0 -Runtime aspnetcore -Architecture x64 -InstallDir "%ProgramFiles%\dotnet"

echo Starting application...
start "" "%~dp0JeekEasyTierManager.exe"

endlocal
