@echo off
setlocal
cd /d "%~dp0"

rem Release build into bin\. Cleans stale outputs first so NetBeauty / dependency
rem churn does not leave orphan DLLs. Strips PDBs for a shippable tree.
taskkill /f /im "JeekEasyTierManager.exe" >nul 2>nul

del /q "bin\*.deps.json" "bin\*.runtimeconfig.json" "bin\*.dll" "bin\*.pdb" "bin\Libs\*" 2>nul
rd /s /q "bin\Logs" 2>nul

dotnet build --configuration Release JeekEasyTierManager.sln
if errorlevel 1 pause

rem The MCP stdio adapter ships beside the app as a single file (see its csproj).
dotnet publish --configuration Release Tools\JeekEasyTierManagerMcp\JeekEasyTierManagerMcp.csproj -v minimal --nologo
if errorlevel 1 echo Warning: MCP adapter publish failed (adapter in use?)

del /q /s bin\*.pdb 2>nul

endlocal
