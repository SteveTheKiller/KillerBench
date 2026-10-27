#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the KillerBench installer (KillerBench.exe) and portable (KillerBench-Portable.exe) packages.
.DESCRIPTION
    Publishes the multi-file KillerBench payload, writes its SHA-256 manifest, compresses it once,
    and embeds it in the Packaging\KillerLauncher launcher. The installer payload is
    framework-dependent and its wizard checks for the .NET 10 Desktop Runtime. The portable payload
    is self-contained, so it runs on a PC without the runtime. The Microsoft Store package
    (KillerBench-Store.exe) is the installer with the portable's bundled runtime, because the Store
    installs silently on PCs that may not have the .NET 10 Desktop Runtime. Both builds all three.
    -RepackOnly reuses a prepared (for example, freshly signed) payload. -RequireSignature refuses
    unsigned first-party payload files and builds a launcher that only installs signed downloads.
#>
param(
    [string]$Configuration = "Release",
    [ValidateSet("Both", "Portable", "Installer", "Store")]
    [string]$PackageKind = "Both",
    [switch]$RepackOnly,
    [switch]$RequireSignature
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectDir = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $projectDir 'KillerBench.csproj'
$launcherProject = Join-Path $PSScriptRoot 'KillerLauncher\KillerLauncher.csproj'
$publicDir = Join-Path $projectDir "bin\$Configuration\net10.0-windows\publish"
$firstPartyNames = @('KillerBench.exe', 'KillerBench.dll', 'KillerBench.Engine.dll', 'KillerBench.Engine.Core.dll', 'KillerBench.Engine.Windows.dll')

function Get-FirstPartyPayloadFiles([string]$PayloadDirectory) {
    foreach ($name in $firstPartyNames) {
        $path = Join-Path $PayloadDirectory $name
        if (-not [IO.File]::Exists($path)) { throw "First-party payload file is missing: $path" }
        $path
    }
}

function Assert-FirstPartyPayloadSignatures([string]$PayloadDirectory) {
    foreach ($path in @(Get-FirstPartyPayloadFiles $PayloadDirectory)) {
        $signature = Get-AuthenticodeSignature -LiteralPath $path
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "First-party Authenticode validation failed for $path with status $($signature.Status)."
        }
    }
}

$versionXml = [xml](Get-Content -Raw -LiteralPath $appProject)
$version = [string]$versionXml.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
$fileVersion = [string]$versionXml.SelectSingleNode('/Project/PropertyGroup/FileVersion').InnerText
if (-not $version -or -not $fileVersion) { throw 'KillerBench.csproj needs Version and FileVersion.' }

