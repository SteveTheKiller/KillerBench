#Requires -Version 5.1
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$global:KbChocoFixture = @{ InstallCalls = 0; UninstallCalls = 0; Registrations = @(); RemoveRegistration = $true; FailInstall = $false }
$priorName = $env:ChocolateyPackageName
$env:ChocolateyPackageName = 'killerbench'
function Install-ChocolateyPackage {
    param($packageName, $fileType, $silentArgs, $url64bit, $checksum64, $checksumType64, $validExitCodes)
    if ($packageName -ne 'killerbench' -or $fileType -ne 'exe' -or $silentArgs -ne '/silent' -or
        $url64bit -ne 'https://github.com/SteveTheKiller/KillerBench/releases/download/v1.0.0/KillerBench.exe' -or
        $checksum64 -ne 'C15CB2262EFB7A44CAC78C67AE182A3C3C0B9867CBB9C4FA58704D4DFD43B1EA' -or
        $checksumType64 -ne 'sha256' -or @($validExitCodes).Count -ne 1 -or $validExitCodes[0] -ne 0) { throw 'Incorrect installer contract.' }
    $global:KbChocoFixture.InstallCalls++
    if ($global:KbChocoFixture.FailInstall) { throw 'Simulated installer failure' }
}
function Get-UninstallRegistryKey { param($SoftwareName) $global:KbChocoFixture.Registrations }
function Test-Path { param($LiteralPath, $PathType) $true }
function Uninstall-ChocolateyPackage {
    param($PackageName, $FileType, $SilentArgs, $File, $ValidExitCodes)
    if ($PackageName -ne 'killerbench' -or $FileType -ne 'exe' -or $SilentArgs -ne '/uninstall-silent' -or
        $File -ne 'C:\OwnedFixture\KillerBench.exe' -or $ValidExitCodes[0] -ne 0) { throw 'Incorrect uninstall contract.' }
    $global:KbChocoFixture.UninstallCalls++
    if ($global:KbChocoFixture.RemoveRegistration) { $global:KbChocoFixture.Registrations = @() }
}
function Refuses([scriptblock]$Action) {
    $failed = $false
    try { & $Action } catch { $failed = $true }
    if (-not $failed) { throw 'Expected package failure was swallowed.' }
}
try {
    $install = Join-Path $RepositoryRoot 'choco/tools/chocolateyInstall.ps1'
    $uninstall = Join-Path $RepositoryRoot 'choco/tools/chocolateyUninstall.ps1'
    & $install
    if ($global:KbChocoFixture.InstallCalls -ne 1) { throw 'Installer was not selected.' }
    $global:KbChocoFixture.FailInstall = $true
    Refuses { & $install }
    & $uninstall
    if ($global:KbChocoFixture.UninstallCalls -ne 0) { throw 'Absent installation was dispatched.' }
    $registration = [pscustomobject]@{ DisplayName='KillerBench'; Publisher='Steve the Killer'; InstallLocation='C:\OwnedFixture' }
    $global:KbChocoFixture.Registrations = @($registration, $registration)
    Refuses { & $uninstall }
    if ($global:KbChocoFixture.UninstallCalls -ne 0) { throw 'Ambiguous installations were dispatched.' }
    $global:KbChocoFixture.Registrations = @($registration)
    & $uninstall
    if ($global:KbChocoFixture.UninstallCalls -ne 1) { throw 'Exact registered uninstall was not dispatched.' }
    $global:KbChocoFixture.Registrations = @($registration)
    $global:KbChocoFixture.RemoveRegistration = $false
    Refuses { & $uninstall }
    Write-Host 'PASS: Chocolatey install arguments, failure propagation, absent and ambiguous uninstall, exact target and post-uninstall registration checks. No native installation or uninstall ran.'
} finally {
    $env:ChocolateyPackageName = $priorName
    Remove-Variable KbChocoFixture -Scope Global
}
