#Requires -Version 5.1
# The release archive may differ from the built source only by the README source-link commit. Uses a throwaway
# git repository in the temp folder; nothing is built, signed, pushed or published.
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Join-Path $env:TEMP ('KillerBench-S008-delta-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
try {
    function Commit([string]$message) {
        & git -C $root add -A | Out-Null
        & git -C $root -c user.name=Fixture -c user.email=fixture@example.invalid commit -q -m $message | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Fixture commit failed: $message" }
        (& git -C $root rev-parse HEAD).Trim()
    }
    & git -C $root init -q | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'README.md'), 'source v0.9.0')
    [IO.File]::WriteAllText((Join-Path $root 'Program.cs'), 'class A {}')
    $built = Commit 'built'
    [IO.File]::WriteAllText((Join-Path $root 'README.md'), 'source v1.0.0')
    $readme = Commit 'readme only'
    [IO.File]::WriteAllText((Join-Path $root 'Program.cs'), 'class B {}')
    $product = Commit 'product change'

    function Check([string]$build, [string]$release) {
        & (Join-Path $RepositoryRoot 'release.ps1') -SourceDeltaOnly -SourceRepository $root -BuildCommit $build -ReleaseCommit $release | Out-Null
    }
    Check $built $built
    Check $built $readme
    foreach ($case in @(@($built, $product, 'a product change after the build'), @($readme, $built, 'a release commit that is not a descendant of the build'))) {
        $refused = $false
        try { Check $case[0] $case[1] } catch { if ($_.Exception.Message -notlike '*Rebuild*') { throw }; $refused = $true }
        if (-not $refused) { throw "The release accepted $($case[2])." }
    }
    Write-Host 'PASS: the release archive may differ from the built source only by the README source-link commit.'
}
finally { Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue }
