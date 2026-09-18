using KillerBench.Engine;

internal static class PrivacyChecks
{
    internal static void Run()
    {
        if (new PrivacyOptions([]).Plan().Count != 0) throw new Exception("Privacy defaults must preserve all settings.");
        var camera = new PrivacyOptions([PrivacyArea.Camera]).Plan();
        if (camera.Count != 1 || camera[0].Area != PrivacyArea.Camera) throw new Exception("Camera selection changed another privacy area.");
        var clipboard = new PrivacyOptions([PrivacyArea.ClipboardSync]).Plan();
        if (clipboard.Count != 1 || clipboard[0].Area != PrivacyArea.ClipboardSync) throw new Exception("Clipboard sync must be independent of history.");
        if (new PrivacyOptions([PrivacyArea.AppActivity]).Plan().Single().Area != PrivacyArea.AppActivity ||
            new PrivacyOptions([PrivacyArea.InputPersonalization]).Plan().Single().Area != PrivacyArea.InputPersonalization)
            throw new Exception("App activity and input personalization must remain independent choices.");
        foreach (var invalid in new[] { new[] { PrivacyArea.Camera, PrivacyArea.Camera }, new[] { (PrivacyArea)999 } })
        {
            try { new PrivacyOptions(invalid).Plan(); throw new Exception("Invalid privacy selection accepted."); }
            catch (ArgumentException) { }
        }
        if (PrivacyCatalog.All.Select(c => c.Area).Distinct().Count() != Enum.GetValues<PrivacyArea>().Length)
            throw new Exception("Privacy catalog coverage mismatch.");
        if (PrivacyInspection.Targets.GroupBy(target => (target.Machine, target.Path, target.Name)).Any(group => group.Count() != 1))
            throw new Exception("Privacy registry targets must be unique.");
        foreach (string required in new[] { "SearchPlatformContext", "SubscribedContent-338387Enabled", "SubscribedContent-338388Enabled",
            "SubscribedContent-338389Enabled", "SubscribedContent-353694Enabled", "SubscribedContent-353696Enabled",
            "SubscribedContent-338393Enabled", "SubscribedContent-310093Enabled", "Start_TrackProgs", "AllowInputPersonalization",
            "RestrictImplicitInkCollection", "RestrictImplicitTextCollection" })
            if (!PrivacyInspection.Targets.Any(target => target.Name == required))
                throw new Exception("Missing reviewed SHADE registry target: " + required);
        int reads = 0;
        var snapshot = PrivacyInspection.Inspect(new([PrivacyArea.Camera]), target =>
        {
            reads++;
            if (target.Area != PrivacyArea.Camera) throw new Exception("Inspected an unselected area.");
            return new(target, "Recorded", "Deny", "String");
        });
        if (reads != 3 || snapshot.Count != 3 || snapshot.Count(value => value.Target.Machine) != 2)
            throw new Exception("Camera inspection must retain user, machine and policy values.");
        var separated = PrivacyInspection.Inspect(new([PrivacyArea.AppActivity, PrivacyArea.InputPersonalization]),
            target => new(target, "Not configured", null, null));
        if (separated.Count(value => value.Target.Area == PrivacyArea.AppActivity) != 1 ||
            separated.Count(value => value.Target.Area == PrivacyArea.InputPersonalization) != 4)
            throw new Exception("App activity and input personalization registry coverage changed.");
        if (PrivacyInspection.Services.Count != 7 ||
            PrivacyInspection.Services.Select(service => service.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 7)
            throw new Exception("Reviewed SHADE service coverage mismatch.");
        int serviceReads = 0;
        var services = PrivacyInspection.InspectServicesAsync(new([PrivacyArea.DiagnosticData]), (target, token) =>
        {
            serviceReads++;
            return Task.FromResult(new ToolResult(0,
                "SERVICE_NAME: " + target.Name + "\nSTATE : " + (target.Name == "WerSvc" ? "1 STOPPED" : "4 RUNNING"), "", false));
        }).GetAwaiter().GetResult();
        if (serviceReads != 4 || services.Count != 4 || services.Count(service => service.State == "Running") != 3 ||
            services.Single(service => service.Target.Name == "WerSvc").State != "Stopped" ||
            !PrivacyInspection.Format([], services).Contains("PRIVACY SERVICE SNAPSHOT"))
            throw new Exception("Selected SHADE service states were not retained accurately.");
        var incompleteService = PrivacyInspection.InspectServicesAsync(new([PrivacyArea.Location]),
            (target, token) => Task.FromResult(new ToolResult(0, "SERVICE_NAME: other\nSTATE : 4 RUNNING", "", false)))
            .GetAwaiter().GetResult().Single();
        if (incompleteService.State != "Unavailable") throw new Exception("Mismatched privacy service identity was accepted.");
        PrivacyInspection.InspectServicesAsync(new([]), (_, _) => throw new Exception("Empty selection queried a service."))
            .GetAwaiter().GetResult();
        PrivacyInspection.Inspect(new([]), _ => throw new Exception("Empty selection read registry."));
        try
        {
            PrivacyInspection.Inspect(new([PrivacyArea.Camera]), _ => throw new Exception("Cancellation ignored."), new CancellationToken(true));
            throw new Exception("Cancelled privacy inspection succeeded.");
        }
        catch (OperationCanceledException) { }
        try
        {
            PrivacyInspection.InspectServicesAsync(new([PrivacyArea.Location]), (_, _) =>
                throw new Exception("Canceled service inspection queried a provider."), new CancellationToken(true)).GetAwaiter().GetResult();
            throw new Exception("Canceled privacy service inspection succeeded.");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: privacy option isolation, reviewed SHADE registry and service coverage, unchanged defaults and invalid selection rejection.");
    }
}
