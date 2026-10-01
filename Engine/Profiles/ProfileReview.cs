namespace KillerBench.Engine.Profiles;

public static class ProfileReview
{
    public static async Task<string> SavePlanAsync(ProfileSelectionPlan plan, string folder, int minimumDays, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (minimumDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(minimumDays));
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "profile-plan-" + Guid.NewGuid().ToString("N") + ".json");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, new { SchemaVersion = 1, CapturedUtc = DateTimeOffset.UtcNow,
            Machine = Environment.MachineName, MinimumDays = minimumDays, Plan = plan, Operation = "Review only" }, Capture.JsonOptions, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
        return path;
    }

    public static async Task<ProfileSelectionPlan> RefreshAsync(IReadOnlyList<string> selectedSids, int minimumDays,
        CancellationToken token = default,
        Func<CancellationToken, Task<ProfileInventoryResult>>? inventoryProvider = null,
        Func<CancellationToken, Task<ProfileDependencyResult>>? dependencyProvider = null,
        Func<CancellationToken, Task<ProfileRegistrationResult>>? registrationProvider = null)
    {
        ProfilePlanning.ValidateSelection(selectedSids);
        if (minimumDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(minimumDays));
        var selection = selectedSids.ToArray();
        token.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        try
        {
            var inventoryTask = (inventoryProvider ?? ProfileInventory.ReadAsync)(deadline.Token)
                ?? throw new InvalidOperationException("Profile inventory provider did not return an operation.");
            var inventory = await inventoryTask.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Profile inventory evidence is missing. Review cannot proceed.");
            deadline.Token.ThrowIfCancellationRequested();
            var dependencyTask = (dependencyProvider ?? ProfileDependencies.ReadAsync)(deadline.Token)
                ?? throw new InvalidOperationException("Profile dependency provider did not return an operation.");
            var dependencies = await dependencyTask.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Profile dependency evidence is missing. Review cannot proceed.");
            deadline.Token.ThrowIfCancellationRequested();
            var registrationTask = (registrationProvider ?? ProfileRegistrations.ReadAsync)(deadline.Token)
                ?? throw new InvalidOperationException("Profile registration provider did not return an operation.");
            var registrations = await registrationTask.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Profile registration evidence is missing. Review cannot proceed.");
            deadline.Token.ThrowIfCancellationRequested();
            return ProfilePlanning.Select(inventory, dependencies, registrations, selection, minimumDays, DateTimeOffset.UtcNow, deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        { throw new TimeoutException("Profile review exceeded its 90-second collection deadline. No removal was authorized."); }
    }
}
