using System.Diagnostics;
using System.Text;

namespace KillerBench.Engine.Infrastructure;

public sealed record ToolResult(int ExitCode, string Output, string Error, bool Truncated)
{
    public bool TimedOut { get; init; }
}
internal static class NativeTool
{
    internal static async Task<ToolResult> RunAsync(string name, string[] arguments, bool unicode = false,
        IProgress<string>? progress = null, CancellationToken token = default) =>
        await RunCoreAsync(name, arguments, unicode, progress, null, token: token).ConfigureAwait(false);

    internal static async Task<ToolResult> RunWithInputAsync(string name, string[] arguments, string standardInput,
        bool unicode = false, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(standardInput);
        return await RunCoreAsync(name, arguments, unicode, progress, standardInput, token: token).ConfigureAwait(false);
    }

    private static async Task<ToolResult> RunCoreAsync(string name, string[] arguments, bool unicode,
        IProgress<string>? progress, string? standardInput, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("This action requires Windows.");
        string path = name.Equals("winget.exe", StringComparison.OrdinalIgnoreCase)
            ? await WinGetExecutableResolver.ResolveAsync(token).ConfigureAwait(false) : ResolvePath(name);
        return await RunExecutableAsync(path, arguments, unicode, progress, standardInput, token).ConfigureAwait(false);
    }

    internal static async Task<ToolResult> RunExecutableAsync(string path, string[] arguments, bool unicode = false,
        IProgress<string>? progress = null, string? standardInput = null, CancellationToken token = default, Action? beforeStart = null)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("This action requires Windows.");
        var start = new ProcessStartInfo(path)
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            RedirectStandardInput = standardInput is not null };
        if (unicode) { start.StandardOutputEncoding = Encoding.Unicode; start.StandardErrorEncoding = Encoding.Unicode; }
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        if (token.IsCancellationRequested) throw new ToolStartCanceledException(token);
        beforeStart?.Invoke();
        Process started;
        try { started = Process.Start(start) ?? throw new ToolNotStartedException("The native executable did not start."); }
        catch (Exception startupError) when (startupError is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        { throw new ToolNotStartedException("The native executable did not start.", startupError); }
        using var process = started;
        using var stop = token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { } });
        // The readers start before stdin is written, so a child that fills its output pipe while
        // still reading a large input cannot deadlock against this writer.
        var output = ReadAsync(process.StandardOutput);
        var error = ReadAsync(process.StandardError);
        if (standardInput is not null)
        {
            // A child that exits without reading all of its input breaks the pipe; its exit code and
            // output still say what happened, so the write failure is not the result.
            try
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), token).ConfigureAwait(false);
                process.StandardInput.Close();
            }
            catch (IOException) { }
        }
        await Task.WhenAll(output, error).ConfigureAwait(false);
        await process.WaitForExitAsync(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return new(process.ExitCode, output.Result.Text, error.Result.Text, output.Result.Truncated || error.Result.Truncated);

        async Task<(string Text, bool Truncated)> ReadAsync(StreamReader reader)
        {
            var text = new StringBuilder();
            char[] buffer = new char[4096];
            bool truncated = false;
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) != 0)
            {
                int keep = Math.Min(count, 250_000 - text.Length);
                text.Append(buffer, 0, keep);
                truncated |= keep != count;
                progress?.Report(new string(buffer, 0, Math.Min(count, 500)).Trim('\0', '\r', '\n', '\uFEFF'));
            }
            return (text.ToString().TrimStart('\uFEFF'), truncated);
        }
    }

    private static string ResolvePath(string name) => name.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe")
        : name.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), name);
}
