using KillerBench.Engine;
using System.IO;

internal static class ReportFilePublicationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string directory = Path.Combine(scratch, "publication-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "report.txt");
            bool refused = false;
            try
            {
                await ReportFilePublication.WriteAsync(path, async token =>
                {
                    await File.WriteAllTextAsync(path, "competing report", token);
                    return "new report";
                });
            }
            catch (IOException) { refused = true; }
            if (!refused || await File.ReadAllTextAsync(path) != "competing report")
                throw new Exception("No-overwrite publication must preserve a destination created while the report is being prepared.");
            Console.WriteLine("PASS: report publication preserves a concurrent destination.");
            await ReportFilePublication.WriteAsync(path, _ => Task.FromResult("authorized replacement"), overwrite: true);
            if (await File.ReadAllTextAsync(path) != "authorized replacement")
                throw new Exception("Explicit overwrite must publish the completed report.");

            string canceledPath = Path.Combine(directory, "canceled.txt");
            using var cancellation = new CancellationTokenSource();
            bool canceled = false;
            try
            {
                await ReportFilePublication.WriteAsync(canceledPath, _ =>
                {
                    cancellation.Cancel();
                    return Task.FromResult("must not publish");
                }, token: cancellation.Token);
            }
            catch (OperationCanceledException) { canceled = true; }
            if (!canceled || File.Exists(canceledPath)) throw new Exception("Canceled report preparation must not publish.");

            bool writeFailed = false;
            try
            {
                await ReportFilePublication.WriteAsync(Path.Combine(directory, "absent", "report.txt"),
                    _ => Task.FromResult("unwritable"));
            }
            catch (DirectoryNotFoundException) { writeFailed = true; }
            if (!writeFailed || Directory.GetFiles(directory).Length != 1)
                throw new Exception("Failed or canceled publication must leave no operation-owned temporary files.");
            string success = Path.Combine(directory, "new.txt");
            await ReportFilePublication.WriteAsync(success, _ => Task.FromResult("complete report"));
            if (await File.ReadAllTextAsync(success) != "complete report") throw new Exception("New reports must publish completely.");
            Console.WriteLine("PASS: report publication honors explicit overwrite, cancellation, failure cleanup and new destinations.");
            foreach (bool publish in new[] { true, false })
            {
                string heldPath = Path.Combine(directory, publish ? "held.txt" : "abandoned.txt");
                bool substituted = false;
                using (var prepared = await PreparedReportFile.CreateAsync(heldPath, "owned complete report"))
                {
                    string temporary = Directory.GetFiles(directory, ".killerbench-report-*.tmp").Single();
                    try
                    {
                        File.Move(temporary, temporary + ".displaced");
                        File.WriteAllText(temporary, "competing temporary");
                        substituted = true;
                    }
                    catch (IOException) { }
                    if (publish) prepared.Publish();
                }
                if (substituted || publish && await File.ReadAllTextAsync(heldPath) != "owned complete report" ||
                    !publish && File.Exists(heldPath))
                    throw new Exception("A prepared report must retain its exact temporary object through publication and abandonment.");
            }
            Console.WriteLine("PASS: completed report objects resist replacement through publication and abandonment.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
