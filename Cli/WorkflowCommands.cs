using System.Text.Json;
using KillerBench.Engine;

internal static class WorkflowCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string? reportPath = null;
        int reportIndex = Array.IndexOf(args, "--report");
        if (reportIndex >= 0)
        {
            if (args.Count(value => value == "--report") != 1 || reportIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[reportIndex + 1]))
            { Console.Error.WriteLine("--report requires a new .html or .txt output path."); return 2; }
            reportPath = args[reportIndex + 1];
            string extension = Path.GetExtension(reportPath);
            if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            { Console.Error.WriteLine("--report output must use .html or .txt."); return 2; }
            if (File.Exists(reportPath)) { Console.Error.WriteLine("--report output already exists. Choose a new path."); return 2; }
            args = [.. args.Where((_, index) => index != reportIndex && index != reportIndex + 1)];
        }
        var definition = args.Length > 1 ? WorkflowCatalog.Find(args[1]) : null;
        if (args.Contains("--set"))
            return await RunOptionPlanAsync(args, definition, reportPath).ConfigureAwait(false);
        List<CommonDiagnosticScope>? diagnosticScopes = null;
        if (args.Contains("--scope"))
        {
            if (definition?.Kind != WorkflowKind.CommonProblems || args.Contains("--auto-repair") || args.Contains("--recipes"))
            { Console.Error.WriteLine("Focused scopes require Common problems without repairs or recipe selection."); return 2; }
            diagnosticScopes = [];
            var remaining = new List<string> { args[0], args[1] };
            for (int index = 2; index < args.Length; index++)
            {
                if (args[index] != "--scope") { remaining.Add(args[index]); continue; }
                if (++index >= args.Length || diagnosticScopes.Count >= 64 ||
                    !Enum.TryParse<CommonDiagnosticScope>(args[index], true, out var scope) || !Enum.IsDefined(scope) ||
                    !scope.ToString().Equals(args[index], StringComparison.OrdinalIgnoreCase))
                { Console.Error.WriteLine("Invalid diagnostic scope. Use diagnostic-scopes to list supported names."); return 2; }
                diagnosticScopes.Add(scope);
            }
            args = [.. remaining];
        }
        if (definition?.Kind == WorkflowKind.BackgroundTransfers && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("Background transfer inspection does not support --auto-repair."); return 2; }
        var packageScope = AppPackageScope.CurrentAccount;
        bool bitsAllUsers = definition?.Kind == WorkflowKind.BackgroundTransfers && args.Contains("--all-users");
        if (bitsAllUsers)
        {
            if (args.Count(value => value == "--all-users") != 1) { Console.Error.WriteLine("Specify --all-users once."); return 2; }
            args = [.. args.Where(value => value != "--all-users")];
        }
        var applicationScope = InstalledApplicationScope.Machine;
        if (definition?.Kind == WorkflowKind.InstalledApps && args.Contains("--current-user"))
        {
            if (args.Count(value => value == "--current-user") != 1) { Console.Error.WriteLine("Specify --current-user once."); return 2; }
            applicationScope = InstalledApplicationScope.CurrentAccount;
            args = [.. args.Where(value => value != "--current-user")];
        }
        bool includeProvisioned = definition?.Kind == WorkflowKind.AppPackages && args.Contains("--include-provisioned");
        if (includeProvisioned)
        {
            if (args.Count(value => value == "--include-provisioned") != 1) { Console.Error.WriteLine("Specify --include-provisioned once."); return 2; }
            args = [.. args.Where(value => value != "--include-provisioned")];
        }
        if (definition?.Kind == WorkflowKind.AppPackages && args.Contains("--all-users"))
        {
            if (args.Count(value => value == "--all-users") != 1) { Console.Error.WriteLine("Specify --all-users once."); return 2; }
            packageScope = AppPackageScope.AllUsers;
            args = [.. args.Where(value => value != "--all-users")];
        }
        string[]? selectedRecipes = null;
        int selectionIndex = Array.IndexOf(args, "--recipes");
        if (selectionIndex >= 0)
        {
            if (definition is null || selectionIndex < 2 || selectionIndex + 1 >= args.Length || args.Count(value => value == "--recipes") != 1)
            { Console.Error.WriteLine("Usage: workflow WORKFLOW --recipes ID[,ID] [--auto-repair] [--json]"); return 2; }
            selectedRecipes = args[selectionIndex + 1].Split(',');
            try { RecipeCatalog.SelectForWorkflow(definition.Kind, selectedRecipes); }
            catch (ArgumentException error) { Console.Error.WriteLine("Invalid recipe selection: " + error.Message); return 2; }
            args = [.. args.Where((_, index) => index != selectionIndex && index != selectionIndex + 1)];
        }
        string? proposedName = null;
        string? storageVolume = null;
        string? networkLookupHost = null;
        string? msiProductCode = null;
        (string Host, int Port)? networkEndpoint = null;
        if (definition?.Kind == WorkflowKind.Network)
        {
            var remaining = new List<string> { args[0], args[1] };
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--host")
                {
                    if (networkLookupHost is not null || ++i >= args.Length)
                    { Console.Error.WriteLine("Specify --host once with a hostname."); return 2; }
                    try { networkLookupHost = DnsDiagnostics.ValidateHost(args[i]); }
                    catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
                }
                else if (args[i] == "--tcp")
                {
                    if (networkEndpoint is not null || i + 2 >= args.Length || !int.TryParse(args[i + 2], out int port))
                    { Console.Error.WriteLine("Specify --tcp once with a destination and port."); return 2; }
                    try { TcpDiagnostics.Validate(args[i + 1], port); }
                    catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
                    networkEndpoint = (args[i + 1], port);
                    i += 2;
                }
                else if (args[i] is "--json" or "--auto-repair" && !remaining.Contains(args[i])) remaining.Add(args[i]);
                else
                { Console.Error.WriteLine("Usage: workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]"); return 2; }
            }
            args = [.. remaining];
        }
        if (definition?.Kind == WorkflowKind.Storage)
        {
            var remaining = new List<string> { args[0], args[1] };
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--volume")
                {
                    if (storageVolume is not null || ++i >= args.Length)
                    { Console.Error.WriteLine("Specify --volume once with a drive letter such as D:."); return 2; }
                    try { storageVolume = StorageDiagnostics.NormalizeVolume(args[i]); }
                    catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
                }
                else if (args[i] == "--json" && !remaining.Contains("--json")) remaining.Add(args[i]);
                else
                { Console.Error.WriteLine("Usage: workflow storage [--volume D:] [--json]. This workflow is read-only."); return 2; }
            }
            args = [.. remaining];
        }
        if (definition?.Kind == WorkflowKind.Installer && args.Contains("--product-code"))
        {
            int productIndex = Array.IndexOf(args, "--product-code");
            if (productIndex < 2 || productIndex + 1 >= args.Length || args.Count(value => value == "--product-code") != 1)
            { Console.Error.WriteLine("Usage: workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]"); return 2; }
            try { msiProductCode = MsiProductDiagnostics.NormalizeProductCode(args[productIndex + 1]); }
            catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
            args = [.. args.Where((_, index) => index != productIndex && index != productIndex + 1)];
        }
        if (definition?.Kind == WorkflowKind.ComputerName)
        {
            if (args.Length is < 4 or > 5 || args[2] != "--name" || (args.Length == 5 && args[4] != "--json"))
            { Console.Error.WriteLine("Usage: workflow computer-name --name PROPOSED_NAME [--json] (review only)"); return 2; }
            proposedName = args[3];
            try { ComputerNameReview.Review(proposedName, new(null, null, null)); }
            catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
            args = args.Length == 5 ? [args[0], args[1], "--json"] : [args[0], args[1]];
        }
        if (definition?.Kind == WorkflowKind.Privacy)
            return PrivacyCommands.Run(["privacy-plan", "--inspect", .. args.Skip(2)]);
        if (definition?.Kind == WorkflowKind.BitLocker)
            return await BitLockerCommands.RunAsync(["bitlocker-plan", "--inspect", .. args.Skip(2)]);
        if (definition?.Kind == WorkflowKind.Mace)
        { Console.Error.WriteLine("Use mace-review with one or more explicit product selections. No product is selected by default."); return 2; }
        if (definition?.Kind == WorkflowKind.Orca)
        { Console.Error.WriteLine("Use orca-review with an explicit Outlook mode and one or more review actions."); return 2; }
        if (definition?.Kind == WorkflowKind.Depot)
        { Console.Error.WriteLine("Use depot-review with one or more explicit application or configuration selections."); return 2; }
        if (definition?.Kind == WorkflowKind.Debloat)
        { Console.Error.WriteLine("Use debloat-review with one or more explicit change areas."); return 2; }
        if (definition?.Kind == WorkflowKind.AvailableUpdates && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("This workflow discovers updates. Use update-plan with explicit selections to preview installation."); return 2; }
        UpdateSearchSource updateSource = UpdateSearchSource.Configured;
        int sourceIndex = Array.IndexOf(args, "--source");
        if (sourceIndex >= 0)
        {
            if (definition?.Kind != WorkflowKind.AvailableUpdates || sourceIndex + 1 >= args.Length || args.Count(value => value == "--source") != 1 ||
                !Enum.TryParse(args[sourceIndex + 1], true, out updateSource) || !Enum.IsDefined(updateSource))
            { Console.Error.WriteLine("Update source must be Configured, WindowsUpdate or MicrosoftUpdate."); return 2; }
            args = [.. args.Where((_, index) => index != sourceIndex && index != sourceIndex + 1)];
        }
        if (definition?.Kind == WorkflowKind.AppPackages && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("Package inventory is read-only. No package removal is supported."); return 2; }
        if (definition?.Kind == WorkflowKind.InstalledApps && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("Application inventory is read-only. No uninstall is supported."); return 2; }
        if (definition?.Kind == WorkflowKind.BrowserExtensions && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("Browser extension review is read-only. No extension removal is supported."); return 2; }
        if (!ReleaseFeatures.ThirdPartyUpdates && args.Contains("--third-party"))
        { Console.Error.WriteLine(ReleaseFeatures.ThirdPartyUpdatesNotice); return 2; }
        if (definition?.Kind == WorkflowKind.CommonProblems && args.Contains("--auto-repair"))
        { Console.Error.WriteLine("Full system scan is diagnostic-only. Choose a focused repair workflow to make changes."); return 2; }
        if (definition is null || args.Skip(2).Any(a => a is not ("--auto-repair" or "--json") &&
            !(definition.Kind == WorkflowKind.Defender && a is "--skip-tpm" or "--skip-secure-boot" or "--skip-virtualization") &&
            !(definition.Kind == WorkflowKind.AvailableUpdates && a is "--drivers" or "--no-software" or "--third-party")))
        { Console.Error.WriteLine("Usage: workflow <" + WorkflowCatalog.CommandChoices + "> [--auto-repair] [--json]. Defender also accepts --skip-tpm, --skip-secure-boot and --skip-virtualization."); return 2; }
        var capabilityIds = (selectedRecipes is null ? RecipeCatalog.ForWorkflow(definition.Kind) : RecipeCatalog.SelectForWorkflow(definition.Kind, selectedRecipes))
            .Select(recipe => recipe.Id).ToHashSet(StringComparer.Ordinal);
        var capabilities = WindowsRepairCapabilities.All.Where(capability => capabilityIds.Contains(capability.Id)).ToArray();
        CapabilityOutput.WriteLabels(capabilities);
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            var kind = definition.Kind;
            var updateOptions = new UpdateSearchOptions(!args.Contains("--no-software"), args.Contains("--drivers"), updateSource);
            if (kind == WorkflowKind.AvailableUpdates) updateOptions.Criteria();
            var progress = new Progress<string>(Console.Error.WriteLine);
            var result = await Workflows.RunAsync(kind, args.Contains("--auto-repair"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Workflows"),
                progress, new WindowsWorkflowHost
                { BitsAllUsers = bitsAllUsers, ApplicationScope = applicationScope, IncludeProvisionedPackages = includeProvisioned, PackageScope = packageScope, NetworkEndpoint = networkEndpoint, NetworkLookupHost = networkLookupHost, MsiProductCode = msiProductCode, StorageVolume = storageVolume, ProposedComputerName = proposedName, SecurityOptions = new(!args.Contains("--skip-tpm"), !args.Contains("--skip-secure-boot"), !args.Contains("--skip-virtualization")), UpdateOptions = updateOptions, IncludeThirdPartyUpdates = args.Contains("--third-party"), Progress = progress }, selectedRecipes, diagnosticScopes, token: stop.Token);
            if (reportPath is not null) await ExportReportAsync(result, reportPath, args.Contains("--auto-repair")).ConfigureAwait(false);
            Console.WriteLine(args.Contains("--json") ? CapabilityOutput.WithStatus(result, capabilities) :
                WorkflowReport.Format(result, commandLine: true));
            return result.GetExitCode();
        }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or OperationCanceledException or TimeoutException or
            InvalidOperationException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine(e.Message); return e is OperationCanceledException ? 130 : 1; }
        finally { Console.CancelKeyPress -= onCancel; }
    }

    private static async Task ExportReportAsync(WorkflowResult result, string reportPath, bool includeRepairs)
    {
        string extension = Path.GetExtension(reportPath);
        string report = extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            ? WorkflowReportExport.BuildHtml(result, includeRepairs ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics)
            : WorkflowReportExport.BuildText(result, includeRepairs ? WorkflowReportScope.DiagnosticsAndRepairs : WorkflowReportScope.Diagnostics);
        await ReportFilePublication.WriteAsync(reportPath, _ => Task.FromResult(report)).ConfigureAwait(false);
        Console.Error.WriteLine("Readable report saved: " + Path.GetFullPath(reportPath));
    }

    private static async Task<int> RunOptionPlanAsync(string[] args, WorkflowDefinition? definition, string? reportPath)
    {
        if (definition is null || WorkflowOptionCatalog.ForWorkflow(definition.Kind) is not { } optionSet ||
            args.Contains("--auto-repair") || args.Contains("--recipes") || args.Contains("--scope"))
        {
            Console.Error.WriteLine("Usage: workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--json]. Shared option plans are read-only.");
            return 2;
        }

        var supplied = new List<KeyValuePair<string, string>>();
        bool json = false;
        for (int index = 2; index < args.Length; index++)
        {
            if (args[index] == "--json" && !json) { json = true; continue; }
            if (args[index] != "--set" || ++index >= args.Length)
            {
                Console.Error.WriteLine("Use only repeated --set KEY=VALUE options and optional --json with a shared option plan.");
                return 2;
            }
            string assignment = args[index];
            int equals = assignment.IndexOf('=');
            if (equals <= 0)
            {
                Console.Error.WriteLine("Each --set value must use KEY=VALUE.");
                return 2;
            }
            supplied.Add(new(assignment[..equals], assignment[(equals + 1)..]));
        }

        WorkflowOptionValidationResult validation = WorkflowOptionValidation.Validate(optionSet.WorkflowKey, supplied);
        if (!validation.IsValid)
        {
            foreach (WorkflowOptionValidationError error in validation.Errors)
                Console.Error.WriteLine((error.Key.Length == 0 ? "Option" : error.Key) + ": " + error.Message);
            return 2;
        }
        if (validation.ChangesMachineRequested)
        {
            Console.Error.WriteLine("Machine-changing options require their dedicated preview and guarded execution command.");
            return 2;
        }

        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            var progress = new Progress<string>(Console.Error.WriteLine);
            if (definition.Kind == WorkflowKind.ServicingActivity)
            {
                if (reportPath is not null) { Console.Error.WriteLine("--report is unavailable for the Servicing monitor. Its report is already written to standard output."); return 2; }
                ServicingMonitorOptions options = WorkflowOptionProjection.CreateServicingOptions(validation);
                IReadOnlyList<ServicingActivitySnapshot> snapshots = await ServicingActivityMonitor.ObserveAsync(
                    options, progress: progress, token: stop.Token).ConfigureAwait(false);
                Console.WriteLine(json ? JsonSerializer.Serialize(new { Options = options, Snapshots = snapshots }, Capture.JsonOptions) :
                    ServicingActivityText.FormatReport(snapshots));
                return snapshots.Any(snapshot => snapshot.State is CheckState.Unknown or CheckState.Attention) ? 3 : 0;
            }

            WindowsWorkflowHost host = WorkflowOptionProjection.CreateHost(validation, progress);
            WorkflowResult result = await Workflows.RunAsync(definition.Kind, false,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Workflows"),
                progress, host, token: stop.Token).ConfigureAwait(false);
            if (reportPath is not null) await ExportReportAsync(result, reportPath, false).ConfigureAwait(false);
            Console.WriteLine(json ? JsonSerializer.Serialize(result, Capture.JsonOptions) :
                WorkflowReport.Format(result, commandLine: true));
            return result.GetExitCode();
        }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or
            OperationCanceledException or TimeoutException or InvalidOperationException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine(error.Message); return error is OperationCanceledException ? 130 : 1; }
        finally { Console.CancelKeyPress -= onCancel; }
    }
}
