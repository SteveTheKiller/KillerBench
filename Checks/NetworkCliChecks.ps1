$ErrorActionPreference = 'Stop'
$cli = Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build KillerBench.Cli before running these checks.' }
$cases = @(
    @('workflow', 'network', '--tcp'),
    @('workflow', 'network', '--tcp', 'localhost'),
    @('workflow', 'network', '--tcp', 'localhost', '0'),
    @('workflow', 'network', '--tcp', 'localhost', '65536'),
    @('workflow', 'network', '--tcp', 'https://example.test', '443'),
    @('workflow', 'network', '--tcp', 'localhost', '443', '--tcp', 'localhost', '80'),
    @('workflow', 'network', '--host'),
    @('workflow', 'network', '--host', 'localhost', '--host', 'example.test'),
    @('workflow', 'network', '--host', 'https://example.test'),
    @('workflow', 'network', '--host', '--json'),
    @('workflow', 'network', '--host', '127.0.0.1'),
    @('workflow', 'network', '--host', 'localhost', '--unknown'),
    @('workflow', 'network', '--json', '--json'),
    @('workflow', 'files', '--host', 'localhost')
)
foreach ($arguments in $cases) {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" ' + ($arguments -join ' ')
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'CLI did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 2) { throw "Expected invalid-input exit for: $arguments" }
    }
    finally { $process.Dispose() }
    if (($output -join "`n") -match 'Checking this PC') { throw 'Invalid hostname options reached collection.' }
}
Write-Output 'PASS: network CLI rejects missing, duplicate, invalid and misplaced options before collection.'
