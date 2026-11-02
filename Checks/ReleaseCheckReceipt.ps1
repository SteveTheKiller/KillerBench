#Requires -Version 5.1
function Get-ReleaseCheckDigest([string[]]$Lines) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($Lines -join "`n"))))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Get-ReleaseCheckSnapshot([string]$RepositoryRoot, [string]$BuildOutput) {
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\')
    $paths = [string[]]@(& git -C $root -c core.quotepath=false ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0 -or $paths.Count -eq 0) { throw 'Cannot fingerprint release source.' }
    [Array]::Sort($paths, [StringComparer]::Ordinal)
    $source = @($paths | Select-Object -Unique | ForEach-Object {
        $path = Join-Path $root $_
        if (-not [IO.File]::Exists($path)) { throw "Release source is missing: $_" }
        $_ + ':' + (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    })
    $binaries = @()
    if ([IO.Directory]::Exists($BuildOutput)) {
        $outputRoot = [IO.Path]::GetFullPath($BuildOutput).TrimEnd('\') + '\'
        $binaryPaths = [string[]]@(Get-ChildItem -LiteralPath $outputRoot -File -Recurse | Where-Object {
            $_.Extension -in @('.dll', '.exe') -or $_.Name -like '*.deps.json' -or $_.Name -like '*.runtimeconfig.json'
        } | ForEach-Object { $_.FullName })
        [Array]::Sort($binaryPaths, [StringComparer]::Ordinal)
        $binaries = @($binaryPaths | ForEach-Object {
            $_.Substring($outputRoot.Length) + ':' + (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash
        })
    }
    [pscustomobject]@{
        RepositoryRoot = $root
        SourceHash = Get-ReleaseCheckDigest $source
        BinaryHash = Get-ReleaseCheckDigest $binaries
        BinaryCount = $binaries.Count
    }
}

function Save-ReleaseCheckReceipt([string]$RepositoryRoot, [string]$BuildOutput, [string]$ReceiptPath, $Before) {
    $current = Get-ReleaseCheckSnapshot $RepositoryRoot $BuildOutput
    if ($current.BinaryCount -eq 0 -or $Before.SourceHash -ne $current.SourceHash -or $Before.BinaryHash -ne $current.BinaryHash) {
        throw 'Source or test binaries changed during verification. Run .\release.ps1 -ChecksOnly again.'
    }
    $receipt = [ordered]@{
        SchemaVersion = 1
        Status = 'Passed'
        CompletedUtc = [DateTime]::UtcNow.ToString('o')
        RepositoryRoot = $current.RepositoryRoot
        SourceHash = $current.SourceHash
        BinaryHash = $current.BinaryHash
    }
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($ReceiptPath)))
    $temporary = $ReceiptPath + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary, ($receipt | ConvertTo-Json), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporary -Destination $ReceiptPath -Force
    } finally { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
}

function Assert-ReleaseCheckReceipt([string]$RepositoryRoot, [string]$BuildOutput, [string]$ReceiptPath) {
    if (-not [IO.File]::Exists($ReceiptPath)) { throw 'No successful verification receipt. Run .\release.ps1 -ChecksOnly first.' }
    try {
        $receipt = Get-Content -LiteralPath $ReceiptPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $current = Get-ReleaseCheckSnapshot $RepositoryRoot $BuildOutput
        $completed = if ($receipt.CompletedUtc -is [DateTime]) { $receipt.CompletedUtc.ToUniversalTime() }
            else { [DateTime]::Parse($receipt.CompletedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime() }
        if ($receipt.SchemaVersion -ne 1 -or $receipt.Status -ne 'Passed' -or $current.BinaryCount -eq 0 -or
            $receipt.RepositoryRoot -ne $current.RepositoryRoot -or $receipt.SourceHash -ne $current.SourceHash -or
            $receipt.BinaryHash -ne $current.BinaryHash -or $completed -gt [DateTime]::UtcNow -or
            $completed -lt [DateTime]::UtcNow.AddHours(-24)) { throw 'Verification receipt is stale or does not match.' }
    } catch { throw 'Verification receipt is invalid, expired or does not match source and binaries. Run .\release.ps1 -ChecksOnly again.' }
}
