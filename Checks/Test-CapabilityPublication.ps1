#Requires -Version 7.0
param(
    [Parameter(Mandatory = $true)][string]$PublishedCli,
    [Parameter(Mandatory = $true)][string]$SharedAssemblyDirectory,
    [string]$OtherHost
)
$ErrorActionPreference = 'Stop'

function Invoke-ReadOnlyCli([string]$Image, [string[]]$Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = [IO.Path]::GetFullPath($Image)
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Published CLI did not start.' }
        $output = $process.StandardOutput.ReadToEndAsync()
        $errorOutput = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); throw 'Read-only publication smoke check timed out.' }
        $text = $output.GetAwaiter().GetResult()
        $errorText = $errorOutput.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { throw ('Read-only publication smoke check failed: ' + $errorText) }
        return $text
    } finally { $process.Dispose() }
}

$expectedFiles = @('KillerBench.Engine', 'KillerBench.Engine.Core', 'KillerBench.Engine.Windows')
[string[]]$lines = foreach ($name in $expectedFiles) {
    $path = Join-Path $SharedAssemblyDirectory ($name + '.dll')
    $digest = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $name + ':' + $digest
}
[Array]::Sort($lines, [StringComparer]::Ordinal)
$bytes = [Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $lines))
$expected = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
$help = Invoke-ReadOnlyCli $PublishedCli @('--help')
if ($help -notmatch 'KillerBench') { throw 'Published CLI help was not returned.' }
$plan = (Invoke-ReadOnlyCli $PublishedCli @('validation-plan', '--recipe', 'hardware.memory-diagnostic')) | ConvertFrom-Json
if ($plan.executionPerformed -ne $false -or @($plan.recipes).Count -ne 1 -or @($plan.recipes[0].implementations).Count -eq 0) { throw 'Published CLI did not return the expected read-only plan.' }
foreach ($implementation in $plan.recipes[0].implementations) {
    if ($implementation.implementationSha256 -ne $expected) { throw 'Published CLI fingerprint does not match the exact three shared DLL images.' }
}
if ($OtherHost) {
    $actual = (Invoke-ReadOnlyCli $OtherHost @()).Trim()
    if ($actual -ne $expected) { throw 'The same shared images received a different identity in another host.' }
}
Write-Output ('PASS: published CLI help and read-only declarative plan; exact shared image SHA-256 ' + $expected + '. No machine changes or UI.')
