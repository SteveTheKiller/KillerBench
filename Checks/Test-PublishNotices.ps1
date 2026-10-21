#Requires -Version 5.1
# Publishes the desktop app into a temporary folder and requires every shipped font's
# unmodified license notice beside the executable. Nothing is signed or uploaded.
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Join-Path $env:TEMP ('KillerBench-notices-' + [guid]::NewGuid().ToString('N'))
try {
    & dotnet publish (Join-Path $RepositoryRoot 'KillerBench.csproj') -c Release -r win-x64 --self-contained false `
        -p:PublishSingleFile=false -p:PublishDir="$root\" -v q -nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }
    if (-not (Test-Path -LiteralPath (Join-Path $root 'KillerBench.exe'))) { throw 'Publish produced no KillerBench.exe.' }
    foreach ($notice in @('KillerSans-OFL.txt', 'Lacquer-OFL.txt', 'Punktype-LICENSE.txt')) {
        $published = Join-Path $root $notice
        if (-not (Test-Path -LiteralPath $published)) { throw "Published payload is missing the font notice $notice." }
        $source = Join-Path $RepositoryRoot (Join-Path 'Fonts' $notice)
        if ((Get-FileHash -LiteralPath $published).Hash -ne (Get-FileHash -LiteralPath $source).Hash) {
            throw "Published font notice $notice differs from Fonts\$notice."
        }
    }
    Write-Host 'PASS: the published desktop payload carries the unmodified Killer Sans, Lacquer and Punktype notices beside the executable.'
}
finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
