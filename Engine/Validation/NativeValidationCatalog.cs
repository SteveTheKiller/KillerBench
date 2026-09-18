namespace KillerBench.Engine;

public enum NativeValidationMode
{
    DiagnosticOnly,
    AutomaticRepair,
    ExplicitOptions
}

public enum NativeValidationScenarioKind
{
    Healthy,
    IncompleteEvidence,
    ProviderFailure,
    Cancellation,
    Ineligible,
    Preview,
    InvalidOptions,
    ChangedOptions,
    ManagedAuthority,
    VerifiedExecution,
    VerificationFailure,
    InterruptionAfterDispatch,
    Reconciliation,
    RestartRequired
}

public sealed record NativeValidationScenario(
    string Id,
    NativeValidationScenarioKind Kind,
    string Title,
    string ExpectedOutcome,
    bool AllowsNativeMutation,
    IReadOnlyList<string> RequiredArtifacts);

public sealed record NativeRecipeValidationPlan(
    string RecipeId,
    int RecipeVersion,
    string Title,
    string Area,
    RepairKind Action,
    NativeValidationMode Mode,
    RepairRecoveryContract RecoveryContract,
    IReadOnlyList<NativeValidationScenario> Scenarios);

public static class NativeValidationCatalog
{
    private static readonly IReadOnlySet<RepairKind> ManagedAuthorityActions = new HashSet<RepairKind>
    {
        RepairKind.TimeSourceConfiguration,
        RepairKind.FirewallProfiles,
        RepairKind.UpdateDataStoreReset,
        RepairKind.DeliveryOptimization,
        RepairKind.PowerTimers,
        RepairKind.HibernationDisable
    };

    private static readonly IReadOnlySet<RepairKind> RestartActions = new HashSet<RepairKind>
    {
        RepairKind.ComputerRename,
        RepairKind.ComponentStore,
        RepairKind.SystemFiles,
        RepairKind.ComponentCleanup,
        RepairKind.StorageFileSystem
    };

    public static IReadOnlyList<NativeRecipeValidationPlan> All { get; } =
        Array.AsReadOnly(RecipeCatalog.All.Select(Create).ToArray());

