param([ValidateSet('Build', 'Checks')][string]$Mode = 'Build')
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../KillerBench.slnx'))
if ($Mode -ne 'Build') { $project = Join-Path $PSScriptRoot 'KillerBench.Checks.csproj' }
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.Arguments = if ($Mode -eq 'Build') { 'build "' + $project + '" --nologo -v:q' } else { 'run --project "' + $project + '"' }
$process = New-Object System.Diagnostics.Process
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Validation process did not start.' }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $output = $stdout.GetAwaiter().GetResult()
    $errors = $stderr.GetAwaiter().GetResult()
    if ($output) { Write-Output $output }
    if ($errors) { Write-Output $errors }
    if ($process.ExitCode -ne 0) { throw "Validation failed with exit code $($process.ExitCode)." }
}
finally { $process.Dispose() }
