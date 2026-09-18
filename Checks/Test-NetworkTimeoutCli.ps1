$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
foreach ($command in @('dns-check localhost', 'tcp-check 127.0.0.1 1')) {
    foreach ($suffix in @('--timeout-seconds', '--timeout-seconds 0', '--timeout-seconds 61', '--timeout-seconds -1', '--timeout-seconds 1.5', '--timeout-seconds 1 --timeout-seconds 2', '--execute', '--timeout-seconds 1')) {
        $start = New-Object Diagnostics.ProcessStartInfo
        $start.FileName = (Get-Command dotnet).Source
        $start.Arguments = '"' + $cli + '" ' + $command + ' ' + $suffix
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.WindowStyle = 'Hidden'
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = New-Object Diagnostics.Process
        $process.StartInfo = $start
        try {
            [void]$process.Start()
            $output = $process.StandardOutput.ReadToEndAsync()
            $errors = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(10000)) { throw 'Network CLI exceeded test observation limit.' }
            $body = $output.GetAwaiter().GetResult()
            $messages = $errors.GetAwaiter().GetResult()
            if ($suffix -eq '--timeout-seconds 1') {
                $result = $body | ConvertFrom-Json
                if ([TimeSpan]::Parse($result.Timeout).TotalSeconds -ne 1) { throw 'Network result lost the selected timeout.' }
                if ($process.ExitCode -notin @(0,3) -or $null -eq $result.State -or [string]::IsNullOrWhiteSpace($result.Detail)) { throw 'Valid timeout lost network result.' }
            } elseif ($process.ExitCode -ne 2 -or $body.Length -ne 0 -or $messages -notmatch 'Usage:') { throw 'Invalid timeout was not rejected before network check.' }
        } finally { $process.Dispose() }
    }
}
'PASS: DNS/TCP timeout arguments rejected or accepted with structured loopback results. No settings changed.'
