using KillerBench.Engine;

internal static class ProfilePlanningChecks
{
    internal static void Run()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (string sid in new[] { "", "fixture", "S-1-5", "S-2-5-18", "S-1-5-4294967296", "S-1-5-018", "S-1-5-18\n" })
        {
            try { ProfilePlanning.ValidateSelection([sid]); throw new Exception("Malformed profile SID accepted"); }
            catch (ArgumentException) { }
        }
        ProfilePlanning.ValidateSelection(["S-1-5-21-1-2-3-1001"]);
        foreach (string invalid in new[] { "{\"items\":null}", "{\"items\":[],\"items\":[]}",
            "{\"items\":[{\"kind\":\"Task\",\"name\":\"Fixture\",\"sid\":null,\"sid\":\"S-1-5-18\"}]}" })
            if (ProfileDependencies.Interpret(new(0, invalid, "", false)).Complete) throw new Exception("Ambiguous dependency accepted");
        var dependency = ProfileDependencies.Interpret(new(0, "{\"items\":[{\"kind\":\"Service\",\"name\":\"Fixture\",\"sid\":\"S-1-5-21-1-2-3-1001\"}]}", "", false));
        if (!dependency.Complete || dependency.ForSid("S-1-5-21-1-2-3-1001").Count != 1 || dependency.ForSid("S-1-5-21-1-2-3-100").Count != 0) throw new Exception("Dependency identity match is not exact");
        if (ProfileDependencies.Interpret(new(0, "{\"items\":[{\"kind\":\"Task\",\"name\":\"Fixture\",\"sid\":null}]}", "", false)).Complete) throw new Exception("Unknown dependency treated as complete");
        string item = "{\"sid\":\"S-1-5-21-1-2-3-1001\",\"path\":\"C:\\\\Users\\\\Fixture\",\"loaded\":false,\"special\":false,\"last\":null,\"current\":false}";
        var evidence = new ToolResult(0, "{\"profiles\":[" + item + "]}", "", false);
        foreach (string badSid in new[] { "fixture", "S-1-5-018", "S-1-5-4294967296" })
        {
            if (ProfileInventory.Interpret(evidence with { Output = evidence.Output.Replace("S-1-5-21-1-2-3-1001", badSid) }).Complete ||
                ProfileDependencies.Interpret(new(0, System.Text.Json.JsonSerializer.Serialize(new { items = new[] { new { kind = "Task", name = "Fixture", sid = badSid } } }), "", false)).Complete)
                throw new Exception("Malformed collected identity accepted as complete");
        }
        if (!ProfileInventory.Interpret(evidence).Complete || ProfileInventory.Interpret(evidence).Profiles.Count != 1) throw new Exception("Profile inventory parsing failed");
        foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { Truncated = true }, evidence with { TimedOut = true },
            evidence with { Output = "{}" }, evidence with { Output = "{\"profiles\":[" + item + "," + item + "]}" } })
            if (ProfileInventory.Interpret(invalid).Complete) throw new Exception("Incomplete profile evidence accepted");
        var old = new ProfileSnapshot("S-1-5-21-1-2-3-1001", @"C:\Users\Fixture", false, false, now.AddDays(-100), false);
        foreach (string path in new[] { @"C:\", @"C:\Users\..\Windows", @"\\server\profiles\user", @"C:\Users\Fixture:stream",
            @"C:\Users\Fixture.", @"C:\Users\Fixture ", @"C:\Users\\Fixture", "relative", @"C:\Users/Fixture" })
            if (ProfilePlanning.Review([old with { Path = path }], 90, now, true).Single().ReviewEligible)
                throw new Exception("Ambiguous profile path passed review: " + path);
        foreach (string ambiguous in new[] {
            evidence.Output.Replace("\"loaded\":false", "\"loaded\":true,\"loaded\":false"),
            evidence.Output.Replace("\"special\":false", "\"special\":true,\"special\":false"),
            evidence.Output.Replace("\"current\":false", "\"current\":true,\"current\":false"),
            "{\"profiles\":[],\"profiles\":[" + item + "]}" })
            if (ProfileInventory.Interpret(evidence with { Output = ambiguous }).Complete) throw new Exception("Ambiguous profile fields accepted");
        if (!ProfilePlanning.Review([old], 90, now, true).Single().ReviewEligible) throw new Exception("Old profile review unavailable");
        var registration = Registration(old);
        if (!ProfilePlanning.Select(new([old], true), new([], true), registration, [old.Sid], 90, now).ReviewPassed) throw new Exception("Selected review failed");
        if (ProfilePlanning.Select(new([old], true), dependency, registration, [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([old], true), new([], false), registration, [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([], true), new([], true), registration, [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([old], true), new([], true), new([], false), [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([old], true), new([], true), Registration(old, state: 1), [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([old], true), new([], true), Registration(old, refCount: 1), [old.Sid], 90, now).ReviewPassed ||
            ProfilePlanning.Select(new([old], true), new([], true), Registration(old, backup: true), [old.Sid], 90, now).ReviewPassed)
            throw new Exception("Blocked selection passed");
        foreach (var blocked in new[] { old with { Loaded = true }, old with { Loaded = null }, old with { Special = true },
            old with { Special = null }, old with { CurrentAccount = true }, old with { LastUsedUtc = null },
            old with { LastUsedUtc = now.AddDays(1) }, old with { LastUsedUtc = now.AddDays(-20) }, old with { Path = "" } })
            if (ProfilePlanning.Review([blocked], 90, now, true).Single().ReviewEligible) throw new Exception("Protected profile passed review");
        if (ProfilePlanning.Review([old, old], 90, now, true).Any(p => p.ReviewEligible) ||
            ProfilePlanning.Review([old], 90, now, false).Any(p => p.ReviewEligible)) throw new Exception("Ambiguous inventory passed review");
        try { ProfilePlanning.Review([old], 90, now, true, new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: profile review age, loaded/system/current account protection and incomplete inventory (fixtures only).");
        CheckRefreshAsync(old).GetAwaiter().GetResult();
    }

    private static async Task CheckRefreshAsync(ProfileSnapshot profile)
    {
        foreach (bool cancelInventory in new[] { false, true })
        {
            int dependencyCount = 0;
            Task<ProfileInventoryResult> CanceledInventory(CancellationToken token) => cancelInventory
                ? Task.FromCanceled<ProfileInventoryResult>(new CancellationToken(true))
                : Task.FromResult(new ProfileInventoryResult([profile], true));
            Task<ProfileDependencyResult> CanceledDependencies(CancellationToken token)
            {
                dependencyCount++;
                return Task.FromCanceled<ProfileDependencyResult>(new CancellationToken(true));
            }
            try
            {
                await ProfileReview.RefreshAsync([profile.Sid], 90, inventoryProvider: CanceledInventory, dependencyProvider: CanceledDependencies);
                throw new Exception("Canceled provider produced a review");
            }
            catch (OperationCanceledException) { }
            if (dependencyCount != (cancelInventory ? 0 : 1)) throw new Exception("Provider cancellation continued collection");
        }
        foreach (bool missingInventory in new[] { false, true })
        foreach (bool missingOperation in new[] { false, true })
        {
            int dispatched = 0;
            Task<ProfileInventoryResult> Inventory(CancellationToken token) => missingInventory
                ? missingOperation ? null! : Task.FromResult<ProfileInventoryResult>(null!)
                : Task.FromResult(new ProfileInventoryResult([profile], true));
            Task<ProfileDependencyResult> MissingDependencies(CancellationToken token)
            {
                dispatched++;
                return missingOperation ? null! : Task.FromResult<ProfileDependencyResult>(null!);
            }
            try
            {
                await ProfileReview.RefreshAsync([profile.Sid], 90, inventoryProvider: Inventory, dependencyProvider: MissingDependencies);
                throw new Exception("Missing provider evidence passed review");
            }
            catch (InvalidOperationException) { }
            if (dispatched != (missingInventory ? 0 : 1)) throw new Exception("Missing inventory dispatched dependency collection");
        }
        foreach (bool stallDependencies in new[] { false, true })
        {
            using var canceled = new CancellationTokenSource();
            int calls = 0;
            Task<ProfileInventoryResult> Inventory(CancellationToken ignored)
            {
                if (stallDependencies) return Task.FromResult(new ProfileInventoryResult([profile], true));
                canceled.Cancel();
                return new TaskCompletionSource<ProfileInventoryResult>().Task;
            }
            Task<ProfileDependencyResult> StalledDependencies(CancellationToken ignored)
            {
                calls++;
                canceled.Cancel();
                return new TaskCompletionSource<ProfileDependencyResult>().Task;
            }
            try
            {
                await ProfileReview.RefreshAsync([profile.Sid], 90, Inventory, StalledDependencies, token: canceled.Token).WaitAsync(TimeSpan.FromSeconds(3));
                throw new Exception("Stalled provider ignored Stop");
            }
            catch (OperationCanceledException) { }
            if (calls != (stallDependencies ? 1 : 0)) throw new Exception("Stopped review dispatched a later provider");
        }
        int dependencyCalls = 0;
        Task<ProfileDependencyResult> Dependencies(CancellationToken token) { dependencyCalls++; return Task.FromResult(new ProfileDependencyResult([], true)); }
        Task<ProfileRegistrationResult> Registrations(CancellationToken token) => Task.FromResult(Registration(profile));
        var changed = await ProfileReview.RefreshAsync([profile.Sid], 90, inventoryProvider: _ => Task.FromResult(new ProfileInventoryResult([profile with { Loaded = true }], true)), dependencyProvider: Dependencies, registrationProvider: Registrations);
        if (changed.ReviewPassed) throw new Exception("Loaded profile passed refreshed plan");
        var selected = new List<string> { profile.Sid };
        var addedDependency = await ProfileReview.RefreshAsync(selected, 90,
            inventoryProvider: _ => { selected[0] = "S-1-5-21-1-2-3-9999"; return Task.FromResult(new ProfileInventoryResult([profile], true)); },
            dependencyProvider: _ => Task.FromResult(new ProfileDependencyResult([new("Task", "New dependency", profile.Sid)], true)),
            registrationProvider: Registrations);
        if (addedDependency.ReviewPassed || addedDependency.Targets.Single().Sid != profile.Sid || !addedDependency.Blockers.Any(b => b.Contains("New dependency")))
            throw new Exception("Refresh lost target identity or ignored a new dependency");
        using var stop = new CancellationTokenSource();
        int before = dependencyCalls;
        try
        {
            await ProfileReview.RefreshAsync([profile.Sid], 90, _ => { stop.Cancel(); return Task.FromResult(new ProfileInventoryResult([profile], true)); }, Dependencies, token: stop.Token);
            throw new Exception("Refresh cancellation ignored");
        }
        catch (OperationCanceledException) { }
        if (dependencyCalls != before) throw new Exception("Canceled refresh continued collection");
        string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KillerBench-profile-plan-" + Guid.NewGuid().ToString("N"));
        string first = await ProfileReview.SavePlanAsync(addedDependency, folder, 90);
        string second = await ProfileReview.SavePlanAsync(addedDependency, folder, 90);
        if (first == second || !System.IO.File.ReadAllText(first).Contains("New dependency") || !System.IO.File.ReadAllText(second).Contains("Review only"))
            throw new Exception("Profile plan record lost evidence or overwrote a review");
    }

    private static ProfileRegistrationResult Registration(ProfileSnapshot profile, uint? state = 0, uint? refCount = 0, bool backup = false)
    {
        var items = new List<ProfileRegistration>
        {
            new(profile.Sid, profile.Path, profile.Sid, false, state, refCount)
        };
        if (backup) items.Add(new(profile.Sid, profile.Path, profile.Sid + ".bak", true, 0, 0));
        return new(items.AsReadOnly(), true);
    }
}
