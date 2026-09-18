using System.Text.Json;

namespace KillerBench.Engine;

public static class ServicingTransitionCorrelation
{
    private static readonly HashSet<string> PendingPackageStates = new(["InstallPending", "UninstallPending", "PartiallyInstalled"], StringComparer.Ordinal);
    private static readonly HashSet<string> PendingFeatureStates = new(["EnablePending", "DisablePending"], StringComparer.Ordinal);

    public static WorkflowCheck Check(WorkflowCheck packages, WorkflowCheck features, WorkflowCheck restart, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string name = "Servicing transition readiness";
        const string scope = " This correlation does not establish component-store health or authorize package installation, removal, cleanup, rollback, feature changes or restart. No package, feature, source, policy, restart indicator or servicing setting changed.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + scope,
            Evidence: packages.Evidence ?? features.Evidence ?? restart.Evidence);
        if (packages.EffectiveState == CheckState.Unknown || packages.Evidence is null)
            return Unknown("Complete servicing package evidence is required before pending transitions can be correlated.");
        if (features.EffectiveState == CheckState.Unknown || features.Evidence is null)
            return Unknown("Complete optional-feature evidence is required before pending transitions can be correlated.");
        if (restart.EffectiveState == CheckState.Unknown || restart.Evidence is null)
            return Unknown("Complete restart-indicator evidence is required before pending transitions can be correlated.");
        try
        {
            int pendingPackages = CountStates(packages.Evidence.Output, "packages", "state", 4096, PendingPackageStates);
            int pendingFeatures = CountStates(features.Evidence.Output, "features", "state", 1024, PendingFeatureStates);
            string[] restartSources = ReadRestartSources(restart.Evidence.Output);
            int total = pendingPackages + pendingFeatures;
            string detail;
            CheckState state;
            if (total == 0)
            {
                state = CheckState.Information;
                detail = $"No pending package or optional-feature transition was reported. {restartSources.Length} supported restart source{(restartSources.Length == 1 ? " remains" : "s remain")}.";
                if (restartSources.Length > 0) detail += " Those indicators may belong to other Windows or application activity: " + string.Join(", ", restartSources) + ".";
            }
            else if (restartSources.Length > 0)
            {
                state = CheckState.Attention;
                detail = $"Pending servicing transitions: {pendingPackages} package{(pendingPackages == 1 ? "" : "s")} and {pendingFeatures} optional feature{(pendingFeatures == 1 ? "" : "s")}. Supported pending restart sources: {string.Join(", ", restartSources)}. Restart only through the user's normal change process, then collect fresh evidence before any servicing repair.";
            }
            else
            {
                state = CheckState.Attention;
                detail = $"Pending servicing transitions: {pendingPackages} package{(pendingPackages == 1 ? "" : "s")} and {pendingFeatures} optional feature{(pendingFeatures == 1 ? "" : "s")}, but no supported restart indicator was found. The transition may be active, stale, or represented by an unsupported source. Recollect after current servicing finishes before selecting repair.";
            }
            return new(name, state, detail + scope, Evidence: packages.Evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Package, optional-feature and restart evidence could not be correlated safely.");
        }
    }

    private static int CountStates(string json, string arrayName, string stateName, int maximum, HashSet<string> pendingStates)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty(arrayName, out var entries) || entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > maximum)
            throw new InvalidDataException("Servicing inventory is incomplete.");
        int pending = 0;
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty(stateName, out var stateValue) || stateValue.ValueKind != JsonValueKind.String || stateValue.GetString() is not { } state)
                throw new InvalidDataException("Servicing state is invalid.");
            if (pendingStates.Contains(state)) pending++;
        }
        return pending;
    }

    private static string[] ReadRestartSources(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True)
            throw new InvalidDataException("Restart evidence is incomplete.");
        var sources = new List<string>();
        Add("componentServicing", "component servicing");
        Add("windowsUpdate", "Windows Update");
        Add("pendingFileRenames", "pending file rename operations");
        Add("computerRename", "computer rename");
        Add("updateExecutable", "update executable");
        return sources.ToArray();

        void Add(string property, string label)
        {
            if (!root.TryGetProperty(property, out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("Restart source is invalid.");
            if (value.GetBoolean()) sources.Add(label);
        }
    }
}
