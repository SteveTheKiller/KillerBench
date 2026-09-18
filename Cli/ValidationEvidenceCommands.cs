using System.Text.Json;
using KillerBench.Engine;

internal static class ValidationEvidenceCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2 || args.Length > NativeValidationEvidence.MaximumRecords + 1 ||
            args[0] is not ("validation-record" or "validation-coverage"))
        {
            Console.Error.WriteLine("Usage: validation-record RECORD.json | validation-coverage RECORD.json [...] (read-only native evidence validation; no diagnostics or repairs)");
            return 2;
        }

        try
        {
            var records = new List<NativeValidationRecord>();
            foreach (string path in args.Skip(1))
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length is 0 or > NativeValidationEvidence.MaximumBytes)
                    throw new InvalidDataException("Each native validation record must be an existing file from 1 byte through 1 MiB.");
                records.Add(NativeValidationEvidence.Read(await File.ReadAllBytesAsync(info.FullName)));
            }

            if (args[0] == "validation-record")
            {
                if (records.Count != 1)
                {
                    Console.Error.WriteLine("Usage: validation-record RECORD.json (validate one record; no diagnostics or repairs)");
                    return 2;
                }
                Console.WriteLine(JsonSerializer.Serialize(new { valid = true, executionPerformed = false, record = records[0] }, Capture.JsonOptions));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    executionPerformed = false,
                    limitation = "Coverage records native evidence for the current recipe versions. It does not broaden any repair claim beyond the recorded scenarios.",
                    coverage = NativeValidationEvidence.Summarize(records)
                }, Capture.JsonOptions));
            }
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }
    }
}
