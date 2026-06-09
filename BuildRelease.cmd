@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

del /q "bin\*.deps.json" "bin\*.runtimeconfig.json" "bin\Libs\*" 2>nul
rd /s /q "bin\Logs" 2>nul

for /f %%i in ('git rev-list --count HEAD') do set VERSION=%%i
if not defined VERSION set VERSION=0
<nul set /p "=!VERSION!" > version.txt
echo commit count = !VERSION!

dotnet publish --configuration Release JeekEasytierManager.sln /p:Version=!VERSION!
if errorlevel 1 pause

del /q /s bin\*.pdb 2>nul

endlocal
