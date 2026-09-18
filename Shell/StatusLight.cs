using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KillerBench;

public partial class MainWindow
{
    private async void OnMainGo(object sender, RoutedEventArgs e)
    {
        if (readCancellation is not null) return;
        OnWorkflows(sender, e);
        RepairChoice.SelectedIndex = MainWorkflow.SelectedIndex;
        var kind = (KillerBench.Engine.WorkflowKind)MainWorkflow.SelectedIndex;
        if (kind == KillerBench.Engine.WorkflowKind.Defender && DefenderScanChoice.SelectedIndex > 0)
        {
            SetDetailsOpen(false);
            await RunDefenderScanAsync(DefenderScanConsent.IsChecked == true);
            return;
        }
        bool focusedOperation = kind == KillerBench.Engine.WorkflowKind.Storage &&
            (StorageRepairReview.IsChecked == true || StorageOptimizationReview.IsChecked == true) ||
            kind == KillerBench.Engine.WorkflowKind.SystemRestore && SystemRestoreCreate.IsChecked == true ||
            kind == KillerBench.Engine.WorkflowKind.Orca && IsOrcaCacheRetentionSelected();
        RepairConsent.IsChecked = (KillerBench.Engine.Workflows.SupportsRepair(kind) || focusedOperation) && MainAutoRepair.IsChecked == true;
        SetDetailsOpen(false);
        await RunWorkflowAsync();
    }

    private void SetScanLight(string state, string detail)
    {
        ScanLight.BeginAnimation(OpacityProperty, null);
        ScanLight.Opacity = 1;
        string color = state switch { "running" => "#E0A317", "attention" => "#D0453A", "incomplete" => "#E0A317", _ => "#3FA95F" };
        ScanLight.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        ScanLight.ToolTip = detail;
        System.Windows.Automation.AutomationProperties.SetName(ScanLight, detail);
        if (state == "running" && SystemParameters.ClientAreaAnimation)
            ScanLight.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.35, TimeSpan.FromMilliseconds(650)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    }
}
