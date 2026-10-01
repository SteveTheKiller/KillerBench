using KillerBench.Engine;

internal static class UpdateDiscoveryChecks
{
    internal static async Task RunAsync()
    {
        var options = new UpdateSearchOptions(false, true);
        string configuredQuery = UpdateDiscovery.QueryStep(new(Source: UpdateSearchSource.Configured)).Arguments[^1];
        string windowsQuery = UpdateDiscovery.QueryStep(new(Source: UpdateSearchSource.WindowsUpdate)).Arguments[^1];
        string microsoftQuery = UpdateDiscovery.QueryStep(new(Source: UpdateSearchSource.MicrosoftUpdate)).Arguments[^1];
        Require(configuredQuery.Contains("ServerSelection=0") && windowsQuery.Contains("ServerSelection=2") &&
            microsoftQuery.Contains("ServerSelection=3") && microsoftQuery.Contains(UpdateSearchOptions.MicrosoftUpdateServiceId) &&
            configuredQuery.Contains("failureHresult") &&
            microsoftQuery.IndexOf("manager.Services", StringComparison.Ordinal) < microsoftQuery.IndexOf("ServerSelection=3", StringComparison.Ordinal),
            "Update source choices map to explicit Windows Update Agent search modes and retain current search HRESULTs");
        try { new UpdateSearchOptions(Source: (UpdateSearchSource)99).Criteria(); throw new Exception("Unknown update source accepted"); }
        catch (ArgumentException) { }
        const string payload = "{\"code\":2,\"total\":1,\"updates\":[{\"id\":\"d32c42e1-2f09-4024-8ccd-654b70c2807d\",\"revision\":2,\"title\":\"Fixture driver\",\"type\":2,\"downloaded\":false,\"license\":false}]}";
        var result = new ToolResult(0, payload, "", false);
        int dataStoreCode = unchecked((int)0x80248003);
        var currentFailureEvidence = result with { Output = "{\"failureHresult\":" + dataStoreCode + "}" };
        var currentFailure = UpdateDiscovery.Interpret(currentFailureEvidence, options);
        Require(currentFailure is { Partial: true, Updates.Count: 0, FailureHResult: not null } &&
            currentFailure.FailureHResult == dataStoreCode && currentFailure.Evidence == currentFailureEvidence &&
            currentFailure.Detail.Contains("0x80248003") && currentFailure.Detail.Contains("WU_E_DS_TABLEMISSING") &&
            currentFailure.Detail.Contains("No update was installed and no repair was selected"),
            "A current recognized search failure retains its exact HRESULT and reviewed reference without selecting a repair");
        int unknownCode = unchecked((int)0x81234567);
        var unknownFailure = UpdateDiscovery.Interpret(result with { Output = "{\"failureHresult\":" + unknownCode + "}" }, options);
        Require(unknownFailure.FailureHResult == unknownCode && unknownFailure.Detail.Contains("0x81234567") &&
            unknownFailure.Detail.Contains("unrecognized HRESULT") && !unknownFailure.Detail.Contains("WU_E_"),
            "A current unknown search failure remains visible without an invented reference");
        foreach (string malformedFailure in new[] { "{\"failureHresult\":0}", "{\"failureHresult\":null}",
            "{\"failureHresult\":" + dataStoreCode + ",\"extra\":true}", "{\"failureHresult\":" + dataStoreCode + ",\"failureHresult\":1}" })
        {
            var malformed = UpdateDiscovery.Interpret(result with { Output = malformedFailure }, options);
            Require(malformed.Partial && malformed.FailureHResult is null && malformed.Evidence?.Output == malformedFailure,
                "Malformed or ambiguous current failure evidence cannot establish a search HRESULT");
        }
        var failureRoundTrip = System.Text.Json.JsonSerializer.Deserialize<UpdateSearchResult>(
            System.Text.Json.JsonSerializer.Serialize(currentFailure, Capture.JsonOptions), Capture.JsonOptions);
        Require(failureRoundTrip?.FailureHResult == dataStoreCode && failureRoundTrip.Evidence == currentFailureEvidence,
            "Current search failure JSON retains HRESULT and command evidence");
        const string productId = "498cdd9d-5522-42d3-9802-f0fc909c58ed";
        string productRow = "{\"id\":\"" + productId + "\",\"name\":\"Fixture product\"}";
        var productEvidence = result with { Output = payload.Replace("\"license\":false", "\"license\":false,\"products\":[" + productRow + "]") };
        var productDiscovery = UpdateDiscovery.Interpret(productEvidence, options);
        Require(productDiscovery.GetProducts().Single().Name == "Fixture product" && productDiscovery.Updates.Single().Behavior is null,
            "Product metadata remains available independently of installation behavior");
        Require(productDiscovery.Filter("driver", productId: Guid.Parse(productId)).Count == 1 &&
            productDiscovery.Filter("driver", downloadedOnly: true, productId: Guid.Parse(productId)).Count == 0 &&
            productDiscovery.Filter("driver", classificationId: Guid.NewGuid(), productId: Guid.Parse(productId)).Count == 0 &&
            productDiscovery.Evidence == productEvidence, "Product filter combines with existing filters without changing discovery evidence");
        foreach (string products in new[] { "null", "[]", "[{}]", "[" + productRow + "," + productRow.Replace(productId, "{" + productId + "}") + "]" })
        {
            var missingProduct = UpdateDiscovery.Interpret(result with { Output = payload.Replace("\"license\":false", "\"license\":false,\"products\":" + products) }, options);
            Require(missingProduct.Updates.Count == 1 && missingProduct.GetProducts().Count == 0 && missingProduct.Filter("", productId: Guid.Parse(productId)).Count == 0,
                "Unavailable or ambiguous products never match a product filter or discard the update identity");
        }
        bool emptyProductRejected = false;
        try { productDiscovery.Filter("", productId: Guid.Empty); } catch (ArgumentException) { emptyProductRejected = true; }
        Require(emptyProductRejected, "Empty product filter is rejected");
        string originalRow = System.Text.Json.JsonDocument.Parse(payload).RootElement.GetProperty("updates")[0].GetRawText();
        foreach (string alternateId in new[] { "{d32c42e1-2f09-4024-8ccd-654b70c2807d}", "D32C42E12F0940248CCD654B70C2807D" })
        {
            var duplicateEvidence = result with { Output = "{\"code\":2,\"total\":2,\"updates\":[" + originalRow + "," +
                originalRow.Replace("d32c42e1-2f09-4024-8ccd-654b70c2807d", alternateId) + "]}" };
            var duplicate = UpdateDiscovery.Interpret(duplicateEvidence, options);
            Require(duplicate.Partial && duplicate.Updates.Count == 0 && duplicate.Evidence == duplicateEvidence,
                "Equivalent GUID spellings cannot establish separate update identities");
        }
        Require(UpdateDiscovery.Interpret(result with { Output = payload.Replace("d32c42e1-2f09-4024-8ccd-654b70c2807d", Guid.Empty.ToString()) }, options).Partial,
            "Empty update identity cannot enter installation discovery");
        var queried = await UpdateDiscovery.SearchAsync(options, new ThrowingProgress(), query: (_, _) => Task.FromResult(result));
        Require(queried.Evidence == result && !queried.Partial, "Display callback failure cannot discard update search evidence");
        foreach (bool missingTask in new[] { false, true })
        {
            bool rejected = false;
            try { await UpdateDiscovery.SearchAsync(options, query: (_, _) => missingTask ? null! : Task.FromResult<ToolResult>(null!)); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Missing update query task or result produces an explicit provider failure");
        }
        using (var completedStop = new CancellationTokenSource())
        {
            bool stopped = false;
            try { await UpdateDiscovery.SearchAsync(options, token: completedStop.Token, query: (_, _) =>
                { completedStop.Cancel(); return Task.FromResult(result); }); }
            catch (OperationCanceledException) { stopped = true; }
            Require(stopped, "Cancellation before publication cannot return a completed search result");
        }
        using (var searchStop = new CancellationTokenSource())
        {
            var lateQuery = new TaskCompletionSource<ToolResult>();
            try
            {
                await UpdateDiscovery.SearchAsync(options, token: searchStop.Token, query: (_, _) =>
                    { searchStop.Cancel(); return lateQuery.Task; }).WaitAsync(TimeSpan.FromSeconds(2));
                throw new Exception("Canceled update query continued");
            }
            catch (OperationCanceledException) { }
            lateQuery.SetResult(result);
        }
        var firstUpdate = new AvailableUpdate("d32c42e1-2f09-4024-8ccd-654b70c2807d", 2, "Fixture driver", "Driver", true, false);
        var secondUpdate = firstUpdate with { Id = "74ba239e-08d2-45bd-b285-b0d35610ca60", Title = "Other update", Downloaded = false };
        var filterSource = new UpdateSearchResult(new(true, true), [firstUpdate, secondUpdate], true, "fixture") { Evidence = result };
        Require(filterSource.Filter(" DRIVER ").Single() == firstUpdate && filterSource.Filter(null, true).Single() == firstUpdate &&
            filterSource.Filter("missing").Count == 0 && filterSource.Filter("").Count == 2 && filterSource.Partial && filterSource.Evidence == result,
            "Update filters combine title/download state without changing source coverage or evidence");
        Require(UpdateDiscovery.Interpret(result, options).Evidence == result, "Complete discovery retains original command evidence");
        var categoryId = Guid.Parse("f40d9756-7bab-4c31-a2b4-9b087e835dbe");
        var classified = firstUpdate with { Behavior = new(false, 0, 0, false, [new(categoryId.ToString().ToUpperInvariant(), "Fixture category")]) };
        var categorySource = filterSource with { Updates = [classified, secondUpdate] };
        Require(categorySource.Filter("driver", true, categoryId).Single() == classified &&
            categorySource.Filter("other", false, categoryId).Count == 0 && categorySource.Partial && categorySource.Evidence == result,
            "Classification filtering combines existing filters and preserves incomplete discovery evidence");
        try { categorySource.Filter(null, classificationId: Guid.Empty); throw new Exception("Empty classification accepted"); }
        catch (ArgumentException) { }
        var repeatedCategories = categorySource with { Updates = [classified, classified with { Behavior = new(false, 0, 0, false,
            [new(categoryId.ToString("B"), "Fixture category"), new("invalid", "Invalid category"), new(Guid.Empty.ToString(), "Empty category")]) }, secondUpdate] };
        Require(repeatedCategories.GetClassifications().SequenceEqual([new UpdateClassification(categoryId.ToString("D"), "Fixture category")]) &&
            (categorySource with { Updates = [secondUpdate] }).GetClassifications().Count == 0,
            "Classification choices deduplicate GUID formats, exclude invalid identities and preserve missing metadata");
        foreach (var failed in new[] { result with { Error = "Search provider error" }, result with { TimedOut = true },
            result with { Truncated = true }, result with { Output = "invalid JSON" } })
        {
            var incomplete = UpdateDiscovery.Interpret(failed, options);
            Require(incomplete.Partial && incomplete.Evidence == failed, "Incomplete discovery retains evidence and cannot imply completeness");
            bool blocked = false;
            try { UpdatePlanning.Create(incomplete, [new(Guid.Parse("d32c42e1-2f09-4024-8ccd-654b70c2807d"), 2)], new(true)); }
            catch (ArgumentException) { blocked = true; }
            Require(blocked, "Incomplete search evidence blocks installation planning");
            var restored = System.Text.Json.JsonSerializer.Deserialize<UpdateSearchResult>(System.Text.Json.JsonSerializer.Serialize(incomplete, Capture.JsonOptions), Capture.JsonOptions);
            Require(restored?.Evidence == failed && restored.Partial, "Discovery JSON round trip retains error and coverage evidence");
        }
        Require(UpdateDiscovery.Interpret(result, options) is { Partial: false, Updates.Count: 1 }, "Driver identity retained");
        Require(UpdateDiscovery.Interpret(result, options).Updates[0].Behavior is null, "Missing installation metadata does not imply unattended readiness");
        string behavior = "\"behavior\":{\"input\":true,\"impact\":2,\"reboot\":1,\"beta\":false,\"classifications\":[{\"id\":\"0fa1201d-4330-4fa8-8ae9-b877473b6441\",\"name\":\"Fixture classification\"}]},";
        var withBehavior = result with { Output = payload.Replace("\"license\":false", behavior + "\"license\":false") };
        Require(UpdateDiscovery.Interpret(withBehavior, options).Updates[0].Behavior is { CanRequestInput: true, Impact: 2, RebootBehavior: 1 }, "Interaction and restart prerequisites retained");
        const string classificationRow = "{\"id\":\"0fa1201d-4330-4fa8-8ae9-b877473b6441\",\"name\":\"Fixture classification\"}";
        foreach (string categoryJson in new[] {
            classificationRow.Replace("0fa1201d-4330-4fa8-8ae9-b877473b6441", Guid.Empty.ToString()),
            classificationRow + "," + classificationRow.Replace("0fa1201d-4330-4fa8-8ae9-b877473b6441", "{0fa1201d-4330-4fa8-8ae9-b877473b6441}"),
            classificationRow.Replace("Fixture classification", "Fixture\\nclassification") })
        {
            var invalidCategory = UpdateDiscovery.Interpret(withBehavior with { Output = withBehavior.Output.Replace(classificationRow, categoryJson) }, options);
            Require(invalidCategory.Updates.Count == 1 && invalidCategory.Updates[0].Behavior is null,
                "Invalid classification evidence cannot supply unattended installation metadata");
        }
        foreach (string conflicting in new[] { withBehavior.Output.Replace("\"code\":2", "\"code\":4,\"code\":2"),
            withBehavior.Output.Replace("\"license\":false", "\"license\":false,\"license\":true"),
            withBehavior.Output.Replace("\"input\":true", "\"input\":true,\"input\":false"),
            withBehavior.Output.Replace("\"name\":\"Fixture classification\"", "\"name\":\"Other\",\"name\":\"Fixture classification\"") })
        {
            var conflictEvidence = result with { Output = conflicting };
            var ambiguous = UpdateDiscovery.Interpret(conflictEvidence, options);
            Require(ambiguous.Partial && ambiguous.Updates.Count == 0 && ambiguous.Evidence == conflictEvidence,
                "Conflicting update metadata cannot supply installation eligibility and retains raw evidence");
        }
        foreach (string invalid in new[] { withBehavior.Output.Replace("\"reboot\":1", "\"reboot\":9"), withBehavior.Output.Replace("\"input\":true", "\"input\":null") })
            Require(UpdateDiscovery.Interpret(result with { Output = invalid }, options).Updates[0].Behavior is null, "Invalid prerequisite metadata stays unavailable without discarding update identity");
        Require(UpdateDiscovery.Interpret(result, new()).Partial, "Wrong category is not accepted");
        foreach (var invalid in new[] { payload.Replace("\"total\":1", "\"total\":2"), payload.Replace("\"code\":2", "\"code\":4"), payload.Replace("\"type\":2", "\"type\":9"), "{}" })
            Require(UpdateDiscovery.Interpret(result with { Output = invalid }, options).Partial, "Malformed or failed searches cannot report complete results");
        Require(UpdateDiscovery.Interpret(result with { Output = payload.Replace("\"code\":2", "\"code\":3") }, options) is { Partial: true, Updates.Count: 1 }, "Succeeded with errors preserves partial records");
        Require(UpdateDiscovery.Interpret(result with { TimedOut = true }, options).Partial, "Timeout is not no updates");
        try { new UpdateSearchOptions(false, false).Criteria(); throw new Exception("Empty selection accepted"); } catch (ArgumentException) { }
        string fixture = "$script:items=[pscustomobject]@{Count=1}; $script:items | Add-Member ScriptMethod Item { param($i) [pscustomobject]@{Identity=[pscustomobject]@{UpdateID='d32c42e1-2f09-4024-8ccd-654b70c2807d'; RevisionNumber=2}; Title='Fixture driver'; Type=2; IsDownloaded=$false; EulaAccepted=$false; IsBeta=$false; InstallationBehavior=[pscustomobject]@{CanRequestUserInput=$false; Impact=0; RebootBehavior=2}; Categories=@([pscustomobject]@{Type='UpdateClassification'; CategoryID='0fa1201d-4330-4fa8-8ae9-b877473b6441'; Name='Fixture classification'})} }; " +
            "$script:searcher=[pscustomobject]@{ServerSelection=-1; ServiceID=''}; $script:searcher | Add-Member ScriptMethod Search { param($criteria) if($criteria -ne \"IsInstalled=0 and IsHidden=0 and Type='Driver'\"){throw 'WRONG_CRITERIA'}; [pscustomobject]@{ResultCode=2; Updates=$script:items} }; " +
            "$script:session=[pscustomobject]@{ClientApplicationID=''}; $script:session | Add-Member ScriptMethod CreateUpdateSearcher { $script:searcher }; $script:manager=[pscustomobject]@{Services=@()}; function New-Object { param($ComObject) if($ComObject -eq 'Microsoft.Update.ServiceManager'){$script:manager}else{$script:session} }; ";
        var step = UpdateDiscovery.QueryStep(options);
        var args = step.Arguments.ToArray();
        args[^1] = fixture + args[^1];
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var collected = await DefenderRecovery.ExecuteAsync(step with { Arguments = args }, token: stop.Token);
        Require(!UpdateDiscovery.Interpret(collected, options).Partial, "Collector honors selected criteria using a substituted update agent");
        Require(UpdateDiscovery.Interpret(collected, options).Updates[0].Behavior is { RebootBehavior: 2, Classifications.Count: 1 }, "Nested installation metadata survives native serialization");
        string currentFailureFixture = fixture.Replace("[pscustomobject]@{ResultCode=2; Updates=$script:items}",
            "throw [Runtime.InteropServices.COMException]::new('fixture search failure', " + dataStoreCode + ")");
        var currentFailureStep = UpdateDiscovery.QueryStep(options);
        var currentFailureArguments = currentFailureStep.Arguments.ToArray();
        currentFailureArguments[^1] = currentFailureFixture + currentFailureArguments[^1];
        var currentFailureCollected = await DefenderRecovery.ExecuteAsync(
            currentFailureStep with { Arguments = currentFailureArguments }, token: stop.Token);
        Require(currentFailureCollected.ExitCode == 0 && string.IsNullOrWhiteSpace(currentFailureCollected.Error) &&
            UpdateDiscovery.Interpret(currentFailureCollected, options) is { Partial: true, FailureHResult: not null } retainedFailure &&
            retainedFailure.FailureHResult == dataStoreCode && retainedFailure.Detail.Contains("WU_E_DS_TABLEMISSING"),
            "The native collector converts a current COM search failure into structured bounded HRESULT evidence");
        var microsoftOptions = options with { Source = UpdateSearchSource.MicrosoftUpdate };
        string microsoftFixture = fixture
            .Replace("Services=@()", "Services=@([pscustomobject]@{ServiceID='" + UpdateSearchOptions.MicrosoftUpdateServiceId + "'})")
            .Replace("param($criteria) if($criteria", "param($criteria) if($script:searcher.ServerSelection -ne 3 -or $script:searcher.ServiceID -ne '" +
                UpdateSearchOptions.MicrosoftUpdateServiceId + "'){throw 'WRONG_SOURCE'}; if($criteria");
        var microsoftStep = UpdateDiscovery.QueryStep(microsoftOptions);
        var microsoftArguments = microsoftStep.Arguments.ToArray();
        microsoftArguments[^1] = microsoftFixture + microsoftArguments[^1];
        var microsoftCollected = await DefenderRecovery.ExecuteAsync(microsoftStep with { Arguments = microsoftArguments }, token: stop.Token);
        Require(!UpdateDiscovery.Interpret(microsoftCollected, microsoftOptions).Partial,
            "Registered Microsoft Update search uses the exact selected service without registration");
        var absentStep = UpdateDiscovery.QueryStep(microsoftOptions);
        var absentArguments = absentStep.Arguments.ToArray();
        absentArguments[^1] = fixture + absentArguments[^1];
        var absent = await DefenderRecovery.ExecuteAsync(absentStep with { Arguments = absentArguments }, token: stop.Token);
        Require(UpdateDiscovery.Interpret(absent, microsoftOptions).Partial && absent.Error.Contains("not registered"),
            "Absent Microsoft Update registration fails without searching or changing enrollment");
        var discovered = UpdateDiscovery.Interpret(collected, options);
        var selection = new UpdateSelection(Guid.Parse(discovered.Updates[0].Id), 2);
        Require(!UpdatePlanning.Create(discovered, [selection], new(true)).Eligible, "Unaccepted licenses block planning");
        var licensed = discovered with { Updates = [discovered.Updates[0] with { LicenseAccepted = true }] };
        Require(!UpdatePlanning.Create(licensed, [selection], new()).Eligible, "Restart choice defaults to exclusion");
        Require(UpdatePlanning.Create(licensed, [selection], new(true)).Eligible, "Explicit restart allowance permits a complete noninteractive update plan");
        var featureBehavior = licensed.Updates[0].Behavior! with
        {
            Classifications = [new(UpdatePlanning.FeatureUpgradeClassification.ToString("D"), "Upgrades")]
        };
        var featureUpdate = licensed.Updates[0] with { Behavior = featureBehavior };
        var featureDiscovery = licensed with { Updates = [featureUpdate] };
        Require(!UpdatePlanning.Create(featureDiscovery, [selection], new(true)).Eligible,
            "Feature upgrade requires separate confirmation");
        Require(!UpdatePlanning.Create(featureDiscovery, [selection], new(false, true)).Eligible,
            "Feature upgrade requires later-restart allowance");
        Require(UpdatePlanning.Create(featureDiscovery, [selection], new(true, true)).Eligible,
            "One exact offered feature upgrade is eligible after both confirmations");
        foreach (var failedEvidence in new[] { collected with { ExitCode = 1 }, collected with { TimedOut = true },
            collected with { Truncated = true }, collected with { Error = "Provider failure" } })
        {
            bool rejected = false;
            try { UpdatePlanning.Create(licensed with { Evidence = failedEvidence }, [selection], new(true)); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Complete provider label cannot override failed collection evidence during planning");
        }
        var goodCategory = licensed.Updates[0].Behavior!.Classifications[0];
        foreach (IReadOnlyList<UpdateClassification>? categories in (IReadOnlyList<UpdateClassification>?[])
        [
            null, [], [null!], [goodCategory with { Id = Guid.Empty.ToString() }], [goodCategory with { Id = "invalid" }],
            [goodCategory with { Name = "" }], [goodCategory with { Name = "invalid\nname" }],
            [goodCategory, goodCategory with { Id = "{" + goodCategory.Id + "}" }], [.. Enumerable.Repeat(goodCategory, 33)]
        ])
        {
            var unrecognized = licensed.Updates[0] with { Behavior = licensed.Updates[0].Behavior! with { Classifications = categories! } };
            Require(!UpdatePlanning.Create(licensed with { Updates = [unrecognized] }, [selection], new(true)).Eligible,
                "Embedded malformed classification evidence cannot pass unattended planning");
        }
        foreach (var changed in new[] { licensed with { Partial = true }, licensed with { Updates = [] } })
        {
            try { UpdatePlanning.Create(changed, [selection], new(true)); throw new Exception("Invalid discovery accepted"); }
            catch (ArgumentException) { }
        }
        foreach (var behaviorCase in new[] { licensed.Updates[0].Behavior! with { CanRequestInput = true }, licensed.Updates[0].Behavior! with { Beta = true } })
            Require(!UpdatePlanning.Create(licensed with { Updates = [licensed.Updates[0] with { Behavior = behaviorCase }] }, [selection], new(true)).Eligible, "Interactive and beta updates remain excluded");
        var exclusive = licensed.Updates[0] with { Behavior = licensed.Updates[0].Behavior! with { Impact = 2 } };
        var second = licensed.Updates[0] with { Id = "c6aee4ed-943f-49f6-b014-97e820d60944" };
        var pair = licensed with { Updates = [exclusive, second] };
        Require(!UpdatePlanning.Create(pair, [selection, new(Guid.Parse(second.Id), second.Revision)], new(true)).Eligible, "Exclusive update cannot share an installation plan");
        Require(UpdatePlanning.Create(pair, [selection], new(true)).Eligible, "Exclusive update can be selected alone");
        var featurePair = featureDiscovery with { Updates = [featureUpdate, second] };
        Require(!UpdatePlanning.Create(featurePair, [selection, new(Guid.Parse(second.Id), second.Revision)], new(true, true)).Eligible,
            "Feature upgrade cannot share an installation plan");
        foreach (var selections in (UpdateSelection[][])[[selection, selection], [selection with { Revision = 3 }]])
        {
            try { UpdatePlanning.Create(licensed, selections, new(true)); throw new Exception("Invalid selection accepted"); }
            catch (ArgumentException) { }
        }
        Require(UpdatePlanning.Create(licensed, [selection], new(true)).Format().Contains("PREVIEW ONLY"), "Plan does not imply installation");
        Console.WriteLine("PASS: update selection, installation planning, partial/failure states and substituted update agent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class ThrowingProgress : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("Fixture display unavailable");
    }
}
