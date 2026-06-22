@echo off
setlocal
cd /d "%~dp0"

taskkill /im JeekEasyTierManager.exe /f >nul 2>nul

dotnet build JeekEasyTierManager.sln
if errorlevel 1 (
    pause
    exit /b 1
)

start "" "bin\JeekEasyTierManager.exe"

endlocal
