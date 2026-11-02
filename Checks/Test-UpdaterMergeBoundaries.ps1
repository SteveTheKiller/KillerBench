param([Parameter(Mandatory = $true)][string]$EmittedScriptPath,
    [Parameter(Mandatory = $true)][string]$InstallerScriptPath,
    [Parameter(Mandatory = $true)][string]$WorkRoot)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2
$root = [IO.Path]::GetFullPath($WorkRoot).TrimEnd('\')
$scratch = [IO.Path]::GetFullPath((Join-Path $env:USERPROFILE 'scratch')).TrimEnd('\') + '\'
if (-not $root.StartsWith($scratch, [StringComparison]::OrdinalIgnoreCase) -or -not [IO.Directory]::Exists($root)) { throw 'An owned scratch root is required.' }
$emitted = [IO.Path]::GetFullPath($EmittedScriptPath)
if (-not $emitted.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'The emitted artifact must be inside the owned root.' }
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($emitted, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'The actual emitted publication artifact did not parse.' }
$body = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-UpdateImagePublication' }, $true)
$opsAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$ops' }, $true)
$planAst = $ast.Find({ param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$plan' }, $true)
$dispatch = $ast.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.IfStatementAst] -and $_.Extent.Text.StartsWith('if (Invoke-UpdateImagePublication $plan $ops)') } | Select-Object -Last 1
if ($null -eq $body -or $null -eq $opsAst -or $null -eq $planAst -or $null -eq $dispatch) { throw 'The emitted publication core/plan/operations/dispatch were not found.' }
Invoke-Expression $body.Extent.Text
Invoke-Expression $planAst.Extent.Text
$template = $plan
# Only the process-exit boundary is replaced in the actual emitted dispatch. Guard compilation,
# Scheduler, registry, installer and application launches are never executed by this fixture.
$dispatchText = $dispatch.Extent.Text.Replace('exit 0', '$script:fixtureExit = 0').Replace('exit 1', '$script:fixtureExit = 1')
$script:results = New-Object Collections.Generic.List[object]
$script:waits = 0
$script:onWait = $null
function Start-Sleep { param($Seconds, $Milliseconds) $script:waits++; if ($null -ne $script:onWait) { & $script:onWait } }
function Assert-Case([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Throw-FileError([int]$code) { throw [IO.IOException]::new(('Owned fixture Windows error ' + $code), (-2147024896 + $code)) }
function New-Case([string]$name, [bool]$existing = $true) {
    $folder = Join-Path $root ($name + '-' + [Guid]::NewGuid().ToString('N'))
    $null = [IO.Directory]::CreateDirectory($folder)
    $script:plan = $template.PSObject.Copy()
    foreach ($field in @('Download', 'Target', 'Staged', 'Backup', 'Restore', 'Report')) { $script:plan.$field = Join-Path $folder ($field.ToLowerInvariant() + '.bin') }
    $script:plan | Add-Member NoteProperty Unsafe $false -Force
    [IO.File]::WriteAllBytes($script:plan.Download, [byte[]]@(1,7,3,9,2))
    if ($existing) { [IO.File]::WriteAllText($script:plan.Target, 'Owned original image') }
    Invoke-Expression $opsAst.Extent.Text
    $script:ops = $ops
    $script:waits = 0; $script:onWait = $null
    $script:launches = 0; $script:recoveries = 0; $script:metadata = 0; $script:swapCalls = 0; $script:deleteCalls = 0
    $script:baseSwap = if ($ops.ContainsKey('Swap')) { $ops.Swap } else { $null }
    $script:baseDelete = $ops.Delete
    $script:fixtureExit = -1
    $script:ops.PinParents = { param($path) [IO.MemoryStream]::new() }
    $script:ops.OpenImage = { param($path) [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read) }
    $script:ops.Publisher = { param($path, $plan) $true }
    $script:ops.Metadata = { param($plan) $script:metadata++ }
    $script:ops.Relaunch = { param($plan) $script:launches++ }
    $script:ops.Recover = { param($plan) $script:recoveries++ }
    $script:ops.ShowReport = { param($path) throw 'Fixture must not open a report application.' }
    return $folder
}
function Run-Case([string]$name, [scriptblock]$action) {
    try { & $action; $script:results.Add([pscustomobject]@{scenario=$name;outcome='Passed'}); Write-Output ('PASS: ' + $name) }
    catch { $script:results.Add([pscustomobject]@{scenario=$name;outcome='Failed';detail=$_.Exception.Message}); Write-Output ('FAIL: ' + $name + ': ' + $_.Exception.Message) }
}
foreach ($failure in @('hash', 'publisher')) {
    foreach ($backup in @($true, $false)) {
        Run-Case ("postpublish-$failure-backup-$backup") {
            $null = New-Case ("post-$failure-$backup") $backup
            $script:corrupt = $failure -eq 'hash'
            if ($failure -eq 'hash') {
                $ops.OpenImage = { param($path)
                    if ($path -eq $plan.Target -and $script:corrupt) { [IO.File]::WriteAllText($path, 'Owned unverified corruption'); $script:corrupt = $false }
                    [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
                }
            } else { $ops.Publisher = { param($path, $plan) $path -ne $plan.Target } }
            Invoke-Expression $dispatchText
            Assert-Case ($script:fixtureExit -eq 1) 'Postpublication verification failure must return failure.'
            if ($backup) { Assert-Case ([IO.File]::ReadAllText($plan.Target) -eq 'Owned original image') 'Postpublication failure did not restore the original backup.' }
            else { Assert-Case (-not [IO.File]::Exists($plan.Target)) 'Postpublication failure left an unverified target with no backup.' }
            Assert-Case (-not $plan.Unsafe) 'Completed rollback must clear Unsafe.'
            Assert-Case ($script:launches -eq 0 -and $script:metadata -eq 0) 'A failed publication reached success launch or metadata.'
            $message = [IO.File]::ReadAllText($plan.Report)
            Assert-Case ($message.Contains('Update publication failed:') -and $message.Contains($plan.Backup) -and $message.Contains($plan.Restore) -and -not $message.Contains('Update completed')) 'Failure text or recovery paths were lost or falsely reported completed.'
            Assert-Case ([IO.File]::Exists($plan.Download)) 'Failure cleanup discarded the retained download.'
        }
    }
}
Run-Case 'rollback-failure-unsafe-no-launch' {
    $null = New-Case 'rollback-failure'
    $ops.Publisher = { param($path, $plan) $path -ne $plan.Target }
    $ops.Restore = { param($backup, $target, $restore) [IO.File]::WriteAllText($restore, 'Owned retained restoration evidence'); Throw-FileError 5 }
    Invoke-Expression $dispatchText
    Assert-Case ($script:fixtureExit -eq 1 -and $plan.Unsafe) 'Failed rollback did not leave Unsafe true.'
    Assert-Case ($script:launches -eq 0 -and $script:recoveries -eq 0) 'Failed rollback attempted to launch or recover an unsafe image.'
    Assert-Case ([IO.File]::Exists($plan.Backup) -and [IO.File]::Exists($plan.Restore) -and [IO.File]::Exists($plan.Download)) 'Failed rollback discarded its owned recovery evidence.'
    Assert-Case ([IO.File]::ReadAllText($plan.Report).Contains('Restoration failed:')) 'Rollback failure report lost the restoration failure.'
}
Run-Case 'replacement-two-sharing-errors-then-success' {
    $null = New-Case 'replacement-retry'
    Assert-Case ($ops.ContainsKey('Retry') -and $ops.ContainsKey('Swap')) 'Emitted operations lack the bounded Retry/Swap contract.'
    [IO.File]::WriteAllText($plan.Staged, 'Owned replacement')
    $ops.Swap = { param($source, $target, $backup) $script:swapCalls++; if ($script:swapCalls -le 2) { Throw-FileError 32 }; & $script:baseSwap $source $target $backup }
    & $ops.Replace $plan.Staged $plan.Target $plan.Backup
    Assert-Case ($script:swapCalls -eq 3 -and $script:waits -eq 2 -and [IO.File]::ReadAllText($plan.Target) -eq 'Owned replacement' -and [IO.File]::ReadAllText($plan.Backup) -eq 'Owned original image') 'Replacement did not retry exactly two sharing errors and preserve the original.'
}
Run-Case 'restoration-two-lock-errors-then-success' {
    $null = New-Case 'restore-retry'
    Assert-Case ($ops.ContainsKey('Retry') -and $ops.ContainsKey('Swap')) 'Emitted operations lack the bounded Retry/Swap contract.'
    [IO.File]::WriteAllText($plan.Backup, 'Owned original backup')
    $ops.Swap = { param($source, $target, $backup) $script:swapCalls++; if ($script:swapCalls -le 2) { Throw-FileError 33 }; & $script:baseSwap $source $target $backup }
    & $ops.Restore $plan.Backup $plan.Target $plan.Restore
    Assert-Case ($script:swapCalls -eq 3 -and $script:waits -eq 2 -and [IO.File]::ReadAllText($plan.Target) -eq 'Owned original backup' -and [IO.File]::Exists($plan.Backup)) 'Restoration did not retry lock errors while retaining its backup.'
}
Run-Case 'removal-two-sharing-errors-then-success' {
    $null = New-Case 'remove-retry' $false
    Assert-Case ($ops.ContainsKey('Retry')) 'Emitted operations lack bounded removal retry.'
    $ops.Publisher = { param($path, $plan) $path -ne $plan.Target }
    $ops.Delete = { param($path) $script:deleteCalls++; if ($script:deleteCalls -le 2) { Throw-FileError 32 }; & $script:baseDelete $path }
    Invoke-Expression $dispatchText
    Assert-Case ($script:fixtureExit -eq 1 -and $script:deleteCalls -eq 3 -and $script:waits -eq 2 -and -not [IO.File]::Exists($plan.Target) -and -not $plan.Unsafe) 'Unverified-target removal did not retry exactly twice and finish safe.'
}
Run-Case 'nonsharing-error-no-retry' {
    $null = New-Case 'no-retry'
    Assert-Case ($ops.ContainsKey('Retry') -and $ops.ContainsKey('Swap')) 'Emitted operations lack bounded Retry/Swap.'
    [IO.File]::WriteAllText($plan.Staged, 'Owned replacement')
    $ops.Swap = { param($source, $target, $backup) $script:swapCalls++; Throw-FileError 5 }
    $failed = $false; try { & $ops.Replace $plan.Staged $plan.Target $plan.Backup } catch { $failed = $true }
    Assert-Case ($failed -and $script:swapCalls -eq 1 -and $script:waits -eq 0) 'A nonsharing error was retried or accepted.'
}
foreach ($change in @('source-before-wait', 'target-during-wait', 'backup-before-wait')) {
    Run-Case ("retry-refuses-$change") {
        $null = New-Case $change
        Assert-Case ($ops.ContainsKey('Retry') -and $ops.ContainsKey('Swap')) 'Emitted operations lack bounded Retry/Swap.'
        [IO.File]::WriteAllText($plan.Staged, 'Owned replacement')
        $script:change = $change
        $ops.Swap = { param($source, $target, $backup)
            $script:swapCalls++
            if ($script:change -eq 'source-before-wait') { [IO.File]::WriteAllText($source, 'Owned changed source') }
            if ($script:change -eq 'backup-before-wait') { [IO.File]::WriteAllText($backup, 'Owned unexpected backup') }
            Throw-FileError 32
        }
        if ($change -eq 'target-during-wait') { $script:onWait = { [IO.File]::WriteAllText($plan.Target, 'Owned changed target') } }
        $failed = $false; try { & $ops.Replace $plan.Staged $plan.Target $plan.Backup } catch { $failed = $true }
        $expectedWaits = if ($change -eq 'target-during-wait') { 1 } else { 0 }
        Assert-Case ($failed -and $script:swapCalls -eq 1 -and $script:waits -eq $expectedWaits) 'Changed source/target/backup state reached another replacement attempt.'
    }
}
$installerPath = [IO.Path]::GetFullPath($InstallerScriptPath)
if (-not $installerPath.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Installer artifact must be inside the owned root.' }
$installerAst = [Management.Automation.Language.Parser]::ParseFile($installerPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'The actual emitted installer artifact did not parse.' }
$installerBody = $installerAst.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-UpdateInstaller' }, $true)
$installerPlan = $installerAst.Find({ param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -eq '$plan' }, $true)
$installerAssignments = @($installerAst.EndBlock.Statements | Where-Object { $_ -is [Management.Automation.Language.AssignmentStatementAst] -and $_.Left.Extent.Text -match '^\$ops(?:\.|$)' })
if ($null -eq $installerBody -or $null -eq $installerPlan -or $installerAssignments.Count -eq 0) { throw 'Actual emitted installer core/plan/operations were not found.' }
Invoke-Expression $installerBody.Extent.Text
Invoke-Expression $installerPlan.Extent.Text
$installerTemplate = $plan
# These disposable counters substitute only native holds. Actual TargetIdentity/CaptureOriginal/
# Recover/RelaunchInstalled script logic remains the emitted implementation, never a fixture copy.
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class FixtureUpdateIdentityHolds { public static int Parents, TargetImages; public static string Target; }
public sealed class FixtureUpdateParent : IDisposable {
    private bool disposed;
    public FixtureUpdateParent() { FixtureUpdateIdentityHolds.Parents++; }
    public void Dispose() { if (!disposed) { disposed = true; FixtureUpdateIdentityHolds.Parents--; } }
}
public sealed class FixtureUpdateImage : FileStream {
    private bool disposed, target;
    public FixtureUpdateImage(string path) : base(path, FileMode.Open, FileAccess.Read, FileShare.Read) {
        target = String.Equals(path, FixtureUpdateIdentityHolds.Target, StringComparison.OrdinalIgnoreCase);
        if (target) FixtureUpdateIdentityHolds.TargetImages++;
    }
    protected override void Dispose(bool disposing) {
        if (!disposed) { disposed = true; if (target) FixtureUpdateIdentityHolds.TargetImages--; }
        base.Dispose(disposing);
    }
}
'@
function Get-OwnedHash([string]$path) {
    $hash = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($hash.ComputeHash([IO.File]::ReadAllBytes($path))).Replace('-', '') } finally { $hash.Dispose() }
}
function New-IdentityCase([string]$name, [string]$original = 'signed') {
    $folder = Join-Path $root ($name + '-' + [Guid]::NewGuid().ToString('N'))
    $null = [IO.Directory]::CreateDirectory($folder)
    $script:plan = $installerTemplate.PSObject.Copy()
    $script:plan.Download = Join-Path $folder 'setup.bin'
    $script:plan.Target = Join-Path $folder 'application.bin'
    $script:plan.StageRoot = Join-Path $folder 'stage'
    $script:plan.Report = Join-Path $folder 'failure-report.txt'
    # Initial data only: old helpers omit these fields; the new core must capture them itself.
    foreach ($field in @('OriginalHash', 'OriginalThumb')) { if ($null -eq $plan.PSObject.Properties[$field]) { $plan | Add-Member NoteProperty $field '' } }
    if ($null -eq $plan.PSObject.Properties['IsInstaller']) { $plan | Add-Member NoteProperty IsInstaller $true }
    [IO.File]::WriteAllBytes($plan.Download, [byte[]]@(1,7,3,9,2))
    $script:oldContent = 'Owned original application identity'
    $script:newContent = 'Owned successful installed application, distinct from setup'
    if ($original -ne 'missing') { [IO.File]::WriteAllText($plan.Target, $script:oldContent) }
    $script:targetThumb = if ($original -eq 'unsigned' -or $original -eq 'missing') { '' } else { 'B' * 40 }
    foreach ($assignment in $installerAssignments) { Invoke-Expression $assignment.Extent.Text }
    $script:ops = $ops
    $script:launchAttempts = 0; $script:identityLaunches = 0; $script:installerRuns = 0; $script:originalCapturedBeforeMutation = $false
    $script:oldHash = if ($original -eq 'missing') { '' } else { Get-OwnedHash $plan.Target }
    $script:expectedLaunchHash = $script:oldHash; $script:expectedLaunchThumb = 'B' * 40
    $script:installerMode = 'failure-unchanged'
    [FixtureUpdateIdentityHolds]::Parents = 0; [FixtureUpdateIdentityHolds]::TargetImages = 0; [FixtureUpdateIdentityHolds]::Target = $plan.Target
    $ops.PinParents = { param($path) [FixtureUpdateParent]::new() }
    $ops.OpenImage = { param($path) [FixtureUpdateImage]::new($path) }
    $ops.VerifiedTargetThumb = { param($path, $signer) if ([IO.File]::Exists($path) -and $script:targetThumb) { $script:targetThumb } else { '' } }
    $ops.Publisher = { param($path, $selected)
        $thumb = if ($path -eq $plan.Target) { $script:targetThumb } else { 'A' * 40 }
        -not [string]::IsNullOrEmpty($thumb) -and $selected.Thumb -eq $thumb
    }
    $ops.StageFolder = { param($selected)
        $script:originalCapturedBeforeMutation = $selected.OriginalHash -eq $script:oldHash -and $selected.OriginalThumb -eq $script:targetThumb
        $path = Join-Path $selected.StageRoot 'owned'
        $null = [IO.Directory]::CreateDirectory($path)
        $path
    }
    $ops.Run = { param($path, $arguments)
        $script:installerRuns++
        if ($script:installerMode -eq 'failure-changed') { [IO.File]::WriteAllBytes($plan.Target, [byte[]]@(1,7,3,9,2)); $script:targetThumb = 'A' * 40; return 5 }
        if ($script:installerMode -eq 'success' -or $script:installerMode -eq 'success-unsigned') {
            [IO.File]::WriteAllText($plan.Target, $script:newContent)
            $script:targetThumb = if ($script:installerMode -eq 'success-unsigned') { '' } else { 'A' * 40 }
            return 0
        }
        return 5
    }
    $ops.Relaunch = { param($selected)
        $script:launchAttempts++
        Assert-Case ($selected.Hash -eq $script:expectedLaunchHash -and $selected.Thumb -eq $script:expectedLaunchThumb) 'Relaunch selected setup or unrelated identity instead of the verified application target.'
        Assert-Case ([FixtureUpdateIdentityHolds]::Parents -gt 0 -and [FixtureUpdateIdentityHolds]::TargetImages -gt 0) 'Relaunch lost the selected target or parent hold.'
        $script:identityLaunches++
    }
    $ops.Releases = { param($url) }
    $ops.Metadata = { param($selected) throw 'Installer identity fixture must not write metadata.' }
    return $folder
}
function Assert-IdentityReleased {
    Assert-Case ([FixtureUpdateIdentityHolds]::Parents -eq 0 -and [FixtureUpdateIdentityHolds]::TargetImages -eq 0) 'Target identity holds leaked after the operation.'
}
Run-Case 'installer-failure-recovers-unchanged-old-target-across-renewal' {
    $null = New-IdentityCase 'installer-old-renewal'
    $ok = Invoke-UpdateInstaller $plan $ops
    Assert-Case (-not $ok -and $script:installerRuns -eq 1) 'The owned installer failure was not exercised.'
    Assert-Case ($script:identityLaunches -eq 1) 'Failed installer did not recover the unchanged old-certificate application.'
    Assert-Case ($script:originalCapturedBeforeMutation) 'Original application identity was not captured before staging mutation.'
    Assert-IdentityReleased
}
Run-Case 'installer-failure-refuses-changed-target-even-matching-new-setup-identity' {
    $null = New-IdentityCase 'installer-changed'
    $script:installerMode = 'failure-changed'
    $ok = Invoke-UpdateInstaller $plan $ops
    Assert-Case (-not $ok -and $script:installerRuns -eq 1) 'Changed-target installer failure was not exercised.'
    Assert-Case ($script:launchAttempts -eq 0) 'Failed installer attempted recovery of a changed/new target.'
    Assert-IdentityReleased
}
foreach ($original in @('missing', 'unsigned')) {
    Run-Case ("installer-$original-original-allows-run-without-failed-recovery") {
        $null = New-IdentityCase ("installer-$original") $original
        $script:installerMode = 'failure-changed'
        $ok = Invoke-UpdateInstaller $plan $ops
        Assert-Case (-not $ok -and $script:installerRuns -eq 1) 'An unknown original prevented safe installer generation or dispatch.'
        Assert-Case ($script:launchAttempts -eq 0) 'Unknown original authorized failed automatic recovery.'
        Assert-IdentityReleased
    }
}
Run-Case 'installer-success-selects-actual-application-digest-not-setup' {
    $null = New-IdentityCase 'installer-success'
    $script:installerMode = 'success'
    $script:expectedLaunchHash = (& $ops.Hash ([Text.Encoding]::UTF8.GetBytes($script:newContent)))
    $script:expectedLaunchThumb = 'A' * 40
    $ok = Invoke-UpdateInstaller $plan $ops
    Assert-Case ($ok -and $script:installerRuns -eq 1 -and $script:identityLaunches -eq 1) 'Successful installer did not launch the actual verified installed application identity.'
    Assert-IdentityReleased
}
Run-Case 'installer-success-refuses-unverified-application-publisher' {
    $null = New-IdentityCase 'installer-unsigned-success'
    $script:installerMode = 'success-unsigned'
    $ok = Invoke-UpdateInstaller $plan $ops
    Assert-Case ($ok -and $script:installerRuns -eq 1 -and $script:launchAttempts -eq 0) 'Successful installer attempted to launch an unverified application target.'
    Assert-IdentityReleased
}
foreach ($kind in @('image', 'installer')) {
    foreach ($change in @('hash', 'publisher')) {
        Run-Case ("$kind-recovery-refuses-wrong-$change") {
            $null = New-IdentityCase ("$kind-recover-$change")
            $plan.IsInstaller = $kind -eq 'installer'
            $plan.OriginalHash = $script:oldHash; $plan.OriginalThumb = 'B' * 40
            if ($change -eq 'hash') { [IO.File]::WriteAllText($plan.Target, 'Owned different target identity') }
            else { $script:targetThumb = '' }
            try { & $ops.Recover $plan } catch { }
            Assert-Case ($script:launchAttempts -eq 0) 'Recovery reached Relaunch for an identity outside its exact permitted pairs.'
            Assert-IdentityReleased
        }
    }
    Run-Case ("$kind-recovery-keeps-original-hash-and-thumb-held") {
        $null = New-IdentityCase ("$kind-recover-original")
        $plan.IsInstaller = $kind -eq 'installer'
        $plan.OriginalHash = $script:oldHash; $plan.OriginalThumb = 'B' * 40
        & $ops.Recover $plan
        Assert-Case ($script:identityLaunches -eq 1) 'Exact original recovery lost its hash, certificate or held target.'
        Assert-IdentityReleased
    }
}
Run-Case 'image-recovery-allows-exact-new-image-identity' {
    $null = New-IdentityCase 'image-recover-new'
    $plan.IsInstaller = $false
    $plan.OriginalHash = $script:oldHash; $plan.OriginalThumb = 'B' * 40
    [IO.File]::WriteAllBytes($plan.Target, [byte[]]@(1,7,3,9,2)); $script:targetThumb = 'A' * 40
    $script:expectedLaunchHash = $plan.Hash; $script:expectedLaunchThumb = $plan.Thumb
    & $ops.Recover $plan
    Assert-Case ($script:identityLaunches -eq 1) 'Exact new image recovery was incorrectly refused.'
    Assert-IdentityReleased
}
$result = [pscustomobject]@{schemaVersion=1;selected=$script:results.Count;passed=@($script:results | Where-Object outcome -eq 'Passed').Count;failed=@($script:results | Where-Object outcome -eq 'Failed').Count;results=$script:results;queuedRecoveryCoverage='Publication/installer identity and recovery boundaries exercised; deferred Scheduler report preservation remains in the coordinator-owned queued fixture.'}
$resultPath = Join-Path $root 'updater-merge-boundaries-results.json'
if ([IO.File]::Exists($resultPath)) { throw 'Owned result already exists.' }
[IO.File]::WriteAllText($resultPath, ($result | ConvertTo-Json -Depth 10))
Write-Output ('RESULT: ' + $resultPath)
if ($result.selected -eq 0 -or $result.failed -ne 0) { exit 1 }
exit 0
