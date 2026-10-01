using System.Text.Json;

namespace KillerBench.Engine.Windows.Updates;

public sealed class WindowsUpdateExecutionHost : IUpdateExecutionHost, IUpdateReconciliationHost
{
    public async Task<bool> IsInstalledAndReadyAsync(UpdateSearchOptions search, UpdateSelection selection, CancellationToken token)
    {
        if (selection.Id == Guid.Empty || selection.Revision < 0) return false;
        if (await ReadinessAsync(token).ConfigureAwait(false) is not null) return false;
        search.Criteria();
        string script = "$ErrorActionPreference='Stop'; $s=New-Object -ComObject Microsoft.Update.Session; $searcher=$s.CreateUpdateSearcher(); $manager=New-Object -ComObject Microsoft.Update.ServiceManager; " + search.ConfigureSearcher() +
            "$r=$searcher.Search(\"IsInstalled=1 and UpdateID='" + selection.Id.ToString("D") + "' and RevisionNumber=" + selection.Revision + "\"); " +
            "$i=$s.CreateUpdateInstaller(); if([int]$r.ResultCode -eq 2 -and $r.Updates.Count -eq 1 -and !$i.IsBusy -and !$i.RebootRequiredBeforeInstallation){'VERIFIED_INSTALLED'}";
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromMinutes(5), token: token).ConfigureAwait(false);
        return result.ExitCode == 0 && !result.Truncated && !result.TimedOut && result.Output.Trim() == "VERIFIED_INSTALLED" &&
            await ReadinessAsync(token).ConfigureAwait(false) is null;
    }

    public Task<string?> ReadinessAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(!OperatingSystem.IsWindows() ? "Windows is required." :
            !WindowsState.IsAdministrator() ? "Administrator access is required." :
            WindowsState.RebootPending() ? "Restart and reassess before installation." : null);
    }

    public Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token) => UpdateDiscovery.SearchAsync(options, token: token);

    public async Task<UpdateExecutionOutcome> InstallAndVerifyAsync(UpdateSearchOptions search, UpdateSelection selection, UpdateInstallOptions options, CancellationToken token)
    {
        string? blocker = await ReadinessAsync(token).ConfigureAwait(false);
        if (blocker is not null) return new(selection, false, false, blocker);
        var result = await BoundedTool.RunAsync(InstallationStep(search, selection, options), TimeSpan.FromHours(1), token: token).ConfigureAwait(false);
        return Interpret(result, selection);
    }

    public static RepairStep InstallationStep(UpdateSearchOptions search, UpdateSelection selection, UpdateInstallOptions options)
    {
        search.Criteria();
        if (selection.Id == Guid.Empty || selection.Revision < 0) throw new ArgumentException("Invalid update identity.");
        string identity = $"UpdateID='{selection.Id:D}' and RevisionNumber={selection.Revision}";
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; Set-StrictMode -Version 2; " +
            "$s=New-Object -ComObject Microsoft.Update.Session; $s.ClientApplicationID='KillerBench'; $searcher=$s.CreateUpdateSearcher(); $manager=New-Object -ComObject Microsoft.Update.ServiceManager; " + search.ConfigureSearcher() +
            "$r=$searcher.Search(\"IsInstalled=0 and IsHidden=0 and " + identity + "\"); if([int]$r.ResultCode -ne 2 -or $r.Updates.Count -ne 1){throw 'Selected update is no longer uniquely available'}; " +
            "$u=$r.Updates.Item(0); $b=$u.InstallationBehavior; if($u.EulaAccepted -ne $true -or $u.IsBeta -ne $false -or $b.CanRequestUserInput -ne $false){throw 'License or interaction requirements block installation'}; " +
            "$feature=@($u.Categories|Where-Object{$_.Type -eq 'UpdateClassification' -and $_.CategoryID -eq '" + UpdatePlanning.FeatureUpgradeClassification.ToString("D") + "'}).Count -eq 1; " +
            (options.AllowFeatureUpgrade ? "" : "if($feature){throw 'Feature upgrade confirmation is required'}; ") +
            (options.AllowRestartRequired ? "" : "if($feature){throw 'Feature upgrade restart allowance is required'}; ") +
            "if([int]$b.Impact -notin 0,1,2 -or [int]$b.RebootBehavior -notin 0,1,2){throw 'Unknown installation behavior'}; " +
            (options.AllowRestartRequired ? "" : "if([int]$b.RebootBehavior -ne 0){throw 'Restart allowance is required'}; ") +
            "$c=New-Object -ComObject Microsoft.Update.UpdateColl; [void]$c.Add($u); " +
            "$installer=$s.CreateUpdateInstaller(); if($installer.IsBusy -or $installer.RebootRequiredBeforeInstallation){throw 'Installer is busy or needs a restart'}; " +
            "if(!$u.IsDownloaded){$d=$s.CreateUpdateDownloader(); $d.Updates=$c; $dr=$d.Download(); if([int]$dr.ResultCode -ne 2 -or [int]$dr.GetUpdateResult(0).ResultCode -ne 2 -or !$u.IsDownloaded){throw 'Download did not complete'}}; " +
            "$installer.Updates=$c; $installer.AllowSourcePrompts=$false; $installer.ForceQuiet=$true; $ir=$installer.Install(); $one=$ir.GetUpdateResult(0); " +
            "$v=$searcher.Search(\"IsInstalled=1 and " + identity + "\"); $verified=([int]$v.ResultCode -eq 2 -and $v.Updates.Count -eq 1); " +
            "[pscustomobject]@{id=$u.Identity.UpdateID; revision=$u.Identity.RevisionNumber; code=[int]$ir.ResultCode; itemCode=[int]$one.ResultCode; hresult=[int]$one.HResult; restart=[bool]($ir.RebootRequired -or $one.RebootRequired); verified=[bool]$verified} | ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], false);
    }

    public static UpdateExecutionOutcome Interpret(ToolResult result, UpdateSelection selection)
    {
        UpdateExecutionOutcome Unknown() => new(selection, false, false, "Installation is unverified. Review the operation journal and Windows Update before retrying.");
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var r = document.RootElement;
            if (!Guid.TryParse(r.GetProperty("id").GetString(), out var id) || id != selection.Id || r.GetProperty("revision").GetInt32() != selection.Revision) return Unknown();
            bool restart = r.GetProperty("restart").GetBoolean();
            bool installed = r.GetProperty("code").GetInt32() == 2 && r.GetProperty("itemCode").GetInt32() == 2 &&
                r.GetProperty("hresult").GetInt32() >= 0 && r.GetProperty("verified").GetBoolean();
            return new(selection, installed, restart, installed ? "Exact update revision verified installed." : "Update installation did not verify successfully. Review before retrying.");
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return Unknown(); }
    }
}
