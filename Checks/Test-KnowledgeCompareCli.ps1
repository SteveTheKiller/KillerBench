param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-compare-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
function Invoke-Comparison([string[]]$Paths, [int]$ExpectedExit) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" knowledge-compare ' + (($Paths | ForEach-Object { '"' + $_ + '"' }) -join ' ')
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Comparison did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(50000)) { $process.Kill(); throw 'Comparison exceeded its deadline.' }
        $output = $stdout.GetAwaiter().GetResult()
        $errors = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Unexpected comparison result: $output $errors" }
        if ($ExpectedExit -eq 0) {
            if ($errors) { throw "Unexpected comparison error: $errors" }
            return ($output | ConvertFrom-Json)
        }
        if ($output -or -not $errors) { throw 'Failed comparison must report an error without success JSON.' }
    } finally { $process.Dispose() }
}
function New-Entry([string]$Code, [string]$Title) {
    return @{ Context='fixture'; Code=$Code; Title=$Title; Explanation='Synthetic reference'; SourceUrl='https://example.com/reference'; VerifiedOn='2026-09-13'; AppliesTo='Fixture only' }
}
$before = Join-Path $scratch 'before.json'
$after = Join-Path $scratch 'after.json'
$pack = @{ SchemaVersion=1; Id='fixture'; Revision=1; PublishedOn='2026-09-13'; License='Synthetic'; Entries=@((New-Entry 'old' 'Removed'), (New-Entry 'same' 'Original')) }
[IO.File]::WriteAllText($before, ($pack | ConvertTo-Json -Depth 8))
$pack.Revision = 2
$pack.Entries = @((New-Entry 'same' 'Changed')) + @(1..110 | ForEach-Object { New-Entry ('new-{0:D3}' -f $_) 'Added' })
[IO.File]::WriteAllText($after, ($pack | ConvertTo-Json -Depth 8))
$hashes = @((Get-FileHash $before).Hash, (Get-FileHash $after).Hash)
$result = Invoke-Comparison @($before, $after) 0
$comparison = $result.Comparison
if ($comparison.BeforeMetadata.SchemaVersion -ne 1 -or $comparison.AfterMetadata.SchemaVersion -ne 1 -or
    $comparison.BeforeMetadata.License -ne 'Synthetic' -or $comparison.AfterMetadata.License -ne 'Synthetic') {
    throw 'CLI comparison lost pack metadata.'
}
if ($comparison.Added -ne 110 -or $comparison.Removed -ne 1 -or $comparison.Changed -ne 1 -or
    $comparison.Changes.Count -ne 100 -or -not $comparison.Truncated -or
    $comparison.BeforeRevision -ne 1 -or $comparison.AfterRevision -ne 2 -or
    $result.Scope -notmatch 'No pack installed') { throw 'Comparison counts, cap or scope are incorrect.' }
$same = (Invoke-Comparison @($before, $before) 0).Comparison
if ($same.Added -or $same.Removed -or $same.Changed -or $same.Truncated) { throw 'Identical pack differs.' }
Invoke-Comparison @($before) 2
Invoke-Comparison @($before, (Join-Path $scratch 'missing.json')) 1
$invalid = Join-Path $scratch 'invalid.json'
[IO.File]::WriteAllText($invalid, '{')
Invoke-Comparison @($before, $invalid) 1
$pack.Id = 'different'
[IO.File]::WriteAllText($invalid, ($pack | ConvertTo-Json -Depth 8))
Invoke-Comparison @($before, $invalid) 1
if ((Get-FileHash $before).Hash -ne $hashes[0] -or (Get-FileHash $after).Hash -ne $hashes[1]) { throw 'Comparison modified an input pack.' }
Write-Output 'Knowledge comparison CLI checks passed. No pack installed or window opened.'
