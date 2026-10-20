#Requires -Version 5.1
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Join-Path $env:TEMP ('KillerBench-S008-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
try {
    $fixture = Join-Path $root 'fixture.dll'
    $wrongVersion = Join-Path $root 'wrong-version.dll'
    $generator = @'
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition '[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")] public sealed class ReleaseArtifactFixture {}' -OutputAssembly (Join-Path $env:KB_RELEASE_FIXTURE_ROOT 'fixture.dll')
Add-Type -TypeDefinition '[assembly:System.Reflection.AssemblyFileVersion("0.0.0.0")] public sealed class WrongReleaseArtifactFixture {}' -OutputAssembly (Join-Path $env:KB_RELEASE_FIXTURE_ROOT 'wrong-version.dll')
'@
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = '-NoLogo -NoProfile -NonInteractive -EncodedCommand ' + [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($generator))
    $start.EnvironmentVariables['KB_RELEASE_FIXTURE_ROOT'] = $root
    $process = [Diagnostics.Process]::Start($start)
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw ('Synthetic PE fixture generation failed: ' + $errorTask.Result) }
    $null = $outputTask.Result
    $process.Dispose()
    foreach ($name in @('KillerBench.exe', 'KillerBench-Portable.exe', 'killerbench-cli.exe')) {
        Copy-Item -LiteralPath $fixture -Destination (Join-Path $root $name)
    }
    $global:KillerBenchS008ReadStoreSignature = $false
    function Get-AuthenticodeSignature {
        param([string]$LiteralPath)
        if ($global:KillerBenchS008ReadStoreSignature -and [IO.Path]::GetFileName($LiteralPath) -eq 'KillerBench-Store.exe') {
            return Microsoft.PowerShell.Security\Get-AuthenticodeSignature -LiteralPath $LiteralPath
        }
        [pscustomobject]@{ Status = [System.Management.Automation.SignatureStatus]::Valid }
    }
    function git { throw 'Read-only artifact preflight invoked Git.' }
    function dotnet { throw 'Read-only artifact preflight invoked a build.' }
    function gh { throw 'Read-only artifact preflight invoked publication.' }
    $rejected = $false
    try { & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root }
    catch {
        if ($_.Exception.Message -notlike '*KillerBench-Store.exe*') { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'Missing Store executable passed the public read-only release preflight.' }
    Write-Host 'PASS: missing Store executable rejected before any build, Git action, or publication.'

    $store = Join-Path $root 'KillerBench-Store.exe'
    Copy-Item -LiteralPath $wrongVersion -Destination $store
    $rejected = $false
    try { & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root }
    catch { if ($_.Exception.Message -notlike '*KillerBench-Store.exe*file version*') { throw }; $rejected = $true }
    if (-not $rejected) { throw 'Wrong-version Store executable passed release preflight.' }
    Copy-Item -LiteralPath $fixture -Destination $store -Force
    $global:KillerBenchS008ReadStoreSignature = $true
    $rejected = $false
    try { & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root }
    catch { if ($_.Exception.Message -notlike '*KillerBench-Store.exe*not validly signed*') { throw }; $rejected = $true }
    if (-not $rejected) { throw 'Unsigned Store executable passed release preflight.' }
    $global:KillerBenchS008ReadStoreSignature = $false
    Write-Host 'PASS: wrong-version and genuinely unsigned Store executables rejected.'

    [IO.File]::WriteAllText((Join-Path $root 'unpublished-extra.exe'), 'unpublished synthetic extra')
    $before = @(Get-ChildItem -LiteralPath $root -File | ForEach-Object { $_.Name + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash } | Sort-Object)
    $output = @(& (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root)
    $expectedNames = @('KillerBench.exe', 'KillerBench-Portable.exe', 'KillerBench-Store.exe', 'killerbench-cli.exe')
    $actualNames = @($output | ForEach-Object { ($_ -split '\s+')[0] })
    if (($actualNames -join '|') -cne ($expectedNames -join '|')) { throw 'Read-only preflight hash inventory omitted or added executable assets.' }
    $storeHash = (Get-FileHash -LiteralPath $store -Algorithm SHA256).Hash
    if (-not ($output | Where-Object { $_ -match ('^KillerBench-Store\.exe\s+' + $storeHash + '$') })) { throw 'Store executable hash was omitted or incorrect.' }
    $after = @(Get-ChildItem -LiteralPath $root -File | ForEach-Object { $_.Name + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash } | Sort-Object)
    if (($before -join '|') -cne ($after -join '|')) { throw 'Read-only preflight changed artifact files.' }
    Write-Host 'PASS: authoritative four-executable inventory includes the Store hash, excludes extra files, and preserves all bytes.'

    [IO.File]::WriteAllLines((Join-Path $root 'SHA256SUMS.txt'), [string[]]@($output | Where-Object { $_ -notmatch '^KillerBench-Store\.exe\s' }))
    $rejected = $false
    try { & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root }
    catch { if ($_.Exception.Message -notlike '*SHA256SUMS.txt*KillerBench-Store.exe*') { throw }; $rejected = $true }
    if (-not $rejected) { throw 'A checksum manifest without the Store executable passed read-only preflight.' }
    Write-Host 'PASS: missing Store checksum rejected by read-only preflight.'
    $badHash = @($output | ForEach-Object { if ($_ -match '^KillerBench-Store\.exe\s') { 'KillerBench-Store.exe ' + ('0' * 64) } else { $_ } })
    [IO.File]::WriteAllLines((Join-Path $root 'SHA256SUMS.txt'), [string[]]$badHash)
    $rejected = $false
    try { & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root }
    catch { if ($_.Exception.Message -notlike '*SHA256SUMS.txt*KillerBench-Store.exe*') { throw }; $rejected = $true }
    if (-not $rejected) { throw 'A stale Store hash passed read-only preflight.' }
    [IO.File]::WriteAllLines((Join-Path $root 'SHA256SUMS.txt'), [string[]]$output)
    $manifestBefore = (Get-FileHash -LiteralPath (Join-Path $root 'SHA256SUMS.txt')).Hash
    $null = & (Join-Path $RepositoryRoot 'release.ps1') -ArtifactPreflightOnly -ArtifactDirectory $root
    if ((Get-FileHash -LiteralPath (Join-Path $root 'SHA256SUMS.txt')).Hash -ne $manifestBefore) { throw 'Read-only preflight changed the valid checksum manifest.' }
    Write-Host 'PASS: stale Store checksum rejected; valid Store checksum accepted without changing the manifest.'
}
finally { Remove-Item -LiteralPath $root -Recurse -Force; Remove-Variable -Name KillerBenchS008ReadStoreSignature -Scope Global -ErrorAction SilentlyContinue }
