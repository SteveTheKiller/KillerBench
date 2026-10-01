using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Services;

internal sealed record ExactServiceFailureEventDefinition(
    string InternalName,
    string DisplayLabel,
    string CheckName,
    string Scope);

internal static class ExactServiceFailureEvents
{
    internal const int MaximumScanned = 2000;
    internal const int MaximumRecords = 200;
    private static readonly XNamespace EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";
    internal const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$service=Get-Service -Name $target -ErrorAction Stop;$display=[string]$service.DisplayName;" +
        "$events=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Service Control Manager';StartTime=(Get-Date).AddDays(-7);Level=1,2} -MaxEvents 2001 -ErrorAction Stop);" +
        "$records=[Collections.Generic.List[string]]::new();foreach($event in @($events|Select-Object -First 2000)){" +
        "[xml]$xml=$event.ToXml();$values=@($xml.Event.EventData.Data|ForEach-Object{[string]$_.'#text'});if($null-ne$xml.Event.UserData){$values+=@($xml.Event.UserData.SelectNodes('.//*[not(*)]')|ForEach-Object{[string]$_.InnerText})};" +
        "if(@($values|Where-Object{$_ -ieq $target -or $_ -ceq $display}).Count-gt 0){if($records.Count-ge 201){break};$records.Add($event.ToXml())}};" +
        "[pscustomobject]@{complete=($events.Count-le 2000-and$records.Count-le 200);scanned=[Math]::Min($events.Count,2000);service=$display;records=@($records|Select-Object -First 200)}|ConvertTo-Json -Depth 4 -Compress";

