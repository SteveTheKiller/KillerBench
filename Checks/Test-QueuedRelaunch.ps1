param([string]$SourceRoot = (Split-Path $PSScriptRoot -Parent),
    [Parameter(Mandatory = $true)][string]$WorkRoot,
    [string]$RecoveryArgumentsPath)
$ErrorActionPreference = 'Stop'
# Execute the production launch function with only Scheduler and delays substituted.
# The native launch double enters the fallback without creating a native process.
$source = Get-Content -LiteralPath (Join-Path $SourceRoot 'Services/UpdateImagePublication.cs') -Raw
$start = $source.IndexOf('function New-KillerUpdateTaskDate {', [StringComparison]::Ordinal)
if ($start -lt 0) { $start = $source.IndexOf('function Invoke-KillerDeferredLaunch {', [StringComparison]::Ordinal) }
if ($start -lt 0) { $start = $source.IndexOf('function Start-KillerUpdatedApp {', [StringComparison]::Ordinal) }
if ($start -lt 0) { throw 'Production relaunch function was not found.' }
$end = $source.IndexOf('""";', $start, [StringComparison]::Ordinal)
$function = $source.Substring($start, $end - $start)
$script:waits = 0
$script:requests = 0
$script:deletes = 0
$script:stops = 0
$script:securityReads = 0
$script:oldTasks = @()
$script:deletedNames = [Collections.Generic.List[string]]::new()
$fixture = Join-Path $WorkRoot ('fixture-' + [Guid]::NewGuid().ToString('N'))
$null = [IO.Directory]::CreateDirectory($fixture)
$fixtureDll = Join-Path $fixture 'guard.dll'
Add-Type -OutputAssembly $fixtureDll -TypeDefinition @'
using System;
using System.IO;
public static class KillerUpdateGuard {
    public static bool ParentHeld, ImageHeld, PublisherValid = true, IdentityValid = true, FailLaunch, CancelLaunch, AllowElevatedLaunch;
    public static int Launches, Reports, Attempts;
    public static string ExpectedThumb = new string('B', 40);
    public static byte[] Image = new byte[] {1,2,3};
    public static IDisposable PinParents(string target) { ParentHeld = true; return new Lease(); }
    public static Stream OpenImage(string target) { ImageHeld = true; return new HeldImage(Image); }
    public static bool Publisher(string path, string thumb, string signer) { return PublisherValid && thumb == ExpectedThumb; }
    public static void Launch(string target, string reopen, bool elevated) {
        Attempts++;
        if (elevated && !AllowElevatedLaunch) throw new IOException("Fixture direct launch unavailable");
        if (!ParentHeld || !ImageHeld) throw new Exception("Launch lost protection");
        if (FailLaunch) throw new IOException("Fixture process creation failed");
        if (CancelLaunch) throw new OperationCanceledException("Fixture creation canceled");
        Launches++;
    }
    public static string Quote(string value) { return "\"" + value + "\""; }
    public static uint ConsoleSession() { return 1; }
    public static string SessionUser(int session) { return "S-1-5-21-1-2-3-1001"; }
    public static void RequireInteractiveUser(string sid, int session) {
        if (!IdentityValid) throw new IOException("Fixture session or identity changed");
    }
    public static void ReportLaunchFailure(string message) { Reports++; }
    private sealed class Lease : IDisposable { public void Dispose() { ParentHeld = false; } }
    private sealed class HeldImage : MemoryStream {
        public HeldImage(byte[] data) : base(data) {}
        protected override void Dispose(bool disposing) { ImageHeld = false; base.Dispose(disposing); }
    }
}
'@
$script:killerDeferredAssembly = [IO.File]::ReadAllBytes($fixtureDll)
$null = [Reflection.Assembly]::Load($script:killerDeferredAssembly)
function Start-Sleep { param($Milliseconds) $script:waits++ }
$script:instance = [pscustomobject]@{ State = 2; EnginePID = 0; InstanceGuid = 'fixture-instance' }
$script:instance | Add-Member ScriptMethod Refresh { if ($script:fastExit) { throw 'Fixture task instance already completed' } }
$script:instance | Add-Member ScriptMethod Stop { $script:stops++; $this.State = 3 }
$script:registered = [pscustomobject]@{ State = 2; Enabled = $true }
$script:registered | Add-Member ScriptMethod RunEx {
    param($a,$b,$c,$d)
    $script:requests++
    if ($script:runFailure) { throw 'Fixture scheduler dispatch failed' }
    if ($script:noInstance) { return $null }
    return $script:instance
}
$script:descriptor = 'O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)'
$script:registered | Add-Member ScriptMethod GetSecurityDescriptor { param($flags) $script:securityReads++; return $script:descriptor }
$script:folder = [pscustomobject]@{}
$script:folder | Add-Member ScriptMethod GetTasks { param($flags) return @($script:oldTasks) }
$script:folder | Add-Member ScriptMethod RegisterTaskDefinition {
    param($a,$b,$c,$d,$e,$f,$g)
    $script:registrationFlags = $c; $script:taskSecurity = $g
    return $script:registered
}
$script:folder | Add-Member ScriptMethod DeleteTask { param($a,$b) $script:deletes++; $script:deletedNames.Add([string]$a) }
$script:actions = [pscustomobject]@{}
$script:actions | Add-Member ScriptMethod Create {
    param($kind)
    $script:action = [pscustomobject]@{Path='';WorkingDirectory='';Arguments=''}
    return $script:action
}
$script:service = [pscustomobject]@{}
$script:service | Add-Member ScriptMethod Connect {}
$script:service | Add-Member ScriptMethod GetFolder { param($name) return $script:folder }
$script:service | Add-Member ScriptMethod NewTask { param($flags) return [pscustomobject]@{
    RegistrationInfo=[pscustomobject]@{Date=''}
    Settings=[pscustomobject]@{Hidden=$false;DisallowStartIfOnBatteries=$true;StopIfGoingOnBatteries=$true;ExecutionTimeLimit=''}
    Principal=[pscustomobject]@{GroupId='';RunLevel=0}; Actions=$script:actions
} }
$script:mutex = [pscustomobject]@{}
$script:mutex | Add-Member ScriptMethod WaitOne { param($milliseconds) return $false }
$script:mutex | Add-Member ScriptMethod Dispose {}
function New-Object {
    param($ComObject)
    if ($ComObject -eq 'Threading.Mutex') { return $script:mutex }
    if ($ComObject -ne 'Schedule.Service') { throw 'Unexpected COM request' }
    return $script:service
}
Invoke-Expression $function
[KillerUpdateGuard]::AllowElevatedLaunch = $true
[KillerUpdateGuard]::PublisherValid = $false
$rejected = $false
try { Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' '' } catch { $rejected = $true }
if (-not $rejected -or [KillerUpdateGuard]::Attempts -ne 0 -or $script:requests -ne 0) {
    throw 'Invalid publisher reached the direct launch boundary before identity verification.'
}
[KillerUpdateGuard]::PublisherValid = $true
Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' ''
if ([KillerUpdateGuard]::Launches -ne 1 -or $script:requests -ne 0 -or [KillerUpdateGuard]::ParentHeld -or [KillerUpdateGuard]::ImageHeld) {
    throw 'Verified direct launch must retain image and parent protection through process creation.'
}
[KillerUpdateGuard]::Launches = 0
[KillerUpdateGuard]::AllowElevatedLaunch = $false
Write-Output 'PASS: direct relaunch verifies publisher before process creation and retains protection through launch.'
$failed = $false
$report = Join-Path $fixture 'queue-report.txt'
[IO.File]::WriteAllText($report, 'Publication failed. Backup: retained-original.exe')
try { Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' $report }
catch { $failed = $true; Write-Output $_.Exception.ToString() }
if ($failed -or $script:requests -ne 1) { throw 'Automatic relaunch did not retain its independent queued helper.' }
$systemPowerShell = Join-Path ([Environment]::SystemDirectory) 'WindowsPowerShell\v1.0\powershell.exe'
if ($script:action.Path -ne $systemPowerShell) {
    throw 'Queued action must invoke the trusted verifier rather than a mutable target.'
}
if ($script:registrationFlags -ne 18 -or $script:taskSecurity -ne 'O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)' -or $script:securityReads -eq 0) { throw 'Task owner and DACL were not verified before dispatch.' }
if ($script:deletes -ne 0) { throw 'Queued task was deleted.' }
if ((Get-Content -Raw -LiteralPath $report) -notmatch 'state: queued' -or [KillerUpdateGuard]::ParentHeld -or [KillerUpdateGuard]::ImageHeld) { throw 'Queued state was not reported accurately before releasing original holds.' }
$reportText = Get-Content -Raw -LiteralPath $report
if (-not $reportText.Contains('Publication failed. Backup: retained-original.exe') -or $reportText.Contains('Update completed.')) {
    throw 'Queued recovery replaced failure evidence or falsely reported successful publication.'
}
if ($script:action.Arguments.Length -gt 28000) { throw 'Deferred helper exceeds its command margin.' }
$match = [regex]::Match($script:action.Arguments, "FromBase64String\('([^']+)'\)")
if (-not $match.Success) { throw 'Deferred helper is not immutable compressed content.' }
$compressed = [IO.MemoryStream]::new([Convert]::FromBase64String($match.Groups[1].Value))
$reader = [IO.StreamReader]::new([IO.Compression.GZipStream]::new($compressed, [IO.Compression.CompressionMode]::Decompress))
try { $deferred = $reader.ReadToEnd() } finally { $reader.Dispose(); $compressed.Dispose() }
[IO.File]::Delete($fixtureDll)
[KillerUpdateGuard]::Image = [byte[]]@(9,9,9)
$rejected = $false
try { & ([scriptblock]::Create($deferred)) } catch { $rejected = $true }
if (-not $rejected -or [KillerUpdateGuard]::Launches -ne 0) { throw 'Delayed helper executed a replaced image.' }
if ([KillerUpdateGuard]::Reports -ne 1) { throw 'Deferred helper failure was not reported.' }
if ([KillerUpdateGuard]::ImageHeld -or [KillerUpdateGuard]::ParentHeld) { throw 'Failed deferred launch leaked protection.' }
[KillerUpdateGuard]::Image = [byte[]]@(1,2,3)
& ([scriptblock]::Create($deferred))
if ([KillerUpdateGuard]::Launches -ne 1 -or [KillerUpdateGuard]::ImageHeld -or [KillerUpdateGuard]::ParentHeld) { throw 'Valid deferred launch failed its protection handoff.' }
Write-Output 'PASS: immutable queued helper rejects replaced image and holds protection through valid process creation.'
foreach ($mode in @('publisher', 'identity', 'failure', 'cancellation')) {
    [KillerUpdateGuard]::PublisherValid = $mode -ne 'publisher'
    [KillerUpdateGuard]::IdentityValid = $mode -ne 'identity'
    [KillerUpdateGuard]::FailLaunch = $mode -eq 'failure'
    [KillerUpdateGuard]::CancelLaunch = $mode -eq 'cancellation'
    $beforeReports = [KillerUpdateGuard]::Reports
    $rejected = $false
    try { & ([scriptblock]::Create($deferred)) } catch { $rejected = $true }
    if (-not $rejected -or [KillerUpdateGuard]::Launches -ne 1 -or [KillerUpdateGuard]::Reports -ne ($beforeReports + 1) -or
        [KillerUpdateGuard]::ParentHeld -or [KillerUpdateGuard]::ImageHeld) { throw "Deferred $mode did not retain correct failure semantics." }
    Write-Output "PASS: deferred $mode prevents process creation, reports failure and releases owned holds."
}
[KillerUpdateGuard]::PublisherValid = $true
[KillerUpdateGuard]::IdentityValid = $true
[KillerUpdateGuard]::FailLaunch = $false
[KillerUpdateGuard]::CancelLaunch = $false
$script:fastExit = $true
$script:registered.State = 3
$script:instance.State = 3
Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' ''
Write-Output 'PASS: a fast helper exit does not become a failed update or lose queued protection.'
$script:fastExit = $false
$script:registered.State = 2
$script:instance.State = 2
$beforeRequests = $script:requests
$script:descriptor = 'O:BAG:BAD:P(A;;GA;;;WD)'
$rejected = $false
try { Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' '' } catch { $rejected = $true }
if (-not $rejected -or $script:requests -ne $beforeRequests) { throw 'Untrusted task DACL reached dispatch.' }
$script:descriptor = 'O:BAG:BAD:P(A;;GA;;;SY)(A;;GA;;;BA)'
foreach ($mode in @('dispatch-failure', 'missing-instance')) {
    $script:runFailure = $mode -eq 'dispatch-failure'
    $script:noInstance = $mode -eq 'missing-instance'
    $beforeDeletes = $script:deletes
    $rejected = $false
    try { Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' '' } catch { $rejected = $true }
    if (-not $rejected -or $script:deletes -ne $beforeDeletes) { throw "Scheduler $mode was not reported without deleting an uncertain task." }
    Write-Output "PASS: scheduler $mode preserves uncertain independent verification."
}
$script:runFailure = $false; $script:noInstance = $false
foreach ($case in @('fresh', 'idle', 'queued', 'running', 'unknown', 'query-failure', 'refresh-failure')) {
    $running = @()
    if ($case -in @('queued', 'running', 'unknown', 'refresh-failure')) {
        $state = if ($case -eq 'queued') { 2 } elseif ($case -eq 'unknown') { 0 } else { 4 }
        $entry = [pscustomobject]@{State=$state; FailRefresh=($case -eq 'refresh-failure')}
        $entry | Add-Member ScriptMethod Refresh { if ($this.FailRefresh) { throw 'Fixture instance query became unavailable' } }
        $running = @($entry)
    }
    $date = if ($case -eq 'fresh') { (Get-Date).AddMinutes(-1) } else { (Get-Date).AddMinutes(-10) }
    $old = [pscustomobject]@{
        Name=('KillerUpdate-fixture-' + $case); State=3; Running=$running; FailQuery=($case -eq 'query-failure')
        Definition=[pscustomobject]@{RegistrationInfo=[pscustomobject]@{Date=$date.ToString('s')}}
    }
    $old | Add-Member ScriptMethod GetInstances { param($flags) if ($this.FailQuery) { throw 'Fixture instance enumeration unavailable' }; return @($this.Running) }
    $script:oldTasks = @($old)
    $beforeDeletes = $script:deletes
    Start-KillerUpdatedApp 'C:\Fixture\KillerBench.exe' '' $true ('B' * 40) 'Fixture publisher' ''
    $expected = if ($case -eq 'idle') { 1 } else { 0 }
    if ($script:deletes -ne ($beforeDeletes + $expected) -or ($expected -eq 1 -and -not $script:deletedNames.Contains($old.Name))) {
        throw "Stale-task cleanup violated the $case instance or registration state."
    }
}
$script:oldTasks = @()
Write-Output 'PASS: stale-task cleanup preserves fresh, queued, running and uncertain instances and removes only proven idle tasks.'
$codeMarker = '$killerGuardCode = @' + "'"
$codeStart = $source.IndexOf($codeMarker, [StringComparison]::Ordinal) + $codeMarker.Length
$codeEnd = $source.IndexOf("`n'@", $codeStart, [StringComparison]::Ordinal)
$realCode = $source.Substring($codeStart, $codeEnd - $codeStart).Replace('KillerUpdateGuard', 'KillerUpdateSizeGuard')
$sizeDll = Join-Path $fixture 'production-guard-size.dll'
Microsoft.PowerShell.Utility\Add-Type -TypeDefinition $realCode -OutputAssembly $sizeDll
$script:killerDeferredAssembly = [IO.File]::ReadAllBytes($sizeDll)
Start-KillerUpdatedApp ('C:\Fixture\' + ('x' * 120) + '\KillerBench.exe') ('C:\Documents\' + ('y' * 120) + '\reopen file.txt') $true ('B' * 40) 'Fixture publisher' ''
if ($script:action.Arguments.Length -gt 28000) { throw 'Production compiled helper exceeds command margin.' }
Write-Output "PASS: production guard compiles under Windows PowerShell and deferred long-path command uses $($script:action.Arguments.Length) characters."
if ($RecoveryArgumentsPath) {
    [KillerUpdateGuard]::ExpectedThumb = 'A' * 40
    $beforeRequests = $script:requests
    $command = Get-Content -Raw -LiteralPath $RecoveryArgumentsPath
    $match = [regex]::Match($command, "FromBase64String\('([^']+)'\)")
    $compressed = [IO.MemoryStream]::new([Convert]::FromBase64String($match.Groups[1].Value))
    $reader = [IO.StreamReader]::new([IO.Compression.GZipStream]::new($compressed, [IO.Compression.CompressionMode]::Decompress))
    try { $originalRecovery = $reader.ReadToEnd() } finally { $reader.Dispose(); $compressed.Dispose() }
    # Force only the process-wait boundary to time out before native mutex/publication work.
    $recovery = $originalRecovery.Replace('$p.WaitForExit(60000)', '$false')
    $recovery = $recovery.Replace('exit 1', 'return')
    & ([scriptblock]::Create($recovery)) 2>$null
    if ($script:requests -ne $beforeRequests) { throw 'A parent that did not exit must not trigger a recovery launch.' }
    Write-Output 'PASS: parent timeout prevents recovery launch while the original app may still run.'
    $recovery = $originalRecovery.Replace('$p.WaitForExit(60000)', '$true').Replace('exit 1', 'return')
    & ([scriptblock]::Create($recovery)) 2>$null
    if ($script:requests -ne ($beforeRequests + 1)) { throw 'Confirmed exits followed by mutex refusal lost original-certificate recovery.' }
    Write-Output 'PASS: confirmed exits and mutex refusal preserve original-certificate automatic relaunch across renewal.'

    $bodyStart = $recovery.IndexOf('function Invoke-UpdateImagePublication {', [StringComparison]::Ordinal)
    $bodyEnd = $recovery.LastIndexOf("`n}catch{`$message=", [StringComparison]::Ordinal)
    if ($bodyStart -lt 0 -or $bodyEnd -lt $bodyStart) { throw 'Emitted publication body boundary missing.' }
    $publicationBody = $recovery.Substring($bodyStart, $bodyEnd - $bodyStart)
    $dispatch = $publicationBody.IndexOf('if (Invoke-UpdateImagePublication $plan $ops)', [StringComparison]::Ordinal)
    if ($dispatch -lt 0) { throw 'Emitted publication dispatch missing.' }
    $substitutedNative = @'
$ops.OpenImage = { param($path) [KillerUpdateGuard]::OpenImage($path) }
$ops.Read = { ,[byte[]]@(1,2,3) }
$ops.Hash = { $plan.Hash }
$ops.HashStream = { '039058C6F2C0CB492C533B0A4D14EF77CC0F78ABCCCED5287D84A1A2011CFB81' }
$ops.Publisher = { $true }
$ops.PinParents = { param($path) [KillerUpdateGuard]::PinParents($path) }
$ops.Exists = { $true }
$ops.Stage = { throw 'Fixture staging failure before replacement' }
$ops.Report = { }
$ops.Releases = { }
'@
    $publicationBody = $publicationBody.Insert($dispatch, $substitutedNative + "`n")
    $beforeRequests = $script:requests
    & ([scriptblock]::Create($publicationBody))
    if ($script:requests -ne ($beforeRequests + 1)) { throw 'Internal publication failure lost original-certificate automatic relaunch.' }
    Write-Output 'PASS: internal staging failure recovers the original certificate through deferred relaunch.'
}
