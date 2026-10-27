using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KillerBench.Engine.Core;
using Microsoft.Win32;

namespace KillerBench.Engine.Windows.Validation;

/// <summary>Result of one injected scenario run: the validation record written and the folder holding its artifacts.</summary>
public sealed record NativeScenarioResult(string ScenarioId, NativeValidationOutcome Outcome, string RecordPath, string ArtifactFolder, string Notes)
{
    public bool RestorationSucceeded { get; init; } = true;
}

/// <summary>
/// Runs fault-injection scenarios for pack recipes on a disposable machine and writes native validation records that
/// validation-record and validation-report accept. It refuses to run unless the caller names this exact machine and the
/// KILLERBENCH_DISPOSABLE environment variable states that the machine is disposable. It attempts and verifies restoration after every mutating request.
/// </summary>
public sealed class NativeScenarioRunner
{
    public const string DisposableVariable = "KILLERBENCH_DISPOSABLE";
    public const string DisposableValue = "yes-this-machine-is-disposable";

    private readonly PrimitiveLibrary library;
    private readonly INativeScenarioHost host;
    private readonly Dictionary<string, RecipeDefinition> recipes =
        WindowsPacks.All().SelectMany(pack => pack.Recipes).ToDictionary(recipe => recipe.Id, StringComparer.Ordinal);

    public NativeScenarioRunner() : this(WindowsPrimitiveLibrary.Create(), new WindowsScenarioHost()) { }