$kinds = if ($PackageKind -eq 'Both') { @('Installer', 'Portable', 'Store') } else { @($PackageKind) }
$results = foreach ($kind in $kinds) {
    $isStore = $kind -eq 'Store'
    $isInstaller = $kind -ne 'Portable'
    $bundlesRuntime = $isStore -or -not $isInstaller
    $slug = if ($isStore) { 'store-package' } elseif ($isInstaller) { 'installer-package' } else { 'portable-package' }
    $artifactRoot = Join-Path $projectDir "bin\$Configuration\net10.0-windows\$slug"
    $payloadDir = Join-Path $artifactRoot 'payload'
    $payloadZip = Join-Path $artifactRoot 'payload.zip'
    $launcherOutput = Join-Path $artifactRoot 'launcher'
    $publicExe = Join-Path $publicDir $(if ($isStore) { 'KillerBench-Store.exe' } elseif ($isInstaller) { 'KillerBench.exe' } else { 'KillerBench-Portable.exe' })

    if (-not $RepackOnly -and [IO.Directory]::Exists($artifactRoot)) { [IO.Directory]::Delete($artifactRoot, $true) }
    if ([IO.Directory]::Exists($launcherOutput)) { [IO.Directory]::Delete($launcherOutput, $true) }
    foreach ($dir in @($artifactRoot, $payloadDir, $launcherOutput, $publicDir)) { [IO.Directory]::CreateDirectory($dir) | Out-Null }

    if (-not $RepackOnly) {
        Write-Host "==> Publishing the $kind payload for KillerBench $version..." -ForegroundColor Cyan
        & dotnet publish $appProject -c $Configuration -r win-x64 `
            --self-contained $bundlesRuntime `
            -p:PublishSingleFile=false `
            -p:PublishReadyToRun=true `
            -p:PublishDir="$payloadDir\"
        if ($LASTEXITCODE -ne 0) { throw "$kind payload publish failed." }
        foreach ($symbol in [IO.Directory]::GetFiles($payloadDir, '*.pdb', [IO.SearchOption]::AllDirectories)) {
            [IO.File]::Delete($symbol)
        }
        [IO.File]::Copy((Join-Path $projectDir 'LICENSE.txt'), (Join-Path $payloadDir 'LICENSE.txt'), $true)
        foreach ($notice in @('KillerSans-OFL.txt', 'Lacquer-OFL.txt', 'Punktype-LICENSE.txt')) {
            if (-not [IO.File]::Exists((Join-Path $payloadDir $notice))) { throw "The $kind payload is missing the font notice $notice." }
        }
    } elseif (-not [IO.File]::Exists((Join-Path $payloadDir 'KillerBench.exe'))) {
        throw "RepackOnly requested but the prepared $kind payload is missing."
    }

    $payloadAssembly = Join-Path $payloadDir 'KillerBench.dll'
    $actualAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($payloadAssembly).Version.ToString()
    if ($actualAssemblyVersion -ne $fileVersion) {
        throw "Payload assembly version $actualAssemblyVersion does not match project file version $fileVersion."
    }
    $null = @(Get-FirstPartyPayloadFiles $payloadDir)
    if ($RequireSignature) { Assert-FirstPartyPayloadSignatures $payloadDir }

    $manifestPath = Join-Path $payloadDir 'payload.manifest'
    $payloadFiles = @([IO.Directory]::GetFiles($payloadDir, '*', [IO.SearchOption]::AllDirectories) |
        Where-Object { -not [string]::Equals($_, $manifestPath, [StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object { $_.Substring($payloadDir.Length + 1) })
    $lines = foreach ($file in $payloadFiles) {
        $relative = $file.Substring($payloadDir.Length + 1).Replace('\', '/')
        "$((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash)`t$(([IO.FileInfo]$file).Length)`t$relative"
    }
    [IO.File]::WriteAllLines($manifestPath, [string[]]$lines, [Text.UTF8Encoding]::new($false))

    Write-Host "==> Compressing the $kind payload..." -ForegroundColor Cyan
    Add-Type -AssemblyName System.IO.Compression
    if ([IO.File]::Exists($payloadZip)) { [IO.File]::Delete($payloadZip) }
    $zipStream = [IO.File]::Open($payloadZip, [IO.FileMode]::CreateNew)
    try {
        $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            foreach ($file in @([IO.Directory]::GetFiles($payloadDir, '*', [IO.SearchOption]::AllDirectories) |
                    Sort-Object { $_.Substring($payloadDir.Length + 1) })) {
                $entry = $archive.CreateEntry($file.Substring($payloadDir.Length + 1).Replace('\', '/'),
                    [IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]::new(2020, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                $input = [IO.File]::OpenRead($file)
                $output = $entry.Open()
                try { $input.CopyTo($output) } finally { $output.Dispose(); $input.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $zipStream.Dispose() }

    Write-Host "==> Building the $kind launcher..." -ForegroundColor Cyan
    $launcherTitle = if ($isInstaller) { 'KillerBench Installer' } else { 'KillerBench' }
    & dotnet publish $launcherProject -c $Configuration `
        -p:LauncherAssemblyName=KillerBench `
        -p:AssemblyTitle="$launcherTitle" `
        -p:Copyright="Copyright $([DateTime]::Now.Year) Steve the Killer" `
        -p:LauncherVersion=$version `
        -p:LauncherFileVersion=$fileVersion `
        -p:LauncherIcon="$(Join-Path $projectDir 'Assets\AppIcon.ico')" `
        -p:PayloadZip="$payloadZip" `
        -p:InstallerPackage="$isInstaller" `
        -p:StorePackage="$isStore" `
        -p:AllowUnsignedInstall="$(!$RequireSignature)" `
        -p:InstallerTestHooks="$(!$RequireSignature)" `
        -p:PublishDir="$launcherOutput\"
    if ($LASTEXITCODE -ne 0) { throw "$kind launcher build failed." }
    $builtLauncher = Join-Path $launcherOutput 'KillerBench.exe'
    if (-not [IO.File]::Exists($builtLauncher)) { throw "Launcher output is missing: $builtLauncher" }
    [IO.File]::Copy($builtLauncher, $publicExe, $true)

    # Installer commands must install exactly once and exit, never falling through to the
    # portable launch path. A disposable install root keeps this off the registry and real copies.
    if ($isInstaller -and -not $RequireSignature) {
        $smokeRoot = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-install-smoke-' + [Guid]::NewGuid().ToString('N'))
        $previousRoot = [Environment]::GetEnvironmentVariable('KILLERBENCH_TEST_INSTALL_ROOT')
        $previousSkip = [Environment]::GetEnvironmentVariable('KILLERBENCH_SKIP_REGISTRATION')
        try {
            [Environment]::SetEnvironmentVariable('KILLERBENCH_TEST_INSTALL_ROOT', $smokeRoot)
            [Environment]::SetEnvironmentVariable('KILLERBENCH_SKIP_REGISTRATION', '1')
            # The Store runs its installer with /silent, so that is the path its package is tested on.
            $smokeArgument = if ($isStore) { '/silent' } else { '/install-user' }
            $install = Start-Process -FilePath $builtLauncher -ArgumentList $smokeArgument -Wait -PassThru
            if ($install.ExitCode -ne 0) { throw "Launcher install smoke test failed with exit code $($install.ExitCode)." }
            foreach ($required in 'KillerBench.exe', 'payload.manifest') {
                if (-not [IO.File]::Exists((Join-Path $smokeRoot $required))) {
                    throw "Launcher install smoke test did not install $required."
                }
            }
        }
        finally {
            [Environment]::SetEnvironmentVariable('KILLERBENCH_TEST_INSTALL_ROOT', $previousRoot)
            [Environment]::SetEnvironmentVariable('KILLERBENCH_SKIP_REGISTRATION', $previousSkip)
            if ([IO.Directory]::Exists($smokeRoot)) { [IO.Directory]::Delete($smokeRoot, $true) }
        }
    }

    Write-Host "    $kind payload files: $($payloadFiles.Count)" -ForegroundColor Green
    Write-Host "    $kind output       : $publicExe ($(([IO.FileInfo]$publicExe).Length) bytes)" -ForegroundColor Green
    [pscustomobject]@{ Kind = $kind; PayloadDirectory = $payloadDir; PublicExe = $publicExe }
}
$results
