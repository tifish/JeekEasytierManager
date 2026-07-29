@echo off
setlocal
cd /d "%~dp0"

dotnet build JeekEasyTierManager.sln
if errorlevel 1 pause

rem The MCP stdio adapter ships beside the app as a single file (see its csproj).
dotnet publish Tools\JeekEasyTierManagerMcp\JeekEasyTierManagerMcp.csproj -v minimal --nologo
if errorlevel 1 echo Warning: MCP adapter publish failed (adapter in use?)

endlocal
