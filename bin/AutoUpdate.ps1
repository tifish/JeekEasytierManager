$ErrorActionPreference = "Stop"
$appName = "JeekEasyTierManager"

if ($args.Count -eq 0) {
    Exit 1
}

$downloadUrl = $args[0]
$restartArgs = @()
if ($args.Count -gt 1) {
    $restartArgs = $args[1..($args.Count - 1)]
}

$packPath = Join-Path $env:TEMP "$appName-update.7z"
$stageRoot = Join-Path $env:TEMP "$appName-update"
$stageDir = Join-Path $stageRoot "package"
$sevenZipTmp = Join-Path $env:TEMP "$appName-7za.exe"

try {
    Get-Process -Name $appName -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $_.WaitForExit()
        } catch {}
    }

    Remove-Item -Recurse -Force -LiteralPath $stageRoot -ErrorAction SilentlyContinue
    Remove-Item -Force -LiteralPath $packPath -ErrorAction SilentlyContinue
    Remove-Item -Force -LiteralPath $sevenZipTmp -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

    $client = New-Object System.Net.WebClient
    $client.Headers.Add("User-Agent", "$appName-Updater/1.0")
    $client.DownloadFile($downloadUrl, $packPath)

    if (-not (Test-Path -LiteralPath $packPath)) {
        Exit 1
    }

    $sevenZipPath = Join-Path $PSScriptRoot "7Zip\7za.exe"
    if (-not (Test-Path -LiteralPath $sevenZipPath)) {
        Exit 1
    }

    Copy-Item -LiteralPath $sevenZipPath -Destination $sevenZipTmp -Force
    & $sevenZipTmp x $packPath "-o$stageDir" "-x!Nssm" -y | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Exit 1
    }

    $stagedExe = Join-Path $stageDir "$appName.exe"
    if (-not (Test-Path -LiteralPath $stagedExe)) {
        Exit 1
    }

    Remove-Item -Recurse -Force -LiteralPath (Join-Path $PSScriptRoot "Libs") -ErrorAction SilentlyContinue
    Remove-Item -Force -Path (Join-Path $PSScriptRoot "*.dll") -ErrorAction SilentlyContinue
    Remove-Item -Force -Path (Join-Path $PSScriptRoot "*.pdb") -ErrorAction SilentlyContinue
    Remove-Item -Force -Path (Join-Path $PSScriptRoot "*.deps.json") -ErrorAction SilentlyContinue
    Remove-Item -Force -Path (Join-Path $PSScriptRoot "*.runtimeconfig.json") -ErrorAction SilentlyContinue

    Copy-Item -Path (Join-Path $stageDir "*") -Destination $PSScriptRoot -Recurse -Force

    Remove-Item -Force -LiteralPath $sevenZipTmp -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force -LiteralPath $stageRoot -ErrorAction SilentlyContinue
    Remove-Item -Force -LiteralPath $packPath -ErrorAction SilentlyContinue
}
catch {
    Start-Sleep -Seconds 5
    Exit 1
}

$exePath = Join-Path $PSScriptRoot "$appName.exe"
if (Test-Path -LiteralPath $exePath) {
    if ($restartArgs.Count -gt 0) {
        Start-Process -FilePath $exePath -ArgumentList $restartArgs
    }
    else {
        Start-Process -FilePath $exePath
    }
}
