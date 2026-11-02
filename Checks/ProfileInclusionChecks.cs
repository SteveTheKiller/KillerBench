using System.IO;
using System.Text.Json.Nodes;
using KillerBench.Engine.Profiles;

internal static class ProfileInclusionChecks
{
    internal static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "KillerBench-inclusion-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "source"), destination = Path.Combine(root, "copy");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(destination);
        try
        {
            const string relative = @"AppData\Local\Microsoft\WindowsApps\Nested\ordinary.txt";
            string ordinary = Path.Combine(source, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(ordinary)!);
            await File.WriteAllTextAsync(ordinary, "ordinary application data");
            var target = new ProfileSnapshot("S-1-5-21-1-2-3-1001", source, false, false,
                DateTimeOffset.UtcNow.AddDays(-120), false);
            var step = ProfileBackupCopy.CreateStep(target, Path.Combine(root, "planned-copy"));
            int exclusions = Array.IndexOf(step.Arguments, "/XD");
            if (exclusions >= 0 && step.Arguments.Skip(exclusions + 1).TakeWhile(value => !value.StartsWith('/'))
                .Any(folder => ordinary.StartsWith(folder.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("The production copy plan excludes ordinary WindowsApps data.");
            string copied = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(copied)!);
            File.Copy(ordinary, copied);
            var verified = await ProfileBackupArtifactWriter.CreateForExistingCopyAsync(target, destination);
            if (!verified.Verified || verified.Receipt?.FileCount != 1 || verified.Receipt.TotalBytes != 25)
                throw new Exception("Ordinary nested WindowsApps data did not receive a truthful verified artifact: " + verified.Detail);
            string receiptPath = Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName);
            string receiptJson = await File.ReadAllTextAsync(receiptPath);
            var receipt = JsonNode.Parse(receiptJson)!.AsObject();
            string omissionProperty = receipt.Select(pair => pair.Key).Single(key => key.Equals("omittedLinks", StringComparison.OrdinalIgnoreCase));
            foreach (var omission in new JsonNode?[] { new JsonArray("absent-link"), new JsonArray("../outside"), new JsonArray((JsonNode?)null) })
            {
                receipt[omissionProperty] = omission;
                await File.WriteAllTextAsync(receiptPath, receipt.ToJsonString());
                if ((await ProfileBackupArtifactVerifier.VerifyAsync(target, receiptPath)).Verified)
                    throw new Exception("A false or malformed omission receipt was accepted.");
            }
            receipt.Remove(omissionProperty);
            await File.WriteAllTextAsync(receiptPath, receipt.ToJsonString());
            if (!(await ProfileBackupArtifactVerifier.VerifyAsync(target, receiptPath)).Verified)
                throw new Exception("An older receipt with no omitted links was rejected.");
            await File.WriteAllTextAsync(receiptPath, receiptJson);
            File.Delete(copied);
            if ((await ProfileBackupArtifactVerifier.VerifyAsync(target,
                Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName))).Verified)
                throw new Exception("A backup missing the ordinary WindowsApps file was accepted.");
            Console.WriteLine("PASS: ordinary nested WindowsApps data remains in the copy plan and verified file set; missing data and false omission receipts refuse verification; old receipts without omissions remain valid.");
        }
        finally { Directory.Delete(root, true); }
    }
}
