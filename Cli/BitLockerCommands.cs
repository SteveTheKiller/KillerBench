using KillerBench.Engine;

internal static class BitLockerCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string volume = "C:";
        var operation = BitLockerOperation.Inspect;
        var encryption = BitLockerEncryption.XtsAes128;
        var backup = RecoveryDestination.None;
        string? file = null;
        bool used = false, inspect = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < args.Length; i++)
        {
            string flag = args[i];
            if (!seen.Add(flag)) return Usage();
            if (flag == "--used-space-only") { used = true; continue; }
            if (flag == "--inspect") { inspect = true; continue; }
            if (++i >= args.Length) return Usage();
            string value = args[i];
            switch (flag)
            {
                case "--volume": volume = value; break;
                case "--operation" when Enum.TryParse(value, true, out BitLockerOperation parsed): operation = parsed; break;
                case "--encryption" when Enum.TryParse(value, true, out BitLockerEncryption parsed): encryption = parsed; break;
                case "--backup" when Enum.TryParse(value, true, out RecoveryDestination parsed): backup = parsed; break;
                case "--backup-file": file = value; break;
                default: return Usage();
            }
        }
        try
        {
            Console.WriteLine(new BitLockerOptions(volume, operation, encryption, used, backup, file).Describe());
            if (inspect)
            {
                using var stop = new CancellationTokenSource();
                ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
                Console.CancelKeyPress += cancel;
                try
                {
                    var checks = await BitLockerDiagnostics.CheckVolumeAsync(volume, stop.Token);
                    foreach (var check in checks) Console.WriteLine($"{check.Name}: {check.State}\n{check.Detail}");
                    return checks.Any(c => c.State == CheckState.Unknown) ? 3 : 0;
                }
                finally { Console.CancelKeyPress -= cancel; }
            }
            return 0;
        }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine("BitLocker inspection stopped. No protection settings changed."); return 130; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
        { Console.Error.WriteLine("BitLocker inspection unavailable. No protection settings changed."); return 1; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("bitlocker-plan [--volume C:] [--operation Inspect|BackupRecovery|Encrypt|ResumeProtection] [--encryption Aes128|Aes256|XtsAes128|XtsAes256] [--used-space-only] [--backup File|ActiveDirectory|EntraId] [--backup-file PATH] [--inspect]. Preview only; --inspect reads the selected volume. No protection changes.");
        return 2;
    }
}
