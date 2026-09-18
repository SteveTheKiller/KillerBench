param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before running this check.' }

function Invoke-KillerBenchCli([string]$Arguments) {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
    $start.Arguments = '"' + $cli + '" ' + $Arguments
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    if (-not $process.Start()) { throw "$Arguments did not start." }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(45000)) {
        $process.Kill()
        throw "$Arguments exceeded the check time limit."
    }
    [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output = $stdout.GetAwaiter().GetResult()
        Error = $stderr.GetAwaiter().GetResult()
    }
}

foreach ($case in @(
    [pscustomobject]@{ Command = 'vpn-profiles'; Name = 'Current-account VPN profiles'; Scope = 'No connection or setting changed' },
    [pscustomobject]@{ Command = 'winrm-listeners'; Name = 'WinRM listeners'; Scope = 'No settings changed' }
)) {
    $result = Invoke-KillerBenchCli $case.Command
    $checks = @($result.Output | ConvertFrom-Json)
    if ($checks.Count -lt 1 -or $checks[0].Name -ne $case.Name -or $checks[0].Detail -notmatch $case.Scope) {
        throw "$($case.Command) lost its identity or read-only boundary."
    }
    if (@($checks | Where-Object { $_.Repair -ne $null }).Count -ne 0) {
        throw "$($case.Command) returned repair eligibility."
    }
    $expectedExit = if (@($checks | Where-Object State -ne 'Information').Count -eq 0) { 0 } else { 3 }
    if ($result.ExitCode -ne $expectedExit) { throw "$($case.Command) exit code disagrees with its evidence." }

    $rejected = Invoke-KillerBenchCli ($case.Command + ' --execute')
    if ($rejected.ExitCode -ne 2 -or $rejected.Output.Length -ne 0 -or $rejected.Error -notmatch 'Usage:') {
        throw "$($case.Command) accepted an execution option."
    }
}

Write-Output 'PASS: focused VPN profile and WinRM listener CLI diagnostics retain read-only evidence and reject execution options.'
