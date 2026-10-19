using System.IO;
using KillerLauncher;

internal static class InstallerRuntimeChecks
{
    internal static void Run()
    {
        Expect(true, false, @"dotnet\shared\Microsoft.WindowsDesktop.App\10.0.3",
            "x64 Windows must find the runtime in the dotnet folder.");
        Expect(true, true, @"dotnet\x64\shared\Microsoft.WindowsDesktop.App\10.0.3",
            "Arm64 Windows must find the x64 runtime in dotnet\\x64.");
        Expect(false, true, @"dotnet\shared\Microsoft.WindowsDesktop.App\10.0.3",
            "Arm64 Windows must not count the Arm64 runtime for the x64 app.");
        Expect(false, false, @"dotnet\shared\Microsoft.WindowsDesktop.App\9.0.10",
            "An older Desktop Runtime must not count.");
        Expect(false, false, null, "A missing dotnet folder must mean no runtime.");
        Console.WriteLine("PASS: setup finds the x64 .NET 10 Desktop Runtime on x64 and Arm64 Windows.");
    }

    private static void Expect(bool expected, bool arm64Host, string? folder, string message)
    {
        string programFiles = Path.Combine(Path.GetTempPath(), "kb-runtime-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(programFiles);
            if (folder != null) Directory.CreateDirectory(Path.Combine(programFiles, folder));
            if (InstallerPrerequisitePolicy.HasDesktopRuntime10(programFiles, arm64Host) != expected)
                throw new InvalidOperationException(message);
        }
        finally
        {
            if (Directory.Exists(programFiles)) Directory.Delete(programFiles, true);
        }
    }
}
