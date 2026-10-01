using System.Text.Json;

namespace KillerBench.Engine.Network;

public enum FirewallProfileTarget { Domain, Private, Public }
public sealed record FirewallProfileState(FirewallProfileTarget Profile, bool? Enabled, bool? Managed);
public sealed record FirewallProfileEnableOptions(IReadOnlyList<FirewallProfileTarget> Profiles);
public enum FirewallProfileEnableOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped, NoPending }
public sealed record FirewallProfileEnableResult(FirewallProfileEnableOutcome Outcome, string Detail,
    IReadOnlyList<FirewallProfileTarget> Profiles, IReadOnlyList<FirewallProfileState>? Before = null,
    ToolResult? Command = null, IReadOnlyList<FirewallProfileState>? After = null, string? RecordPath = null)
{
    public int GetExitCode() => Outcome is FirewallProfileEnableOutcome.Preview or FirewallProfileEnableOutcome.Verified or FirewallProfileEnableOutcome.NoPending ? 0 :
        Outcome == FirewallProfileEnableOutcome.Stopped ? 130 : 3;
}

public interface IFirewallProfileEnableHost
{
    bool IsAdministrator { get; }
    Task<IReadOnlyList<FirewallProfileState>> InspectAsync(CancellationToken token);
    Task<ToolResult> EnableAsync(IReadOnlyList<FirewallProfileTarget> profiles, CancellationToken token);
}

