$ErrorActionPreference = 'Stop'
$cli = Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build KillerBench.Cli first.' }
$cases = @(
    @{ Arguments = @('monthly','2024-02-01','08:30','UTC','last'); Exit = 0; First = '2024-02-29T08:30:00'; State = 'Unique' },
    @{ Arguments = @('monthly','2026-02-01','08:30','UTC','31'); Exit = 0; First = '2026-03-31T08:30:00'; State = 'Unique' },
    @{ Arguments = @('daily','2026-03-08','02:30','Pacific Standard Time'); Exit = 3; State = 'Invalid' },
    @{ Arguments = @('daily','2026-11-01','01:30','Pacific Standard Time'); Exit = 3; State = 'Ambiguous' },
    @{ Arguments = @('weekly','2026-02-01','08:30','UTC','Bad'); Exit = 2 },
    @{ Arguments = @('weekly','2026-02-01','08:30','UTC','Mon,Mon'); Exit = 2 },
    @{ Arguments = @('monthly','2026-02-01','08:30','UTC','32'); Exit = 2 },
    @{ Arguments = @('daily','2026-02-30','08:30','UTC'); Exit = 2 },
    @{ Arguments = @('daily','2026-02-01','25:30','UTC'); Exit = 2 },
    @{ Arguments = @('daily','2026-02-01','08:30','KillerBench-Unknown-Zone'); Exit = 2 }
)
foreach ($case in $cases) {
    $start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet).Source)
    $start.ArgumentList.Add($cli)
    $start.ArgumentList.Add('schedule-preview')
    foreach ($argument in $case.Arguments) { $start.ArgumentList.Add($argument) }
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(10000)) { $process.Kill($true); throw 'Schedule preview exceeded ten seconds.' }
        $output = $stdout.GetAwaiter().GetResult()
        $errorText = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $case.Exit) { throw "Unexpected exit for $($case.Arguments): $errorText" }
        if ($case.Exit -eq 2) {
            if ($output.Length -ne 0) { throw 'Invalid schedule produced a preview.' }
            continue
        }
        $preview = $output | ConvertFrom-Json
        if (-not $preview.previewOnly -or $preview.taskRegistered -or $preview.dates.Count -ne 5) { throw 'Preview contract failed.' }
        if ($preview.dates[0].state -ne $case.State) { throw 'Incorrect local-time state.' }
        if ($case.First -and ([datetime]$preview.dates[0].localTime).ToString('s') -ne $case.First) { throw 'Incorrect first date.' }
        if ($case.State -eq 'Invalid' -and $preview.dates[0].candidates.Count -ne 0) { throw 'Invalid local time has candidates.' }
        if ($case.State -eq 'Ambiguous' -and $preview.dates[0].candidates.Count -ne 2) { throw 'Repeated local time lost a candidate.' }
    }
    finally { $process.Dispose() }
}
Write-Output 'PASS: ten CLI schedule cases, leap dates, DST and rejected inputs; no tasks registered.'
