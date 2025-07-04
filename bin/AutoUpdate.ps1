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
# Download .7z to system temp directory
$packPath = "$env:TEMP\$appName.7z"
Invoke-WebRequest -Uri $downloadUrl -OutFile $packPath

# Check if $packPath exists
if (-not (Test-Path $packPath)) {
    Write-Host "Failed to download $downloadUrl. Exiting..."
    Pause
    Exit
}

# Delete old Libs directory
Remove-Item -Recurse -Force -Path "$PSScriptRoot\Libs"

# Extract .7z in to $PSScriptRoot
& {
    7Zip\7za.exe x $packPath -o"$PSScriptRoot" -x!7Zip -x!Nssm -y
} -ErrorAction SilentlyContinue

# Delete pack file
Remove-Item -Force -Path $packPath

# Start .exe
if ($args.Count -gt 1) {
    Start-Process -FilePath "$PSScriptRoot\$appName.exe" -ArgumentList $args[1..$args.Length]
}
else {
    Start-Process -FilePath "$PSScriptRoot\$appName.exe"
}
