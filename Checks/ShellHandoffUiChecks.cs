using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using KillerBench.Engine;

/// <summary>Raw transcript completeness, export source identity and accessible names in the live shell.</summary>
internal static class ShellHandoffUiChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static void Run(KillerBench.MainWindow window)
    {
        T Get<T>(string name) => (T)window.FindName(name);
        string Resource(string key) => (string)window.FindResource(key);
        string NameOf(UIElement element) => UIElementAutomationPeer.CreatePeerForElement(element)!.GetName();

        Require(NameOf(Get<Slider>("ProfileAge")) == Resource("Str_Profile_MinimumAge"),
            "The profile age slider is named by its visible label");
        Require(NameOf(Get<ListBox>("ProfileSelection")) == Resource("Str_Profile_SelectAfterReview"),
            "The profile selection list is named by its visible heading");

        Require(Get<CheckBox>("UpdateThirdParty").Visibility == Visibility.Collapsed &&
            Get<Expander>("ThirdPartyUpdateSection").Visibility == Visibility.Collapsed,
            "Third-party application updates are hidden until 1.1");

        foreach (bool guarded in new[] { false, true })
        {
            string marker = "Final BitLocker fixture line " + (guarded ? "guarded" : "ordinary");
            RunBitLocker(window, marker, guarded);
            Require(Get<TextBox>("EvidenceBox").Text.Contains(marker, StringComparison.Ordinal),
                "The final BitLocker result lines reach Raw output (" + (guarded ? "guarded" : "ordinary") + " branch)");
        }

        var register = typeof(KillerBench.MainWindow).GetMethod("RegisterWorkflowResult", Flags)!;
        var begin = typeof(KillerBench.MainWindow).GetMethod("BeginWorkflowOutput", Flags)!;
        var picker = typeof(KillerBench.MainWindow).GetMethod("CreateExportPicker", Flags)
            ?? throw new Exception("Export must build its save picker in one place so the source it names can be checked.");
        var sources = Get<ListBox>("SourcesList");
        register.Invoke(window, [new WorkflowResult(WorkflowKind.Services, "fixture", DateTimeOffset.UtcNow, "Needs attention",
            [new("Fixture A", CheckState.Attention, "Run A")], [], "fixture-a.json"), "Run A", false]);
        string first = ((TextBlock)((ListBoxItem)sources.SelectedItem).Content).Text;
        begin.Invoke(window, [WorkflowKind.Network, false]);
        var duringRun = (KillerBench.Controls.FileDialog)picker.Invoke(window, [])!;
        Require(duringRun.Title.Contains(first, StringComparison.Ordinal) && duringRun.Title.Contains("earlier", StringComparison.OrdinalIgnoreCase),
            "Export during a live run names the earlier report it will save: " + duringRun.Title);
        register.Invoke(window, [new WorkflowResult(WorkflowKind.Network, "fixture", DateTimeOffset.UtcNow.AddSeconds(1), "Needs attention",
            [new("Fixture B", CheckState.Attention, "Run B")], [], "fixture-b.json"), "Run B", false]);
        var afterRun = (KillerBench.Controls.FileDialog)picker.Invoke(window, [])!;
        Require(!afterRun.Title.Contains("earlier", StringComparison.OrdinalIgnoreCase),
            "After the new run completes, Export uses the new result");
        Console.WriteLine("PASS: final BitLocker output reaches Raw output, export names its source during a live run, and profile controls have accessible names.");
    }

    private static void RunBitLocker(KillerBench.MainWindow window, string marker, bool guarded)
    {
        var outcomeType = typeof(KillerBench.MainWindow).GetNestedType("BitLockerOperationOutcome", BindingFlags.NonPublic)!;
        object outcome = guarded
            ? Activator.CreateInstance(outcomeType, "Fixture detail", true, new[] { marker }, "BitLocker fixture", "BitLocker fixture action", "fixture-journal.json", "Nothing further")!
            : Activator.CreateInstance(outcomeType, "Fixture detail", true, new[] { marker }, null, null, null, null)!;
        object operation = typeof(ShellHandoffUiChecks).GetMethod(nameof(Returning), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(outcomeType).Invoke(null, [outcome])!;
        var task = (Task)typeof(KillerBench.MainWindow).GetMethod("RunBitLockerOperationAsync", Flags)!.Invoke(window, [operation])!;
        Require(task.IsCompleted, "The inert BitLocker fixture completes without a provider");
        task.GetAwaiter().GetResult();
    }

    private static Func<CancellationToken, Task<T>> Returning<T>(T value) => _ => Task.FromResult(value);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
