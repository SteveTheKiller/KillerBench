$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitOperatingSystem) { throw 'KillerBench requires 64-bit Windows.' }
$packageArgs = @{
    packageName = $env:ChocolateyPackageName
    fileType = 'exe'
    silentArgs = '/silent'
    url64bit = 'https://github.com/SteveTheKiller/KillerBench/releases/download/v1.0.0/KillerBench.exe'
    checksum64 = 'C15CB2262EFB7A44CAC78C67AE182A3C3C0B9867CBB9C4FA58704D4DFD43B1EA'
    checksumType64 = 'sha256'
    validExitCodes = @(0)
}
Install-ChocolateyPackage @packageArgs