    public static NativeRecipeValidationPlan? Find(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId)) return null;
        return All.FirstOrDefault(plan => string.Equals(plan.RecipeId, recipeId, StringComparison.Ordinal));
    }

    public static IReadOnlyList<NativeRecipeValidationPlan> ForWorkflow(WorkflowKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        var byId = All.ToDictionary(plan => plan.RecipeId, StringComparer.Ordinal);
        return Array.AsReadOnly(RecipeCatalog.ForWorkflow(kind).Select(recipe => byId[recipe.Id]).ToArray());
    }

    private static NativeRecipeValidationPlan Create(RepairRecipe recipe)
    {
        var scenarios = new List<NativeValidationScenario>
        {
            Scenario(recipe, NativeValidationScenarioKind.Healthy, "Healthy baseline",
                "Complete healthy evidence stays Clear and no repair is offered or executed.", false,
                "machine snapshot", "provider evidence", "workflow journal"),
            Scenario(recipe, NativeValidationScenarioKind.IncompleteEvidence, "Incomplete evidence",
                "Missing, truncated, malformed or contradictory evidence stays Unknown and cannot authorize a repair.", false,
                "provider evidence", "workflow journal"),
            Scenario(recipe, NativeValidationScenarioKind.ProviderFailure, "Provider failure",
                "Access denial, timeout and provider failure remain explicit and do not become a healthy or repairable result.", false,
                "provider exit state", "workflow journal"),
            Scenario(recipe, NativeValidationScenarioKind.Cancellation, "Cancellation before dispatch",
                "Cancellation stops collection or preparation without dispatching a native mutation.", false,
                "progress log", "workflow journal")
        };

        if (!recipe.DiagnosticOnly)
        {
            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.Ineligible, "Rejected prerequisite",
                "Unsupported, stale or changed eligibility evidence sends no native mutation.", false,
                "before snapshot", "final eligibility snapshot", "workflow journal"));
            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.Preview, "Eligible preview",
                "Eligible evidence produces the exact proposed target, effects and verification scope without changing Windows.", false,
                "before snapshot", "preview output", "workflow journal"));

            if (recipe.RequiresExplicitOptions)
            {
                scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.InvalidOptions, "Invalid or missing options",
                    "Missing, malformed, unsupported or conflicting choices are rejected before dispatch.", false,
                    "submitted options", "validation result", "workflow journal"));
                scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.ChangedOptions, "Options changed after preparation",
                    "A target or option change after preparation invalidates the run and sends no native mutation.", false,
                    "prepared options", "final options", "workflow journal"));
            }

            if (ManagedAuthorityActions.Contains(recipe.Action))
                scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.ManagedAuthority, "Managed authority",
                    "Domain, device-management or policy ownership is preserved and an uncertain authority blocks local mutation.", false,
                    "authority evidence", "effective settings", "workflow journal"));

            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.VerifiedExecution, "Verified native execution",
                "One eligible action runs within its bound and fresh evidence proves only the documented verification scope.", true,
                "before snapshot", "prepared record", "native result", "after snapshot", "workflow journal"));
            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.VerificationFailure, "Unverified native outcome",
                "A dispatched action that cannot be verified reports an uncertain or failed outcome without claiming success.", true,
                "prepared record", "native result", "verification evidence", "workflow journal"));
            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.InterruptionAfterDispatch, "Interruption after dispatch",
                "An interruption after possible dispatch retains a durable pending record and never silently retries.", true,
                "prepared record", "dispatch evidence", "pending journal"));
            scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.Reconciliation, "Read-only reconciliation",
                "A later run inspects fresh state, resolves or retains the pending outcome, and does not repeat the mutation.", false,
                "pending journal", "fresh machine snapshot", "reconciliation result"));

            if (RestartActions.Contains(recipe.Action))
                scenarios.Add(Scenario(recipe, NativeValidationScenarioKind.RestartRequired, "Restart-required outcome",
                    "The result identifies the required restart and defers the final verdict until later fresh evidence is available.", true,
                    "native result", "restart evidence", "pending or completed journal"));
        }

        return new NativeRecipeValidationPlan(recipe.Id, recipe.Version, recipe.Title, recipe.Area, recipe.Action,
            recipe.DiagnosticOnly ? NativeValidationMode.DiagnosticOnly :
                recipe.RequiresExplicitOptions ? NativeValidationMode.ExplicitOptions : NativeValidationMode.AutomaticRepair,
            recipe.RecoveryContract, Array.AsReadOnly(scenarios.ToArray()));
    }

    private static NativeValidationScenario Scenario(RepairRecipe recipe, NativeValidationScenarioKind kind,
        string title, string expectedOutcome, bool allowsNativeMutation, params string[] requiredArtifacts) =>
        new(recipe.Id + "." + ScenarioSuffix(kind), kind, title, expectedOutcome, allowsNativeMutation,
            Array.AsReadOnly(requiredArtifacts));

    private static string ScenarioSuffix(NativeValidationScenarioKind kind) => kind switch
    {
        NativeValidationScenarioKind.Healthy => "healthy",
        NativeValidationScenarioKind.IncompleteEvidence => "incomplete-evidence",
        NativeValidationScenarioKind.ProviderFailure => "provider-failure",
        NativeValidationScenarioKind.Cancellation => "cancellation-before-dispatch",
        NativeValidationScenarioKind.Ineligible => "rejected-prerequisite",
        NativeValidationScenarioKind.Preview => "eligible-preview",
        NativeValidationScenarioKind.InvalidOptions => "invalid-options",
        NativeValidationScenarioKind.ChangedOptions => "changed-options",
        NativeValidationScenarioKind.ManagedAuthority => "managed-authority",
        NativeValidationScenarioKind.VerifiedExecution => "verified-execution",
        NativeValidationScenarioKind.VerificationFailure => "verification-failure",
        NativeValidationScenarioKind.InterruptionAfterDispatch => "interruption-after-dispatch",
        NativeValidationScenarioKind.Reconciliation => "reconciliation",
        NativeValidationScenarioKind.RestartRequired => "restart-required",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
