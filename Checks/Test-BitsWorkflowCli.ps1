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
    if (@($result.Checks).Count -ne 1 -or @($result.Repairs).Count -ne 0 -or @($result.Recipes).Count -ne 0) {
        throw 'BITS workflow scope or non-repair boundary changed.'
    }
    $check = $result.Checks[0]
    if ($null -eq $check.Evidence -or $check.Repair -or -not $check.Detail) { throw 'BITS evidence was not retained.' }
    $expected = if ($check.State -in @('Clear', 'Information')) { 0 } else { 3 }
    if ($process.ExitCode -ne $expected) { throw 'Workflow exit code contradicts collected evidence.' }
    if (-not (Test-Path -LiteralPath $result.JournalPath)) { throw 'Workflow journal is missing.' }
    $saved = Get-Content -LiteralPath $result.JournalPath -Raw | ConvertFrom-Json
    if ($saved.Checks[0].Evidence.Output -ne $check.Evidence.Output -or $saved.Status -ne $result.Status) {
        throw 'Saved workflow evidence differs from the CLI result.'
    }
    Write-Output "PASS: read-only BITS workflow retained its evidence and journal; exit $expected agrees with its result. No transfer changes requested."
} finally { $process.Dispose() }
