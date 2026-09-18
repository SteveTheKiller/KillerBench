param([switch]$AllUsers, [switch]$IncludeProvisioned)
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
foreach ($repairFlag in @($false, $true)) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
    $start.Arguments = '"' + $cli + '" workflow app-packages --json' + $(if ($repairFlag) { ' --auto-repair' } else { '' })
    if ($AllUsers) { $start.Arguments += ' --all-users' }
    if ($IncludeProvisioned) { $start.Arguments += ' --include-provisioned' }
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Package workflow did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(90000)) { $process.Kill(); throw 'Package workflow exceeded its test limit.' }
        $body = $stdout.GetAwaiter().GetResult()
        $messages = $stderr.GetAwaiter().GetResult()
        if ($repairFlag) {
            if ($process.ExitCode -ne 2 -or $body.Length -ne 0 -or $messages -notmatch 'read-only') { throw 'Package workflow accepted auto-repair.' }
            continue
        }
        $report = $body | ConvertFrom-Json
        if ($report.Kind -ne 'AppPackages' -or @($report.Repairs).Count -ne 0 -or @($report.PlannedRecipeIds).Count -ne 0) { throw 'Package workflow returned invalid scope or repairs.' }
        $summary = @($report.Checks | Where-Object Name -eq 'Registered app packages')
        if ($summary.Count -ne 1 -or $null -eq $summary[0].Evidence) { throw 'Package workflow lost raw evidence.' }
        $provisioned = @($report.Checks | Where-Object Name -eq 'Provisioned app packages')
        if ($IncludeProvisioned -and ($provisioned.Count -ne 1 -or $null -eq $provisioned[0].Evidence)) { throw 'Provisioned evidence was lost.' }
        if (-not $IncludeProvisioned -and $provisioned.Count -ne 0) { throw 'Provisioned collection ran without selection.' }
        $expectedScope = if ($AllUsers) { 'AllUsers' } else { 'CurrentAccount' }
        if ($summary[0].Detail -notmatch [regex]::Escape("scope: $expectedScope.")) { throw 'Package workflow used the wrong account scope.' }
        if (@($report.Checks | Where-Object { $null -ne $_.Repair }).Count -ne 0) { throw 'Package inventory suggested a repair.' }
        $saved = Get-Content -LiteralPath $report.JournalPath -Raw | ConvertFrom-Json
        if ($saved.Kind -ne $report.Kind -or $saved.Status -ne $report.Status -or @($saved.Checks).Count -ne @($report.Checks).Count) { throw 'Saved package outcome differs from CLI output.' }
        $expected = if (@($report.Checks | Where-Object { $_.State -in @('Unknown', 'Attention') }).Count -gt 0) { 3 } else { 0 }
        if ($process.ExitCode -ne $expected) { throw 'Package exit code differs from coverage.' }
        Write-Output "Package workflow: $(@($report.Checks).Count) evidence rows, exit $expected; journal verified."
    }
    finally { $process.Dispose() }
}
Write-Output 'Package auto-repair flag rejected; no packages changed.'
