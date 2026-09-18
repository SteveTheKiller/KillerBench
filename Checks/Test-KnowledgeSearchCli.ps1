param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before running these checks.' }
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-search-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$file = Join-Path $scratch 'pack.json'
$entries = @(1..75 | ForEach-Object { @{
    Context = 'fixture'; Code = $_.ToString('D3'); Title = 'Synthetic symptom'; Explanation = 'Test reference';
    SourceUrl = 'https://example.com/reference'; VerifiedOn = '2026-09-13'; AppliesTo = 'Fixture product'
} })
$entries += @{ Context = 'other'; Code = '000'; Title = 'Synthetic symptom'; Explanation = 'Other context';
    SourceUrl = 'https://example.com/other'; VerifiedOn = '2026-09-13'; AppliesTo = 'Other product' }
$pack = @{ SchemaVersion = 1; Id = 'fixture'; Revision = 1; PublishedOn = '2026-09-13'; License = 'Synthetic'; Entries = $entries }
[IO.File]::WriteAllText($file, ($pack | ConvertTo-Json -Depth 8))
$before = (Get-FileHash -LiteralPath $file).Hash
function Invoke-Search([string]$Context, [string]$Query, [int]$ExpectedExit, [string]$Extra = '') {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" knowledge-search "' + $file + '" "' + $Context + '" "' + $Query + '"' + $Extra
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Search did not start.' }
        $output = $process.StandardOutput.ReadToEndAsync()
        $errorOutput = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(35000)) { $process.Kill(); throw 'Search exceeded deadline.' }
        $body = $output.GetAwaiter().GetResult()
        $errors = $errorOutput.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Unexpected search exit: $errors $body" }
        if ($ExpectedExit -in @(0, 3)) { return ($body | ConvertFrom-Json) }
        if ($body) { throw 'Rejected arguments produced result data.' }
    } finally { $process.Dispose() }
}
$result = Invoke-Search 'FIXTURE' 'synthetic' 0
if ($result.totalMatches -ne 75 -or -not $result.truncated -or $result.matches.Count -ne 50 -or
    $result.matches[0].code -ne '001' -or $result.matches[49].code -ne '050' -or
    $result.note -notmatch 'no diagnosis or repair authorized') { throw 'Bounded search metadata is incorrect.' }
$other = Invoke-Search 'other' 'synthetic' 0
if ($other.totalMatches -ne 1 -or $other.truncated -or $other.matches[0].context -ne 'other') { throw 'Context isolation failed.' }
$empty = Invoke-Search 'fixture' 'absent' 3
if ($empty.totalMatches -ne 0 -or $empty.matches.Count -ne 0 -or $empty.truncated) { throw 'No-match result is incorrect.' }
Invoke-Search 'fixture' 'synthetic' 2 ' --execute'
if ((Get-FileHash -LiteralPath $file).Hash -ne $before) { throw 'Search modified the pack.' }
Write-Output "PASS: CLI search bounds, context isolation, no-match exit, execution-option rejection and source preservation. Scratch: $scratch"
