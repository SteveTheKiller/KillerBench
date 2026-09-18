param([string]$Configuration = 'Debug', [switch]$NativeSearch)
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before checking update search.' }
function Invoke-SearchCli([string]$Arguments, [int]$Limit = 10000) {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
    $start.Arguments = '"' + $cli + '" update-search ' + $Arguments
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
        if (-not $process.WaitForExit($Limit)) { $process.Kill(); throw 'Update search CLI exceeded its check limit.' }
        return [pscustomobject]@{ Exit = $process.ExitCode; Output = $stdout.GetAwaiter().GetResult(); Error = $stderr.GetAwaiter().GetResult() }
    } finally { $process.Dispose() }
}
foreach ($arguments in @('--execute', '--filter', '--no-software', '--drivers --drivers', '--filter --execute',
    '--allow-restart-required', '--downloaded-only --downloaded-only', '--select bad', ('--filter ' + ('a' * 257)),
    '--product', '--product invalid', '--product 00000000-0000-0000-0000-000000000000',
    '--product 498cdd9d-5522-42d3-9802-f0fc909c58ed --product 498cdd9d-5522-42d3-9802-f0fc909c58ed',
    '--classification', '--classification invalid', '--classification 00000000-0000-0000-0000-000000000000',
    '--classification f40d9756-7bab-4c31-a2b4-9b087e835dbe --classification f40d9756-7bab-4c31-a2b4-9b087e835dbe')) {
    $result = Invoke-SearchCli $arguments
    if ($result.Exit -ne 2 -or $result.Output.Length -ne 0 -or $result.Error -notmatch 'Usage:') {
        throw "Invalid update-search arguments were not rejected: $arguments"
    }
}
Write-Output 'PASS: invalid or mutating argument combinations rejected before search.'
if ($NativeSearch) {
    $selectedClassification = 'f40d9756-7bab-4c31-a2b4-9b087e835dbe'
    $selectedProduct = '498cdd9d-5522-42d3-9802-f0fc909c58ed'
    $result = Invoke-SearchCli "--filter KillerBenchNonexistentFixture --downloaded-only --classification $selectedClassification --product $selectedProduct" 330000
    $report = $result.Output | ConvertFrom-Json
    if ($null -eq $report.Discovery.Evidence -or $report.Discovery.Partial -isnot [bool] -or
        $report.DownloadedOnly -ne $true -or $report.Filter -ne 'KillerBenchNonexistentFixture' -or
        $report.ClassificationId -ne $selectedClassification -or $report.ProductId -ne $selectedProduct) {
        throw 'Search lost raw evidence, coverage or filter options.'
    }
    $expectedMatches = @($report.Discovery.Updates | Where-Object {
        $_.Downloaded -and $_.Title.IndexOf($report.Filter, [StringComparison]::OrdinalIgnoreCase) -ge 0 -and
        @($_.Behavior.Classifications | Where-Object Id -eq $selectedClassification).Count -gt 0 -and
        @($_.Products | Where-Object Id -eq $selectedProduct).Count -gt 0
    })
    $sourceProducts = @($report.Discovery.Updates | ForEach-Object { $_.Products } | Where-Object { $null -ne $_ })
    if (@($report.AvailableProducts).Count -ne @($sourceProducts.Id | Sort-Object -Unique).Count) {
        throw 'Product choices lost discovered identities or retained duplicates.'
    }
    foreach ($choice in $report.AvailableProducts) {
        if (@($sourceProducts | Where-Object { $_.Id -eq $choice.Id -and $_.Name -eq $choice.Name }).Count -eq 0) {
            throw 'Product choice has no matching discovery metadata.'
        }
    }
    $sourceCategories = @($report.Discovery.Updates | ForEach-Object { $_.Behavior.Classifications } | Where-Object { $null -ne $_ })
    if (@($report.AvailableClassifications).Count -ne @($sourceCategories.Id | Sort-Object -Unique).Count) {
        throw 'Classification choices lost discovered identities or retained duplicates.'
    }
    foreach ($choice in $report.AvailableClassifications) {
        if (@($sourceCategories | Where-Object { $_.Id -eq $choice.Id -and $_.Name -eq $choice.Name }).Count -eq 0) {
            throw 'Classification choice has no matching discovery metadata.'
        }
    }
    if (@($report.Matches).Count -ne $expectedMatches.Count) { throw 'Filter result count disagrees with discovery.' }
    foreach ($match in $report.Matches) {
        if (@($expectedMatches | Where-Object { $_.Id -eq $match.Id -and $_.Revision -eq $match.Revision }).Count -ne 1) {
            throw 'Filter lost an exact update identity or revision.'
        }
    }
    $expectedExit = if ($report.Discovery.Partial) { 3 } else { 0 }
    if ($result.Exit -ne $expectedExit) { throw 'Search exit disagrees with completeness.' }
    Write-Output "PASS: native search retained $(@($report.Discovery.Updates).Count) updates and $(@($report.Matches).Count) matches, exit $expectedExit. No installation requested."
}
