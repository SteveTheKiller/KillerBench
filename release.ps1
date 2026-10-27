#Requires -Version 5.1
<#
.SYNOPSIS
    KillerBench release script: check, build, sign, verify, bundle source, and publish.
.DESCRIPTION
    1. Preflight: version sources, CHANGELOG date, ReleaseDate, translations, license copies,
       package vulnerabilities, and a clean default branch that matches origin.
    2. Builds the solution in Release and runs the non-UI checks.
    3. Builds the installer (KillerBench.exe) and portable (KillerBench-Portable.exe) packages with
       Packaging\build-packages.ps1, and publishes the CLI as a framework-dependent win-x64
       single-file executable. The installer checks for the .NET 10 Desktop Runtime; the portable
       package carries its own runtime.
    4. Signs the first-party payload files, repacks both packages so they verify the signed bytes,
       then signs the installer, portable and CLI executables, retrying the timestamp across three
       TSA endpoints, and runs "signtool verify /pa /v" as a post-sign gate.
    5. Builds the GPLv3 source archive from the exact tagged commit. Source always ships
       with the app.
    6. Writes SHA256SUMS.txt, extracts release notes from the CHANGELOG, tags, and creates
       the GitHub release.

    WinGet and Chocolatey are intentionally absent until KillerBench is published. Add them
    from the KillerShell or KillerPDF scripts once the first release exists.

.PARAMETER CertThumbprint
    Preferred. SHA1 thumbprint of the code-signing certificate (40 hex chars, no spaces).
    Run: Get-ChildItem Cert:\CurrentUser\My | Select Thumbprint, Subject

.PARAMETER CertName
    Fallback. CN (Subject) of the certificate in the Windows cert store.
    Ignored when CertThumbprint is supplied.

.PARAMETER SkipSign
    Skip signing for local test builds. Only allowed with -DryRun.

.PARAMETER DryRun
    Build and validate without committing, tagging, or publishing. Requires the README source
    link to already point at this version so the archive can match the prospective tag commit.

.PARAMETER PublishOnly
    Skip build and sign and publish the artifacts already in the release folder
    (use after a completed normal run, so the signed executables are not rebuilt).
    Requires the existing source archive to match the commit being tagged.

.PARAMETER ArtifactPreflightOnly
    Read existing executable versions, signatures, and SHA256 hashes, then return without
    building, signing, changing files, invoking Git, or publishing.

.PARAMETER ArtifactDirectory
    Optional artifact folder for -ArtifactPreflightOnly. Normal releases use the fixed release folder.

.PARAMETER SourceDeltaOnly
    Check that -ReleaseCommit differs from -BuildCommit only by the README source-link change in
    -SourceRepository, then return. Used by Checks\Test-ReleaseSourceDelta.ps1.

.EXAMPLE
    .\release.ps1 -DryRun -SkipSign
.EXAMPLE
    .\release.ps1 -CertThumbprint "AABBCC..."
.EXAMPLE
    .\release.ps1 -CertName "Open Source Developer Stephen Riley"