    internal static async Task<IReadOnlyList<WorkflowCheck>> CheckManyAsync(
        IReadOnlyList<ExactServiceFailureEventDefinition> definitions, CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        token.ThrowIfCancellationRequested();
        if (definitions.Count is < 1 or > 64 || definitions.Any(definition => definition is null) ||
            definitions.Select(definition => definition.InternalName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != definitions.Count)
            throw new ArgumentException("Use between one and 64 distinct service definitions.", nameof(definitions));
        foreach (var definition in definitions) Validate(definition);
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Exact service failure history requires Windows.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            string targets = string.Join(",", definitions.Select(definition => "'" + definition.InternalName + "'"));
            string command = "$targets=@(" + targets + ");" +
                "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
                "$services=@($targets|ForEach-Object{try{$item=Get-Service -Name $_ -ErrorAction Stop;[pscustomobject]@{target=$_;service=[string]$item.DisplayName;available=$true}}catch{[pscustomobject]@{target=$_;service=$_;available=$false}}});" +
                "$events=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Service Control Manager';StartTime=(Get-Date).AddDays(-7);Level=1,2} -MaxEvents 2001 -ErrorAction Stop);" +
                "$items=@($services|ForEach-Object{$target=[string]$_.target;$display=[string]$_.service;$available=[bool]$_.available;$records=[Collections.Generic.List[string]]::new();$complete=$true;" +
                "if($available){" +
                "foreach($event in @($events|Select-Object -First 2000)){[xml]$xml=$event.ToXml();$values=@($xml.Event.EventData.Data|ForEach-Object{[string]$_.'#text'});" +
                "if($null-ne$xml.Event.UserData){$values+=@($xml.Event.UserData.SelectNodes('.//*[not(*)]')|ForEach-Object{[string]$_.InnerText})};" +
                "if(@($values|Where-Object{$_ -ieq $target -or $_ -ceq $display}).Count-gt 0){if($records.Count-ge 200){$complete=$false;break};$records.Add($event.ToXml())}}};" +
                "[pscustomobject]@{target=$target;service=$display;available=$available;complete=$complete;records=@($records)}});" +
                "[pscustomobject]@{complete=($events.Count-le 2000);scanned=[Math]::Min($events.Count,2000);items=$items}|ConvertTo-Json -Depth 5 -Compress";
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true), deadline.Token)
                ?? throw new InvalidOperationException("Exact service event provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Exact service event provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return InterpretMany(definitions, evidence, DateTimeOffset.UtcNow, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Array.AsReadOnly(definitions.Select(definition => Unknown(definition,
                definition.DisplayLabel + " event inspection exceeded its overall deadline.",
                new(-1, "", definition.DisplayLabel + " event inspection deadline expired.", false) { TimedOut = true })).ToArray());
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Array.AsReadOnly(definitions.Select(definition => Unknown(definition,
                definition.DisplayLabel + " event inspection was unavailable.", new(-1, "", error.Message, false))).ToArray());
        }
    }

    internal static async Task<WorkflowCheck> CheckAsync(ExactServiceFailureEventDefinition definition,
        CancellationToken token = default, Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        token.ThrowIfCancellationRequested();
        Validate(definition);
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(definition.DisplayLabel + " failure history requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: cancellationToken);
            string command = "$target='" + definition.InternalName + "';" + Script;
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true), deadline.Token)
                ?? throw new InvalidOperationException(definition.DisplayLabel + " event provider returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException(definition.DisplayLabel + " event provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(definition, evidence, DateTimeOffset.UtcNow, token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown(definition, definition.DisplayLabel + " event inspection exceeded its overall deadline.",
                new(-1, "", definition.DisplayLabel + " event inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown(definition, definition.DisplayLabel + " event inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    internal static IReadOnlyList<WorkflowCheck> InterpretMany(IReadOnlyList<ExactServiceFailureEventDefinition> definitions,
        ToolResult evidence, DateTimeOffset nowUtc, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Array.AsReadOnly(definitions.Select(definition => Unknown(definition,
                definition.DisplayLabel + " event collection failed or was incomplete.", evidence)).ToArray());
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) ||
                !Count(root, "scanned", MaximumScanned, out int scanned) ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() != definitions.Count)
                return MalformedMany(definitions, evidence);

            var byTarget = definitions.ToDictionary(definition => definition.InternalName, StringComparer.OrdinalIgnoreCase);
            var results = new List<WorkflowCheck>(definitions.Count);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (!Unique(item, 5) || !Text(item, "target", 256, out string target) || !seen.Add(target) || !byTarget.TryGetValue(target, out var definition) ||
                    !Text(item, "service", 256, out string service) || !item.TryGetProperty("records", out var records) ||
                    !Boolean(item, "available", out bool available) || !Boolean(item, "complete", out bool itemComplete) ||
                    records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > MaximumRecords || !available && records.GetArrayLength() != 0)
                    return MalformedMany(definitions, evidence);
                if (!available)
                {
                    results.Add(Unknown(definition, definition.DisplayLabel + " is not registered or its display identity could not be read.", evidence));
                    continue;
                }
                string single = JsonSerializer.Serialize(new { complete = complete && itemComplete, scanned, service, records = records.Clone() });
                results.Add(Interpret(definition, new(evidence.ExitCode, single, evidence.Error, evidence.Truncated)
                {
                    TimedOut = evidence.TimedOut
                }, nowUtc, token));
            }
            if (seen.Count != definitions.Count) return MalformedMany(definitions, evidence);
            return Array.AsReadOnly(definitions.Select(definition => results.Single(result =>
                string.Equals(result.Name, definition.CheckName, StringComparison.Ordinal))).ToArray());
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException or OverflowException or ArgumentException)
        {
            return MalformedMany(definitions, evidence);
        }
    }

    internal static WorkflowCheck Interpret(ExactServiceFailureEventDefinition definition, ToolResult evidence,
        DateTimeOffset nowUtc, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown(definition, definition.DisplayLabel + " event collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            if (!Unique(root, 4) || !Boolean(root, "complete", out bool complete) || !Count(root, "scanned", MaximumScanned, out int scanned) ||
                !Text(root, "service", 256, out string service) ||
                !root.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > MaximumRecords)
                return Unknown(definition, definition.DisplayLabel + " event evidence was malformed or exceeded its bounds.", evidence);

            var identities = new HashSet<long>();
            var groups = new Dictionary<(int Id, string Level), (int Count, DateTimeOffset Newest)>();
            foreach (var record in records.EnumerateArray())
            {
                token.ThrowIfCancellationRequested();
                if (record.ValueKind != JsonValueKind.String || record.GetString() is not { } xml || xml.Length is < 1 or > 262144)
                    return Unknown(definition, "A " + definition.DisplayLabel + " event record was malformed.", evidence);
                var eventRoot = XElement.Parse(xml, LoadOptions.None);
                var system = eventRoot.Element(EventNamespace + "System") ?? throw new InvalidDataException("Event metadata is missing.");
                string? provider = (string?)system.Element(EventNamespace + "Provider")?.Attribute("Name");
                if (!string.Equals(provider, "Service Control Manager", StringComparison.Ordinal) ||
                    !int.TryParse(system.Element(EventNamespace + "EventID")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int eventId) || eventId < 1 ||
                    system.Element(EventNamespace + "Level")?.Value is not { } level || level is not ("1" or "2") ||
                    !long.TryParse(system.Element(EventNamespace + "EventRecordID")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out long identity) || identity < 1 || !identities.Add(identity) ||
                    !DateTimeOffset.TryParse((string?)system.Element(EventNamespace + "TimeCreated")?.Attribute("SystemTime"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ||
                    time > nowUtc.AddMinutes(5) || time < nowUtc.AddDays(-8) || !HasIdentity(eventRoot, definition.InternalName, service))
                    return Unknown(definition, "A " + definition.DisplayLabel + " event record had an unexpected identity or time.", evidence);
                var key = (eventId, level);
                groups[key] = groups.TryGetValue(key, out var current) ? (current.Count + 1, current.Newest > time ? current.Newest : time) : (1, time);
            }

            string detail = $"Scanned {scanned} recent Service Control Manager error or critical record{(scanned == 1 ? "" : "s")} from the last seven days; retained {records.GetArrayLength()} exact {definition.DisplayLabel} match{(records.GetArrayLength() == 1 ? "" : "es")}.";
            if (groups.Count > 0)
                detail += " Groups: " + string.Join("; ", groups.OrderByDescending(group => group.Value.Newest).ThenBy(group => group.Key.Id)
                    .Select(group => $"event {group.Key.Id}, level {group.Key.Level}: {group.Value.Count}, newest {group.Value.Newest:O}")) + ".";
            if (!complete) detail += " The source scan or retained match set reached its limit, so coverage is incomplete.";
            CheckState state = !complete ? CheckState.Unknown : records.GetArrayLength() > 0 ? CheckState.Attention : CheckState.Information;
            return new(definition.CheckName, state, detail + definition.Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or XmlException or InvalidDataException or InvalidOperationException or OverflowException)
        {
            return Unknown(definition, definition.DisplayLabel + " event evidence could not be interpreted.", evidence);
        }
    }

    private static bool HasIdentity(XElement root, string internalName, string displayName) =>
        (root.Element(EventNamespace + "EventData")?.Elements(EventNamespace + "Data") ?? [])
            .Concat(root.Element(EventNamespace + "UserData")?.Descendants() ?? [])
            .Where(element => !element.HasElements)
            .Select(element => element.Value.Trim())
            .Any(value => string.Equals(value, internalName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, displayName, StringComparison.Ordinal));
    private static void Validate(ExactServiceFailureEventDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.InternalName.Length is < 1 or > 256 ||
            definition.InternalName.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Service identity is invalid.", nameof(definition));
    }
    private static System.Collections.ObjectModel.ReadOnlyCollection<WorkflowCheck> MalformedMany(IReadOnlyList<ExactServiceFailureEventDefinition> definitions, ToolResult evidence) =>
        Array.AsReadOnly(definitions.Select(definition => Unknown(definition,
            definition.DisplayLabel + " event evidence was malformed or exceeded its bounds.", evidence)).ToArray());
    private static WorkflowCheck Unknown(ExactServiceFailureEventDefinition definition, string detail, ToolResult evidence) =>
        new(definition.CheckName, CheckState.Unknown, detail + definition.Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Count(JsonElement owner, string name, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= 0 && value <= maximum;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
