<#
.SYNOPSIS
    Runs the fault-injection scenarios for every service-pack recipe on a disposable Windows VM and builds the coverage report.
    Created by Steve the Killer | Updated: 2026-09-18

.DESCRIPTION
    Only run this on a disposable machine with a snapshot to revert to. It stops, disables and restarts real Windows services.
    It refuses to run unless the machine name matches -DisposableMachine and KILLERBENCH_DISPOSABLE is set to the exact marker value.
    Works in PowerShell 5.1 and 7.

.PARAMETER Cli
    Path to killerbench-cli.exe.
.PARAMETER Output
    Folder that receives one JSON record per scenario, artifact folders and the HTML report.
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

$packJson = Join-Path $Output 'windows.services.json'
if (Test-Path $packJson) { Remove-Item $packJson }
& $Cli pack export windows.services $packJson | Out-Null
$recipes = (Get-Content $packJson -Raw | ConvertFrom-Json).Recipes | ForEach-Object { $_.Id }
if ($Recipe) { $recipes = $recipes | Where-Object { $_ -eq $Recipe }; if (-not $recipes) { throw "Recipe '$Recipe' is not in the service pack." } }

$summary = @()
foreach ($id in $recipes) {
    Write-Host ("=== " + $id)
    & $Cli validate-native $id all $Output --disposable $DisposableMachine 2>&1 | ForEach-Object { Write-Host $_ }
    $summary += [pscustomobject]@{ Recipe = $id; ExitCode = $LASTEXITCODE }
}
$records = Get-ChildItem $Output -Filter '*.json' | Where-Object { $_.Name -ne 'windows.services.json' } | ForEach-Object { $_.FullName }
$report = Join-Path $Output 'native-validation-report.html'
& $Cli validation-report $report @records | ForEach-Object { Write-Host $_ }
$summary | Format-Table -AutoSize
Write-Host ("Report: " + $report)
