$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-xml-cli-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$path = Join-Path $scratch 'events.xml'
function Invoke-Xml([int]$ExpectedExit) {
    $before = (Get-FileHash -LiteralPath $path).Hash
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" investigate "' + $path + '" --json'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'XML CLI did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(20000)) { $process.Kill(); throw 'XML CLI exceeded its bound.' }
        $output = $stdout.GetAwaiter().GetResult()
        $errors = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Unexpected XML exit $($process.ExitCode): $errors" }
        if ((Get-FileHash -LiteralPath $path).Hash -ne $before) { throw 'XML source changed.' }
        if ($ExpectedExit -eq 0) {
            if ($errors) { throw 'Successful XML analysis reported an error.' }
            return ($output | ConvertFrom-Json)
        }
        if ($output -or $errors -notmatch '^Cannot analyze evidence:') { throw 'Malformed XML did not produce a controlled error.' }
    } finally { $process.Dispose() }
}
$entry = '<Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event"><System><Provider Name="Microsoft-Windows-PrintService"/><EventID>372</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime="2026-09-12T12:00:00Z"/></System></Event>'
[IO.File]::WriteAllText($path, '<Events>' + $entry + '</Events>')
$report = Invoke-Xml 0
if ($report.Source -ne $path -or $report.ParsedLines -ne 1 -or $report.Partial -or
    $report.Findings[0].RuleId -ne 'KB-PRINT-EVENT-372-2' -or $report.Findings[0].Evidence[0].LineNumber -ne 1) {
    throw 'XML CLI lost finding identity or record reference.'
}
[IO.File]::WriteAllText($path, '<Events>' + ($entry * 201) + '</Events>')
$limited = Invoke-Xml 0
if (-not $limited.Partial -or $limited.ParsedLines -ne 200 -or $limited.Findings[0].Occurrences -ne 200) { throw 'XML CLI record cap is not explicit.' }
foreach ($invalid in @('<Events>', '<Other/>', '<!DOCTYPE Events [<!ENTITY x SYSTEM "file:///unread-fixture">]><Events>&x;</Events>')) {
    [IO.File]::WriteAllText($path, $invalid)
    Invoke-Xml 1
}
Write-Output 'PASS: XML CLI findings, record cap, source preservation and controlled malformed/DTD errors. No machine changes.'
