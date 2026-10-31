$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$id = [guid]::NewGuid().ToString()
$journal = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KillerBench/BitsRetry'
$pending = Join-Path $journal ($id.Replace('-', '') + '.pending.json')
foreach ($arguments in @('bits-retry', 'bits-retry invalid --execute', "bits-retry $id --all-users", "bits-retry $id --execute --execute", "bits-retry $id")) {
    $preview = $arguments -eq "bits-retry $id"
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" ' + $arguments
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
        if (-not $process.WaitForExit(45000)) { $process.Kill(); throw 'BITS CLI check timed out.' }
        $output = $stdout.GetAwaiter().GetResult()
        $errors = ($stderr.GetAwaiter().GetResult() -replace '(?m)^Native lab: .*\r?\n', '')
        if ($preview) {
            $result = $output | ConvertFrom-Json
            if ($process.ExitCode -ne 3 -or $result.Mode -ne 'Preview' -or $result.Review.JobId -ne $id -or $result.Review.Selected) { throw 'Missing-job preview outcome incorrect.' }
        } elseif ($process.ExitCode -ne 2 -or $output -or $errors -notlike 'Usage: bits-retry*') { throw 'Invalid execution arguments were not rejected.' }
        if (Test-Path $pending) { throw 'Preview or invalid arguments prepared a retry.' }
    } finally { $process.Dispose() }
}
Write-Output 'PASS: BITS CLI rejects invalid execution arguments and previews a missing exact job without creating a retry record; no retry executed.'
