using KillerBench.Engine;

internal static class BitsChecks
{
    internal static void Run()
    {
        const string row = "{\"id\":\"a7d4d9df-f366-4b29-9482-50ee15c8a608\",\"name\":\"Fixture\",\"state\":\"Suspended\",\"error\":\"\"}";
        var raw = new ToolResult(0, "{\"scope\":\"CurrentAccount\",\"complete\":true,\"jobs\":[" + row + "]}", "", false);
        foreach (string state in new[] { "Queued", "Connecting", "Transferring", "Suspended", "Error", "TransientError", "Transferred", "Acknowledged", "Cancelled" })
        {
            var check = BitsDiagnostics.Interpret(raw with { Output = raw.Output.Replace("Suspended", state) });
            if (check.State != (state is "Error" or "TransientError" ? CheckState.Attention : CheckState.Information) || check.Repair is not null || check.Evidence is null)
                throw new Exception("BITS state classification or preservation failed.");
        }
        foreach (var invalid in new[] { raw with { Truncated = true }, raw with { TimedOut = true }, raw with { Error = "Denied" },
            raw with { Output = "{}" }, raw with { Output = raw.Output.Replace("Suspended", "Unexpected") },
            raw with { Output = raw.Output.Replace("true", "false") },
            raw with { Output = raw.Output.Replace(row, row + "," + row) },
            raw with { Output = raw.Output.Replace("\"complete\":true", "\"complete\":true,\"complete\":true") } })
            if (BitsDiagnostics.Interpret(invalid).State != CheckState.Unknown) throw new Exception("Incomplete BITS coverage claimed success.");
        var empty = BitsDiagnostics.Interpret(raw with { Output = raw.Output.Replace(row, "") });
        string metadata = ",\"owner\":\"DOMAIN\\\\Fixture\",\"errorCode\":-2147024891,\"bytesTotal\":\"18446744073709551615\",\"bytesTransferred\":\"42\"";
        var detailed = raw with { Output = raw.Output.Replace("\"error\":\"\"", "\"error\":\"\"" + metadata) };
        var detail = BitsDiagnostics.Interpret(detailed);
        var inventory = BitsDiagnostics.Parse(detailed);
        var selected = inventory.Jobs.Single();
        if (!inventory.Complete || selected.BytesTotal is not null || selected.BytesTransferred != 42 || selected.ErrorCode != -2147024891 || selected.Owner != "DOMAIN\\Fixture")
            throw new Exception("Structured BITS inventory lost metadata.");
        var review = BitsJobReview.Review(inventory, selected.Id);
        var later = BitsDiagnostics.Parse(detailed with { Output = detailed.Output.Replace("\"42\"", "\"84\"") });
        var advancing = BitsJobReview.Compare(inventory, later, selected.Id);
        if (!advancing.Comparable || advancing.BytesAdvanced != 42 || !advancing.Detail.Contains("unverified"))
            throw new Exception("BITS progress comparison lost byte delta or completion scope.");
        var unchanged = BitsJobReview.Compare(inventory, inventory, selected.Id);
        if (!unchanged.Comparable || unchanged.BytesAdvanced != 0 || !unchanged.Detail.Contains("does not establish"))
            throw new Exception("Unchanged byte counts implied a stalled transfer.");
        foreach (var altered in new[]
        {
            detailed with { Truncated = true },
            detailed with { Output = detailed.Output.Replace("CurrentAccount", "AllUsers") },
            detailed with { Output = detailed.Output.Replace("\"42\"", "\"41\"") },
            detailed with { Output = detailed.Output.Replace("\"name\":\"Fixture\"", "\"name\":\"Changed\"") },
            detailed with { Output = detailed.Output.Replace("18446744073709551615", "100") }
        })
            if (BitsJobReview.Compare(inventory, BitsDiagnostics.Parse(altered), selected.Id) is not { Comparable: false, BytesAdvanced: null })
                throw new Exception("BITS progress comparison accepted changed or incomplete evidence.");
        if (review.Selected != selected || !review.Notes.Any(note => note.Contains("may be intentional"))) throw new Exception("Exact BITS review lost suspension semantics.");
        if (BitsJobReview.Review(inventory, Guid.NewGuid()).Selected is not null) throw new Exception("Missing job was replaced by another target.");
        var forged = inventory with { Evidence = detailed with { Truncated = true } };
        if (BitsJobReview.Review(forged, selected.Id).Complete || BitsJobReview.Review(forged, selected.Id).Selected is not null)
            throw new Exception("Job review trusted summary data over incomplete evidence.");
        if (detail.State != CheckState.Information || !detail.Detail.Contains("0x80070005") || !detail.Detail.Contains("42/unknown bytes") || !detail.Detail.Contains("DOMAIN\\Fixture"))
            throw new Exception("BITS metadata lost owner, signed error code or unknown-size sentinel.");
        foreach (string invalid in new[] { detailed.Output.Replace("18446744073709551615", "1"), detailed.Output.Replace("-2147024891", "4294967295"),
            detailed.Output.Replace("\"42\"", "\"-1\""), detailed.Output.Replace("\"owner\"", "\"missingOwner\"") })
            if (BitsDiagnostics.Interpret(raw with { Output = invalid }).State != CheckState.Unknown) throw new Exception("Invalid BITS metadata claimed coverage.");
        if (empty.State != CheckState.Information || !empty.Detail.Contains("0 retained")) throw new Exception("Empty BITS queue is not explicit.");
        Console.WriteLine("PASS: BITS job states, scope, duplicate identity, incomplete evidence and non-mutating findings; fixtures only.");
    }
}
