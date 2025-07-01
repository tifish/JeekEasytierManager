$appName = "JeekEasytierManager"

# Wait for .exe to exit
$process = Get-Process -Name $appName -ErrorAction SilentlyContinue
if ($process) {
    $process.WaitForExit()
}

# Check if there are any command line arguments
if ($args.Count -eq 0) {
    Write-Host "No command line arguments provided. Exiting..."
    Pause
    Exit
}

# Get the first command line argument as the download URL
$downloadUrl = $args[0]
# Download .zip to system temp directory
$packPath = "$env:TEMP\$appName.zip"
Invoke-WebRequest -Uri $downloadUrl -OutFile $packPath

# Check if $packPath exists
if (-not (Test-Path $packPath)) {
    Write-Host "Failed to download $downloadUrl. Exiting..."
    Pause
    Exit
}

# Delete old Libs directory
Remove-Item -Recurse -Force -Path "$PSScriptRoot\Libs"

# Extract .zip in to $PSScriptRoot
Expand-Archive -Path $packPath -DestinationPath $PSScriptRoot -Force

# Start .exe
Start-Process -FilePath "$PSScriptRoot\$appName.exe"
