$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$id = [guid]::NewGuid().ToString()
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = (Get-Command dotnet).Source
$start.Arguments = '"' + $cli + '" bits-job-review ' + $id + ' --observe'
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Hidden'
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$process = New-Object Diagnostics.Process
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Observation did not start.' }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(100000)) { $process.Kill(); throw 'Observation exceeded its deadline.' }
    $output = $stdout.GetAwaiter().GetResult()
    $messages = $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 3) { throw "Missing-job observation returned unexpected exit $($process.ExitCode): $messages" }
    $result = $output | ConvertFrom-Json
    if ($result.Comparison.JobId -ne $id -or $result.Comparison.Comparable -or
        $null -ne $result.Comparison.BytesAdvanced -or $null -ne $result.Comparison.Before.Selected -or
        $null -ne $result.Comparison.After.Selected) { throw 'Missing job was interpreted as a comparable transfer.' }
    if ($null -eq $result.BeforeEvidence -or $null -eq $result.AfterEvidence -or
        -not $result.FirstCollected -or -not $result.SecondCollected) { throw 'Observation evidence or timestamps missing.' }
    foreach ($review in @($result.Comparison.Before, $result.Comparison.After)) {
        if (($review.Notes -join ' ') -notmatch 'absence does not establish completion') { throw 'Missing-job limitation was lost.' }
    }
    if ($messages -notmatch 'No retry, completion or cancellation requested') { throw 'Read-only scope missing.' }
    Write-Output 'PASS: native read-only BITS observation retained two snapshots and reported a missing job as not comparable. No transfer changes requested.'
} finally { $process.Dispose() }
