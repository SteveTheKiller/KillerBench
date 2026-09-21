$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-compare-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
$before = Join-Path $scratch 'before.json'
$after = Join-Path $scratch 'after.json'
function Save-Fixture([string]$Path, [string]$Version, [bool]$Complete) {
    $raw = @{scope='Machine';complete=$Complete;applications=@(@{view='Registry64';key='fixture';name='Fixture';version=$Version;publisher='Vendor';hasUninstallCommand=$true;hasInstallLocation=$true})}|ConvertTo-Json -Depth 5 -Compress
    @{Evidence=@{ExitCode=0;Output=$raw;Error='';Truncated=$false;TimedOut=$false}}|ConvertTo-Json -Depth 5 | ForEach-Object {[IO.File]::WriteAllText($Path, $_)}
}
Save-Fixture $before '1.0' $true
foreach ($scenario in @('complete', 'partial', 'bom', 'invalid-number', 'duplicate')) {
    $complete = $scenario -ne 'partial'
    Save-Fixture $after '2.0' $complete
    $invalid = $scenario -in @('invalid-number', 'duplicate')
    if ($scenario -eq 'bom') { [IO.File]::WriteAllText($after, [IO.File]::ReadAllText($after), (New-Object Text.UTF8Encoding($true))) }
    if ($scenario -eq 'invalid-number') { [IO.File]::WriteAllText($after, '{"Evidence":{"ExitCode":1e100,"Output":"","Error":"","Truncated":false,"TimedOut":false}}') }
    if ($scenario -eq 'duplicate') { [IO.File]::WriteAllText($after, '{"Evidence":{},"Evidence":{}}') }
    $hashes = @((Get-FileHash $before).Hash, (Get-FileHash $after).Hash)
    $start=New-Object Diagnostics.ProcessStartInfo
    $start.FileName=(Get-Command dotnet).Source
    $start.Arguments='"'+$cli+'" installed-apps-compare "'+$before+'" "'+$after+'"'
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true; $start.WindowStyle='Hidden'
    $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    $process=New-Object Diagnostics.Process; $process.StartInfo=$start
    try {
        $null=$process.Start(); $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit(45000)){ $process.Kill(); throw 'Comparison timed out.' }
        $output=$stdout.GetAwaiter().GetResult()
        $errors=$stderr.GetAwaiter().GetResult()
        if ($invalid) {
            if ($process.ExitCode -ne 1 -or $errors -notlike 'Cannot compare inventories:*' -or $output) { throw 'Invalid inventory was not rejected cleanly.' }
            continue
        }
        $result=$output|ConvertFrom-Json
        $expected=if($complete){0}else{3}
        if($process.ExitCode -ne $expected -or $result.Comparable -ne $complete){throw 'Comparison coverage mismatch.'}
        if($complete -and (@($result.Differences).Count -ne 1 -or $result.Differences[0].Change -ne 'VersionChanged')){throw 'Version change missing.'}
        if(-not $complete -and @($result.Differences).Count -ne 0){throw 'Incomplete evidence produced definitive differences.'}
        if((Get-FileHash $before).Hash -ne $hashes[0] -or (Get-FileHash $after).Hash -ne $hashes[1]){throw 'Source inventory changed.'}
    } finally {$process.Dispose()}
}
Write-Output 'PASS: CLI version comparison, incomplete coverage rejection, UTF-8 BOM, malformed input rejection and source preservation.'
