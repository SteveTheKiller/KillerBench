$ErrorActionPreference = 'Stop'
$cli = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Cli/bin/Debug/net10.0/killerbench-cli.dll'))
$protector = 'b19a63d0-55d6-44f1-a89b-82e2f784b982'
$destination = 'D:\KillerBench-preview-' + [Guid]::NewGuid().ToString('N') + '\recovery.txt'
$cases = @(
    @{ Args = "bitlocker-backup C: $protector $destination"; Exit = 0 },
    @{ Args = "bitlocker-backup C: $protector C:\recovery.txt --execute"; Exit = 2 },
    @{ Args = 'bitlocker-backup C: invalid D:\recovery.txt --execute'; Exit = 2 },
    @{ Args = "bitlocker-backup C: $protector relative.txt --execute"; Exit = 2 },
    @{ Args = "bitlocker-backup C: $protector $destination --unknown"; Exit = 2 },
    @{ Args = "bitlocker-backup C: $protector $destination --execute extra"; Exit = 2 },
    @{ Args = 'bitlocker-protectors C: --execute'; Exit = 2 },
    @{ Args = 'bitlocker-protectors invalid'; Exit = 2 }
)
foreach ($case in $cases) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $cli + '" ' + $case.Args
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = 'Hidden'
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        $null = $process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(15000)) { $process.Kill(); throw 'BitLocker CLI validation timed out.' }
        $output = $stdout.GetAwaiter().GetResult()
        $errors = ($stderr.GetAwaiter().GetResult() -replace '(?m)^Native lab: .*\r?\n', '')
        if ($process.ExitCode -ne $case.Exit) { throw 'BitLocker CLI returned the wrong exit code.' }
        if ($case.Exit -eq 0) {
            $preview = $output | ConvertFrom-Json
            if ($errors -or $preview.Mode -ne 'Preview' -or $preview.Protector -ne $protector -or
                $preview.Volume -ne 'C:' -or $output.Contains($destination) -or
                $preview.Verification -notlike 'No machine inspection performed*') { throw 'Backup preview contract failed.' }
        } elseif ($output -or $errors -notlike '*Usage: bitlocker-protectors*') { throw 'Invalid options reached execution.' }
    } finally { $process.Dispose() }
}
if (Test-Path -LiteralPath $destination) { throw 'Preview created a destination file.' }
Write-Output 'PASS: BitLocker backup preview and seven invalid argument cases; no native backup or protector collection executed.'
