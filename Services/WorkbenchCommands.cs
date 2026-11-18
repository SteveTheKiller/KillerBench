using System.Windows.Input;

namespace KillerBench.Services;

public static class WorkbenchCommands
{
    public static RoutedUICommand Overview { get; } = new();
    public static RoutedUICommand Investigate { get; } = new();
    public static RoutedUICommand Tools { get; } = new();
    public static RoutedUICommand History { get; } = new();
    public static RoutedUICommand ToolbarSmallIcons { get; } = new();
    public static RoutedUICommand ToolbarTextNone { get; } = new();
    public static RoutedUICommand ToolbarTextBeside { get; } = new();
    public static RoutedUICommand ToolbarTextOnly { get; } = new();
    public static RoutedUICommand OpenCapture { get; } = new();
    public static RoutedUICommand SaveCapture { get; } = new();
    public static RoutedUICommand ExportReport { get; } = new();
    public static RoutedUICommand ExportActivity { get; } = new();
    public static RoutedUICommand FindEvidence { get; } = new();
    public static RoutedUICommand SearchWorkspace { get; } = new();
}
