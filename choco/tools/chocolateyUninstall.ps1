$ErrorActionPreference = 'Stop'
$registrations = @(Get-UninstallRegistryKey -SoftwareName 'KillerBench' | Where-Object {
    $_.DisplayName -eq 'KillerBench' -and $_.Publisher -eq 'Steve the Killer'
})
if ($registrations.Count -eq 0) { return }
if ($registrations.Count -ne 1) { throw 'Multiple KillerBench installations were found. Uninstall the intended copy from Windows Settings.' }
$directory = [string]$registrations[0].InstallLocation
if ([string]::IsNullOrWhiteSpace($directory) -or -not [IO.Path]::IsPathRooted($directory)) { throw 'The installed KillerBench location is unavailable.' }
$executable = Join-Path $directory 'KillerBench.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'The registered KillerBench executable is missing. Repair the installation before retrying.' }
Uninstall-ChocolateyPackage -PackageName $env:ChocolateyPackageName -FileType 'exe' -SilentArgs '/uninstall-silent' -File $executable -ValidExitCodes @(0)
$remaining = @(Get-UninstallRegistryKey -SoftwareName 'KillerBench' | Where-Object {
    $_.DisplayName -eq 'KillerBench' -and $_.Publisher -eq 'Steve the Killer' -and $_.InstallLocation -eq $directory
})
if ($remaining.Count -gt 0) { throw 'KillerBench uninstall returned without removing its registration.' }
Write-Host 'KillerBench registration and shortcuts removed. Application files, reports, and settings are preserved.'
