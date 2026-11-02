using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using KillerBench.Engine;

namespace KillerBench.Shell;

public partial class MainWindow
{
    private void OnDiagnosticAreasChanged(object sender, RoutedEventArgs e)
    {
        if (DiagnosticAreaList is null || DiagnosticAreaSummary is null || FocusDiagnosticAreas is null || MainAutoRepair is null) return;
        bool focused = FocusDiagnosticAreas.IsChecked == true;
        DiagnosticAreaSummary.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticAreaList.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        ShowDetailedDiagnosticAreas?.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
        if (focused && DiagnosticAreaList.ItemsSource is null)
            SetDiagnosticAreaChoices(false);
        if (focused && ReferenceEquals(sender, FocusDiagnosticAreas) && DiagnosticAreaList.SelectedItems.Count == 0)
            DiagnosticAreaList.SelectAll();
        if (focused) CustomizeRecipes?.IsChecked = false;
        MainAutoRepair.IsChecked = false;
        DiagnosticAreaSummary.Text = focused ? DiagnosticAreaList.SelectedItems.Count + " diagnostic areas selected. Other areas and repair recipes will not run." : "Full system scan remains selected.";
        RecipeSelectionList?.Visibility = focused ? Visibility.Collapsed : Visibility.Visible;
        CustomizeRecipes?.IsEnabled = !focused;
        OnWorkflowOptionsChanged(this, null);
    }

    private void OnDiagnosticDetailChanged(object sender, RoutedEventArgs e)
    {
        if (DiagnosticAreaList is null || ShowDetailedDiagnosticAreas is null) return;
        SetDiagnosticAreaChoices(ShowDetailedDiagnosticAreas.IsChecked == true);
        OnDiagnosticAreasChanged(sender, e);
    }

    private void SetDiagnosticAreaChoices(bool detailed)
    {
        DiagnosticAreaList.ItemsSource = detailed ? CommonDiagnosticCatalog.All : CommonDiagnosticCatalog.SummaryAreas;
        DiagnosticAreaList.SelectAll();
    }
    private void RefreshRecipeSelection()
    {
        if (RecipeSelectionPanel is null) return;
        bool common = MainWorkflow.SelectedItem is WorkflowDefinition { Kind: WorkflowKind.CommonProblems };
        RecipeSelectionPanel.Visibility = common ? Visibility.Visible : Visibility.Collapsed;
        if (RecipeSelectionList.ItemsSource is null)
        {
            var recipes = RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).ToList();
            var view = new ListCollectionView(recipes);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RepairRecipe.Area)));
            RecipeSelectionList.ItemsSource = view;
            RecipeSelectionList.SelectAll();
        }
        OnRecipeSelectionChanged(this, new RoutedEventArgs());
    }

    private void OnRecipeSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (RecipeSelectionSummary is null || RecipeSelectionList is null || CustomizeRecipes is null) return;
        bool showRecipes = CustomizeRecipes.IsChecked == true && FocusDiagnosticAreas?.IsChecked != true;
        RecipeSelectionList.Visibility = showRecipes ? Visibility.Visible : Visibility.Collapsed;
        RecipeActions.Visibility = RecipeSelectionList.Visibility;
        if (FocusDiagnosticAreas?.IsChecked == true)
        {
            RecipeSelectionSummary.Text = "Recipe checks are excluded from this focused diagnostic run.";
            RecipeEffectsSummary?.Text = "No repairs selected.";
            return;
        }
        MainAutoRepair?.IsChecked = false;
        var ids = RecipeSelectionList.SelectedItems.Cast<RepairRecipe>().Select(recipe => recipe.Id).ToArray();
        var plan = RecipeCatalog.SelectForWorkflow(WorkflowKind.CommonProblems, ids);
        RecipeSelectionSummary.Text = CustomizeRecipes.IsChecked != true
            ? $"All {CommonDiagnosticCatalog.All.Count} diagnostic areas and all {RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems).Count} recipe checks are selected. Full system scan is diagnostic-only." :
            ids.Length == 0 ? "No recipe checks selected. General diagnostic snapshots still run." :
            $"{ids.Length} selected; {plan.Count} checks including prerequisites. General diagnostic snapshots still run.";
        if (RecipeEffectsSummary is not null)
        {
            var reviewed = CustomizeRecipes.IsChecked == true ? plan : RecipeCatalog.ForWorkflow(WorkflowKind.CommonProblems);
            RecipeEffectsSummary.Text = reviewed.Count == 0 ? "No recipe checks selected." :
                "These checks do not run repairs. Use a focused repair workflow after reviewing current findings. Included prerequisites are listed too.\n\n" +
                string.Join("\n\n", reviewed.Select(recipe => recipe.Title + "\n" + recipe.Effects + "\nVerifies: " + recipe.VerificationScope +
                    "\n" + NativeCapabilityLabel(recipe.Id)));
        }
    }

    private void OnSelectAllRecipes(object sender, RoutedEventArgs e)
    {
        RecipeSelectionList?.SelectAll();
        OnRecipeSelectionChanged(sender, e);
    }

    private void OnClearRecipes(object sender, RoutedEventArgs e)
    {
        RecipeSelectionList?.UnselectAll();
        OnRecipeSelectionChanged(sender, e);
    }
}
