using System.Text.Json;

namespace KillerBench.Engine.Updates;

public enum UpdateSearchSource { Configured, WindowsUpdate, MicrosoftUpdate }
public sealed record UpdateSearchOptions(bool Software = true, bool Drivers = false, UpdateSearchSource Source = UpdateSearchSource.Configured)
{
    public const string MicrosoftUpdateServiceId = "7971f918-a847-4430-9279-4a52d1efe18d";
    public string Criteria()
    {
        if (!Software && !Drivers) throw new ArgumentException("Select software updates or drivers.");
        if (!Enum.IsDefined(Source)) throw new ArgumentException("Select a recognized update source.");
        return "IsInstalled=0 and IsHidden=0" + (Software && Drivers ? "" : Software ? " and Type='Software'" : " and Type='Driver'");
    }

    internal string ConfigureSearcher() => Source switch
    {
        UpdateSearchSource.Configured => "$searcher.ServerSelection=0; ",
        UpdateSearchSource.WindowsUpdate => "$searcher.ServerSelection=2; ",
        UpdateSearchSource.MicrosoftUpdate => "$registered=@($manager.Services | Where-Object {$_.ServiceID -eq '" + MicrosoftUpdateServiceId + "'}); if($registered.Count -ne 1){throw 'Microsoft Update is not registered on this PC.'}; $searcher.ServerSelection=3; $searcher.ServiceID='" + MicrosoftUpdateServiceId + "'; ",
        _ => throw new ArgumentException("Select a recognized update source.")
    };
}
public sealed record UpdateClassification(string Id, string Name);
public sealed record UpdateBehavior(bool CanRequestInput, int Impact, int RebootBehavior, bool Beta, IReadOnlyList<UpdateClassification> Classifications)
{
    public string Describe() => $"Interaction: {(CanRequestInput ? "may be required" : "not requested")}; restart: {RebootBehavior switch { 0 => "not required by this update", 1 => "required", _ => "may be required" }}; beta: {Beta}; " +
        "classification: " + string.Join(", ", Classifications.Select(c => c.Name));
}
public sealed record AvailableUpdate(string Id, int Revision, string Title, string Type, bool Downloaded, bool LicenseAccepted)
{
    public UpdateBehavior? Behavior { get; init; }
    public IReadOnlyList<UpdateProduct>? Products { get; init; }
}
public sealed record UpdateSearchResult(UpdateSearchOptions Options, IReadOnlyList<AvailableUpdate> Updates, bool Partial, string Detail)
{
    public ToolResult? Evidence { get; init; }
    public int? FailureHResult { get; init; }
    public IReadOnlyList<UpdateClassification> GetClassifications() => Array.AsReadOnly(Updates
        .SelectMany(update => update.Behavior?.Classifications ?? [])
        .Where(category => Guid.TryParse(category.Id, out var id) && id != Guid.Empty &&
            !string.IsNullOrWhiteSpace(category.Name) && category.Name.Length <= 256 && !category.Name.Any(char.IsControl))
        .GroupBy(category => Guid.Parse(category.Id))
        .Select(group => new UpdateClassification(group.Key.ToString("D"), group.Select(category => category.Name).Order(StringComparer.Ordinal).First()))
        .OrderBy(category => category.Name, StringComparer.Ordinal).ThenBy(category => category.Id, StringComparer.Ordinal).ToArray());
    public IReadOnlyList<UpdateProduct> GetProducts() => UpdateProducts.Choices(Updates);
    public IReadOnlyList<AvailableUpdate> Filter(string? title, bool downloadedOnly = false, Guid? classificationId = null, Guid? productId = null)
    {
        title = title?.Trim() ?? "";
        if (productId == Guid.Empty) throw new ArgumentException("Select a nonempty product identity.", nameof(productId));
        if (classificationId == Guid.Empty) throw new ArgumentException("Select a nonempty classification identity.", nameof(classificationId));
        if (title.Length > 256 || title.Any(char.IsControl)) throw new ArgumentException("Use up to 256 printable characters for the update filter.", nameof(title));
        return Array.AsReadOnly(Updates.Where(update => (!downloadedOnly || update.Downloaded) &&
            update.Title.Contains(title, StringComparison.OrdinalIgnoreCase) &&
            (productId is null || update.Products?.Any(product => Guid.TryParse(product.Id, out var id) && id == productId) == true) &&
            (classificationId is null || update.Behavior?.Classifications.Any(category =>
                Guid.TryParse(category.Id, out var id) && id == classificationId) == true)).ToArray());
    }
    public string Format() => "AVAILABLE UPDATES / DISCOVERY ONLY\n" + Detail + "\n" +
        $"Source: {Options.Source}; software selected: {Options.Software}; drivers selected: {Options.Drivers}\nReturned: {Updates.Count}; partial: {Partial}\n\n" +
        string.Join("\n", Updates.Select(u => $"{u.Type}: {u.Title}\n  {u.Id} revision {u.Revision}; downloaded: {u.Downloaded}; license accepted: {u.LicenseAccepted}\n  {u.Behavior?.Describe() ?? "Installation behavior unavailable; not eligible for unattended planning."}")) +
        "\n\nNo update installed or license accepted. An empty list is not proof that Windows is fully updated. Software can include other Microsoft products already offered by the configured source.";
}
public static class UpdateDiscovery
{
    public static RepairStep QueryStep(UpdateSearchOptions options) => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$session=New-Object -ComObject Microsoft.Update.Session; $session.ClientApplicationID='KillerBench'; $searcher=$session.CreateUpdateSearcher(); $manager=New-Object -ComObject Microsoft.Update.ServiceManager; " +
        options.ConfigureSearcher() +
        "try{$result=$searcher.Search(\"" + options.Criteria() + "\")}catch{$failure=$_.Exception;while($failure.InnerException){$failure=$failure.InnerException};[pscustomobject]@{failureHresult=[int]$failure.HResult}|ConvertTo-Json -Compress;exit 0}; $items=@(); " +
        "for($i=0; $i -lt [Math]::Min(500,$result.Updates.Count); $i++) { $u=$result.Updates.Item($i); $behavior=$null; try { $b=$u.InstallationBehavior; $behavior=[pscustomobject]@{input=$b.CanRequestUserInput; impact=$b.Impact; reboot=$b.RebootBehavior; beta=$u.IsBeta; classifications=@($u.Categories | Where-Object {$_.Type -eq 'UpdateClassification'} | ForEach-Object {[pscustomobject]@{id=$_.CategoryID; name=$_.Name}})} } catch {} ; $items+=[pscustomobject]@{id=$u.Identity.UpdateID; revision=$u.Identity.RevisionNumber; title=$u.Title; type=[int]$u.Type; downloaded=$u.IsDownloaded; license=$u.EulaAccepted; behavior=$behavior} }; " +
        "for($i=0; $i -lt $items.Count; $i++) { $products=$null; try { $products=@($result.Updates.Item($i).Categories | Where-Object {$_.Type -eq 'Product'} | ForEach-Object {[pscustomobject]@{id=$_.CategoryID; name=$_.Name}}) } catch {}; $items[$i] | Add-Member -NotePropertyName products -NotePropertyValue $products }; " +
        "[pscustomobject]@{code=[int]$result.ResultCode; total=$result.Updates.Count; updates=$items} | ConvertTo-Json -Depth 8 -Compress"], true);

    public static async Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, IProgress<string>? progress = null, Func<RepairStep, CancellationToken, Task<ToolResult>>? query = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var step = QueryStep(options);
        progress = progress is null ? null : new DiagnosticProgress(progress);
        progress?.Report("Searching the configured Windows Update source. This can take several minutes...");
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
        var pending = query is null ? BoundedTool.RunAsync(step, TimeSpan.FromMinutes(5), token: deadline.Token)
            : query(step, deadline.Token) ?? throw new InvalidOperationException("Update query provider returned no task.");
        var search = pending.WaitAsync(deadline.Token);
        while (!search.IsCompleted)
        {
            await Task.WhenAny(search, Task.Delay(TimeSpan.FromSeconds(20))).ConfigureAwait(false);
            if (!search.IsCompleted) progress?.Report($"Windows Update search is still running ({elapsed.Elapsed.TotalSeconds:F0} seconds). No installation started.");
        }
        try
        {
            var result = await search.ConfigureAwait(false)
                ?? throw new InvalidOperationException("Update query provider returned no result.");
            token.ThrowIfCancellationRequested();
            return Interpret(result, options);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return Interpret(new ToolResult(-1, "", "Update search deadline expired.", false) { TimedOut = true }, options); }
    }

    public static UpdateSearchResult Interpret(ToolResult result, UpdateSearchOptions options)
    {
        options.Criteria();
        UpdateSearchResult Unknown() => new(options, [], true, "Update search was unavailable, incomplete or unrecognized. Review service, policy and connectivity; no source settings changed.") { Evidence = result };
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (!UniqueFields(root)) return Unknown();
            if (root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 1 &&
                root.TryGetProperty("failureHresult", out var failureCode) && failureCode.TryGetInt32(out int hresult) && hresult != 0)
            {
                var reference = WindowsUpdateErrorCatalog.Find(hresult);
                string identity = $"0x{unchecked((uint)hresult):X8}" + (reference is null ? "" : " " + reference.Symbol);
                string explanation = reference is null
                    ? "The current Windows Update search returned an unrecognized HRESULT. Review the retained evidence before selecting any repair."
                    : reference.Meaning + " " + reference.DiagnosticScope;
                return new(options, [], true, "Current Windows Update search failed with " + identity + ". " + explanation +
                    " No update was installed and no repair was selected.") { Evidence = result, FailureHResult = hresult };
            }
            int code = root.GetProperty("code").GetInt32(), total = root.GetProperty("total").GetInt32();
            var list = root.GetProperty("updates");
            if (code is not (2 or 3) || total < 0 || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > 500 || list.GetArrayLength() != Math.Min(500, total)) return Unknown();
            var items = new List<AvailableUpdate>();
            var ids = new HashSet<(Guid Id, int Revision)>();
            foreach (var item in list.EnumerateArray())
            {
                string? id = item.GetProperty("id").GetString(), title = item.GetProperty("title").GetString();
                int revision = item.GetProperty("revision").GetInt32(), type = item.GetProperty("type").GetInt32();
                if (!Guid.TryParse(id, out var identity) || identity == Guid.Empty || revision < 0 || string.IsNullOrWhiteSpace(title) || title.Length > 4096 ||
                    type is not (1 or 2) || (type == 1 && !options.Software) || (type == 2 && !options.Drivers) || !ids.Add((identity, revision))) return Unknown();
                items.Add(new(id!, revision, title, type == 1 ? "Software" : "Driver", item.GetProperty("downloaded").GetBoolean(), item.GetProperty("license").GetBoolean()) { Behavior = ReadBehavior(item), Products = UpdateProducts.Read(item) });
            }
            return new(options, items, code != 2 || total > 500,
                "Search used the existing configured source. Policies, pauses, service registration and update approvals were not changed.") { Evidence = result };
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return Unknown(); }
    }

    private static UpdateBehavior? ReadBehavior(JsonElement item)
    {
        try
        {
            if (!item.TryGetProperty("behavior", out var behavior) || behavior.ValueKind != JsonValueKind.Object) return null;
            int impact = behavior.GetProperty("impact").GetInt32(), reboot = behavior.GetProperty("reboot").GetInt32();
            if (impact is < 0 or > 2 || reboot is < 0 or > 2) return null;
            bool input = behavior.GetProperty("input").GetBoolean(), beta = behavior.GetProperty("beta").GetBoolean();
            var categories = behavior.GetProperty("classifications");
            if (categories.ValueKind != JsonValueKind.Array || categories.GetArrayLength() is < 1 or > 32) return null;
            var classifications = new List<UpdateClassification>();
            var classificationIds = new HashSet<Guid>();
            foreach (var category in categories.EnumerateArray())
            {
                string? id = category.GetProperty("id").GetString(), name = category.GetProperty("name").GetString();
                if (!Guid.TryParse(id, out var identity) || identity == Guid.Empty || !classificationIds.Add(identity) ||
                    string.IsNullOrWhiteSpace(name) || name.Length > 256 || name.Any(char.IsControl)) return null;
                classifications.Add(new(id!, name));
            }
            return new(input, impact, reboot, beta, classifications);
        }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return null; }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
}
