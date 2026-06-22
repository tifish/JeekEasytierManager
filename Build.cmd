@echo off
setlocal
cd /d "%~dp0"

dotnet build JeekEasyTierManager.sln
if errorlevel 1 pause

endlocal