#>
param(
    [string]$CertThumbprint = "",
    [string]$CertName       = "Open Source Developer Stephen Riley",
    [switch]$SkipSign,
    [switch]$DryRun,
    [switch]$PublishOnly,
    [switch]$ArtifactPreflightOnly,
    [string]$ArtifactDirectory = "",
    [switch]$SourceDeltaOnly,
    [string]$SourceRepository = "",
    [string]$BuildCommit = "",
    [string]$ReleaseCommit = ""
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($SkipSign -and -not $DryRun) {
    throw "-SkipSign is only allowed with -DryRun. Unsigned artifacts cannot be published."
}

$proj        = Join-Path $PSScriptRoot "KillerBench.csproj"
$solution    = Join-Path $PSScriptRoot "KillerBench.slnx"
$cliProj     = Join-Path $PSScriptRoot "Cli\KillerBench.Cli.csproj"
$checksProj  = Join-Path $PSScriptRoot "Checks\KillerBench.Checks.csproj"
$packageBuild = Join-Path $PSScriptRoot "Packaging\build-packages.ps1"
$packageDir  = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\publish"
$payloadDirs = @(
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows\installer-package\payload"),
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows\portable-package\payload"),
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows\store-package\payload")
)
$firstPartyNames = @('KillerBench.exe', 'KillerBench.dll', 'KillerBench.Engine.dll', 'KillerBench.Engine.Core.dll', 'KillerBench.Engine.Windows.dll')
$cliDir      = Join-Path $PSScriptRoot "Cli\bin\Release\net10.0\publish\single-file-win-x64"
$releaseDir  = Join-Path $PSScriptRoot "bin\Release\release"
if ($ArtifactDirectory) {
    if (-not $ArtifactPreflightOnly) { throw "-ArtifactDirectory is only available with -ArtifactPreflightOnly." }
    $releaseDir = [IO.Path]::GetFullPath($ArtifactDirectory)
}
if (($SourceRepository -or $BuildCommit -or $ReleaseCommit) -and -not $SourceDeltaOnly) {
    throw "-SourceRepository, -BuildCommit and -ReleaseCommit are only available with -SourceDeltaOnly."
}
# Records the commit the signed binaries were built from, so publication can prove the archive matches them.
$provenancePath = Join-Path $releaseDir 'build-provenance.json'
$desktopExe  = Join-Path $releaseDir "KillerBench.exe"
$portableExe = Join-Path $releaseDir "KillerBench-Portable.exe"
# The Microsoft Store installer carries its own .NET runtime; the Store downloads it through killerbench.net/store.
$storeExe    = Join-Path $releaseDir "KillerBench-Store.exe"
$cliExe      = Join-Path $releaseDir "killerbench-cli.exe"
$licenseOut  = Join-Path $releaseDir "LICENSE.txt"
$releaseExecutables = @(
    @{ Path = $desktopExe;  Description = 'KillerBench' },
    @{ Path = $portableExe; Description = 'KillerBench Portable' },
    @{ Path = $storeExe;    Description = 'KillerBench' },
    @{ Path = $cliExe;      Description = 'KillerBench CLI' }
)
$releaseExecutablePaths = @($releaseExecutables | ForEach-Object { $_.Path })

# TSA endpoints, tried in order; first success wins.
$tsaList = @(
    "http://timestamp.digicert.com",
    "http://timestamp.sectigo.com",
    "http://ts.ssl.com"
)

# Resolve the repo's default branch instead of hardcoding it, so the same script works across
# the Killer family. origin/HEAD can go stale, so a candidate is only accepted if it still
# exists on the remote. Order: origin/HEAD, then main, then master.
function Get-DefaultBranch {
    $remoteHeads = @(git ls-remote --heads origin 2>$null) |
        ForEach-Object { ($_ -split '\s+')[-1] -replace '^refs/heads/', '' }
    if (-not $remoteHeads) { return $null }

    $candidates = @()
    $originHead = git symbolic-ref --quiet refs/remotes/origin/HEAD 2>$null
    if ($originHead) { $candidates += (($originHead -replace '^refs/remotes/origin/', '').Trim()) }
    foreach ($c in @('main', 'master')) { if ($candidates -notcontains $c) { $candidates += $c } }

    foreach ($c in $candidates) {
        if ($c -and $remoteHeads -contains $c) { return $c }
    }
    return $null
}

function Assert-CleanSyncedBranch {
    $defaultBranch = Get-DefaultBranch
    if (-not $defaultBranch) { throw "Could not determine the default branch from origin" }
    $branch = git rev-parse --abbrev-ref HEAD
    if ($LASTEXITCODE -ne 0) { throw "Could not read the current branch" }
    if ($branch.Trim() -ne $defaultBranch) { throw "On branch '$branch', expected $defaultBranch" }
    $dirty = git status --porcelain
    if ($LASTEXITCODE -ne 0) { throw "Could not read the working tree status" }
    if ($dirty) { throw "Working tree is not clean. Commit your changes first:`n$($dirty -join "`n")" }
    git fetch origin $defaultBranch --quiet
    if ($LASTEXITCODE -ne 0) { throw "Could not fetch origin/$defaultBranch; cannot verify release source" }
    $localHead = (git rev-parse HEAD).Trim()
    $remoteHead = (git rev-parse "refs/remotes/origin/$defaultBranch").Trim()
    if ($localHead -ne $remoteHead) {
        throw "Local $defaultBranch and origin/$defaultBranch differ. The published source must match the release. Push or pull first."
    }
    return $defaultBranch
}

function Assert-ReleaseArtifactVersion([string]$Path, [string]$ExpectedVersion) {
    if (-not [IO.File]::Exists($Path)) { throw "Release artifact not found at: $Path" }
    $actual = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path).FileVersion
    if ($actual -ne $ExpectedVersion) {
        throw "Release artifact $([IO.Path]::GetFileName($Path)) is file version $actual, expected $ExpectedVersion. Rebuild before publishing."
    }
}

function Assert-ExistingReleaseArtifacts([string[]]$Paths, [string]$ExpectedVersion) {
    foreach ($artifact in $Paths) {
        Assert-ReleaseArtifactVersion $artifact $ExpectedVersion
        $signature = Get-AuthenticodeSignature -LiteralPath $artifact
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "Existing artifact $artifact is not validly signed. Run a full release."
        }
    }
}

function Get-ReleaseArtifactHashes([string[]]$Paths) {
    $hashes = @{}
    foreach ($artifact in $Paths) {
        $hashes[[IO.Path]::GetFileName($artifact)] = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash
    }
    return $hashes
}

function Get-ReleaseChecksumLines([string[]]$Paths, [hashtable]$Hashes) {
    foreach ($artifact in $Paths) {
        $name = [IO.Path]::GetFileName($artifact)
        Write-Output "$($name.PadRight(24)) $($Hashes[$name])"
    }
}

function Assert-ReleaseChecksums([string]$Path, [string[]]$Lines) {
    $sumsRaw = Get-Content -LiteralPath $Path -Raw
    foreach ($line in $Lines) {
        $parts = $line -split '\s+'
        $pattern = '(?im)^' + [regex]::Escape($parts[0]) + '\s+' + [regex]::Escape($parts[1]) + '\s*$'
        if ($sumsRaw -notmatch $pattern) {
            throw "SHA256SUMS.txt does not match $($parts[0]). Run a full release build before publishing."
        }
    }
}

function Find-Python {
    foreach ($candidate in @('py', 'python', 'python3')) {
        $cmd = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    throw "Python 3 was not found. It is needed for check-license.py."
}

# The binaries are built from one commit. The tagged source archive may differ from it only by the
# README source-link commit this script makes; anything else means the archive is not the built source.
function Assert-ReleaseSourceDelta([string]$Repository, [string]$Built, [string]$Released) {
    if ($Built -notmatch '^[0-9a-f]{40}$' -or $Released -notmatch '^[0-9a-f]{40}$') { throw "Build or release commit is not a full commit id. Rebuild the release." }
    if ($Built -eq $Released) { return }
    & git -C $Repository merge-base --is-ancestor $Built $Released
    if ($LASTEXITCODE -ne 0) { throw "Release commit $Released does not descend from the built commit $Built. Rebuild the release artifacts." }
    $changed = @(& git -C $Repository diff --name-only $Built $Released)
    if ($LASTEXITCODE -ne 0) { throw "Could not compare the built and release commits. Rebuild the release artifacts." }
    if ($changed.Count -ne 1 -or $changed[0] -ne 'README.md') {
        throw "Product source changed after the build ($($changed -join ', ')). Rebuild the release artifacts from $Released."
    }
}

if ($SourceDeltaOnly) {
    Assert-ReleaseSourceDelta $SourceRepository $BuildCommit $ReleaseCommit
    Write-Host 'PASS: release source matches the built source.'
    return
}

if ($ArtifactPreflightOnly) {
    [xml]$metadata = Get-Content -LiteralPath $proj -Raw
    Assert-ExistingReleaseArtifacts $releaseExecutablePaths ([string]$metadata.Project.PropertyGroup.FileVersion)
    $hashes = Get-ReleaseArtifactHashes $releaseExecutablePaths
    $checksumLines = @(Get-ReleaseChecksumLines $releaseExecutablePaths $hashes)
    $existingSums = Join-Path $releaseDir 'SHA256SUMS.txt'
    if ([IO.File]::Exists($existingSums)) { Assert-ReleaseChecksums $existingSums $checksumLines }
    Write-Output $checksumLines
    Write-Host 'PASS: existing release artifact preflight.'
    return
}

# --- 1. Release metadata preflight ---
Write-Host "`n==> Release metadata preflight..." -ForegroundColor Cyan
$csprojRaw = Get-Content -Path $proj -Raw
if ($csprojRaw -notmatch '<Version>([0-9]+\.[0-9]+\.[0-9]+)</Version>') {
    throw "No <Version>x.y.z</Version> found in KillerBench.csproj"
}
$Version = $Matches[1]
$Tag = "v$Version"
$expectedFileVersion = "$Version.0"
foreach ($field in 'AssemblyVersion', 'FileVersion') {
    if ($csprojRaw -notmatch "<$field>([0-9.]+)</$field>") { throw "No <$field> found in KillerBench.csproj" }
    if ($Matches[1] -ne $expectedFileVersion) {
        throw "KillerBench.csproj <$field> is $($Matches[1]), expected $expectedFileVersion"
    }
}
[xml]$cliMetadata = Get-Content -LiteralPath $cliProj -Raw
$cliVersion = [string]$cliMetadata.Project.PropertyGroup.Version
if ($cliVersion -ne $Version) {
    throw "CLI version $cliVersion does not match app version $Version. Update both before releasing."
}
Write-Host "    Version: $Version (tag $Tag)"

$changelog = Get-Content -Path (Join-Path $PSScriptRoot 'CHANGELOG.md') -Raw
if ($changelog -notmatch [regex]::Escape("## [$Version]")) {
    throw "CHANGELOG.md has no [$Version] section"
}
if ($changelog -match ('(?im)^## \[' + [regex]::Escape($Version) + '\] - UNRELEASED\s*$')) {
    throw "CHANGELOG.md section [$Version] is still marked Unreleased"
}

# The About card shows <ReleaseDate> beside the version so users can tell how old their build
# is. It must equal the date on this version's CHANGELOG section.
if ($csprojRaw -notmatch '<ReleaseDate>([0-9]{4}-[0-9]{2}-[0-9]{2})</ReleaseDate>') {
    throw "No <ReleaseDate>yyyy-MM-dd</ReleaseDate> found in KillerBench.csproj"
}
$releaseDate = $Matches[1]
if ($changelog -notmatch ('## \[' + [regex]::Escape($Version) + '\] - ([0-9]{4}-[0-9]{2}-[0-9]{2})')) {
    throw "CHANGELOG.md section [$Version] has no yyyy-MM-dd date"
}
if ($releaseDate -ne $Matches[1]) {
    throw "csproj <ReleaseDate> is $releaseDate but CHANGELOG [$Version] is dated $($Matches[1]). Bump the csproj."
}
Write-Host "    Release date: $releaseDate"

# --- 2. Git preflight ---
Write-Host "`n==> Git preflight..." -ForegroundColor Cyan
Push-Location $PSScriptRoot
try {
    $defaultBranch = Assert-CleanSyncedBranch
    if (git tag --list $Tag) { throw "Tag $Tag already exists" }
    if (git ls-remote --tags origin $Tag) { throw "Tag $Tag already exists on origin" }
    $releaseCommit = (git rev-parse HEAD).Trim()
    Write-Host "    $defaultBranch at $releaseCommit matches origin"
} finally {
    Pop-Location
}

if (-not $PublishOnly) {

# --- 3. SimplySign preflight ---
if (-not $SkipSign) {
    $ssProc = Get-Process -Name "SimplySignDesktop" -ErrorAction SilentlyContinue
    if (-not $ssProc) {
        Write-Host ""
        Write-Warning "SimplySign Desktop does not appear to be running."
        Write-Host    "    Start it and wait for it to show 'Connected', then press Enter to continue."
        Write-Host    "    Or press Ctrl+C to abort."
        $null = Read-Host
    } else {
        Write-Host "`n==> SimplySign Desktop is running (PID $($ssProc.Id))." -ForegroundColor Green
    }
}

# --- 4. Translation parity ---
# Every localization must carry the complete English key set with matching placeholders, or a
# translation loads fine and then throws when string.Format is handed a value it dropped.
Write-Host "`n==> Checking translations..." -ForegroundColor Cyan

function Read-StringMap([string]$Path) {
    [xml]$document = Get-Content -Path $Path -Raw -Encoding UTF8
    $map = @{}
    foreach ($node in $document.ResourceDictionary.ChildNodes) {
        if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
        $key = $node.GetAttribute('Key', 'http://schemas.microsoft.com/winfx/2006/xaml')
        if ($key) { $map[$key] = [string]$node.InnerText }
    }
    return $map
}

$stringsDir = Join-Path $PSScriptRoot 'Strings'
$englishStrings = Read-StringMap (Join-Path $stringsDir 'en-US.xaml')
if ($englishStrings.Count -eq 0) { throw "English translation file contains no resource keys." }
$localeFiles = @(Get-ChildItem $stringsDir -Filter '*.xaml')
foreach ($localeFile in $localeFiles) {
    if ($localeFile.Name -eq 'en-US.xaml') { continue }
    $localized = Read-StringMap $localeFile.FullName
    $missing = @($englishStrings.Keys | Where-Object { -not $localized.ContainsKey($_) })
    $extra   = @($localized.Keys | Where-Object { -not $englishStrings.ContainsKey($_) })
    $empty   = @($localized.Keys | Where-Object { [string]::IsNullOrWhiteSpace($localized[$_]) })
    $placeholderMismatch = @()
    foreach ($key in $englishStrings.Keys) {
        if (-not $localized.ContainsKey($key)) { continue }
        $englishPlaceholders = @([regex]::Matches($englishStrings[$key], '\{\d+(?::[^}]*)?\}') |
            ForEach-Object Value | Sort-Object)
        $localizedPlaceholders = @([regex]::Matches($localized[$key], '\{\d+(?::[^}]*)?\}') |
            ForEach-Object Value | Sort-Object)
        if ([string]::Join('|', $englishPlaceholders) -ne [string]::Join('|', $localizedPlaceholders)) {
            $placeholderMismatch += $key
        }
    }
    if ($missing.Count -or $extra.Count -or $empty.Count -or $placeholderMismatch.Count) {
        if ($missing.Count)             { Write-Host "    missing: $($missing -join ', ')" -ForegroundColor Yellow }
        if ($extra.Count)               { Write-Host "    extra:   $($extra -join ', ')" -ForegroundColor Yellow }
        if ($empty.Count)               { Write-Host "    empty:   $($empty -join ', ')" -ForegroundColor Yellow }
        if ($placeholderMismatch.Count) { Write-Host "    placeholders: $($placeholderMismatch -join ', ')" -ForegroundColor Yellow }
        throw "$($localeFile.Name) is incomplete: missing=$($missing.Count), extra=$($extra.Count), empty=$($empty.Count), placeholder mismatches=$($placeholderMismatch.Count)"
    }
}
Write-Host "    Translations OK: $($englishStrings.Count) keys across $($localeFiles.Count) languages" -ForegroundColor Green

# --- 5. License copies and publish declarations ---
Write-Host "`n==> Checking license copies..." -ForegroundColor Cyan
$python = Find-Python
& $python (Join-Path $PSScriptRoot 'check-license.py')
if ($LASTEXITCODE -ne 0) { throw "check-license.py failed." }

# --- 6. Vulnerability audit ---
Write-Host "`n==> Auditing package vulnerabilities..." -ForegroundColor Cyan
$auditOutput = @(dotnet list $solution package --vulnerable --include-transitive 2>&1)
$auditExitCode = $LASTEXITCODE
$auditText = ($auditOutput | Out-String).TrimEnd()
if ($auditText) { Write-Host $auditText }
if ($auditExitCode -ne 0) { throw "Package vulnerability audit failed to run." }
if ($auditText -match '(?i)has the following vulnerable packages') {
    throw "Vulnerable packages were found. Update them before releasing."
}

# --- 7. Build and non-UI checks ---
# A red check cannot ship. The UI suites need an isolated desktop and stay a manual step.
Write-Host "`n==> Building the solution..." -ForegroundColor Cyan
dotnet build $solution -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "Release build failed." }
Write-Host "`n==> Running non-UI checks..." -ForegroundColor Cyan
dotnet run --project $checksProj -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Non-UI checks failed. Fix them before releasing." }
Write-Host "    Non-UI checks passed" -ForegroundColor Green

# --- 8. Build the installer and portable packages, and publish the CLI ---
Write-Host "`n==> Building the installer and portable packages..." -ForegroundColor Cyan
& powershell -NoProfile -ExecutionPolicy Bypass -File $packageBuild
if ($LASTEXITCODE -ne 0) { throw "Package build failed." }
Write-Host "`n==> Publishing the CLI executable..." -ForegroundColor Cyan
dotnet publish $cliProj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true `
    -p:DebugType=none -p:DebugSymbols=false -o $cliDir --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed." }

if (-not [IO.Directory]::Exists($releaseDir)) { [void][IO.Directory]::CreateDirectory($releaseDir) }
Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench.exe') -Destination $desktopExe -Force
Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench-Portable.exe') -Destination $portableExe -Force
Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench-Store.exe') -Destination $storeExe -Force
Copy-Item -LiteralPath (Join-Path $cliDir 'killerbench-cli.exe') -Destination $cliExe -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE.txt') -Destination $licenseOut -Force
foreach ($artifact in $releaseExecutablePaths) { Assert-ReleaseArtifactVersion $artifact $expectedFileVersion }
Write-Host "    Installer: $desktopExe" -ForegroundColor Green
Write-Host "    Portable : $portableExe" -ForegroundColor Green
Write-Host "    Store    : $storeExe" -ForegroundColor Green
Write-Host "    CLI      : $cliExe" -ForegroundColor Green

# --- 9. Sign ---
if (-not $SkipSign) {
    Write-Host "`n==> Locating signtool..." -ForegroundColor Cyan
    $signtool = $null
    $kitBase  = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (Test-Path $kitBase) {
        $signtool = Get-ChildItem "$kitBase\*\x64\signtool.exe" -Recurse -ErrorAction SilentlyContinue |
                    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $signtool) { throw "signtool.exe not found. Install the Windows SDK." }
    Write-Host "    $signtool"

    $certArgs = if ($CertThumbprint) {
        Write-Host "`n==> Signing with thumbprint $CertThumbprint..." -ForegroundColor Cyan
        @("/sha1", $CertThumbprint)
    } else {
        Write-Host "`n==> Signing with CN: $CertName..." -ForegroundColor Cyan
        @("/n", $CertName)
    }

    # Sign the first-party payload files before they are compressed into the public packages.
    # Third-party runtime and library files keep their publishers' signatures.
    Write-Host "`n==> Signing first-party payload files..." -ForegroundColor Cyan
    foreach ($payloadDir in $payloadDirs) {
        foreach ($name in $firstPartyNames) {
            $innerFile = Join-Path $payloadDir $name
            if (-not (Test-Path -LiteralPath $innerFile)) { throw "First-party payload file is missing: $innerFile" }
            $innerSigned = $false
            foreach ($tsa in $tsaList) {
                & $signtool sign /fd sha256 /tr $tsa /td sha256 @certArgs `
                    /d "KillerBench Application" /du "https://killerbench.net" /v $innerFile
                if ($LASTEXITCODE -eq 0) { $innerSigned = $true; break }
                Start-Sleep -Seconds 3
            }
            if (-not $innerSigned) { throw "Signing failed on all TSA endpoints: $innerFile" }
        }
    }

    # Signing changes the payload hashes. Repack so each package carries and verifies the signed
    # bytes, and so the installer only accepts a signed download.
    Write-Host "`n==> Repacking the signed payloads..." -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File $packageBuild -RepackOnly -RequireSignature
    if ($LASTEXITCODE -ne 0) { throw "Signed payload repack failed." }
    Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench.exe') -Destination $desktopExe -Force
    Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench-Portable.exe') -Destination $portableExe -Force
    Copy-Item -LiteralPath (Join-Path $packageDir 'KillerBench-Store.exe') -Destination $storeExe -Force

    $signTargets = $releaseExecutables
    foreach ($target in $signTargets) {
        $signed = $false
        foreach ($tsa in $tsaList) {
            Write-Host "    Trying TSA for $([IO.Path]::GetFileName($target.Path)): $tsa"
            & $signtool sign /fd sha256 /tr $tsa /td sha256 @certArgs `
                /d $target.Description /du "https://killerbench.net" /v $target.Path
            if ($LASTEXITCODE -eq 0) { $signed = $true; break }
            Start-Sleep -Seconds 3
        }
        if (-not $signed) { throw "Signing failed on all TSA endpoints: $($target.Path)" }
    }

    Write-Host "`n==> Verifying signature chain (/pa)..." -ForegroundColor Cyan
    foreach ($target in $signTargets) {
        & $signtool verify /pa /v $target.Path
        if ($LASTEXITCODE -ne 0) { throw "signtool verify FAILED for $($target.Path). DO NOT RELEASE." }
        $signature = Get-AuthenticodeSignature -LiteralPath $target.Path
        if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
            throw "Authenticode validation failed for $($target.Path) with status $($signature.Status)."
        }
    }
    Write-Host "    Signature chain OK." -ForegroundColor Green

    try {
        $cert = [System.Security.Cryptography.X509Certificates.X509Certificate]::CreateFromSignedFile($desktopExe)
        $cert2 = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($cert)
        $actualThumb = $cert2.Thumbprint
        $actualCN    = $cert2.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false)
    } catch {
        Write-Warning "    Could not read signer info from the signed EXE: $_"
        $actualThumb = "(unknown)"
        $actualCN    = "(unknown)"
    }
} else {
    Write-Host ""
    Write-Host "  #####################################################" -ForegroundColor Red
    Write-Host "  ##  WARNING: -SkipSign is set. EXE IS NOT SIGNED.  ##" -ForegroundColor Red
    Write-Host "  ##  DO NOT DISTRIBUTE this build as a release.     ##" -ForegroundColor Red
    Write-Host "  #####################################################" -ForegroundColor Red
    $actualThumb = "(not signed)"
    $actualCN    = "(not signed)"
}

# --- 10. CLI smoke test ---
# The desktop app is a GUI and has no headless switch, so the signed CLI proves the shared
# Engine loads on this machine's runtime.
Write-Host "`n==> CLI startup smoke test..." -ForegroundColor Cyan
$cliHelp = @(& $cliExe --help 2>&1)
if ($LASTEXITCODE -ne 0) { throw "killerbench-cli --help failed with exit code $LASTEXITCODE." }
Write-Host "    killerbench-cli --help returned $($cliHelp.Count) lines" -ForegroundColor Green
$buildCommit = $releaseCommit
[IO.File]::WriteAllText($provenancePath, (ConvertTo-Json -Compress @{
    Version = $Version; BuildCommit = $buildCommit; Artifacts = (Get-ReleaseArtifactHashes $releaseExecutablePaths)
}))

} else {
    Write-Host "`n==> PublishOnly: skipping build and sign, using existing artifacts." -ForegroundColor Yellow
    Assert-ExistingReleaseArtifacts $releaseExecutablePaths $expectedFileVersion
    if (-not [IO.File]::Exists($provenancePath)) { throw "Build provenance is missing. Run a full release build before publishing." }
    $provenance = Get-Content -LiteralPath $provenancePath -Raw | ConvertFrom-Json
    if ($provenance.Version -ne $Version) { throw "Build provenance is for $($provenance.Version), not $Version. Run a full release build." }
    $currentHashes = Get-ReleaseArtifactHashes $releaseExecutablePaths
    foreach ($name in $currentHashes.Keys) {
        if ($provenance.Artifacts.$name -ne $currentHashes[$name]) { throw "$name changed after it was built and recorded. Run a full release build." }
    }
    $buildCommit = [string]$provenance.BuildCommit
    $srcZipPath = Join-Path $releaseDir "KillerBench-$Version-src.zip"
    $actualThumb = "(existing signature)"
    $actualCN    = "(existing signature)"
}

# --- 11. README source link and GPLv3 source archive ---
# The README update must be in the commit that the archive and tag identify.
Push-Location $PSScriptRoot
try {
    $readmePath = Join-Path $PSScriptRoot 'README.md'
    $readmeRaw  = [System.IO.File]::ReadAllText($readmePath)
    $readmeNew  = $readmeRaw -replace 'releases/download/v[0-9]+\.[0-9]+\.[0-9]+/KillerBench-[0-9]+\.[0-9]+\.[0-9]+-src\.zip', "releases/download/v$Version/KillerBench-$Version-src.zip"
    if ($readmeNew -ne $readmeRaw) {
        if ($DryRun -or $PublishOnly) {
            throw "README source link is stale for $Tag. Update and sync README before using -DryRun or -PublishOnly."
        }
        Write-Host "    Updating README source link to $Tag"
        [System.IO.File]::WriteAllText($readmePath, $readmeNew)
        git commit README.md -m "v${Version}: point README source link at release" --quiet
        if ($LASTEXITCODE -ne 0) { throw "README source-link commit failed" }
        git push origin $defaultBranch --quiet
        if ($LASTEXITCODE -ne 0) { throw "README source-link commit failed to push" }
    }
    $defaultBranch = Assert-CleanSyncedBranch
    $releaseCommit = (git rev-parse HEAD).Trim()
    Assert-ReleaseSourceDelta $PSScriptRoot $buildCommit $releaseCommit

    Write-Host "`n==> Checking the source archive against $releaseCommit..." -ForegroundColor Cyan
    $srcZipPath = Join-Path $releaseDir "KillerBench-$Version-src.zip"
    if (-not [IO.Directory]::Exists($releaseDir)) { [void][IO.Directory]::CreateDirectory($releaseDir) }
    $candidateZip = Join-Path $releaseDir "KillerBench-$Version-src-$([guid]::NewGuid().ToString('N')).zip"
    try {
        git archive --format=zip --prefix="KillerBench-$Version/" -o $candidateZip $releaseCommit -- . ':(exclude)bench-landing'
        if ($LASTEXITCODE -ne 0) { throw "git archive failed." }
        if ([IO.File]::Exists($srcZipPath)) {
            $existingHash = (Get-FileHash -LiteralPath $srcZipPath -Algorithm SHA256).Hash
            $candidateHash = (Get-FileHash -LiteralPath $candidateZip -Algorithm SHA256).Hash
            if ($existingHash -ne $candidateHash) {
                throw "Existing source archive does not match tag commit $releaseCommit. Remove the stale archive and rerun the full release build."
            }
            Write-Host "    Source archive matches the tag commit: $srcZipPath" -ForegroundColor DarkGray
        } else {
            if ($PublishOnly) { throw "Source archive is missing. Run a full release build before publishing." }
            Move-Item -LiteralPath $candidateZip -Destination $srcZipPath
        }
    } finally {
        if ([IO.File]::Exists($candidateZip)) { Remove-Item -LiteralPath $candidateZip }
    }
} finally {
    Pop-Location
}

if (-not [IO.File]::Exists($srcZipPath)) {
    throw "The exact source archive for $Version is missing: $srcZipPath"
}
$srcZip = Get-Item -LiteralPath $srcZipPath

# --- 12. SHA256SUMS.txt ---
Write-Host "`n==> Computing SHA256 values..." -ForegroundColor Cyan
$artifactHashes = Get-ReleaseArtifactHashes $releaseExecutablePaths
$desktopHash = $artifactHashes[[IO.Path]::GetFileName($desktopExe)]
$portableHash = $artifactHashes[[IO.Path]::GetFileName($portableExe)]
$storeHash = $artifactHashes[[IO.Path]::GetFileName($storeExe)]
$cliHash = $artifactHashes[[IO.Path]::GetFileName($cliExe)]
$srcHash     = (Get-FileHash $srcZip.FullName -Algorithm SHA256).Hash
$sumsPath    = Join-Path $releaseDir "SHA256SUMS.txt"
$lines = [System.Collections.Generic.List[string]]::new()
foreach ($line in @(Get-ReleaseChecksumLines $releaseExecutablePaths $artifactHashes)) { $lines.Add($line) }
$lines.Add("$($srcZip.Name.PadRight(24)) $srcHash")
if ($PublishOnly -and (Test-Path $sumsPath)) {
    Assert-ReleaseChecksums $sumsPath $lines
} else {
    [System.IO.File]::WriteAllLines($sumsPath, $lines, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "`n==============================================================" -ForegroundColor Cyan
Write-Host   "  KillerBench $Tag release artifacts" -ForegroundColor White
Write-Host   "  APP    : $desktopExe"
Write-Host   "  PORTABLE: $portableExe"
Write-Host   "  STORE  : $storeExe"
Write-Host   "  CLI    : $cliExe"
Write-Host   "  SOURCE : $($srcZip.FullName)"
Write-Host   ""
Write-Host   "  SHA256 (App)   : $desktopHash" -ForegroundColor Green
Write-Host   "  SHA256 (Portable): $portableHash" -ForegroundColor Green
Write-Host   "  SHA256 (Store) : $storeHash" -ForegroundColor Green
Write-Host   "  SHA256 (CLI)   : $cliHash" -ForegroundColor Green
Write-Host   "  SHA256 (Source): $srcHash" -ForegroundColor Green
Write-Host   ""
Write-Host   "  Signer     : $actualCN"
Write-Host   "  Thumbprint : $actualThumb"
Write-Host "==============================================================" -ForegroundColor Cyan

# --- 13. Publish ---
Push-Location $PSScriptRoot
try {
    $defaultBranch = Assert-CleanSyncedBranch

    $tagCommit = (git rev-parse HEAD).Trim()
    if ($tagCommit -ne $releaseCommit) {
        throw "HEAD changed after the source archive was verified. Rebuild the release artifacts before tagging."
    }

    # Prose claims no other gate reaches: compare the interface language count to the copy.
    $localeCount = @(Get-ChildItem (Join-Path $PSScriptRoot 'Strings') -Filter '*.xaml').Count
    $numberWords = @{
        'seventeen' = 17; 'eighteen' = 18; 'nineteen' = 19; 'twenty' = 20; 'twenty-one' = 21
    }
    $claimPages = @('README.md', 'bench-landing\index.html', 'bench-landing\about.html') |
        ForEach-Object { Join-Path $PSScriptRoot $_ } | Where-Object { Test-Path $_ }
    foreach ($page in $claimPages) {
        foreach ($hit in (Select-String -Path $page -Pattern '(?i)\b([\w-]+)\s+(?:interface\s+)?languages\b' -AllMatches)) {
            foreach ($m in $hit.Matches) {
                $word = $m.Groups[1].Value.ToLower()
                $claimed = if ($numberWords.ContainsKey($word)) { $numberWords[$word] }
                           elseif ($word -match '^\d+$') { [int]$word } else { $null }
                if ($null -ne $claimed -and $claimed -ne $localeCount) {
                    Write-Warning "$(Split-Path $page -Leaf):$($hit.LineNumber) says '$($m.Value)' but $localeCount locales ship."
                }
            }
        }
    }

    # Release notes from the CHANGELOG section
    Write-Host "`n==> Extracting release notes from CHANGELOG.md..." -ForegroundColor Cyan
    $clLines = Get-Content -Path (Join-Path $PSScriptRoot 'CHANGELOG.md') -Encoding UTF8
    $notes = New-Object System.Collections.Generic.List[string]
    $inSection = $false
    foreach ($line in $clLines) {
        if ($line -match "^## \[$([regex]::Escape($Version))\]") { $inSection = $true; continue }
        if ($inSection -and $line -match '^## \[') { break }
        if ($inSection) { $notes.Add($line) }
    }
    if ($notes.Count -eq 0) { throw "Could not extract [$Version] notes from CHANGELOG.md" }
    $notes.Add('')
    $notes.Add('KillerBench.exe installs the app and checks for the .NET 10 Desktop Runtime (x64). KillerBench-Portable.exe runs without installing and carries its own runtime. killerbench-cli.exe requires the .NET 10 Runtime (x64). The complete GPLv3 source for this release is attached as ' + $srcZip.Name + '.')
    $notesFile = Join-Path $env:TEMP "KillerBench-$Version-notes.md"
    [System.IO.File]::WriteAllLines($notesFile, $notes, [System.Text.UTF8Encoding]::new($false))
    Write-Host "    Notes written to $notesFile ($($notes.Count) lines)"

    if ($DryRun) {
        Write-Host "`n==> DryRun: stopping before tag and release." -ForegroundColor Yellow
        $executableNames = @($releaseExecutablePaths | ForEach-Object { [IO.Path]::GetFileName($_) }) -join ', '
        Write-Host "    Would tag $Tag and publish $executableNames, LICENSE.txt, $($srcZip.Name) and SHA256SUMS.txt."
        exit 0
    }

    Write-Host "`n==> Tagging $Tag..." -ForegroundColor Cyan
    git tag -a $Tag -m "KillerBench $Tag"
    if ($LASTEXITCODE -ne 0) { throw "Creating tag $Tag failed (git exit $LASTEXITCODE). Nothing was pushed or published." }
    git push origin $Tag
    if ($LASTEXITCODE -ne 0) {
        throw ("Tag push failed (git exit $LASTEXITCODE). The local tag $Tag was created but not pushed, and the next run " +
            "refuses an existing tag. Delete it with: git tag -d $Tag")
    }

    Write-Host "`n==> Creating GitHub release..." -ForegroundColor Cyan
    $assets = @($releaseExecutablePaths) + @($licenseOut, $srcZip.FullName, $sumsPath)
    gh release create $Tag @assets --title "KillerBench $Tag" --notes-file $notesFile --verify-tag
    if ($LASTEXITCODE -ne 0) { throw "gh release create failed" }

    Write-Host "`n==> Release $Tag published:" -ForegroundColor Green
    gh release view $Tag --json url --jq '.url'
    Write-Host "    killerbench.net does NOT auto-deploy. Drag bench-landing/ into Cloudflare Pages." -ForegroundColor Yellow
} finally {
    Pop-Location
}
