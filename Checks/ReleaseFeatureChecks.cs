using System.IO;
using KillerBench.Cli;

/// <summary>Features held for a later release must be unreachable from every public entry point.</summary>
internal static class ReleaseFeatureChecks
{
    internal static async Task RunAsync()
    {
        foreach (string[] command in new[]
        {
            new[] { "third-party-updates" },
            new[] { "third-party-update", "Fixture.App", "winget", "1.0.0", "--execute" },
            new[] { "third-party-update-recover" },
            new[] { "workflow", "updates", "--third-party" }
        })
        {
            var (code, _, error) = await Run(command);
            if (code != 2 || !error.Contains("1.1", StringComparison.Ordinal))
                throw new Exception(string.Join(' ', command) + " must refuse with the 1.1 notice before doing anything, got " + code + ": " + error);
        }
        var (_, help, _) = await Run(["--help"]);
        if (help.Contains("third-party-update", StringComparison.Ordinal) || help.Contains("--third-party", StringComparison.Ordinal))
            throw new Exception("Help must not advertise third-party application updates before 1.1.");
        Console.WriteLine("PASS: third-party application updates are unreachable from the CLI and unlisted in help until 1.1.");
    }

    private static async Task<(int Code, string Output, string Error)> Run(string[] args)
    {
        var previousOut = Console.Out;
        var previousError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int code = await CommandEngine.RunAsync(args);
            return (code, output.ToString(), error.ToString());
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
        }
    }
}
