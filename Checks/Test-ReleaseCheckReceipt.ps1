#Requires -Version 5.1
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $RepositoryRoot 'Checks/ReleaseCheckReceipt.ps1')
$fixture = Join-Path ([Environment]::GetFolderPath('UserProfile')) ('scratch/KillerBench-release-receipt-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($fixture)
try {
    & git -C $fixture init -q
    if ($LASTEXITCODE -ne 0) { throw 'Fixture initialization failed.' }
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'original source')
    & git -C $fixture add source.cs
    $output = Join-Path $fixture 'output'
    [void][IO.Directory]::CreateDirectory($output)
    [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), 'output/')
    [IO.File]::WriteAllText((Join-Path $output 'KillerBench.Checks.dll'), 'original binary')
    $receipt = Join-Path $output 'receipt.json'
    function Refuses([scriptblock]$Action) {
        $refused = $false
        try { & $Action } catch { $refused = $true }
        if (-not $refused) { throw 'Invalid verification evidence was accepted.' }
    }
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    $before = Get-ReleaseCheckSnapshot $fixture $output
    Save-ReleaseCheckReceipt $fixture $output $receipt $before
    Assert-ReleaseCheckReceipt $fixture $output $receipt
    Assert-ReleaseCheckReceipt $fixture $output $receipt
    foreach ($shellName in @('powershell.exe', 'pwsh.exe')) {
        $shell = Get-Command $shellName -ErrorAction SilentlyContinue
        if (-not $shell) { continue }
        $helperPath = (Join-Path $RepositoryRoot 'Checks/ReleaseCheckReceipt.ps1').Replace("'", "''")
        $command = ". '$helperPath'; Assert-ReleaseCheckReceipt '$($fixture.Replace("'", "''"))' '$($output.Replace("'", "''"))' '$($receipt.Replace("'", "''"))'"
        & $shell.Source -NoProfile -NonInteractive -Command $command
        if ($LASTEXITCODE -ne 0) { throw "Receipt cannot be shared with $shellName." }
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'changed source')
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    Refuses { Save-ReleaseCheckReceipt $fixture $output $receipt $before }
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'original source')
    Assert-ReleaseCheckReceipt $fixture $output $receipt
    [IO.File]::WriteAllText((Join-Path $fixture 'new-source.cs'), 'new untracked source')
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    [IO.File]::Delete((Join-Path $fixture 'new-source.cs'))
    [IO.File]::WriteAllText((Join-Path $output 'KillerBench.Checks.dll'), 'changed binary')
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    [IO.File]::WriteAllText((Join-Path $output 'KillerBench.Checks.dll'), 'original binary')
    $saved = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    $saved.Status = 'Failed'
    $saved | ConvertTo-Json | Set-Content -LiteralPath $receipt -Encoding UTF8
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    Save-ReleaseCheckReceipt $fixture $output $receipt $before
    $saved = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    $saved.CompletedUtc = [DateTime]::UtcNow.AddDays(-2).ToString('o')
    $saved | ConvertTo-Json | Set-Content -LiteralPath $receipt -Encoding UTF8
    Refuses { Assert-ReleaseCheckReceipt $fixture $output $receipt }
    Write-Host 'PASS: verification can be reused; missing, failed, expired, changed-source, new-source and changed-binary receipts are refused.'
} finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $boundary = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'scratch/'
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath($boundary), [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup boundary failed.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
