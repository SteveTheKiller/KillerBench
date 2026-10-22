<#
.SYNOPSIS
    Runs the fault-injection scenarios for every service-pack recipe on a disposable Windows VM and builds the coverage report.
    Created by Steve the Killer | Updated: 2026-10-21

.DESCRIPTION
    Only run this on a disposable machine with a snapshot to revert to. It stops, disables and restarts real Windows services.
    It refuses to run unless the machine name matches -DisposableMachine and KILLERBENCH_DISPOSABLE is set to the exact marker value.
    Works in PowerShell 5.1 and 7.

.PARAMETER Cli
    Path to killerbench-cli.exe.
.PARAMETER Output
    Parent folder for a new run folder containing scenario records, artifacts and the HTML report.
.PARAMETER DisposableMachine
    This machine's name, typed on purpose.
.PARAMETER Recipe
    Optional single recipe id. Default: every recipe in the windows.services pack.
#>
param(
    [Parameter(Mandatory = $true)][string]$Cli,
    [Parameter(Mandatory = $true)][string]$Output,
    [Parameter(Mandatory = $true)][string]$DisposableMachine,
    [string]$Recipe
)
$ErrorActionPreference = 'Stop'
if ($DisposableMachine -ne $env:COMPUTERNAME) { throw "DisposableMachine '$DisposableMachine' is not this machine ($env:COMPUTERNAME)." }
if ($env:KILLERBENCH_DISPOSABLE -ne 'yes-this-machine-is-disposable') { throw "Set KILLERBENCH_DISPOSABLE=yes-this-machine-is-disposable on the disposable VM first." }
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run from an elevated PowerShell.' }
if (-not (Test-Path $Cli)) { throw "CLI not found: $Cli" }
New-Item -ItemType Directory -Force $Output | Out-Null
$runOutput = Join-Path $Output ('run-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory $runOutput | Out-Null

$packJson = Join-Path $runOutput 'windows.services.json'
& $Cli pack export windows.services $packJson | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Service pack export failed with exit code $LASTEXITCODE." }
$recipes = @((Get-Content $packJson -Raw | ConvertFrom-Json).Recipes | ForEach-Object { $_.Id })
if ($Recipe) { $recipes = $recipes | Where-Object { $_ -eq $Recipe }; if (-not $recipes) { throw "Recipe '$Recipe' is not in the service pack." } }
if (-not $recipes) { throw 'The service pack contains no recipes.' }
$planJson = & $Cli validation-plan
if ($LASTEXITCODE -ne 0) { throw "Validation plan failed with exit code $LASTEXITCODE." }
$plans = @(($planJson | ConvertFrom-Json).recipes | Where-Object { $recipes -contains $_.recipeId })
if ($plans.Count -ne $recipes.Count) { throw 'The validation plan does not cover every requested service recipe.' }
$scenarios = @($plans | ForEach-Object { $_.scenarios })
if ($scenarios.Count -eq 0) { throw 'The validation plan contains no scenarios for the requested recipes.' }
$injectableKinds = @('Healthy', 'Preview', 'Ineligible', 'VerifiedExecution', 'Cancellation')

$summary = @()
$stoppedAt = $null
foreach ($id in $recipes) {
    Write-Host ("=== " + $id)
    & $Cli validate-native $id all $runOutput --disposable $DisposableMachine 2>&1 | ForEach-Object { Write-Host $_ }
    $exitCode = $LASTEXITCODE
    $summary += [pscustomobject]@{ Recipe = $id; ExitCode = $exitCode }
    # A nonzero exit can mean restoration was not proven. Never fault-inject again on an unreconciled machine.
    if ($exitCode -ne 0) { $stoppedAt = $id; break }
}
$records = @(Get-ChildItem $runOutput -Filter '*.json' | Where-Object { $_.Name -ne 'windows.services.json' } | ForEach-Object { $_.FullName })
if ($records.Count -eq 0) { throw "No scenario records were written to $runOutput." }
$report = Join-Path $runOutput 'native-validation-report.html'
& $Cli validation-report $report @records | ForEach-Object { Write-Host $_ }
$reportExitCode = $LASTEXITCODE
$summary | Format-Table -AutoSize
if ($stoppedAt) {
    $partial = if ($reportExitCode -eq 0 -and (Test-Path $report)) { " Partial report: $report." } else { " The partial report could not be written." }
    throw "Stopped after $stoppedAt exited with code $(($summary | Select-Object -Last 1).ExitCode); no further scenarios ran. Revert the VM snapshot before validating again. Evidence: $runOutput.$partial"
}
if ($reportExitCode -ne 0) { throw "Validation report failed with exit code $reportExitCode." }
if (-not (Test-Path $report)) { throw "Validation report was not written to $report." }
Write-Host ("Report: " + $report)
$failed = @($summary | Where-Object { $_.ExitCode -ne 0 })
if ($failed.Count -gt 0) { throw "$($failed.Count) service recipe validation run(s) failed. See $runOutput." }
$outcomes = @($records | ForEach-Object { Get-Content $_ -Raw | ConvertFrom-Json })
$incomplete = @()
$scenarioIds = @($scenarios | ForEach-Object { $_.id })
foreach ($outcome in $outcomes) {
    if ($scenarioIds -notcontains $outcome.scenarioId) { $incomplete += "$($outcome.scenarioId) (unexpected record)" }
}
foreach ($scenario in $scenarios) {
    $matching = @($outcomes | Where-Object { $_.scenarioId -eq $scenario.id })
    if ($matching.Count -ne 1) { $incomplete += "$($scenario.id) (missing or duplicate record)"; continue }
    if ($injectableKinds -contains $scenario.kind) {
        if ($matching[0].outcome -ne 'Passed') { $incomplete += "$($scenario.id) ($($matching[0].outcome))" }
    } elseif ($matching[0].outcome -ne 'Blocked' -or $matching[0].notes -notmatch 'not injectable by this runner yet') {
        $incomplete += "$($scenario.id) (expected an explicit unsupported-kind Blocked record)"
    }
}
if ($incomplete.Count -gt 0) { throw "$($incomplete.Count) scenario record(s) need attention: $($incomplete -join ', '). See $report." }
