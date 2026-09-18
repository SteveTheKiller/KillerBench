param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"))
function Read-RecipeCli([string]$Arguments, [string]$Command = 'recipes') {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
    $start.Arguments = '"' + $cli + '" ' + $Command + ' ' + $Arguments
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
        if (-not $process.WaitForExit(10000)) { $process.Kill(); throw 'Catalog listing exceeded its time limit.' }
        return [pscustomobject]@{ Exit = $process.ExitCode; Output = $stdout.GetAwaiter().GetResult(); Error = $stderr.GetAwaiter().GetResult() }
    } finally { $process.Dispose() }
}
foreach ($argument in @('', 'common-problems', 'update', 'files', 'vitals', 'backup-services')) {
    $result = Read-RecipeCli $argument
    if ($result.Exit -ne 0 -or $result.Error.Length -ne 0) { throw "Catalog failed: $argument" }
    $recipes = @($result.Output | ConvertFrom-Json)
    $expected = switch ($argument) { '' {114} 'common-problems' {96} 'update' {9} 'files' {1} 'vitals' {0} 'backup-services' {4} }
    if ($recipes.Count -ne $expected) { throw "Unexpected scope count: $argument" }
    foreach ($recipe in $recipes) {
        if (-not $recipe.Id -or -not $recipe.VerificationScope -or -not $recipe.Effects -or [TimeSpan]::Parse($recipe.InspectionTimeout).TotalMinutes -ne 65) { throw 'Missing recipe metadata.' }
        if ($recipe.DiagnosticOnly -and $recipe.Effects -notmatch 'does not execute a repair') { throw 'Diagnostic-only effects are misleading.' }
        if ($recipe.Id -eq 'network.workstation' -and $recipe.Effects -notmatch 'credentials') { throw 'Service effects lost their preservation scope.' }
        if ($recipe.Id -eq 'printing.paused-queues' -and $recipe.Effects -notmatch 'waiting print jobs') { throw 'Print effects lost their job-release warning.' }
    }
    if ($argument -in @('common-problems', 'update')) {
        $files = $recipes | Where-Object Id -eq 'windows.system-files'
        if (@($files.Dependencies).Count -ne 1 -or $files.Dependencies[0] -ne 'windows.component-store') { throw 'Missing workflow-specific servicing dependency.' }
    }
}
foreach ($argument in @('unknown', '--execute', 'common-problems --auto-repair', 'files extra')) {
    $result = Read-RecipeCli $argument
    if ($result.Exit -ne 2 -or $result.Output.Length -ne 0 -or $result.Error -notmatch 'Usage:') { throw 'Invalid catalog argument was accepted.' }
}
Write-Output 'PASS: installed and workflow recipe metadata, dependencies, empty scopes and execution-option rejection. No diagnostics or repairs run.'
foreach ($argument in @('common-problems --recipes', 'common-problems --recipes missing',
    'common-problems --recipes windows.component-cleanup --auto-repair', 'files --recipes windows.system-files --recipes windows.system-files',
    'vitals --recipes windows.system-files', 'files --recipes windows.system-files,')) {
    $result = Read-RecipeCli $argument 'workflow'
    if ($result.Exit -ne 2 -or $result.Output.Length -ne 0 -or $result.Error.Length -eq 0) { throw 'Invalid selected workflow was accepted.' }
}
Write-Output 'PASS: workflow selection rejects missing, duplicate, empty and out-of-scope arguments before collection.'