    public NativeScenarioRunner(PrimitiveLibrary library, INativeScenarioHost host)
    {
        this.library = library ?? throw new ArgumentNullException(nameof(library));
        this.host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public static bool IsDisposable(string? declaredMachine) =>
        OperatingSystem.IsWindows() &&
        MatchesDisposableIdentity(declaredMachine, Environment.MachineName, Environment.GetEnvironmentVariable(DisposableVariable));

    /// <summary>Validates captured gate inputs without changing the current process environment.</summary>
    public static bool MatchesDisposableIdentity(string? declaredMachine, string actualMachine, string? marker) =>
        !string.IsNullOrWhiteSpace(declaredMachine) &&
        string.Equals(declaredMachine, actualMachine, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(marker, DisposableValue, StringComparison.Ordinal);

    /// <summary>Scenarios this runner can inject natively for a service recipe. Others are recorded as Blocked with the reason.</summary>
    public static IReadOnlyList<NativeValidationScenarioKind> Injectable { get; } =
    [
        NativeValidationScenarioKind.Healthy, NativeValidationScenarioKind.Preview, NativeValidationScenarioKind.Ineligible,
        NativeValidationScenarioKind.VerifiedExecution, NativeValidationScenarioKind.Cancellation
    ];

    public async Task<IReadOnlyList<NativeScenarioResult>> RunAsync(string recipeId, string? scenarioSuffix, string outputFolder,
        string declaredMachine, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        if (!host.IsWindows || !MatchesDisposableIdentity(declaredMachine, host.MachineName, host.DisposableMarker))
            throw new InvalidOperationException("Refusing to inject faults: the declared machine must equal this machine's name and " + DisposableVariable + " must be set to " + DisposableValue + ".");
        var plan = NativeValidationCatalog.Find(recipeId) ?? throw new ArgumentException("Unknown recipe '" + recipeId + "'.", nameof(recipeId));
        if (!recipes.TryGetValue(recipeId, out var recipe)) throw new ArgumentException("Recipe '" + recipeId + "' is not in a built-in pack; only pack recipes can be injected.", nameof(recipeId));
        if (!WindowsServicePack.IsServiceStart(plan.Action)) throw new ArgumentException("Only service-start recipes are injectable in this version.", nameof(recipeId));
        string service = ServiceRecovery.ServiceName(plan.Action)!;
        var scenarios = plan.Scenarios.Where(scenario => scenarioSuffix is null || scenario.Id == recipeId + "." + scenarioSuffix).ToArray();
        if (scenarios.Length == 0) throw new ArgumentException("No scenario '" + scenarioSuffix + "' exists for " + recipeId + ".", nameof(scenarioSuffix));
        Directory.CreateDirectory(outputFolder);

        var results = new List<NativeScenarioResult>();
        foreach (var scenario in scenarios)
        {
            token.ThrowIfCancellationRequested();
            progress?.Report(scenario.Id + ": " + scenario.Title);
            var result = await RunScenarioAsync(recipe, plan, scenario, service, outputFolder, progress, token).ConfigureAwait(false);
            results.Add(result);
            if (!result.RestorationSucceeded) break;
        }
        return results.AsReadOnly();
    }

    private async Task<NativeScenarioResult> RunScenarioAsync(RecipeDefinition recipe, NativeRecipeValidationPlan plan, NativeValidationScenario scenario,
        string service, string outputFolder, IProgress<string>? progress, CancellationToken token)
    {
        string folder = Path.Combine(outputFolder, scenario.Id);
        Directory.CreateDirectory(folder);
        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal);
        var notes = new StringBuilder();
        NativeValidationOutcome outcome;
        INativeScenarioService serviceBoundary = host.Service(service);
        var before = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
        artifacts["before snapshot"] = await WriteAsync(folder, "before-snapshot.txt", before.Text, token).ConfigureAwait(false);
        artifacts["machine snapshot"] = artifacts["before snapshot"];
        string journalPath = Path.Combine(folder, "workflow-journal.jsonl");
        var journal = new FileRecipeJournal(journalPath);
        var progressLines = new StringBuilder();
        var capture = new Progress<string>(line => { progressLines.AppendLine(line); progress?.Report(line); });
        // Scenario-scoped barriers: a record left by one scenario must not block the next or reach the per-user store real repairs use.
        var runner = new RecipeRunner(library, journal, new FileRecipeOperationStore(Path.Combine(folder, "recipe-operations")));
        var injection = new NativeServiceScenario(serviceBoundary, before);
        bool restored = true;
        try
        {
            if (!injection.SupportedBaseline)
            {
                outcome = NativeValidationOutcome.Blocked;
                notes.Append("The original service runtime state or configuration cannot be safely restored; no injection was requested.");
            }
            else switch (scenario.Kind)
            {
                case NativeValidationScenarioKind.Healthy:
                {
                    if (before.State != 4) { outcome = NativeValidationOutcome.Blocked; notes.Append("The service was not running before the scenario, so a healthy baseline could not be observed."); break; }
                    var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Progress = capture }, token).ConfigureAwait(false);
                    artifacts["provider evidence"] = await WriteAsync(folder, "provider-evidence.txt", Evidence(result), token).ConfigureAwait(false);
                    artifacts["native result"] = artifacts["provider evidence"];
                    var after = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
                    artifacts["after snapshot"] = await WriteAsync(folder, "after-snapshot.txt", after.Text, token).ConfigureAwait(false);
                    outcome = result.Outcome == RecipeRunOutcome.Clear && result.Action is null ? NativeValidationOutcome.Passed : NativeValidationOutcome.Failed;
                    notes.Append("Healthy service produced ").Append(result.Outcome).Append(" with no action. ").Append(result.Detail);
                    break;
                }
                case NativeValidationScenarioKind.Preview:
                {
                    bool injected = await injection.StopAsync(token).ConfigureAwait(false);
                    if (!injected) { outcome = NativeValidationOutcome.Blocked; notes.Append("The service could not be stopped to inject the fault."); break; }
                    var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = false, Elevated = true, Progress = capture }, token).ConfigureAwait(false);
                    artifacts["preview output"] = await WriteAsync(folder, "preview-output.txt", Evidence(result), token).ConfigureAwait(false);
                    artifacts["native result"] = artifacts["preview output"];
                    var after = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
                    artifacts["after snapshot"] = await WriteAsync(folder, "after-snapshot.txt", after.Text, token).ConfigureAwait(false);
                    outcome = result.Outcome == RecipeRunOutcome.RepairAvailable && result.Action is null && after.State == 1 ? NativeValidationOutcome.Passed : NativeValidationOutcome.Failed;
                    notes.Append("Stopped service produced ").Append(result.Outcome).Append("; service state afterwards ").Append(after.State).Append(". ").Append(result.Detail);
                    break;
                }
                case NativeValidationScenarioKind.Ineligible:
                {
                    bool injected = await injection.StopAsync(token).ConfigureAwait(false) && await injection.DisableAsync(token).ConfigureAwait(false);
                    if (!injected) { outcome = NativeValidationOutcome.Blocked; notes.Append("The service could not be stopped and disabled to inject the fault."); break; }
                    var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Progress = capture }, token).ConfigureAwait(false);
                    var after = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
                    artifacts["final eligibility snapshot"] = await WriteAsync(folder, "final-eligibility-snapshot.txt", after.Text + "\n\n" + Evidence(result), token).ConfigureAwait(false);
                    artifacts["native result"] = await WriteAsync(folder, "native-result.txt", Evidence(result), token).ConfigureAwait(false);
                    artifacts["after snapshot"] = await WriteAsync(folder, "after-snapshot.txt", after.Text, token).ConfigureAwait(false);
                    outcome = result.Action is null && result.Outcome is RecipeRunOutcome.NeedsAttention or RecipeRunOutcome.Unknown && after.State == 1 ? NativeValidationOutcome.Passed : NativeValidationOutcome.Failed;
                    notes.Append("Disabled service produced ").Append(result.Outcome).Append(" with ").Append(result.Action is null ? "no dispatch" : "a dispatch").Append(". ").Append(result.Detail);
                    break;
                }
                case NativeValidationScenarioKind.VerifiedExecution:
                {
                    bool injected = await injection.StopAsync(token).ConfigureAwait(false);
                    if (!injected) { outcome = NativeValidationOutcome.Blocked; notes.Append("The service could not be stopped to inject the fault."); break; }
                    var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Progress = capture }, token).ConfigureAwait(false);
                    var after = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
                    artifacts["prepared record"] = await WriteAsync(folder, "prepared-record.txt", PreparedEntries(journalPath), token).ConfigureAwait(false);
                    artifacts["native result"] = await WriteAsync(folder, "native-result.txt", result.Action is null ? "no action" : JsonSerializer.Serialize(result.Action, RecipePackJson.Options), token).ConfigureAwait(false);
                    artifacts["after snapshot"] = await WriteAsync(folder, "after-snapshot.txt", after.Text, token).ConfigureAwait(false);
                    outcome = result.Outcome == RecipeRunOutcome.Repaired && result.Action is { Dispatched: true } && after.State == 4 ? NativeValidationOutcome.Passed : NativeValidationOutcome.Failed;
                    notes.Append("Stopped service produced ").Append(result.Outcome).Append("; service state afterwards ").Append(after.State).Append(". ").Append(result.Detail);
                    break;
                }
                case NativeValidationScenarioKind.Cancellation:
                {
                    bool injected = await injection.StopAsync(token).ConfigureAwait(false);
                    if (!injected) { outcome = NativeValidationOutcome.Blocked; notes.Append("The service could not be stopped to inject the fault."); break; }
                    using var canceled = new CancellationTokenSource();
                    canceled.Cancel();
                    var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Progress = capture }, canceled.Token).ConfigureAwait(false);
                    var after = await serviceBoundary.SnapshotAsync(token).ConfigureAwait(false);
                    artifacts["native result"] = await WriteAsync(folder, "native-result.txt", Evidence(result), token).ConfigureAwait(false);
                    artifacts["after snapshot"] = await WriteAsync(folder, "after-snapshot.txt", after.Text, token).ConfigureAwait(false);
                    outcome = result.Outcome == RecipeRunOutcome.Canceled && result.Action is null && after.State == 1 ? NativeValidationOutcome.Passed : NativeValidationOutcome.Failed;
                    notes.Append("Pre-canceled run produced ").Append(result.Outcome).Append("; service state afterwards ").Append(after.State).Append('.');
                    break;
                }
                default:
                    outcome = NativeValidationOutcome.Blocked;
                    notes.Append("Scenario kind ").Append(scenario.Kind).Append(" is not injectable by this runner yet; substituted-provider fixtures cover it.");
                    break;
            }
        }
        catch (Exception exception)
        {
            outcome = NativeValidationOutcome.Failed;
            notes.Append(" Scenario interrupted: ").Append(exception.GetType().Name).Append('.');
        }
        finally
        {
            restored = await injection.RestoreAsync().ConfigureAwait(false);
            notes.Append(restored ? " Restoration responsibility completed." : " WARNING: injected state could not be fully restored; stop validation and review the service manually.");
        }
        if (!restored) outcome = NativeValidationOutcome.Failed;
        // Evidence and restoration must survive cancellation of the scenario itself.
        token = CancellationToken.None;
        artifacts["restoration stages"] = await WriteAsync(folder, "restoration-stages.txt", string.Join("\n", injection.Stages), token).ConfigureAwait(false);
        artifacts["restoration evidence"] = await WriteAsync(folder, "restoration-evidence.txt", "RestorationSucceeded=" + restored + "\n" + string.Join("\n", injection.Stages), token).ConfigureAwait(false);
        artifacts["progress log"] = await WriteAsync(folder, "progress-log.txt", progressLines.ToString(), token).ConfigureAwait(false);
        if (!File.Exists(journalPath)) await File.WriteAllTextAsync(journalPath, "", token).ConfigureAwait(false);
        artifacts["workflow journal"] = "workflow-journal.jsonl";

        var machine = await host.ReadMachineAsync(token).ConfigureAwait(false);
        var record = new NativeValidationRecord(NativeValidationEvidence.SchemaVersion, scenario.Id, plan.RecipeId, recipe.Version, outcome,
            machine.Product, machine.Version, machine.Build, machine.Architecture,
            typeof(WindowsEngine).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            DateTimeOffset.UtcNow,
            [.. artifacts.Select(pair => new NativeValidationArtifact(pair.Key, pair.Value, Sha256(Path.Combine(folder, pair.Value))))],
            notes.Length > 2000 ? notes.ToString(0, 2000) : notes.ToString());
        record = BindRetainedEvidence(record, folder);
        outcome = record.Outcome;
        string json = JsonSerializer.Serialize(record, Capture.JsonOptions);
        string recordPath = Path.Combine(outputFolder, scenario.Id + ".json");
        await File.WriteAllTextAsync(recordPath, json, token).ConfigureAwait(false);
        RepairCapabilityInventory.ReadRecord(recordPath, WindowsRepairCapabilities.All);
        return new NativeScenarioResult(scenario.Id, outcome, recordPath, folder, record.Notes) { RestorationSucceeded = restored };
    }

    /// <summary>Binds a native pack scenario to its exact implementation and refuses a pass without retained required artifacts.</summary>
    public static NativeValidationRecord BindRetainedEvidence(NativeValidationRecord record, string artifactDirectory)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);
        var capability = WindowsRepairCapabilities.Find(record.RecipeId)
            ?? throw new ArgumentException("Unknown native repair capability.", nameof(record));
        var implementation = capability.Implementations.Single(item => item.Key.StartsWith("pack/", StringComparison.Ordinal));
        var scenario = implementation.Scenarios.Single(item => item.Id == record.ScenarioId);
        bool complete = scenario.RequiredArtifacts.All(kind => record.Artifacts.Count(artifact => artifact.Kind == kind) == 1);
        foreach (var artifact in record.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.Name) || Path.GetFileName(artifact.Name) != artifact.Name)
            { complete = false; continue; }
            string path = Path.Combine(artifactDirectory, artifact.Name);
            try
            {
                if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                    !Sha256(path).Equals(artifact.Sha256, StringComparison.OrdinalIgnoreCase)) complete = false;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { complete = false; }
        }
        string notes = !complete ? "Required retained native artifacts are missing or changed. " + record.Notes : record.Notes;
        return record with
        {
            RecipeVersion = implementation.Version,
            ImplementationKey = implementation.Key,
            DefinitionSha256 = implementation.DefinitionSha256,
            ImplementationSha256 = implementation.ImplementationSha256,
            ArtifactDirectory = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(artifactDirectory))),
            Outcome = !complete && record.Outcome == NativeValidationOutcome.Passed ? NativeValidationOutcome.Inconclusive : record.Outcome,
            Notes = notes[..Math.Min(2000, notes.Length)]
        };
    }

    private static async Task<NativeServiceSnapshot> SnapshotAsync(string service, CancellationToken token)
    {
        var query = await NativeTool.RunAsync("sc.exe", ["query", service], token: token).ConfigureAwait(false);
        var config = await NativeTool.RunAsync("sc.exe", ["qc", service], token: token).ConfigureAwait(false);
        int? start = null;
        bool delayed = false;
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service);
            start = key?.GetValue("Start") as int?;
            delayed = key?.GetValue("DelayedAutostart") is int value && value == 1;
        }
        return new NativeServiceSnapshot(ServiceRecovery.State(query), start, delayed,
            DateTimeOffset.UtcNow.ToString("O") + "\n" + query.Output + "\n" + config.Output + "\nStart=" + start + " Delayed=" + delayed);
    }

    private static async Task<bool> StopAsync(string service, CancellationToken token)
    {
        var stop = await NativeTool.RunAsync("sc.exe", ["stop", service], token: token).ConfigureAwait(false);
        if (stop.ExitCode != 0 && stop.ExitCode != 1062) return false;
        for (int i = 0; i < 60; i++)
        {
            var query = await NativeTool.RunAsync("sc.exe", ["query", service], token: token).ConfigureAwait(false);
            if (ServiceRecovery.State(query) == 1) return true;
            await Task.Delay(500, token).ConfigureAwait(false);
        }
        return false;
    }

    private static async Task<bool> SetStartAsync(string service, string start, CancellationToken token)
    {
        var result = await NativeTool.RunAsync("sc.exe", ["config", service, "start=", start], token: token).ConfigureAwait(false);
        return result.ExitCode == 0;
    }

    private sealed class WindowsScenarioHost : INativeScenarioHost
    {
        public bool IsWindows => OperatingSystem.IsWindows();
        public string MachineName => Environment.MachineName;
        public string? DisposableMarker => Environment.GetEnvironmentVariable(DisposableVariable);
        public INativeScenarioService Service(string name) => new ServiceBoundary(name);
        public async Task<NativeScenarioMachine> ReadMachineAsync(CancellationToken token) => new(
            await NativeWindowsProductMetadata.ReadAsync(token).ConfigureAwait(false),
            Read(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion") ?? Environment.OSVersion.Version.ToString(),
            (Read(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild") ?? Environment.OSVersion.Version.Build.ToString()) + "." +
                (Read(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR") ?? "0"), RuntimeInformation.OSArchitecture.ToString());
    }

    private sealed class ServiceBoundary(string service) : INativeScenarioService
    {
        public Task<NativeServiceSnapshot> SnapshotAsync(CancellationToken token) => NativeScenarioRunner.SnapshotAsync(service, token);
        public Task<bool> StopAsync(CancellationToken token) => NativeScenarioRunner.StopAsync(service, token);
        public Task<bool> SetStartAsync(string start, CancellationToken token) => NativeScenarioRunner.SetStartAsync(service, start, token);
        public async Task<bool> StartAsync(CancellationToken token)
        {
            var result = await NativeTool.RunAsync("sc.exe", ["start", service], token: token).ConfigureAwait(false);
            if (result.ExitCode is not (0 or 1056)) return false;
            for (int i = 0; i < 60; i++)
            {
                var query = await NativeTool.RunAsync("sc.exe", ["query", service], token: token).ConfigureAwait(false);
                if (ServiceRecovery.State(query) == 4) return true;
                await Task.Delay(500, token).ConfigureAwait(false);
            }
            return false;
        }
    }

    private static string Evidence(RecipeRunResult result) =>
        JsonSerializer.Serialize(new { result.RecipeId, result.Outcome, result.Detail, result.ExecutionUncertain,
            Dispatched = result.Action?.Dispatched ?? false, result.Action,
            Probes = result.Probes.Select(probe => new { probe.Role, probe.Evaluation.Verdict, probe.Evaluation.Detail, probe.Result.Raw }) }, RecipePackJson.Options);

    private static string PreparedEntries(string journalPath) =>
        File.Exists(journalPath) ? string.Join("\n", File.ReadLines(journalPath).Where(line => line.Contains("\"Prepared\"", StringComparison.Ordinal) || line.Contains("\"Dispatched\"", StringComparison.Ordinal))) : "";

    private static async Task<string> WriteAsync(string folder, string name, string content, CancellationToken token)
    {
        await File.WriteAllTextAsync(Path.Combine(folder, name), content, token).ConfigureAwait(false);
        return name;
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string? Read(string key, string name)
    {
        if (!OperatingSystem.IsWindows()) return null;
        using var registryKey = Registry.LocalMachine.OpenSubKey(key);
        return registryKey?.GetValue(name)?.ToString();
    }
}
