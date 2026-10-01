using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KillerBench.Engine.Windows.Scheduling;

public sealed class WindowsTaskRegistrationHost(DateOnly startDate) : ITaskRegistrationHost
{
    private byte[]? preparedPlan;
    private string? preparedExecutableHash;
    private string? requestedXml;
    private ScheduledTaskPlan? requestedPlan;

    public async Task<TaskRegistrationPreflight> CheckAsync(ScheduledTaskPlan draft, CancellationToken token)
    {
        var plan = draft.Validate();
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Blocked(plan, "Task registration requires Windows.");
        if (!WindowsState.IsAdministrator()) return Blocked(plan, "Administrator access is required for task registration.");
        if (plan.Principal.Kind != ScheduledTaskAccountKind.LocalSystem || plan.Principal.UserId != "S-1-5-18" || !plan.Principal.HighestPrivileges)
            return Blocked(plan, "Native registration currently supports only the explicit Local System principal with highest privileges.");

        byte[] planBytes = ScheduledTaskPlanJson.Serialize(plan);
        var executable = await ScheduledTaskExecutable.InspectAsync(plan.Action, token).ConfigureAwait(false);
        if (executable.RequiresReview || executable.Sha256 is null)
            return Blocked(plan, "The executable did not produce a complete executable PE content identity.");
        var signature = await FileSignature.InspectAsync(plan.Action, token).ConfigureAwait(false);
        if (signature.Status != "Valid") return Blocked(plan, "The executable does not have a currently valid Authenticode signature.");

        ToolResult targetEvidence = await ProbeTargetAsync(plan.Identity, token).ConfigureAwait(false);
        if (!TaskRegistration.Complete(targetEvidence) || !TryReadTargetProbe(targetEvidence, out bool folderExists, out bool targetExists))
            return new(plan.Identity, false, "Exact target availability is incomplete.", targetEvidence);
        if (!folderExists) return new(plan.Identity, false, "The exact task folder does not exist. KillerBench will not create scheduler folders.", targetEvidence);
        if (targetExists) return new(plan.Identity, false, "A task already exists at the exact target. KillerBench will not replace it.", targetEvidence);

        string xml;
        try { xml = ScheduledTaskXml.Serialize(plan, startDate); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException)
        { return Blocked(plan, error.Message); }
        if (preparedPlan is not null && (!preparedPlan.SequenceEqual(planBytes) ||
            !string.Equals(preparedExecutableHash, executable.Sha256, StringComparison.Ordinal)))
            return Blocked(plan, "The plan or executable content changed after the first eligibility check.");

        preparedPlan ??= planBytes;
        preparedExecutableHash ??= executable.Sha256;
        requestedXml ??= xml;
        requestedPlan ??= plan;
        string summary = JsonSerializer.Serialize(new
        {
            exactTargetAbsent = true,
            taskFolderExists = true,
            administrator = true,
            principal = "S-1-5-18",
            executableSha256 = executable.Sha256,
            signature = signature.Status,
            definitionEnabled = false
        });
        return new(plan.Identity, true,
            "Exact target is absent in an existing folder. Local System, stable signed executable identity and disabled definition are eligible.",
            new ToolResult(0, summary, "", false));
    }

    public async Task<ToolResult> CreateNewAsync(ScheduledTaskPlan plan, CancellationToken token)
    {
        plan = plan.Validate();
        token.ThrowIfCancellationRequested();
        if (preparedPlan is null || requestedXml is null || requestedPlan is null ||
            !preparedPlan.SequenceEqual(ScheduledTaskPlanJson.Serialize(plan)))
            return new(-1, "", "No matching final eligibility snapshot is available.", false);
        string folder = Encode(plan.Identity.Path);
        string name = Encode(plan.Identity.Name);
        string xml = Encode(requestedXml);
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$folderName=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + folder + "')); " +
            "$taskName=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + name + "')); " +
            "$xml=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + xml + "')); " +
            "$scheduler=New-Object -ComObject Schedule.Service; $scheduler.Connect(); " +
            "$folder=$scheduler.GetFolder($folderName); $definition=$scheduler.NewTask(0); $definition.XmlText=$xml; " +
            "$registered=$folder.RegisterTaskDefinition($taskName,$definition,2,'SYSTEM',$null,5,$null); " +
            "[pscustomobject]@{path=$registered.Path;enabled=[bool]$registered.Enabled}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return ValidateCreationResult(plan.Identity, result);
    }

    public async Task<TaskRegistrationVerification> ReadBackAsync(ScheduledTaskIdentity target, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (requestedPlan is null || requestedXml is null || !requestedPlan.Identity.Matches(target))
            return new(null, new ToolResult(-1, "", "No matching requested definition is available for read-back.", false));
        var observed = await ScheduledTaskDefinition.ReadAsync(target, token).ConfigureAwait(false);
        bool verified = observed.Xml is not null && TaskRegistration.Complete(observed.Evidence) &&
            ScheduledTaskXml.MatchesRequested(requestedXml, observed.Xml);
        return new(verified ? requestedPlan : null, observed.Evidence);
    }

    private static TaskRegistrationPreflight Blocked(ScheduledTaskPlan plan, string detail) =>
        new(plan.Identity, false, detail, new ToolResult(3, "", detail, false));

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    private static async Task<ToolResult> ProbeTargetAsync(ScheduledTaskIdentity target, CancellationToken token)
    {
        string folder = Encode(target.Path);
        string name = Encode(target.Name);
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$folderName=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + folder + "')); " +
            "$taskName=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + name + "')); " +
            "$scheduler=New-Object -ComObject Schedule.Service; $scheduler.Connect(); $folderExists=$true; $exists=$false; " +
            "try {$folder=$scheduler.GetFolder($folderName)} catch {$folderExists=$false}; " +
            "if($folderExists){try {$null=$folder.GetTask($taskName); $exists=$true} catch [Runtime.InteropServices.COMException] {if($_.Exception.HResult -ne -2147024894){throw}}}; " +
            "[pscustomobject]@{folderExists=$folderExists;targetExists=$exists}|ConvertTo-Json -Compress";
        return await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
    }

    private static bool TryReadTargetProbe(ToolResult evidence, out bool folderExists, out bool targetExists)
    {
        folderExists = false;
        targetExists = false;
        try
        {
            using var json = JsonDocument.Parse(evidence.Output);
            var root = json.RootElement;
            var properties = root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().ToArray() : [];
            if (properties.Length != 2 || properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != 2 ||
                !root.TryGetProperty("folderExists", out var folder) || !root.TryGetProperty("targetExists", out var target) ||
                folder.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                target.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
            folderExists = folder.GetBoolean();
            targetExists = target.GetBoolean();
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static ToolResult ValidateCreationResult(ScheduledTaskIdentity target, ToolResult result)
    {
        if (!TaskRegistration.Complete(result)) return result;
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            var properties = root.ValueKind == JsonValueKind.Object ? root.EnumerateObject().ToArray() : [];
            if (properties.Length == 2 && properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == 2 &&
                string.Equals(root.GetProperty("path").GetString(), target.FullName, StringComparison.OrdinalIgnoreCase) &&
                root.GetProperty("enabled").ValueKind == JsonValueKind.False) return result;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { }
        return new(-1, result.Output, "Task Scheduler did not confirm the exact disabled target after the create-only request.", result.Truncated)
        { TimedOut = result.TimedOut };
    }
}
