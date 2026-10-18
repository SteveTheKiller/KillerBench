using Microsoft.Win32;

internal static class CommandLinePathChecks
{
    internal static void Run()
    {
        string name = @"Software\KillerBench-PathContract-" + Guid.NewGuid().ToString("N");
        const string folder = @"C:\KillerBench Path Contract";
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(name, writable: true);
            key.SetValue("Path", @"%USERPROFILE%\bin", RegistryValueKind.ExpandString);
            if (!KillerBench.Services.CommandLinePath.AddTo(key, folder)
                || KillerBench.Services.CommandLinePath.AddTo(key, folder.ToUpperInvariant() + "\\")
                || key.GetValueKind("Path") != RegistryValueKind.ExpandString
                || !Equals(key.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames), @"%USERPROFILE%\bin;" + folder))
                throw new InvalidOperationException("Command registration must preserve PATH spelling and type and add its folder once.");
            if (!KillerBench.Services.CommandLinePath.RemoveFrom(key, folder)
                || !Equals(key.GetValue("Path", null, RegistryValueOptions.DoNotExpandEnvironmentNames), @"%USERPROFILE%\bin"))
                throw new InvalidOperationException("Uninstall must remove only its own PATH entry.");
            Console.WriteLine("PASS: command PATH registration preserves variables and type, deduplicates and removes its folder.");
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(name, throwOnMissingSubKey: false); }
    }
}
