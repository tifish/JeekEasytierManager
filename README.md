# JeekEasyTierManager

An easy EasyTier Manager.

## Install

Run in PowerShell:

```powershell
irm https://raw.githubusercontent.com/tifish/JeekEasytierManager/main/install.ps1 | iex
```

Mirror for mainland China:

```powershell
irm https://ghfast.top/https://raw.githubusercontent.com/tifish/JeekEasytierManager/main/install.ps1 | iex
```

The installer downloads the latest release, installs it to `%LOCALAPPDATA%\Programs\JeekEasyTierManager`, creates a Start Menu shortcut, and starts the app. If the .NET runtime is missing, it runs the bundled `Setup.cmd` to install it first.

To uninstall: uninstall the services from the app, quit it, then delete the install directory and the Start Menu shortcut. Nothing is written to the registry (unless you enable "start on boot" in settings).
