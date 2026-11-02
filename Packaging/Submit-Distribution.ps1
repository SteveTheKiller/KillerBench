#Requires -Version 5.1
<#
.SYNOPSIS
Prepare a WinGet manifest or Chocolatey package for a published KillerBench release.
.DESCRIPTION
Downloads the release installer, checks SHA256SUMS.txt and its Authenticode publisher,
then prepares files in a new output directory. Only -Submit creates a WinGet pull request
or pushes a Chocolatey package. A failed check stops before submission. Does not build,
sign, install, uninstall, publish a GitHub release or submit to Microsoft Store.
#>
param(
    [Parameter(Mandatory=$true)][ValidateSet('WinGet','Chocolatey')][string]$Channel,
    [Parameter(Mandatory=$true)][ValidatePattern('\Av[0-9]+\.[0-9]+\.[0-9]+\z')][string]$ReleaseTag,
    [string]$OutputDirectory,
    [switch]$Submit
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = Split-Path -Parent $PSScriptRoot
$version = $ReleaseTag.Substring(1)
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path ([Environment]::GetFolderPath('UserProfile')) ('scratch/KillerBench-distribution-' + [guid]::NewGuid().ToString('N'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$repoBoundary = [IO.Path]::GetFullPath($repo).TrimEnd('\') + '\'
if ($OutputDirectory -eq $repoBoundary.TrimEnd('\') -or $OutputDirectory.StartsWith($repoBoundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'Distribution output must be outside the repository.' }
if ([IO.Directory]::Exists($OutputDirectory) -or [IO.File]::Exists($OutputDirectory)) { throw 'Use a new distribution output directory.' }
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$url = "https://github.com/SteveTheKiller/KillerBench/releases/download/$ReleaseTag/KillerBench.exe"
$installer = Join-Path $OutputDirectory 'KillerBench.exe'
Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing -TimeoutSec 120
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
$sums = (Invoke-WebRequest -Uri "https://github.com/SteveTheKiller/KillerBench/releases/download/$ReleaseTag/SHA256SUMS.txt" -UseBasicParsing -TimeoutSec 60).Content
if ($sums -is [byte[]]) { $sums = [Text.Encoding]::UTF8.GetString($sums) }
$checksumMatches = [regex]::Matches($sums, '(?m)^KillerBench\.exe[ \t]+([A-Fa-f0-9]{64})[ \t]*\r?$')
if ($checksumMatches.Count -ne 1 -or $checksumMatches[0].Groups[1].Value -ne $hash) { throw 'KillerBench.exe does not match the unique published checksum.' }
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false) -ne 'Open Source Developer Stephen Riley') { throw 'The release installer does not have the expected valid publisher signature.' }
if ((Get-Item -LiteralPath $installer).VersionInfo.FileVersion -ne "$version.0") { throw 'Installer file version does not match the release tag.' }
if ($Channel -eq 'Chocolatey') {
    $packageRoot = Join-Path $OutputDirectory 'choco'
    Copy-Item -LiteralPath (Join-Path $repo 'choco') -Destination $packageRoot -Recurse
    $nuspec = Join-Path $packageRoot 'killerbench.nuspec'
    [xml]$spec = Get-Content -LiteralPath $nuspec -Raw
    $spec.package.metadata.version = $version
    foreach ($field in @('licenseUrl','releaseNotes')) {
        $spec.package.metadata.$field = ([string]$spec.package.metadata.$field) -replace '/v[0-9]+\.[0-9]+\.[0-9]+(?=/|$)', "/$ReleaseTag"
    }
    $spec.Save($nuspec)
    $installScript = Join-Path $packageRoot 'tools/chocolateyInstall.ps1'
    $text = Get-Content -LiteralPath $installScript -Raw
    if ([regex]::Matches($text, '(?m)^\s*checksum64\s*=').Count -ne 1) { throw 'Chocolatey install script has no unique checksum field.' }
    $text = $text -replace '/download/v[0-9]+\.[0-9]+\.[0-9]+/', "/download/$ReleaseTag/"
    $text = [regex]::Replace($text, "(?m)^(\s*checksum64\s*=\s*)'[A-Fa-f0-9]{64}'", { param($m) $m.Groups[1].Value + "'$hash'" })
    if (-not $text.Contains($hash) -or -not $text.Contains("/download/$ReleaseTag/KillerBench.exe")) { throw 'Chocolatey release stamping failed.' }
    [IO.File]::WriteAllText($installScript, $text, [Text.UTF8Encoding]::new($false))
    choco pack $nuspec --output-directory $OutputDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Chocolatey packing failed.' }
    $package = Join-Path $OutputDirectory "killerbench.$version.nupkg"
    if (-not [IO.File]::Exists($package)) { throw 'Chocolatey did not produce the expected package.' }
    if ($Submit) {
        if (-not $env:CHOCO_API_KEY) { throw 'CHOCO_API_KEY is required for submission.' }
        choco push $package --source https://push.chocolatey.org/ --api-key $env:CHOCO_API_KEY
        if ($LASTEXITCODE -ne 0) { throw "Chocolatey push failed. Check https://community.chocolatey.org/packages/killerbench/$version for an existing version or moderation result before retrying." }
        Write-Host "Chocolatey upload accepted. Validation and moderation remain separate: https://community.chocolatey.org/packages/killerbench/$version"
    } else { Write-Host "Prepared: $package" }
    return
}
$manifestRoot = Join-Path $OutputDirectory 'manifests-output'
komac update SteveTheKiller.KillerBench --version $version --urls $url --dry-run --output $manifestRoot
if ($LASTEXITCODE -ne 0) { throw 'Komac preparation failed. The initial package must already be merged in microsoft/winget-pkgs.' }
$manifestDirectory = Join-Path $manifestRoot "manifests/s/SteveTheKiller/KillerBench/$version"
$manifestPath = Join-Path $manifestDirectory 'SteveTheKiller.KillerBench.installer.yaml'
if (-not [IO.File]::Exists($manifestPath)) { throw 'Komac did not save an installer manifest.' }
$text = Get-Content -LiteralPath $manifestPath -Raw
$manifestHashes = [regex]::Matches($text, '(?m)^\s*InstallerSha256:\s*([A-Fa-f0-9]{64})\s*$')
if ($manifestHashes.Count -ne 1 -or $manifestHashes[0].Groups[1].Value -ne $hash) { throw 'Komac downloaded a different release installer.' }
$schema = [regex]::Match($text, '(?m)^ManifestVersion:\s*([0-9]+\.[0-9]+\.[0-9]+)\s*$').Groups[1].Value
if (-not $schema) { throw 'Komac manifest has no schema version.' }
$lines = @(
    "# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.$schema.schema.json"
    'PackageIdentifier: SteveTheKiller.KillerBench'
    "PackageVersion: $version"
    'InstallerType: exe'
    'InstallModes:'
    '- interactive'
    '- silent'
    'InstallerSwitches:'
    '  Silent: /silent'
    '  SilentWithProgress: /silent'
    'UpgradeBehavior: install'
    'Commands:'
    '- KillerBench'
    'Dependencies:'
    '  PackageDependencies:'
    '  - PackageIdentifier: Microsoft.DotNet.DesktopRuntime.10'
    'AppsAndFeaturesEntries:'
    '- DisplayName: KillerBench'
    '  Publisher: Steve the Killer'
    "  DisplayVersion: $version"
    '  ProductCode: KillerBench'
    'Installers:'
    '- Architecture: x64'
    "  InstallerUrl: $url"
    "  InstallerSha256: $hash"
    'ManifestType: installer'
    "ManifestVersion: $schema"
)
[IO.File]::WriteAllText($manifestPath, (($lines -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
if ($Submit) {
    if (-not $env:GITHUB_TOKEN) { throw 'GITHUB_TOKEN is required for WinGet submission.' }
    $login = gh api user --jq .login
    if ($LASTEXITCODE -ne 0 -or $login -ne 'SteveTheKiller') { throw 'WinGet submission requires the SteveTheKiller GitHub identity.' }
    for ($attempt=1; $attempt -le 4; $attempt++) {
        komac submit $manifestDirectory --yes
        if ($LASTEXITCODE -eq 0) { Write-Host 'WinGet submission completed. PR review and merge remain separate.'; return }
        if ($attempt -lt 4) { Start-Sleep -Seconds (60 * $attempt) }
    }
    throw 'WinGet submission failed after bounded retries. Retry the WinGet Release workflow with the same tag; do not republish the GitHub release.'
}
Write-Host "Prepared: $manifestDirectory"
