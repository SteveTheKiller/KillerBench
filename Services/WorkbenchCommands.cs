using System.Windows.Input;

namespace KillerBench.Services;

public static class WorkbenchCommands
{
    public static RoutedUICommand OpenCapture { get; } = new();
    public static RoutedUICommand SaveCapture { get; } = new();
    public static RoutedUICommand ExportReport { get; } = new();
    public static RoutedUICommand ExportActivity { get; } = new();
    public static RoutedUICommand FindEvidence { get; } = new();
}
