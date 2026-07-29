@echo off
setlocal
cd /d "%~dp0"

taskkill /im JeekEasyTierManager.exe /f >nul 2>nul

dotnet build JeekEasyTierManager.sln
if errorlevel 1 (
    pause
    exit /b 1
)

rem The MCP stdio adapter ships beside the app as a single file (see its csproj).
dotnet publish Tools\JeekEasyTierManagerMcp\JeekEasyTierManagerMcp.csproj -v minimal --nologo
if errorlevel 1 echo Warning: MCP adapter publish failed (adapter in use?)

start "" "bin\JeekEasyTierManager.exe"

endlocal
