using KillerBench.Engine;

internal static class NativeUpdateChecks
{
    internal static async Task RunAsync()
    {
        var selection = new UpdateSelection(Guid.Parse("74ba239e-08d2-45bd-b285-b0d35610ca60"), 1);
        const string fixture = """
            $script:installed=$false; $script:downloaded=$false; $script:rebootKey=$false;
            function Test-Path { param($Path) $script:rebootKey -and $Path -like '*Component Based Servicing\RebootPending' };
            function Get-ItemProperty { param($Path,$ErrorAction) $null };
            $script:u=[pscustomobject]@{Identity=[pscustomobject]@{UpdateID='74ba239e-08d2-45bd-b285-b0d35610ca60';RevisionNumber=1};EulaAccepted=$true;IsBeta=$false;IsDownloaded=$false;Categories=@();InstallationBehavior=[pscustomobject]@{CanRequestUserInput=$false;Impact=0;RebootBehavior=0}};
            $script:coll=[pscustomobject]@{Count=1}; $script:coll | Add-Member ScriptMethod Item {param($i) if($i -ne 0){throw 'BAD_INDEX'}; $script:u};
            $script:coll | Add-Member ScriptMethod Add {param($u) if($u -ne $script:u){throw 'BAD_TARGET'}; 0};
            $script:q=[pscustomobject]@{ServerSelection=-1;ServiceID=''}; $script:q | Add-Member ScriptMethod Search {param($criteria)
                if($criteria -notlike "*UpdateID='74ba239e-08d2-45bd-b285-b0d35610ca60' and RevisionNumber=1"){throw 'BAD_CRITERIA'};
                if($criteria.StartsWith('IsInstalled=1') -and !$script:installed){throw 'VERIFY_BEFORE_INSTALL'};
                [pscustomobject]@{ResultCode=2;Updates=$script:coll}
            };
            $script:dr=[pscustomobject]@{ResultCode=2}; $script:dr | Add-Member ScriptMethod GetUpdateResult {param($i) [pscustomobject]@{ResultCode=2}};
            $script:d=[pscustomobject]@{Updates=$null}; $script:d | Add-Member ScriptMethod Download {$script:downloaded=$true; $script:u.IsDownloaded=$true; $script:dr};
            $script:ir=[pscustomobject]@{ResultCode=2;RebootRequired=$false}; $script:ir | Add-Member ScriptMethod GetUpdateResult {param($i) [pscustomobject]@{ResultCode=2;HResult=0;RebootRequired=$false}};
            $script:installer=[pscustomobject]@{IsBusy=$false;RebootRequiredBeforeInstallation=$false;Updates=$null;AllowSourcePrompts=$true;ForceQuiet=$false};
            $script:installer | Add-Member ScriptMethod Install {
                if(!$script:downloaded -or !$this.ForceQuiet -or $this.AllowSourcePrompts -or $this.Updates -ne $script:coll){throw 'UNSAFE_INSTALL_SEQUENCE'};
                $script:installed=$true; $script:ir
            };
            $script:s=[pscustomobject]@{ClientApplicationID=''};
            $script:s | Add-Member ScriptMethod CreateUpdateSearcher {$script:q};
            $script:s | Add-Member ScriptMethod CreateUpdateDownloader {$script:d};
            $script:s | Add-Member ScriptMethod CreateUpdateInstaller {$script:installer};
            $script:manager=[pscustomobject]@{Services=@()};
            function New-Object {param($ComObject) switch($ComObject){'Microsoft.Update.Session' {$script:s} 'Microsoft.Update.ServiceManager' {$script:manager} 'Microsoft.Update.UpdateColl' {$script:coll} default {throw 'UNEXPECTED_COM'}}};
            """;
        foreach (bool license in new[] { true, false })
        {
            var step = WindowsUpdateExecutionHost.InstallationStep(new(), selection, new());
            var args = step.Arguments.ToArray();
            args[^1] = fixture + (license ? "" : "$script:u.EulaAccepted=$false; ") + args[^1];
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await DefenderRecovery.ExecuteAsync(step with { Arguments = args }, token: stop.Token);
            if (license && !WindowsUpdateExecutionHost.Interpret(result, selection).Installed)
                throw new Exception("Substituted update install failed: " + result.Error);
            if (!license && (result.ExitCode == 0 || !result.Error.Contains("License or interaction")))
                throw new Exception("License gate did not stop substituted update installation");
        }
        foreach (bool allowed in new[] { false, true })
        {
            var step = WindowsUpdateExecutionHost.InstallationStep(new(), selection, new(true, allowed));
            var args = step.Arguments.ToArray();
            args[^1] = fixture + "$script:u.Categories=@([pscustomobject]@{Type='UpdateClassification';CategoryID='" +
                UpdatePlanning.FeatureUpgradeClassification.ToString("D") + "'}); " + args[^1];
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await DefenderRecovery.ExecuteAsync(step with { Arguments = args }, token: stop.Token);
            if (allowed && !WindowsUpdateExecutionHost.Interpret(result, selection).Installed)
                throw new Exception("Explicit feature-upgrade confirmation did not reach substituted installation: " + result.Error);
            if (!allowed && (result.ExitCode == 0 || !result.Error.Contains("Feature upgrade confirmation")))
                throw new Exception("Native feature-upgrade gate did not require separate confirmation");
        }
        foreach (var (change, why) in new[]
        {
            ("$script:installer.RebootRequiredBeforeInstallation=$true", "the installer requiring a restart"),
            ("$script:installer.IsBusy=$true", "another installation starting"),
            ("$script:rebootKey=$true", "a servicing restart becoming pending")
        })
        {
            var step = WindowsUpdateExecutionHost.InstallationStep(new(), selection, new());
            var args = step.Arguments.ToArray();
            // Readiness changes while the selected update downloads.
            args[^1] = fixture + "$script:d | Add-Member -Force ScriptMethod Download {$script:downloaded=$true; $script:u.IsDownloaded=$true; " + change + "; $script:dr}; " + args[^1];
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result = await DefenderRecovery.ExecuteAsync(step with { Arguments = args }, token: stop.Token);
            var outcome = WindowsUpdateExecutionHost.Interpret(result, selection);
            if (outcome.Installed || outcome.InstallDispatched || !outcome.Detail.Contains("not started", StringComparison.Ordinal) || result.Output.Contains("UNSAFE_INSTALL"))
                throw new Exception("Installation must not start after " + why + " during download: " + outcome + " " + result.Output + result.Error);
        }
        Console.WriteLine("PASS: native update command sequence, license gate and separate feature-upgrade confirmation with substituted COM objects only.");
        Console.WriteLine("PASS: readiness is rechecked after download and a change stops installation before Install with a distinct pre-install blocker.");
    }
}
