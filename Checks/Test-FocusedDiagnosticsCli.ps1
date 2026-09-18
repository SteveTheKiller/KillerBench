$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before running focused diagnostic checks.' }
function Invoke-Focused([string]$options, [string]$command = 'common-check') {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" ' + $command + ' ' + $options
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $child = New-Object Diagnostics.Process
    $child.StartInfo = $start
    try {
        if (-not $child.Start()) { throw 'Focused diagnostic did not start.' }
        $output = $child.StandardOutput.ReadToEndAsync()
        $errors = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(60000)) { $child.Kill(); throw 'Focused diagnostic exceeded its test bound.' }
        return [pscustomobject]@{ Exit = $child.ExitCode; Output = $output.GetAwaiter().GetResult(); Error = $errors.GetAwaiter().GetResult() }
    } finally { $child.Dispose() }
}
$result = Invoke-Focused '--scope printerports --scope Vitals --scope PrinterPorts'
$checks = foreach ($check in ($result.Output | ConvertFrom-Json)) { $check }
$vitalsNames = @('Memory snapshot','Physical disk health','System uptime','Processor snapshot','Battery snapshot',
    'System identity','Local Administrators','Windows version','Graphics adapters','Physical memory modules',
    'Windows Memory Diagnostic results')
if (@($checks | Where-Object Name -eq 'Memory snapshot').Count -ne 1 -or
    @($checks | Where-Object Name -eq 'Physical disk health').Count -ne 1 -or
    @($checks | Where-Object { $_.Name -like 'Printer port*' }).Count -lt 1 -or
    @($checks | Where-Object { $_.Name -notlike 'Printer port*' -and $_.Name -notin $vitalsNames }).Count -gt 0) {
    throw 'Focused scan lost selected areas or collected unrelated areas.'
}
if (([regex]::Matches($result.Error, 'Diagnostic area 2 of 2: Printer port registrations\.\.\.')).Count -ne 1 -or
    ([regex]::Matches($result.Error, 'Diagnostic area ')).Count -ne 2) { throw 'Duplicate selection caused repeated or unrelated collection.' }
foreach ($check in $checks) {
    if ($null -eq $check.Evidence -or $null -ne $check.Repair -or [string]::IsNullOrWhiteSpace($check.Detail)) {
        throw 'Focused scan lost evidence or selected a repair.'
    }
}
$expected = if (@($checks | Where-Object { $_.State -notin @('Clear','Information') }).Count -eq 0) { 0 } else { 3 }
if ($result.Exit -ne $expected) { throw 'Focused exit status disagrees with findings.' }
foreach ($invalid in @('--scope', '--scope 0', '--scope InvalidScope', '--scope Vitals,PrinterPorts',
    '--execute', '--scope Vitals --execute', ('--scope Vitals ' * 65))) {
    $rejected = Invoke-Focused $invalid
    if ($rejected.Exit -ne 2 -or $rejected.Output -or $rejected.Error -notmatch 'Usage:' -or $rejected.Error -match 'Diagnostic area ') {
        throw 'Invalid scope options reached collection or failed usage rejection.'
    }
}
Write-Output "PASS: focused CLI returned $($checks.Count) findings from two areas, deduplicated selection and rejected invalid/execution options. No repairs requested."
$workflow = Invoke-Focused '--scope SystemVolumeCapacity --json' 'workflow common-problems'
$run = $workflow.Output | ConvertFrom-Json
if ($workflow.Exit -notin @(0,3) -or @($run.selectedDiagnosticScopes).Count -ne 1 -or
    $run.selectedDiagnosticScopes[0] -ne 'SystemVolumeCapacity' -or @($run.checks).Count -ne 1 -or
    $run.checks[0].Name -ne 'Volume capacity' -or @($run.repairs).Count -ne 0 -or @($run.plannedRecipeIds).Count -ne 0) {
    throw 'Journaled focused workflow expanded its scope or lost the selection.'
}
$saved = Get-Content -LiteralPath $run.journalPath -Raw | ConvertFrom-Json
if ($saved.selectedDiagnosticScopes[0] -ne 'SystemVolumeCapacity' -or $saved.status -ne $run.status -or
    @($saved.repairs).Count -ne 0 -or @($saved.checks).Count -ne 1 -or $null -eq $saved.checks[0].Evidence) {
    throw 'Focused workflow journal lost scope, outcome or evidence.'
}
foreach ($invalid in @('--scope Vitals --auto-repair', '--scope Vitals --recipes windows.system-files', '--scope', '--scope 0')) {
    $rejected = Invoke-Focused $invalid 'workflow common-problems'
    if ($rejected.Exit -ne 2 -or $rejected.Output -or $rejected.Error -match 'Checking this PC') {
        throw 'Invalid focused workflow reached collection.'
    }
}
Write-Output 'PASS: native focused workflow preserved one selected area in its journal with no recipes or repairs; incompatible options rejected.'
$alias = Invoke-Focused '--scope SystemVolumeCapacity --json' 'full-scan'
$aliasRun = $alias.Output | ConvertFrom-Json
if ($alias.Exit -notin @(0,3) -or $aliasRun.kind -ne 'CommonProblems' -or
    $aliasRun.selectedDiagnosticScopes[0] -ne 'SystemVolumeCapacity' -or @($aliasRun.repairs).Count -ne 0) {
    throw 'Full-scan alias did not preserve the focused journaled workflow.'
}
Write-Output 'PASS: full-scan CLI alias uses the same focused journaled workflow with no repairs.'
