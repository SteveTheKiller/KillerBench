$ErrorActionPreference = 'Stop'
$cli = Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build KillerBench.Cli before running these checks.' }
$cases = @(
    @('workflow', 'storage', '--volume'),
    @('workflow', 'storage', '--volume', 'D:', '--volume', 'C:'),
    @('workflow', 'storage', '--volume', 'D:\folder'),
    @('workflow', 'storage', '--volume', '--json'),
    @('workflow', 'storage', '--volume', 'D:', '/f'),
    @('workflow', 'storage', '--volume', 'D:', '--auto-repair'),
    @('workflow', 'storage', '--json', '--json'),
    @('workflow', 'files', '--volume', 'D:')
)
foreach ($arguments in $cases) {
    $output = & dotnet $cli @arguments 2>&1
    if ($LASTEXITCODE -ne 2) { throw "Expected invalid-input exit for: $arguments" }
    if (($output -join "`n") -match 'Checking this PC') { throw 'Invalid selection reached collection.' }
}
Write-Output 'PASS: storage CLI rejects missing, duplicate, injected and misplaced options before collection.'
