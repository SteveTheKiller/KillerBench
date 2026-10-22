#Requires -Version 5.1
# Runs the native service validation wrapper against a fake CLI that changes nothing. The first validate-native call
# reports a failed restoration; the wrapper must stop there, keep that evidence, write a partial report and exit nonzero.
param([string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Join-Path $env:TEMP ('KillerBench-W004-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($root)
try {
    $fake = Join-Path $root 'fake-cli.ps1'
    Set-Content -LiteralPath $fake -Encoding UTF8 -Value @'
Add-Content -LiteralPath (Join-Path $env:KB_FAKE_ROOT 'calls.txt') -Value ($args -join ' ')
switch ($args[0]) {
    'pack' { Set-Content -LiteralPath $args[3] -Value '{"Recipes":[{"Id":"svc.one"},{"Id":"svc.two"}]}'; exit 0 }
    'validation-plan' { '{"recipes":[{"recipeId":"svc.one","scenarios":[{"id":"svc.one.healthy","kind":"Healthy"}]},{"recipeId":"svc.two","scenarios":[{"id":"svc.two.healthy","kind":"Healthy"}]}]}'; exit 0 }
    'validate-native' { Set-Content -LiteralPath (Join-Path $args[3] ($args[1] + '.json')) -Value ('{"scenarioId":"' + $args[1] + '.healthy","outcome":"RestorationFailed"}'); exit 4 }
    'validation-report' { Set-Content -LiteralPath $args[1] -Value 'partial'; exit 0 }
}
exit 9
'@
    $wrapper = Join-Path $RepositoryRoot 'Tools\Validation\Invoke-NativeServiceValidation.ps1'
    $output = Join-Path $root 'out'
    # The fake CLI changes nothing, so the elevation check is answered by a stub that exists only in this child process.
    $child = @"
`$ErrorActionPreference = 'Stop'
function New-Object { param([string]`$TypeName, `$ArgumentList) [pscustomobject]@{} | Add-Member -MemberType ScriptMethod -Name IsInRole -Value { `$true } -PassThru }
& '$wrapper' -Cli '$fake' -Output '$output' -DisposableMachine `$env:COMPUTERNAME
"@
    $start = [Diagnostics.ProcessStartInfo]::new((Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Arguments = '-NoLogo -NoProfile -NonInteractive -EncodedCommand ' + [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($child))
    $start.EnvironmentVariables['KB_FAKE_ROOT'] = $root
    $start.EnvironmentVariables['KILLERBENCH_DISPOSABLE'] = 'yes-this-machine-is-disposable'
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $exit = $process.ExitCode
    $calls = @(Get-Content -LiteralPath (Join-Path $root 'calls.txt'))
    $native = @($calls | Where-Object { $_ -like 'validate-native *' })
    if ($exit -eq 0) { throw 'The wrapper must exit nonzero after a failed restoration.' }
    if ($native.Count -ne 1) { throw "The wrapper started $($native.Count) validate-native runs; it must stop after the first failure. Calls: $($calls -join ' / ')" }
    $run = @(Get-ChildItem -LiteralPath $output -Directory)[0].FullName
    if (-not (Test-Path -LiteralPath (Join-Path $run 'svc.one.json'))) { throw 'The failed scenario record must be kept.' }
    if (-not (Test-Path -LiteralPath (Join-Path $run 'native-validation-report.html'))) { throw 'A partial report must be written for the evidence collected.' }
    if ($stderr.Result + $stdout.Result -notmatch 'snapshot') { throw 'The wrapper must tell the operator to revert the VM snapshot.' }
    Write-Host 'PASS: the native validation wrapper stops after a failed restoration, keeps the record, writes a partial report and exits nonzero.'
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
