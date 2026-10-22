using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Infrastructure;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>
/// Scripts print <see cref="Marker"/> on its own line immediately before their one mutating command. A failed run
/// that never printed it stopped before any change; a failed run that did is dispatched and its outcome uncertain.
/// </summary>
internal static class MutationBoundary
{
    internal const string Marker = "KB-MUTATION-DISPATCH";
    internal const string Script = "'" + Marker + "';";

    internal static bool Reached(ToolResult result) => result.Truncated || Lines(result.Output).Contains(Marker, StringComparer.Ordinal);

    /// <summary>The script's own result text without the dispatch line.</summary>
    internal static string Output(ToolResult result) =>
        string.Join("\n", Lines(result.Output).Where(line => !string.Equals(line, Marker, StringComparison.Ordinal))).Trim();

    internal static ActionResult Failed(ToolResult result, string raw, string error) => Reached(result)
        ? new(true, false, raw, error + " The change was sent and may be partly applied; check the current state before retrying.")
        : new(false, false, raw, error);

    private static string[] Lines(string output) => output.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim()).ToArray();
}
