using KillerBench.Engine;

internal static class WindowsErrorReferenceChecks
{
    internal static void Run()
    {
        var message = WindowsErrorReference.Interpret(5, "  Access\r\n\tis denied.  ");
        Require(message.Code == 5 && message.Message == "Access is denied." &&
            message.Source == "Installed Windows system message table" &&
            message.Limitation.Contains("does not identify", StringComparison.Ordinal),
            "Windows message normalization lost code, text or scope");

        foreach (string? invalid in new[] { null, "", " \r\n\t", new string('x', 4097) })
            Require(WindowsErrorReference.Interpret(uint.MaxValue, invalid).Message is null,
                "Missing or oversized Windows message text was accepted");

        Require(WindowsErrorReference.Interpret(0, new string('x', 4096)).Message?.Length == 4096,
            "Maximum bounded Windows message text was rejected");
        Console.WriteLine("PASS: installed Windows message lookup normalizes bounded text and rejects missing or oversized results.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
