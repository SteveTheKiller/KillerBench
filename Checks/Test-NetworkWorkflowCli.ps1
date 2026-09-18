$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = (Get-Command dotnet).Source
$start.Arguments = '"' + $cli + '" workflow network --json'
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
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Read-only network workflow timed out.' }
    $result = $stdout.GetAwaiter().GetResult() | ConvertFrom-Json
    if ($process.ExitCode -notin @(0, 3, 4) -or @($result.Repairs).Count -ne 0 -or
        @($result.Recipes | Where-Object { $null -ne $_.Repair }).Count -ne 0) {
        throw 'Network workflow failed or crossed its read-only boundary.'
    }
    $links = @($result.Checks | Where-Object { $_.Name -like '* / Link' })
    $adapters = @([Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        Where-Object { $_.NetworkInterfaceType -ne 'Loopback' })
    if ($adapters.Count -gt 0 -and $links.Count -eq 0) { throw 'Network link evidence is missing.' }
    foreach ($link in $links) {
        if ($null -eq $link.Evidence -or $link.Repair) { throw 'Link evidence missing or unexpectedly repairable.' }
        if ($link.Evidence.ExitCode -ne 0) {
            if ($link.State -ne 'Unknown' -or -not $link.Evidence.Error) { throw 'Failed link collection was presented as complete.' }
            continue
        }
        $evidence = $link.Evidence.Output | ConvertFrom-Json
        $expected = if ($evidence.Status -eq 'Up') { 'Clear' } elseif ($evidence.Status -eq 'Unknown') { 'Unknown' } else { 'Information' }
        if ($link.State -ne $expected -or $link.Name -ne ($evidence.Name + ' / Link')) { throw 'Link classification contradicts its evidence.' }
    }
    $saved = Get-Content -LiteralPath $result.JournalPath -Raw | ConvertFrom-Json
    $savedLinks = @($saved.Checks | Where-Object { $_.Name -like '* / Link' })
    if ($savedLinks.Count -ne $links.Count) { throw 'Saved report lost link evidence.' }
    for ($i = 0; $i -lt $links.Count; $i++) {
        if ($savedLinks[$i].Evidence.Output -ne $links[$i].Evidence.Output -or $savedLinks[$i].State -ne $links[$i].State -or
            $savedLinks[$i].Evidence.Error -ne $links[$i].Evidence.Error -or $savedLinks[$i].Evidence.ExitCode -ne $links[$i].Evidence.ExitCode) {
            throw 'Saved link evidence differs from CLI output.'
        }
    }
    Write-Output "PASS: read-only network workflow retained $($links.Count) link records in CLI and saved report; zero repairs dispatched."
} finally { $process.Dispose() }
