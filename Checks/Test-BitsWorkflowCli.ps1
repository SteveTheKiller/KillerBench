$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = (Get-Command dotnet).Source
$start.Arguments = '"' + $cli + '" workflow background-transfers --json'
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Hidden'
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$process = New-Object Diagnostics.Process
$process.StartInfo = $start
try {
    $null = $process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'Read-only BITS workflow timed out.' }
    $result = $stdout.GetAwaiter().GetResult() | ConvertFrom-Json
    if (@($result.Checks).Count -lt 1 -or @($result.Repairs).Count -ne 0 -or @($result.Recipes).Count -ne 0 -or
        @($result.Checks | Where-Object Repair).Count -ne 0) {
        throw 'BITS workflow scope or non-repair boundary changed.'
    }
    $check = @($result.Checks | Where-Object Name -eq 'BITS transfer jobs')
    if ($check.Count -ne 1) { throw 'BITS transfer inventory evidence is missing or duplicated.' }
    $check = $check[0]
    if ($null -eq $check.Evidence -or $check.Repair -or -not $check.Detail) { throw 'BITS evidence was not retained.' }
    $review = @($result.Checks | Where-Object { $_.State -in @('Repairable', 'Attention', 'Unknown') }).Count -gt 0
    $failedClear = @($result.Checks | Where-Object {
        $_.State -eq 'Clear' -and ($_.Evidence.ExitCode -ne 0 -or $_.Evidence.TimedOut -or $_.Evidence.Truncated -or $_.Evidence.Error)
    }).Count -gt 0
    $expected = if ($result.Status -eq 'Needs attention' -or $review -or $failedClear) { 3 } else { 0 }
    if ($process.ExitCode -ne $expected) { throw 'Workflow exit code contradicts collected evidence.' }
    if (-not (Test-Path -LiteralPath $result.JournalPath)) { throw 'Workflow journal is missing.' }
    $saved = Get-Content -LiteralPath $result.JournalPath -Raw | ConvertFrom-Json
    $savedCheck = @($saved.Checks | Where-Object Name -eq 'BITS transfer jobs')
    if ($savedCheck.Count -ne 1 -or $savedCheck[0].Evidence.Output -ne $check.Evidence.Output -or $saved.Status -ne $result.Status) {
        throw 'Saved workflow evidence differs from the CLI result.'
    }
    Write-Output "PASS: read-only BITS workflow retained its evidence and journal; exit $expected agrees with its result. No transfer changes requested."
} finally { $process.Dispose() }
