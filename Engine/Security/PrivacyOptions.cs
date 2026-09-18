namespace KillerBench.Engine;

public enum PrivacyArea
{
    Location, Sensors, DiagnosticData, Advertising, Camera, Microphone,
    ActivityHistory, AppActivity, InputPersonalization, ClipboardHistory, ClipboardSync, Feedback, Spotlight,
    DeliveryOptimization, OnlineSpeech, WebSearch, NetworkDiscovery
}

public sealed record PrivacyChoice(PrivacyArea Area, string Title, string Impact);

public static class PrivacyCatalog
{
    public static IReadOnlyList<PrivacyChoice> All { get; } = Array.AsReadOnly(new PrivacyChoice[]
    {
        new(PrivacyArea.Location, "Location", "Review location access and Find My Device availability."),
        new(PrivacyArea.Sensors, "Sensors", "Review sensor access separately from location."),
        new(PrivacyArea.DiagnosticData, "Diagnostic data", "Review diagnostic collection, error reporting and compatibility services separately; supported levels depend on Windows edition and policy."),
        new(PrivacyArea.Advertising, "Advertising and suggestions", "Review advertising ID, tailored experiences and suggested content."),
        new(PrivacyArea.Camera, "Camera", "Restricting access can prevent video calls. Keep app permissions and managed policy visible."),
        new(PrivacyArea.Microphone, "Microphone", "Restricting access can prevent calls and recording. Keep app permissions and managed policy visible."),
        new(PrivacyArea.ActivityHistory, "Activity history", "Review activity recording, publishing and upload independently."),
        new(PrivacyArea.AppActivity, "App activity", "Review local application launch tracking separately from cloud activity history."),
        new(PrivacyArea.InputPersonalization, "Typing and inking", "Review input personalization and implicit typing and inking collection policy."),
        new(PrivacyArea.ClipboardHistory, "Clipboard history", "Restricting history affects access to previously copied items."),
        new(PrivacyArea.ClipboardSync, "Clipboard sync", "Restricting sync affects copying between devices; local history is a separate choice."),
        new(PrivacyArea.Feedback, "Feedback prompts", "Review feedback notifications without changing diagnostic services."),
        new(PrivacyArea.Spotlight, "Spotlight", "Review rotating content and Windows suggestions."),
        new(PrivacyArea.DeliveryOptimization, "Update sharing", "Review peer download and upload settings. Cache removal is a separate cleanup operation."),
        new(PrivacyArea.OnlineSpeech, "Online speech", "Restricting cloud speech recognition can affect dictation."),
        new(PrivacyArea.WebSearch, "Web search", "Review online results independently from local search."),
        new(PrivacyArea.NetworkDiscovery, "Network privacy", "Review connectivity probes, LLMNR and wireless behavior separately because applications and name resolution may depend on them.")
    });
}

public sealed record PrivacyOptions(IReadOnlyList<PrivacyArea> Selected)
{
    public IReadOnlyList<PrivacyChoice> Plan()
    {
        ArgumentNullException.ThrowIfNull(Selected);
        var copy = Selected.ToArray();
        if (copy.Length > PrivacyCatalog.All.Count || copy.Any(a => !Enum.IsDefined(a)) || copy.Distinct().Count() != copy.Length)
            throw new ArgumentException("Select each supported privacy area at most once.");
        return PrivacyCatalog.All.Where(c => copy.Contains(c.Area)).ToArray();
    }

    public string Describe()
    {
        var choices = Plan();
        return "PRIVACY OPTIONS / PREVIEW ONLY\n" +
            (choices.Count == 0 ? "No areas selected. All settings remain unchanged.\n" : string.Join("\n", choices.Select(c => c.Title + ": " + c.Impact)) + "\n") +
            "No settings read or changed. These are review selections, not an executable repair plan. Separate guarded controls provide fresh policy checks and exact-value undo for Clipboard history and the supported account settings shown in the Privacy setting selector.";
    }
}
