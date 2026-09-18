$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-journal-cli-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$path = Join-Path $scratch 'workflow.json'
function Invoke-Review([string]$argument) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" workflow-journal' + $argument
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $child = New-Object Diagnostics.Process
    $child.StartInfo = $start
    try {
        if (-not $child.Start()) { throw 'Review process did not start.' }
        $output = $child.StandardOutput.ReadToEndAsync()
        $errors = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(20000)) { $child.Kill(); throw 'Review fixture exceeded its bound.' }
        return [pscustomobject]@{ Exit = $child.ExitCode; Output = $output.GetAwaiter().GetResult(); Error = $errors.GetAwaiter().GetResult() }
    } finally { $child.Dispose() }
}
foreach ($status in @('Running', 'Stopped', 'Needs attention')) {
    $body = @{ kind = 'Vitals'; machine = 'Fixture'; status = $status; startedUtc = '2026-09-13T00:00:00Z'; checks = @(); repairs = @(); journalPath = 'not-followed' } | ConvertTo-Json
    [IO.File]::WriteAllText($path, $body)
    $before = (Get-FileHash $path).Hash
    $result = Invoke-Review (' "' + $path + '"')
    if ($result.Exit -ne 0 -or $result.Error) { throw 'Historical review failed.' }
    $review = $result.Output | ConvertFrom-Json
    if ($review.recordedStatus -ne $status -or $review.scope -notmatch 'Historical file contents only' -or
        $review.record.journalPath -ne 'not-followed' -or $review.source -ne $path -or (Get-FileHash $path).Hash -ne $before) {
        throw 'Review lost historical scope, altered input or followed recorded path.'
    }
    $exportPath = Join-Path $scratch 'exported-review.json'
    $review.recordedStatus = 'Misleading wrapper status'
    $review.machine = 'Misleading wrapper machine'
    [IO.File]::WriteAllText($exportPath, ($review | ConvertTo-Json -Depth 32))
    $exportHash = (Get-FileHash $exportPath).Hash
    $reopened = Invoke-Review (' "' + $exportPath + '"')
    if ($reopened.Exit -ne 0 -or $reopened.Error) { throw 'Exported review did not reopen.' }
    $roundtrip = $reopened.Output | ConvertFrom-Json
    if ($roundtrip.recordedStatus -ne $status -or $roundtrip.machine -ne 'Fixture' -or
        $roundtrip.source -ne $exportPath -or (Get-FileHash $exportPath).Hash -ne $exportHash) {
        throw 'Wrapper metadata overrode the retained record or exported evidence changed.'
    }
}
foreach ($bad in @('{}', '{', '[]')) {
    [IO.File]::WriteAllText($path, $bad)
    $result = Invoke-Review (' "' + $path + '"')
    if ($result.Exit -ne 1 -or $result.Output -or $result.Error -notmatch 'Journal review unavailable') { throw 'Invalid journal did not produce controlled failure.' }
}
$usage = Invoke-Review ''
if ($usage.Exit -ne 2 -or $usage.Output) { throw 'Missing path did not produce usage error.' }
Write-Output 'PASS: journal CLI preserves historical statuses and source files, rejects malformed files and distinguishes read success from health.'
