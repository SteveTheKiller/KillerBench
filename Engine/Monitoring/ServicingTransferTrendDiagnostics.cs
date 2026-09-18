using System.Text.Json;

namespace KillerBench.Engine;

public static class ServicingTransferTrendDiagnostics
{
    public static WorkflowCheck Compare(ServicingActivitySnapshot previous, IReadOnlyList<WorkflowCheck> currentChecks,
        DateTimeOffset capturedUtc)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(currentChecks);
        if (capturedUtc <= previous.CapturedUtc || capturedUtc - previous.CapturedUtc > TimeSpan.FromMinutes(5))
            return Result("The transfer interval is unavailable or outside its five-minute bound.");

        var details = new List<string>();
        bool comparable = false;
        if (TryBits(previous.Checks, out var priorBits) && TryBits(currentChecks, out var currentBits))
        {
            ulong moved = 0;
            int matched = 0;
            int changed = 0;
            foreach (var prior in priorBits.Jobs.Where(job => job.BytesTransferred is not null))
            {
                var current = currentBits.Jobs.SingleOrDefault(job => job.Id == prior.Id);
                if (current?.BytesTransferred is not ulong currentBytes || prior.BytesTransferred is not ulong priorBytes)
                    continue;
                if (currentBytes < priorBytes) { changed++; continue; }
                matched++;
                moved = Add(moved, currentBytes - priorBytes, ref changed);
            }
            if (matched > 0)
            {
                comparable = true;
                details.Add($"BITS: {moved} bytes moved across {matched} stable job{(matched == 1 ? "" : "s")}" +
                    (changed == 0 ? "." : $"; {changed} changed or reset counter{(changed == 1 ? " was" : "s were")} excluded."));
            }
            else if (priorBits.Jobs.Count == 0 && currentBits.Jobs.Count == 0)
            {
                comparable = true;
                details.Add("BITS: no current-account jobs were present in either snapshot.");
            }
            else details.Add("BITS: no stable job identity had comparable byte counters.");
        }

        if (TryDeliveryOptimization(previous.Checks, out ulong priorDo, out ulong priorEntries) &&
            TryDeliveryOptimization(currentChecks, out ulong currentDo, out ulong currentEntries))
        {
            if (currentDo >= priorDo)
            {
                comparable = true;
                details.Add($"Delivery Optimization: {currentDo - priorDo} aggregate downloaded bytes changed across snapshots ({priorEntries} to {currentEntries} jobs).");
            }
            else details.Add("Delivery Optimization: aggregate downloaded bytes decreased because the observed job set or counters changed, so no byte delta was inferred.");
        }

        string interval = $"Interval: {(capturedUtc - previous.CapturedUtc).TotalSeconds:F1} seconds. ";
        string conclusion = comparable
            ? " Zero observed movement does not establish a stall, and positive movement does not establish eventual completion."
            : " No comparable transfer interval was available; this does not establish inactivity or a fault.";
        return Result(interval + string.Join(" ", details) + conclusion);
    }

    private static bool TryBits(IReadOnlyList<WorkflowCheck> checks, out BitsInventoryResult inventory)
    {
        inventory = new(null, false, [], new(-1, "", "", false));
        var check = checks.SingleOrDefault(item => item.Name == "BITS transfer jobs");
        if (check is not { State: not CheckState.Unknown, Evidence: { } evidence }) return false;
        inventory = BitsDiagnostics.Parse(evidence);
        return inventory is { Scope: "CurrentAccount", Complete: true };
    }

    private static bool TryDeliveryOptimization(IReadOnlyList<WorkflowCheck> checks, out ulong downloaded, out ulong entries)
    {
        downloaded = 0;
        entries = 0;
        var check = checks.SingleOrDefault(item => item.Name == "Delivery Optimization activity");
        if (check is not { State: not CheckState.Unknown, Evidence: { ExitCode: 0 } evidence }) return false;
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("entryCount", out var entryCount) || !entryCount.TryGetUInt64(out entries) || entries > 4096 ||
                !root.TryGetProperty("groups", out var groups) || groups.ValueKind != JsonValueKind.Array || groups.GetArrayLength() > 4)
                return false;
            foreach (var group in groups.EnumerateArray())
            {
                if (!group.TryGetProperty("downloadedBytes", out var value) || !value.TryGetUInt64(out ulong bytes)) return false;
                downloaded = checked(downloaded + bytes);
            }
            return true;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            downloaded = 0;
            entries = 0;
            return false;
        }
    }

    private static ulong Add(ulong total, ulong value, ref int changed)
    {
        try { return checked(total + value); }
        catch (OverflowException) { changed++; return total; }
    }

    private static WorkflowCheck Result(string detail) => new("Transfer interval", CheckState.Information,
        detail + " File, URL, host, peer, process and user identities are not added by this comparison. No transfer or setting changed.");
}