public static class FirewallProfileEnable
{
    private const string PendingName = "firewall-profile-enable.pending.json";
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions) { RespectRequiredConstructorParameters = true };

    public static FirewallProfileEnableOptions Validate(FirewallProfileEnableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var profiles = options.Profiles?.ToArray() ?? [];
        if (profiles.Length is < 1 or > 3 || profiles.Any(profile => !Enum.IsDefined(profile)) || profiles.Distinct().Count() != profiles.Length)
            throw new ArgumentException("Choose one or more unique Domain, Private or Public firewall profiles.", nameof(options));
        return new(Array.AsReadOnly(profiles.OrderBy(profile => profile).ToArray()));
    }

    public static bool Eligible(FirewallProfileEnableOptions options, IReadOnlyList<FirewallProfileState> state)
    {
        var selected = Validate(options).Profiles;
        if (!Complete(state)) return false;
        return selected.All(profile => state.Single(item => item.Profile == profile) is { Enabled: false, Managed: false });
    }

    public static WorkflowCheck Assess(IReadOnlyList<FirewallProfileState> state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var snapshot = state.OrderBy(item => item.Profile).ToArray();
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (!Complete(snapshot)) return new("Windows Firewall profiles", CheckState.Unknown,
            "Complete, unique Domain, Private and Public profile state and policy ownership were not available. No profile change is eligible.", Evidence: evidence);
        var localDisabled = snapshot.Where(item => item is { Enabled: false, Managed: false }).Select(item => item.Profile).ToArray();
        var managedDisabled = snapshot.Where(item => item is { Enabled: false, Managed: true }).Select(item => item.Profile).ToArray();
        if (localDisabled.Length > 0)
        {
            string detail = "Disabled unmanaged profiles: " + string.Join(", ", localDisabled) +
                ". Select the exact profiles in the guarded firewall controls before execution.";
            if (managedDisabled.Length > 0) detail += " Disabled managed profiles require review with the policy owner: " +
                string.Join(", ", managedDisabled) + ".";
            return new("Windows Firewall profiles", CheckState.Repairable, detail, RepairKind.FirewallProfiles, evidence);
        }
        if (managedDisabled.Length > 0) return new("Windows Firewall profiles", CheckState.Attention,
            "Disabled managed profiles require review with the policy owner: " + string.Join(", ", managedDisabled) +
            ". KillerBench will not override their policy.", Evidence: evidence);
        return new("Windows Firewall profiles", CheckState.Clear,
            "Domain, Private and Public profiles are enabled. Individual rules, default actions, third-party filtering and traffic reachability remain outside this check.", Evidence: evidence);
    }

    public static WorkflowCheck ReviewSelection(FirewallProfileEnableOptions options,
        IReadOnlyList<FirewallProfileState> state)
    {
        var selected = Validate(options).Profiles;
        ArgumentNullException.ThrowIfNull(state);
        var snapshot = state.OrderBy(item => item.Profile).ToArray();
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        string names = string.Join(", ", selected);
        if (!Complete(snapshot))
            return new("Selected firewall profiles", CheckState.Unknown,
                "Complete, unique Domain, Private and Public profile state and policy ownership were not available. Selected profiles: " + names + ". No change is authorized.", Evidence: evidence);
        var targets = selected.Select(profile => snapshot.Single(item => item.Profile == profile)).ToArray();
        var managed = targets.Where(item => item.Managed == true).Select(item => item.Profile).ToArray();
        var disabled = targets.Where(item => item.Enabled == false).Select(item => item.Profile).ToArray();
        if (managed.Length > 0)
            return new("Selected firewall profiles", CheckState.Attention,
                "Selected profiles: " + names + ". Managed profiles require review with the policy owner: " + string.Join(", ", managed) + ". KillerBench will not override them.", Evidence: evidence);
        if (disabled.Length == 0)
            return new("Selected firewall profiles", CheckState.Information,
                "Selected profiles are already enabled: " + names + ". Rules, default actions, third-party filtering and traffic behavior remain unverified.", Evidence: evidence);
        return new("Selected firewall profiles", CheckState.Attention,
            "Selected unmanaged profiles are disabled: " + string.Join(", ", disabled) + ". A separate guarded enable action can review exact eligibility. This read-only review does not authorize or run that action.", Evidence: evidence);
    }

    public static async Task<FirewallProfileEnableResult> RunAsync(FirewallProfileEnableOptions options, bool execute,
        string directory, IFirewallProfileEnableHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        var selected = Validate(options).Profiles;
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();
        string root = Path.GetFullPath(directory);
        string pending = Path.Combine(root, PendingName);
        if (File.Exists(pending)) throw new InvalidOperationException("An earlier firewall profile change is unresolved. No command was repeated.");
        Report(progress, "Inspecting selected Windows Firewall profiles and policy ownership.");
        var first = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Eligible(new(selected), first)) return Result(FirewallProfileEnableOutcome.Blocked,
            "Every selected profile must be confirmed disabled and unmanaged. Enabled, managed, missing or ambiguous profiles are preserved.", selected, first);
        if (!execute) return Result(FirewallProfileEnableOutcome.Preview,
            "Eligible preview. Execution will recheck the same profiles, enable only their local persistent settings, then verify the active profile states. Rules, default actions and logging settings are preserved.", selected, first);
        if (!host.IsAdministrator) return Result(FirewallProfileEnableOutcome.Blocked,
            "Administrator access is required. No firewall profile was changed.", selected, first);
        var second = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Same(first, second) || !Eligible(new(selected), second)) return Result(FirewallProfileEnableOutcome.Blocked,
            "Firewall profile state or policy ownership changed during the fresh recheck. No command was run.", selected, second);

        Directory.CreateDirectory(root);
        byte[] record = JsonSerializer.SerializeToUtf8Bytes(new FirewallProfilePendingRecord(1, DateTimeOffset.UtcNow, selected, second)
            { Machine = Environment.MachineName }, Capture.JsonOptions);
        await WriteNewAsync(pending, record, token).ConfigureAwait(false);
        ToolResult? command = null;
        bool commandDispatched = false;
        try
        {
            Report(progress, "Confirming final selected firewall-profile eligibility.");
            var final = await InspectAsync(host, token).ConfigureAwait(false);
            if (!host.IsAdministrator || !Same(second, final) || !Eligible(new(selected), final))
                return await FinishAsync(Result(FirewallProfileEnableOutcome.Blocked,
                    "Firewall profile state, policy ownership or administrator access changed after preparation. No command was run.",
                    selected, second, after: final, recordPath: pending), root, true).ConfigureAwait(false);
            Report(progress, "Enabling the selected local Windows Firewall profiles.");
            commandDispatched = true;
            command = await (host.EnableAsync(selected, token) ?? throw new InvalidDataException("The firewall provider returned no command task."))
                .WaitAsync(TimeSpan.FromMinutes(2), token).ConfigureAwait(false);
            if (command is null || command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                return await FinishAsync(Result(FirewallProfileEnableOutcome.Uncertain,
                    "The enable command was not confirmed. The pending record remains for read-only reconciliation.", selected, second, command, recordPath: pending), root, false).ConfigureAwait(false);
            var after = await InspectAsync(host, token).ConfigureAwait(false);
            bool verified = Complete(after) && selected.All(profile => after.Single(item => item.Profile == profile) is { Enabled: true, Managed: false });
            bool unchanged = Same(second, after);
            return await FinishAsync(Result(verified ? FirewallProfileEnableOutcome.Verified : unchanged ? FirewallProfileEnableOutcome.Failed : FirewallProfileEnableOutcome.Uncertain,
                verified ? "The selected local firewall profiles are enabled in the active configuration. Traffic behavior and individual rules were not tested." :
                unchanged ? "The command returned, but the selected profiles remain disabled. No change was verified." :
                "Profile state or policy ownership changed after the command. The result is unresolved.", selected, second, command, after, pending), root, verified || unchanged).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!commandDispatched)
                return await FinishAsync(Result(FirewallProfileEnableOutcome.Stopped,
                    "The operation stopped after durable preparation and before any firewall command was sent.",
                    selected, second, recordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(Result(FirewallProfileEnableOutcome.Stopped,
                "The operation stopped after its pending record was saved. The command may have started; recovery will inspect state without repeating it.", selected, second, command, recordPath: pending), root, false).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            if (!commandDispatched)
                return await FinishAsync(Result(FirewallProfileEnableOutcome.Blocked,
                    "Final firewall eligibility could not be established: " + error.Message + ". No command was run.",
                    selected, second, recordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(Result(FirewallProfileEnableOutcome.Uncertain,
                "Firewall profile enablement was interrupted: " + error.Message + ". The pending record remains and no retry was started.", selected, second, command, recordPath: pending), root, false).ConfigureAwait(false);
        }
    }

    public static async Task<FirewallProfileEnableResult> ReconcileAsync(string directory, IFirewallProfileEnableHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pending = Path.Combine(root, PendingName);
        if (!File.Exists(pending)) return Result(FirewallProfileEnableOutcome.NoPending,
            "No interrupted firewall profile change is present.", []);
        FirewallProfilePendingRecord record;
        try { record = await ReadAsync(pending, token).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Result(FirewallProfileEnableOutcome.Uncertain,
                "The pending firewall record is unavailable or invalid: " + error.Message +
                ". It was retained and no profile was inspected or changed.", [], recordPath: pending);
        }
        var options = Validate(new(record.Profiles));
        if (!Complete(record.Before) || !Eligible(options, record.Before))
            return Result(FirewallProfileEnableOutcome.Uncertain, "The pending record contains incomplete eligibility evidence and was retained.", options.Profiles, record.Before, recordPath: pending);
        Report(progress, "Inspecting current firewall profile state without repeating the enable command.");
        var current = await InspectAsync(host, token).ConfigureAwait(false);
        bool verified = Complete(current) && options.Profiles.All(profile => current.Single(item => item.Profile == profile) is { Enabled: true, Managed: false });
        bool notApplied = Same(record.Before, current);
        if (!verified && !notApplied) return Result(FirewallProfileEnableOutcome.Uncertain,
            "Current profile state or policy ownership is incomplete or changed. The pending record was retained.", options.Profiles, record.Before, after: current, recordPath: pending);
        return await FinishAsync(Result(verified ? FirewallProfileEnableOutcome.Verified : FirewallProfileEnableOutcome.Failed,
            verified ? "The selected profiles are now enabled. The interrupted command was not repeated." :
            "The selected profiles remain in their exact prior disabled state. No change was verified and the command was not repeated.",
            options.Profiles, record.Before, after: current, recordPath: pending), root, true).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<FirewallProfileState>> InspectAsync(IFirewallProfileEnableHost host, CancellationToken token)
    {
        var value = await (host.InspectAsync(token) ?? throw new InvalidDataException("The firewall provider returned no inspection task."))
            .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
        return value ?? throw new InvalidDataException("The firewall provider returned no profile state.");
    }

    private static bool Complete(IReadOnlyList<FirewallProfileState>? state) => state is { Count: 3 } &&
        state.All(item => Enum.IsDefined(item.Profile) && item.Enabled is not null && item.Managed is not null) &&
        state.Select(item => item.Profile).Distinct().Count() == 3;
    private static bool Same(IReadOnlyList<FirewallProfileState> left, IReadOnlyList<FirewallProfileState> right) =>
        Complete(left) && Complete(right) && left.OrderBy(item => item.Profile).SequenceEqual(right.OrderBy(item => item.Profile));
    private static FirewallProfileEnableResult Result(FirewallProfileEnableOutcome outcome, string detail,
        IReadOnlyList<FirewallProfileTarget> profiles, IReadOnlyList<FirewallProfileState>? before = null, ToolResult? command = null,
        IReadOnlyList<FirewallProfileState>? after = null, string? recordPath = null) => new(outcome, detail, profiles, before, command, after, recordPath);

    private static async Task<FirewallProfileEnableResult> FinishAsync(FirewallProfileEnableResult result, string root, bool resolve)
    {
        string journal = Path.Combine(root, "firewall-profile-enable-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        string pending = Path.Combine(root, PendingName);
        if (resolve && File.Exists(pending)) File.Move(pending, journal + ".prepared.json");
        return result with { RecordPath = resolve ? journal : pending };
    }

    private static async Task WriteNewAsync(string path, byte[] data, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(data, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
    }

    private static async Task<FirewallProfilePendingRecord> ReadAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("Firewall recovery record size is invalid.");
        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Firewall recovery record contains conflicting fields.");
            var record = document.RootElement.Deserialize<FirewallProfilePendingRecord>(StrictJson);
            return record is { Schema: 1 } && record.StartedUtc != default &&
                string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                ? record : throw new InvalidDataException("Firewall recovery record is invalid.");
        }
        catch (JsonException error) { throw new InvalidDataException("Firewall recovery record is not valid JSON.", error); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed record FirewallProfilePendingRecord(int Schema, DateTimeOffset StartedUtc,
    IReadOnlyList<FirewallProfileTarget> Profiles, IReadOnlyList<FirewallProfileState> Before)
{
    public string Machine { get; init; } = "";
}

public sealed class WindowsFirewallProfileEnableHost : IFirewallProfileEnableHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public async Task<IReadOnlyList<FirewallProfileState>> InspectAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Firewall profile inspection requires Windows.");
        var result = await BoundedTool.RunAsync(ReadStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            throw new InvalidDataException("Firewall profile inspection was incomplete.");
        return InterpretInspection(result.Output);
    }

    public static IReadOnlyList<FirewallProfileState> InterpretInspection(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() != 3)
                throw new InvalidDataException("Firewall profile inspection returned incomplete state.");
            var values = new List<FirewallProfileState>(3);
            var seen = new HashSet<FirewallProfileTarget>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 3 ||
                    !item.TryGetProperty("Profile", out var profileValue) || profileValue.ValueKind != JsonValueKind.String ||
                    !Enum.TryParse(profileValue.GetString(), false, out FirewallProfileTarget profile) || !Enum.IsDefined(profile) || !seen.Add(profile) ||
                    !item.TryGetProperty("Enabled", out var enabledValue) || !BooleanOrNull(enabledValue, out bool? enabled) ||
                    !item.TryGetProperty("Managed", out var managedValue) || !BooleanOrNull(managedValue, out bool? managed))
                    throw new InvalidDataException("Firewall profile inspection returned invalid state.");
                values.Add(new(profile, enabled, managed));
            }
            return [.. values.OrderBy(value => value.Profile)];
        }
        catch (JsonException error) { throw new InvalidDataException("Firewall profile inspection returned invalid data.", error); }
    }

    private static bool BooleanOrNull(JsonElement value, out bool? result)
    {
        result = value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
        return value.ValueKind is JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null;
    }
    public Task<ToolResult> EnableAsync(IReadOnlyList<FirewallProfileTarget> profiles, CancellationToken token) =>
        BoundedTool.RunAsync(EnableStep(profiles), TimeSpan.FromMinutes(2), token: token);

    public static RepairStep EnableStep(IReadOnlyList<FirewallProfileTarget> profiles)
    {
        var selected = FirewallProfileEnable.Validate(new(profiles)).Profiles;
        string names = string.Join(",", selected.Select(profile => "'" + profile + "'"));
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Set-NetFirewallProfile -Name @(" + names + ") -PolicyStore PersistentStore -Enabled True -Confirm:$false -ErrorAction Stop";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], false);
    }

    private static RepairStep ReadStep()
    {
        const string script = """
            $ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue'
            $items=@(Get-NetFirewallProfile -PolicyStore ActiveStore -ErrorAction Stop|Where-Object Name -in @('Domain','Private','Public')|ForEach-Object{
              $path='HKLM:\SOFTWARE\Policies\Microsoft\WindowsFirewall\'+$_.Name+'Profile'
              $managed=$false
              if(Test-Path -LiteralPath $path){$value=(Get-ItemProperty -LiteralPath $path -Name EnableFirewall -ErrorAction SilentlyContinue).EnableFirewall;if($null -ne $value){$managed=if($value -is [int] -and $value -in 0,1){$true}else{$null}}}
              $enabled=if($null -eq $_.Enabled){$null}elseif($_.Enabled.ToString() -eq 'True'){$true}elseif($_.Enabled.ToString() -eq 'False'){$false}else{$null}
              [pscustomobject]@{Profile=$_.Name;Enabled=$enabled;Managed=$managed}
            })
            @($items)|ConvertTo-Json -Compress
            """;
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }
}
