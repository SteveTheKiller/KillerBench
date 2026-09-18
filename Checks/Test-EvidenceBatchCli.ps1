$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-batch-cli-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$first = Join-Path $scratch 'first.log'
$last = Join-Path $scratch 'last.log'
$missing = Join-Path $scratch 'missing.log'
[IO.File]::WriteAllText($first, '2026-09-12 12:00:00, Info CBS Synthetic evidence')
[IO.File]::WriteAllText($last, '2026-09-12 12:00:00, Info CBS Synthetic second evidence')
$before = (Get-FileHash $first).Hash
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = (Get-Command dotnet).Source
$start.Arguments = '"' + $cli + '" investigate-batch "' + $first + '" "' + $missing + '" "' + $last + '"'
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Hidden'
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$process = New-Object Diagnostics.Process
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Batch CLI did not start.' }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(20000)) { $process.Kill(); throw 'Fixture batch exceeded its bound.' }
    $body = $stdout.GetAwaiter().GetResult()
    $errors = $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 3 -or $errors) { throw "Unexpected batch exit or error: $errors" }
    $rows = @($body -split '\r?\n' | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json })
    if ($rows.Count -ne 3 -or $rows[0].Report.Source -ne $first -or $rows[0].Error -or
        $rows[1].Path -ne $missing -or -not $rows[1].Error -or $null -ne $rows[1].Report -or
        $rows[2].Report.Source -ne $last -or $rows[2].Error) { throw 'Batch JSON Lines lost order or partial-success outcomes.' }
    if ((Get-FileHash $first).Hash -ne $before) { throw 'Batch changed input evidence.' }
    Write-Output 'PASS: batch CLI streams ordered per-file results, continues after a missing file and returns partial-failure status. No repairs.'
} finally { $process.Dispose() }

function Invoke-BatchFixture([string]$arguments) {
    $start.Arguments = '"' + $cli + '" investigate-batch' + $arguments
    $child = New-Object Diagnostics.Process
    $child.StartInfo = $start
    try {
        if (-not $child.Start()) { throw 'Batch fixture did not start.' }
        $output = $child.StandardOutput.ReadToEndAsync()
        $errorOutput = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(20000)) { $child.Kill(); throw 'Batch fixture exceeded its bound.' }
        return [pscustomobject]@{ Exit = $child.ExitCode; Output = $output.GetAwaiter().GetResult(); Error = $errorOutput.GetAwaiter().GetResult() }
    } finally { $child.Dispose() }
}

$complete = Invoke-BatchFixture (' "' + $first + '" "' + $last + '"')
$completeRows = @($complete.Output -split '\r?\n' | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json })
if ($complete.Exit -ne 0 -or $complete.Error -or $completeRows.Count -ne 2 -or
    $completeRows[0].Report.Partial -or $completeRows[1].Report.Partial) { throw 'Complete batch did not return success.' }

$invalidXml = Join-Path $scratch 'broken.xml'
[IO.File]::WriteAllText($invalidXml, '<Events><Event>')
$invalid = Invoke-BatchFixture (' "' + $invalidXml + '" "' + $last + '"')
$invalidRows = @($invalid.Output -split '\r?\n' | Where-Object { $_ } | ForEach-Object { $_ | ConvertFrom-Json })
if ($invalid.Exit -ne 3 -or $invalid.Error -or $invalidRows.Count -ne 2 -or
    -not $invalidRows[0].Error -or $null -ne $invalidRows[0].Report -or
    $invalidRows[1].Report.Source -ne $last) { throw 'Malformed XML prevented later-file investigation.' }

foreach ($arguments in @('', (' "' + $first + '" "capture.kbench"'), ((' "' + $first + '"') * 13))) {
    $rejected = Invoke-BatchFixture $arguments
    if ($rejected.Exit -ne 2 -or $rejected.Output -or $rejected.Error -notmatch 'Usage:') {
        throw 'Invalid batch must be rejected before emitting evidence.'
    }
}
if ((Get-FileHash $first).Hash -ne $before) { throw 'Batch fixtures changed input evidence.' }
Write-Output 'PASS: complete batches, malformed XML continuation and argument preflight have distinct outcomes.'
