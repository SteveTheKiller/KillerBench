#Requires -Version 5.1
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Join-Path ([Environment]::GetFolderPath('UserProfile')) ('scratch/KillerBench-distribution-test-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
$global:KbDistributionFixture = @{ Pushes = 0; Submits = 0; Attempts = 0; KomacFailures = 0; ChocoFailure = $false; BadHash = $false; BadSignature = $false; BadVersion = $false }
function Get-Item {
    param($LiteralPath)
    $version = if ($global:KbDistributionFixture.BadVersion) { '0.0.0.0' } else { '2.3.4.0' }
    [pscustomobject]@{ VersionInfo = [pscustomobject]@{ FileVersion = $version } }
}
function gh { $global:LASTEXITCODE = 0; 'SteveTheKiller' }
function Invoke-WebRequest {
    param($Uri, $OutFile, [switch]$UseBasicParsing, $TimeoutSec)
    if ($OutFile) { [IO.File]::WriteAllBytes($OutFile, [byte[]]@(1,2,3)); return }
    $hash = if ($global:KbDistributionFixture.BadHash) { '0' * 64 } else { '039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81' }
    [pscustomobject]@{ Content = "KillerBench.exe $hash" }
}
function Get-AuthenticodeSignature {
    param($LiteralPath)
    $status = if ($global:KbDistributionFixture.BadSignature) { 'NotSigned' } else { 'Valid' }
    $certificate = New-Object PSObject
    $certificate | Add-Member ScriptMethod GetNameInfo { param($type, $issuer) 'Open Source Developer Stephen Riley' }
    [pscustomobject]@{ Status=$status; SignerCertificate=$certificate }
}
function choco {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'pack') {
        $specPath = [string]$args[1]
        [xml]$spec = Get-Content -LiteralPath $specPath -Raw
        if ($spec.package.metadata.version -ne '2.3.4') { throw 'Package version was not stamped.' }
        $install = Get-Content -LiteralPath (Join-Path (Split-Path $specPath) 'tools/chocolateyInstall.ps1') -Raw
        if ($install -notmatch '/v2\.3\.4/KillerBench\.exe' -or $install -notmatch '039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81') { throw 'Installer URL or hash was not stamped.' }
        [IO.File]::WriteAllText((Join-Path $args[3] 'killerbench.2.3.4.nupkg'), 'fixture')
    } elseif ($args[0] -eq 'push') {
        $global:KbDistributionFixture.Pushes++
        if ($global:KbDistributionFixture.ChocoFailure) { $global:LASTEXITCODE = 1 }
    }
    else { throw 'Unexpected Chocolatey command.' }
}
function komac {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'update') {
        $output = $args[[Array]::IndexOf($args, '--output') + 1]
        $dir = Join-Path $output 'manifests/s/SteveTheKiller/KillerBench/2.3.4'
        [void][IO.Directory]::CreateDirectory($dir)
        [IO.File]::WriteAllText((Join-Path $dir 'SteveTheKiller.KillerBench.installer.yaml'), "InstallerType: portable`nInstallerSha256: 039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81`nManifestVersion: 1.12.0`n")
    } elseif ($args[0] -eq 'submit') {
        $global:KbDistributionFixture.Attempts++
        if ($global:KbDistributionFixture.KomacFailures -gt 0) {
            $global:KbDistributionFixture.KomacFailures--
            $global:LASTEXITCODE = 1
        } else { $global:KbDistributionFixture.Submits++ }
    }
    else { throw 'Unexpected Komac command.' }
}
function Start-Sleep { param($Seconds) }
function Refuses([scriptblock]$Action) {
    $failed = $false
    try { & $Action } catch { $failed = $true }
    if (-not $failed) { throw 'Unsafe distribution request was accepted.' }
}
$scriptPath = Join-Path $RepositoryRoot 'Packaging/Submit-Distribution.ps1'
$oldKey = $env:CHOCO_API_KEY
$oldToken = $env:GITHUB_TOKEN
try {
    & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -OutputDirectory (Join-Path $root 'choco')
    & $scriptPath -Channel WinGet -ReleaseTag v2.3.4 -OutputDirectory (Join-Path $root 'winget')
    if ($global:KbDistributionFixture.Pushes -ne 0 -or $global:KbDistributionFixture.Submits -ne 0) { throw 'Default preparation published.' }
    $manifest = Get-Content -LiteralPath (Join-Path $root 'winget/manifests-output/manifests/s/SteveTheKiller/KillerBench/2.3.4/SteveTheKiller.KillerBench.installer.yaml') -Raw
    if ($manifest -notmatch 'InstallerType: exe' -or $manifest -notmatch 'Microsoft.DotNet.DesktopRuntime.10' -or $manifest -notmatch 'Silent: /silent') { throw 'WinGet installer behavior was lost.' }
    $global:KbDistributionFixture.BadHash = $true
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'bad-hash') }
    $global:KbDistributionFixture.BadHash = $false
    $global:KbDistributionFixture.BadSignature = $true
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'bad-signature') }
    $global:KbDistributionFixture.BadSignature = $false
    $global:KbDistributionFixture.BadVersion = $true
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'bad-version') }
    $global:KbDistributionFixture.BadVersion = $false
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag 'v2.3.4;bad' -OutputDirectory (Join-Path $root 'invalid-tag') }
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -OutputDirectory (Join-Path $root 'choco') }
    if ($global:KbDistributionFixture.Pushes -ne 0 -or $global:KbDistributionFixture.Submits -ne 0) { throw 'Refused input published.' }
    $env:CHOCO_API_KEY = 'synthetic'
    $env:GITHUB_TOKEN = 'synthetic'
    & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'submit-choco')
    & $scriptPath -Channel WinGet -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'submit-winget')
    if ($global:KbDistributionFixture.Pushes -ne 1 -or $global:KbDistributionFixture.Submits -ne 1) { throw 'Explicit submission did not dispatch each channel once.' }
    $global:KbDistributionFixture.KomacFailures = 1
    & $scriptPath -Channel WinGet -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'retry-winget')
    if ($global:KbDistributionFixture.Attempts -ne 3 -or $global:KbDistributionFixture.Submits -ne 2) { throw 'Transient WinGet failure did not retry correctly.' }
    $global:KbDistributionFixture.KomacFailures = 99
    Refuses { & $scriptPath -Channel WinGet -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'failed-winget') }
    if ($global:KbDistributionFixture.Attempts -ne 7 -or $global:KbDistributionFixture.Submits -ne 2) { throw 'WinGet failure retry bound was not enforced.' }
    $global:KbDistributionFixture.ChocoFailure = $true
    Refuses { & $scriptPath -Channel Chocolatey -ReleaseTag v2.3.4 -Submit -OutputDirectory (Join-Path $root 'failed-choco') }
    if ($global:KbDistributionFixture.Pushes -ne 2) { throw 'Chocolatey refusal was retried or ignored.' }
    Write-Host 'PASS: safe preparation, version/hash stamping, WinGet EXE/runtime settings, exact checksum/publisher refusal and explicit channel submission. Network and publication boundaries substituted.'
} finally {
    $env:CHOCO_API_KEY = $oldKey
    $env:GITHUB_TOKEN = $oldToken
    Remove-Variable KbDistributionFixture -Scope Global
    $resolved = [IO.Path]::GetFullPath($root)
    $boundary = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'scratch/'))
    if (-not $resolved.StartsWith($boundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup boundary failed.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
