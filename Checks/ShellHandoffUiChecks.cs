using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using KillerBench.Engine;

/// <summary>Raw transcript completeness, export source identity and accessible names in the live shell.</summary>
internal static class ShellHandoffUiChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static void Run(KillerBench.Shell.MainWindow window)
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

        var register = typeof(KillerBench.Shell.MainWindow).GetMethod("RegisterWorkflowResult", Flags)!;
        var begin = typeof(KillerBench.Shell.MainWindow).GetMethod("BeginWorkflowOutput", Flags)!;
        var picker = typeof(KillerBench.Shell.MainWindow).GetMethod("CreateExportPicker", Flags)
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

        var zoomKey = typeof(KillerBench.Shell.MainWindow).GetMethod("HandleZoomKey", Flags)
            ?? throw new Exception("Interface zoom needs a keyboard handler.");
        var scaleField = typeof(KillerBench.Shell.MainWindow).GetField("appScale", Flags)!;
        var preference = typeof(KillerBench.Shell.MainWindow).Assembly.GetType("KillerBench.Services.Preferences")!.GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic)!;
        const System.Windows.Input.ModifierKeys chord = System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift;
        double Zoom(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers = chord)
        {
            Require((bool)zoomKey.Invoke(window, [key, modifiers])!, "Zoom key " + modifiers + "+" + key + " is handled");
            return (double)scaleField.GetValue(window)!;
        }
        Require(Zoom(System.Windows.Input.Key.D0) == 1 && Zoom(System.Windows.Input.Key.OemPlus) == 1.05 && Zoom(System.Windows.Input.Key.Add) == 1.1 &&
            Zoom(System.Windows.Input.Key.OemMinus) == 1.05 && (string?)preference.Invoke(null, ["AppScale"]) == "1.05" &&
            Zoom(System.Windows.Input.Key.Subtract) == 1 && Zoom(System.Windows.Input.Key.OemPlus) == 1.05 && Zoom(System.Windows.Input.Key.NumPad0) == 1,
            "Ctrl+Shift+Plus, Minus and 0 step and reset the whole-app zoom and save it");
        Require(!(bool)zoomKey.Invoke(window, [System.Windows.Input.Key.OemPlus, System.Windows.Input.ModifierKeys.Control])!,
            "Ctrl alone does not change the interface zoom");
        Console.WriteLine("PASS: interface zoom is keyboard-operable with Ctrl+Shift+Plus, Minus and 0, and persists.");

        var hold = typeof(KillerBench.Shell.MainWindow).GetMethods(Flags).Single(method => method.Name.EndsWith("HoldUpdateForRepair", StringComparison.Ordinal));
        var defender = new DelayedDefenderHost();
        typeof(KillerBench.Shell.MainWindow).GetField("createDefenderScanHost", Flags)!.SetValue(window, (Func<KillerBench.Engine.Security.IDefenderScanHost>)(() => defender));
        Get<ComboBox>("DefenderScanChoice").SelectedIndex = 1;
        var scan = (Task)typeof(KillerBench.Shell.MainWindow).GetMethod("RunDefenderScanAsync", Flags)!.Invoke(window, [true])!;
        Require(!scan.IsCompleted && (bool)hold.Invoke(window, [])!, "A confirmed Defender request holds close and self-update while it is being prepared");
        defender.Protection.SetException(new InvalidOperationException("Fixture provider ended the request."));
        while (!scan.IsCompleted) System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        scan.GetAwaiter().GetResult();
        Require(!(bool)hold.Invoke(window, [])!, "The Defender hold ends once the request has an outcome");
        Console.WriteLine("PASS: a confirmed Defender request holds close and self-update until its immediate outcome is recorded.");
    }

    private sealed class DelayedDefenderHost : KillerBench.Engine.Security.IDefenderScanHost
    {
        internal readonly TaskCompletionSource<WorkflowCheck> Protection = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsAdministrator() => true;
        public bool RebootPending() => false;
        public Task<WorkflowCheck> ReadProtectionAsync(CancellationToken token) => Protection.Task;
        public Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token) => throw new InvalidOperationException("Not reached.");
        public Task<KillerBench.Engine.Infrastructure.ToolResult> StartAsync(KillerBench.Engine.Security.DefenderScanType type, CancellationToken token) =>
            throw new InvalidOperationException("No Defender scan is started by this check.");
    }

    private static void RunBitLocker(KillerBench.Shell.MainWindow window, string marker, bool guarded)
    {
        var outcomeType = typeof(KillerBench.Shell.MainWindow).GetNestedType("BitLockerOperationOutcome", BindingFlags.NonPublic)!;
        object outcome = guarded
            ? Activator.CreateInstance(outcomeType, "Fixture detail", true, new[] { marker }, "BitLocker fixture", "BitLocker fixture action", "fixture-journal.json", "Nothing further")!
            : Activator.CreateInstance(outcomeType, "Fixture detail", true, new[] { marker }, null, null, null, null)!;
        object operation = typeof(ShellHandoffUiChecks).GetMethod(nameof(Returning), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(outcomeType).Invoke(null, [outcome])!;
        var task = (Task)typeof(KillerBench.Shell.MainWindow).GetMethod("RunBitLockerOperationAsync", Flags)!.Invoke(window, [operation])!;
        Require(task.IsCompleted, "The inert BitLocker fixture completes without a provider");
        task.GetAwaiter().GetResult();
    }

    private static Func<CancellationToken, Task<T>> Returning<T>(T value) => _ => Task.FromResult(value);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
