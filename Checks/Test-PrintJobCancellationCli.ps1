param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$cli = Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before running this check.' }
$preview = & dotnet $cli print-job-cancel 'Synthetic Queue' 42 2>&1
if ($LASTEXITCODE -ne 0) { throw "Print-job preview failed: $preview" }
$result = $preview | ConvertFrom-Json
if ($result.outcome -ne 'Preview' -or $result.queueName -ne 'Synthetic Queue' -or $result.jobId -ne 42 -or
    $result.detail -notmatch 'No queue purge') { throw 'Print-job preview lost its exact target or effects.' }
$invalid = & dotnet $cli print-job-cancel 'Synthetic Queue' 0 2>&1
if ($LASTEXITCODE -ne 2 -or $invalid -notmatch 'Choose one exact target') {
    throw 'Print-job CLI accepted an invalid identity.'
}
$invalidList = & dotnet $cli print-job-list --folder fixture 2>&1
if ($LASTEXITCODE -ne 2 -or $invalidList -notmatch 'takes no options') {
    throw 'Print-job inventory accepted a mutation-only option.'
}
$help = & dotnet $cli --help 2>&1
$helpText = $help -join "`n"
if ($LASTEXITCODE -ne 0 -or $helpText -notmatch 'print-job-list' -or
    $helpText -notmatch 'print-job-cancel QUEUE JOB_ID' -or $helpText -notmatch 'print-job-cancel-recover') {
    throw 'Print-job CLI help is incomplete.'
}
Write-Host 'PASS: print-job CLI exposes exact preview, validation, inventory and no-repeat recovery routes without native mutation.'
