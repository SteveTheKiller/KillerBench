using System.Globalization;
using System.Windows;
using System.Windows.Data;
using KillerBench.Engine.Workflows;

namespace KillerBench.Controls;

/// <summary>
/// Looks up text that code builds at run time in the active string resources, so section names,
/// workflow titles and Engine choices follow the selected language. A missing key keeps the
/// Engine's English text instead of showing the key.
/// </summary>
public static class LocalizedText
{
    public static string Get(string key) => Application.Current?.TryFindResource(key) as string ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    public static string Workflow(WorkflowKind kind) =>
        Application.Current?.TryFindResource("Str_Workflow_" + kind) as string
        ?? WorkflowCatalog.All.Single(workflow => workflow.Kind == kind).Title;

    public static string Choice(Enum value) =>
        Application.Current?.TryFindResource("Str_Choice_" + value.GetType().Name + "_" + value) as string
        ?? value.ToString();
}

/// <summary>Shows a workflow picker entry in the selected language.</summary>
public sealed class WorkflowTitleConverter : IValueConverter
{
    public static WorkflowTitleConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is WorkflowDefinition workflow ? LocalizedText.Workflow(workflow.Kind) : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Shows an Engine enum choice, such as a BitLocker operation, in the selected language.</summary>
public sealed class ChoiceLabelConverter : IValueConverter
{
    public static ChoiceLabelConverter Instance { get; } = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Enum choice ? LocalizedText.Choice(choice) : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
