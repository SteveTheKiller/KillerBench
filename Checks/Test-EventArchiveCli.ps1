param(
    [Parameter(Mandatory = $true)][string]$Archive,
    [string]$Configuration = 'Debug'
)
$ErrorActionPreference = 'Stop'
$archivePath = (Resolve-Path -LiteralPath $Archive).Path
if ([IO.Path]::GetExtension($archivePath) -ine '.evtx') { throw 'Supply an existing EVTX archive.' }
$before = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
$start = [Diagnostics.ProcessStartInfo]::new((Get-Command dotnet -ErrorAction Stop).Source)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.Arguments = '"' + $cli + '" investigate "' + $archivePath + '" --json'
$process = [Diagnostics.Process]::new()
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Archive investigation did not start.' }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(30000)) {
        $process.Kill()
        throw 'Archive investigation exceeded its deadline.'
    }
    $body = $stdout.GetAwaiter().GetResult()
    $errors = $stderr.GetAwaiter().GetResult()
    if ($process.ExitCode -notin @(0, 3)) { throw "Archive investigation failed: $errors" }
    $report = $body | ConvertFrom-Json
    $expectedExit = if ($report.Partial) { 3 } else { 0 }
    if ($process.ExitCode -ne $expectedExit) { throw 'Archive exit code disagrees with its coverage state.' }
    if ($report.Source -ne $archivePath -or $report.Partial -isnot [bool] -or
        $report.ParsedLines -lt 0 -or $report.ParsedLines -gt 200 -or
        @($report.Timeline).Count -ne $report.ParsedLines -or $report.Limitation -notmatch 'no date filter') {
        throw 'Archive source, record bounds or coverage metadata was lost.'
    }
    foreach ($finding in $report.Findings) {
        foreach ($reference in $finding.Evidence) {
            if ($reference.LineNumber -lt 1 -or $reference.LineNumber -gt $report.LinesAnalyzed) {
                throw 'Finding points outside retained archive evidence.'
            }
        }
    }
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $before) {
        throw 'Source archive changed during investigation.'
    }
    Write-Output "PASS: archive preserved; $($report.ParsedLines) parsed records; partial=$($report.Partial); source and finding references retained."
}
finally { $process.Dispose() }
