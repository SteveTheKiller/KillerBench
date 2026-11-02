using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using KillerBench.Cli;

internal static class CliDocumentationChecks
{
    internal static void Run()
    {
        string root = FindRoot();
        string help = File.ReadAllText(Path.Combine(root, "bench-landing", "help.html"));
        string overview = File.ReadAllText(Path.Combine(root, "bench-landing", "engine", "cli.html"));
        Verify(help, overview, CommandEngine.CommandNames);

        // Variant rows must not hide a missing command with the same overall row count.
        string withoutCapabilities = Regex.Replace(help,
            "<li id=\"cli_cmd-capabilities\"[^>]*>.*?</li>", "", RegexOptions.Singleline);
        if (withoutCapabilities == help) throw new Exception("Capability reference fixture was not selected.");
        bool rejected = false;
        try { Verify(withoutCapabilities, overview, CommandEngine.CommandNames); }
        catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new Exception("Duplicate variant rows concealed a missing public command.");
        Console.WriteLine("PASS: help landing documents all " + CommandEngine.CommandNames.Distinct(StringComparer.Ordinal).Count() +
            " public CLI names with syntax, descriptions, unique anchors and a discoverable full reference.");
    }

    private static void Verify(string help, string overview, IEnumerable<string> commands)
    {
        var ids = Regex.Matches(help, "\\bid=\"([^\"]+)\"").Select(match => match.Groups[1].Value).ToArray();
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new InvalidDataException("Help contains duplicate deep-link anchors.");
        var entries = Regex.Matches(help,
            "<li id=\"cli_cmd-([^\"]+)\"[^>]*><code>(.*?)</code>:(.*?)</li>", RegexOptions.Singleline);
        foreach (string command in commands.Distinct(StringComparer.Ordinal))
        {
            bool documented = entries.Any(entry => entry.Groups[1].Value == command &&
                WebUtility.HtmlDecode(entry.Groups[2].Value).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() == command &&
                Regex.Replace(entry.Groups[3].Value, "<[^>]+>", "").Trim().Length >= 8);
            if (!documented) throw new InvalidDataException("Missing CLI syntax and meaning on help landing: " + command);
        }
        foreach (string id in new[] { "cli", "use-the-cli", "cli_discovery-and-triage", "cli_cmd-investigate", "cli_cmd-validate-native", "cli_cmd-third-party-update" })
            if (!ids.Contains(id, StringComparer.Ordinal)) throw new InvalidDataException("Missing CLI deep link: " + id);
        if (!overview.Contains("href=\"../help.html#cli\"", StringComparison.Ordinal))
            throw new InvalidDataException("Engine CLI overview must link to the full help reference.");
        string investigation = entries.Single(entry => entry.Groups[1].Value == "investigate").Value;
        foreach (string detail in new[] { "EVTX", "XML", "1,000,000", "10,000", "newest 200", "first 200", "15-second" })
            if (!investigation.Contains(detail, StringComparison.Ordinal))
                throw new InvalidDataException("Investigation reference omitted supported input or its limit: " + detail);
    }

    private static string FindRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "bench-landing", "help.html"))) return directory.FullName;
        throw new DirectoryNotFoundException("Run CLI documentation checks from the KillerBench checkout or its build output.");
    }
}
