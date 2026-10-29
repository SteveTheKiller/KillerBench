using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using KillerBench.Engine.Windows.Primitives;
using Microsoft.Win32.SafeHandles;

internal static class WindowsRetentionScopeChecks
{
    internal static async Task RunAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "KillerBenchRetentionScope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string unselected = Path.Combine(root, "unselected");
            Directory.CreateDirectory(unselected);
            string sentinel = Path.Combine(unselected, "sentinel.txt");
            await File.WriteAllTextAsync(sentinel, "unselected sentinel");
            string source = Path.Combine(root, "selected");
            await CreateJunctionAsync(source, unselected);
            var action = new WindowsRetainFolderFilesAction(_ => (source, "*"), () => Path.Combine(root, "retained"));
            var result = await action.ExecuteAsync(new Dictionary<string, string> { ["folder"] = "recent-automatic-destinations" }, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (result.Dispatched || result.Succeeded || !File.Exists(sentinel))
                throw new Exception("Redirected source escaped the selected retention scope: dispatched=" + result.Dispatched + ", succeeded=" + result.Succeeded + ", sentinel=" + File.Exists(sentinel));
            var count = await new WindowsFileCountProbe(_ => (source, "*")).ObserveAsync(Target(), TimeSpan.FromSeconds(10), CancellationToken.None);
            if (count.Complete) throw new Exception("Redirected source count was reported as complete.");
            Console.WriteLine("PASS: redirected retention source refused and unselected sentinel preserved.");
            Directory.Delete(source);
            await CheckDestinationAndAncestorsAsync(root);
            await CheckConfiguredAnchorsAsync(root);
            await CheckInPlaceRedirectionAsync(root);
            await CheckCreationRedirectionAsync(root);
            await CheckSourceFileRedirectionAsync(root);
            string? secondVolumeRoot = Environment.GetEnvironmentVariable("KILLERBENCH_RETENTION_SECOND_VOLUME");
            if (string.IsNullOrWhiteSpace(secondVolumeRoot))
                Console.WriteLine("PENDING: cross-volume retention refusal requires an owned fixture root on a second disposable volume; native retention supports same-volume moves only.");
            else await CheckCrossVolumeRefusalAsync(root, secondVolumeRoot);
        }
        finally
        {
            RemoveJunctions(root);
            Directory.Delete(root, true);
        }
    }

    private static async Task CheckDestinationAndAncestorsAsync(string root)
    {
        string source = Path.Combine(root, "ordinary-source");
        string outside = Path.Combine(root, "outside-destination");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outside);
        string sentinel = Path.Combine(source, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "selected sentinel");
        string destinationLink = Path.Combine(root, "destination-link");
        await CreateJunctionAsync(destinationLink, outside);
        foreach (string destination in new[] { destinationLink, Path.Combine(destinationLink, "new-retained") })
        {
            var action = new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination);
            var result = await action.ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (result.Dispatched || result.Succeeded || !File.Exists(sentinel) || Directory.EnumerateFileSystemEntries(outside).Any())
                throw new Exception("A redirected retention destination was followed or its target modified.");
        }
        string outsideParent = Path.Combine(root, "outside-parent");
        Directory.CreateDirectory(Path.Combine(outsideParent, "child"));
        string outsideSentinel = Path.Combine(outsideParent, "child", "keep.txt");
        await File.WriteAllTextAsync(outsideSentinel, "unselected ancestor sentinel");
        string ancestorLink = Path.Combine(root, "ancestor-link");
        await CreateJunctionAsync(ancestorLink, outsideParent);
        string redirectedSource = Path.Combine(ancestorLink, "child");
        var redirected = await new WindowsRetainFolderFilesAction(_ => (redirectedSource, "*"), () => Path.Combine(root, "retained-ancestor"))
            .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        var count = await new WindowsFileCountProbe(_ => (redirectedSource, "*")).ObserveAsync(Target(), TimeSpan.FromSeconds(10), CancellationToken.None);
        if (redirected.Dispatched || redirected.Succeeded || count.Complete || !File.Exists(outsideSentinel)) throw new Exception("An unreviewed source ancestor redirection was followed.");
        Console.WriteLine("PASS: source and destination ancestor junctions refused without changing unselected directories.");
    }

    private static async Task CheckConfiguredAnchorsAsync(string root)
    {
        string physicalSource = Path.Combine(root, "configured-source");
        string physicalDestination = Path.Combine(root, "configured-destination");
        string otherSource = Path.Combine(root, "other-source");
        string otherDestination = Path.Combine(root, "other-destination");
        foreach (string directory in new[] { physicalSource, physicalDestination, otherSource, otherDestination }) Directory.CreateDirectory(directory);
        string relative = Path.Combine("Microsoft", "Recent");
        Directory.CreateDirectory(Path.Combine(physicalSource, relative));
        Directory.CreateDirectory(Path.Combine(otherSource, relative));
        string selectedFile = Path.Combine(physicalSource, relative, "selected.txt");
        string otherFile = Path.Combine(otherSource, relative, "unselected.txt");
        await File.WriteAllTextAsync(selectedFile, "selected anchor file");
        await File.WriteAllTextAsync(otherFile, "unselected anchor file");
        string sourceAlias = Path.Combine(root, "configured-source-alias");
        string destinationAlias = Path.Combine(root, "configured-destination-alias");
        await CreateJunctionAsync(sourceAlias, physicalSource);
        await CreateJunctionAsync(destinationAlias, physicalDestination);
        string source = Path.Combine(sourceAlias, relative);
        string destination = Path.Combine(destinationAlias, "KillerBench", "Retained");
        var count = await new WindowsFileCountProbe(_ => (source, "*"), _ => sourceAlias).ObserveAsync(Target(), TimeSpan.FromSeconds(10), CancellationToken.None);
        if (!count.Complete || count.Value != "1") throw new Exception("A configured redirected anchor was rejected by read-only counting.");

        int replacementRefusals = 0;
        string callbackStage = "not reached";
        string? retainedFile = null;
        void MoveWithReplacementAttempts(string from, string to, Action guardedMove)
        {
            callbackStage = "canonical identity";
            if (!from.Equals(selectedFile, StringComparison.OrdinalIgnoreCase) || !to.StartsWith(physicalDestination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Retention did not use the reviewed canonical source and destination anchors.");
            foreach (string pinned in new[] { Path.GetDirectoryName(from)!, Path.GetDirectoryName(to)! })
            {
                try { Directory.Move(pinned, pinned + "-replaced"); }
                catch (IOException) { replacementRefusals++; }
            }
            callbackStage = "source alias removal";
            Directory.Delete(sourceAlias);
            callbackStage = "destination alias removal";
            Directory.Delete(destinationAlias);
            callbackStage = "source alias replacement";
            CreateJunctionAsync(sourceAlias, otherSource).GetAwaiter().GetResult();
            callbackStage = "destination alias replacement";
            CreateJunctionAsync(destinationAlias, otherDestination).GetAwaiter().GetResult();
            callbackStage = "canonical file move";
            guardedMove();
            retainedFile = to;
        }
        var result = await new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination, null, _ => sourceAlias, () => destinationAlias, MoveWithReplacementAttempts)
            .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (!result.Dispatched || !result.Succeeded || replacementRefusals != 2 || retainedFile is null || !File.Exists(retainedFile) || File.Exists(selectedFile)
            || !File.Exists(otherFile) || Directory.EnumerateFileSystemEntries(otherDestination).Any())
            throw new Exception("Configured anchor redirection or directory replacement escaped the pinned retention scope: " + result.Error + "; stage=" + callbackStage + "; replacementRefusals=" + replacementRefusals);
        Console.WriteLine("PASS: configured redirected anchors remain supported; pinned roots resist replacement and alias swaps cannot redirect moves.");
    }

    private static Dictionary<string, string> Target() => new() { ["folder"] = "recent-automatic-destinations" };

    private static async Task CheckCreationRedirectionAsync(string root)
    {
        string source = Path.Combine(root, "creation-source");
        string outside = Path.Combine(root, "creation-unselected");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outside);
        string sentinel = Path.Combine(source, "selected.txt");
        await File.WriteAllTextAsync(sentinel, "selected creation sentinel");
        string destination = Path.Combine(root, "creation-parent", "retained");
        bool redirected = false;
        void RedirectBeforeCreation(string path, Action create)
        {
            if (Path.GetFileName(path) == "retained")
            {
                (redirected, int error) = SetJunctionInPlace(Path.GetDirectoryName(path)!, outside);
                if (!redirected) throw new Exception("Creation-redirection fixture could not tag its owned parent: " + error);
            }
            create();
        }
        var result = await new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination, null, null, null, createDirectory: RedirectBeforeCreation)
            .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (!redirected || result.Dispatched || result.Succeeded || !File.Exists(sentinel) || Directory.EnumerateFileSystemEntries(outside).Any())
            throw new Exception("In-place parent redirection escaped directory creation: redirected=" + redirected + ", outsideEntries=" + Directory.EnumerateFileSystemEntries(outside).Count());
        Console.WriteLine("PASS: in-place parent redirection cannot create a directory outside the selected retention scope.");
    }

    private static async Task CheckInPlaceRedirectionAsync(string root)
    {
        string source = Path.Combine(root, "inplace-source");
        string destination = Path.Combine(root, "inplace-destination");
        string outside = Path.Combine(root, "inplace-unselected");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outside);
        string sentinel = Path.Combine(source, "selected.txt");
        await File.WriteAllTextAsync(sentinel, "selected file");
        bool redirected = false;
        int nativeError = 0;
        void RedirectBeforeMove(string from, string to, Action guardedMove)
        {
            (redirected, nativeError) = SetJunctionInPlace(Path.GetDirectoryName(to)!, outside);
            guardedMove();
        }
        var result = await new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination, null, null, null, RedirectBeforeMove)
            .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (Directory.EnumerateFileSystemEntries(outside).Any())
            throw new Exception("In-place directory redirection escaped the pinned destination: redirected=" + redirected + ", nativeError=" + nativeError + ", succeeded=" + result.Succeeded);
        if (!redirected)
        {
            if (nativeError != 32 || !result.Succeeded || File.Exists(sentinel))
                throw new Exception("The in-place redirection fixture neither changed the tag nor proved handle protection: error=" + nativeError);
            Console.WriteLine("PASS: pinned handles rejected the in-place reparse request with a sharing violation; normal retention stayed in scope.");
        }
        else
        {
            if (result.Succeeded || !File.Exists(sentinel)) throw new Exception("A changed destination reparse tag did not stop retention.");
            Console.WriteLine("PASS: in-place destination reparse change succeeded in the fixture and the guarded move refused it without changing unselected files.");
        }
    }

    private static async Task CheckSourceFileRedirectionAsync(string root)
    {
        string source = Path.Combine(root, "file-redirection-source");
        string destination = Path.Combine(root, "file-redirection-retained");
        string outside = Path.Combine(root, "file-redirection-unselected.txt");
        Directory.CreateDirectory(source);
        string selected = Path.Combine(source, "selected.txt");
        await File.WriteAllTextAsync(selected, "selected synthetic bytes");
        await File.WriteAllTextAsync(outside, "unselected synthetic bytes");
        bool replaced = false;
        void ReplaceSourceWithLink(string from, string to, Action guardedMove)
        {
            File.Delete(from);
            File.CreateSymbolicLink(from, outside);
            replaced = true;
            guardedMove();
        }
        var result = await new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination, null, null, null, ReplaceSourceWithLink)
            .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
        if (!replaced || result.Succeeded || !result.Raw.Contains("moved 0, failed 1") || !File.Exists(selected)
            || await File.ReadAllTextAsync(outside) != "unselected synthetic bytes" || Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Any())
            throw new Exception("A source file replaced by a symbolic link escaped retention's no-follow boundary.");
        Console.WriteLine("PASS: source-file symbolic-link replacement is refused and unselected file bytes stay intact.");
    }

    private static async Task CheckCrossVolumeRefusalAsync(string root, string secondVolumeRoot)
    {
        // This opt-in fixture uses an existing lab-owned root. It never creates or mounts a volume.
        string secondAnchor = Path.GetFullPath(secondVolumeRoot);
        if (!Directory.Exists(secondAnchor)) throw new Exception("The second-volume fixture root does not exist.");
        string destination = Path.Combine(secondAnchor, "KillerBenchRetentionScope-" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "cross-volume-source");
        Directory.CreateDirectory(source);
        string sentinel = Path.Combine(source, "selected.txt");
        await File.WriteAllTextAsync(sentinel, "cross-volume synthetic bytes");
        try
        {
            var result = await new WindowsRetainFolderFilesAction(_ => (source, "*"), () => destination, null, null, () => secondAnchor)
                .ExecuteAsync(Target(), TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (result.Succeeded || result.Error?.Contains("different volumes") != true || !result.Raw.Contains("moved 0, failed 1")
                || !File.Exists(sentinel) || await File.ReadAllTextAsync(sentinel) != "cross-volume synthetic bytes"
                || Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Any())
                throw new Exception("The cross-volume fixture did not prove fail-closed retention without a copy fallback: " + result.Error);
            Console.WriteLine("PASS: cross-volume retention refused before moving bytes; the same-volume limitation remains explicit.");
        }
        finally { if (Directory.Exists(destination)) Directory.Delete(destination, true); }
    }

    private static (bool Success, int Error) SetJunctionInPlace(string directory, string target)
    {
        using var handle = CreateFileW(directory, 0x40000000, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
        if (handle.IsInvalid) return (false, Marshal.GetLastWin32Error());
        byte[] substitute = Encoding.Unicode.GetBytes(@"\??\" + target);
        byte[] display = Encoding.Unicode.GetBytes(target);
        byte[] buffer = new byte[16 + substitute.Length + 2 + display.Length + 2];
        BitConverter.GetBytes(0xA0000003u).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)(buffer.Length - 8)).CopyTo(buffer, 4);
        BitConverter.GetBytes((ushort)substitute.Length).CopyTo(buffer, 10);
        BitConverter.GetBytes((ushort)(substitute.Length + 2)).CopyTo(buffer, 12);
        BitConverter.GetBytes((ushort)display.Length).CopyTo(buffer, 14);
        substitute.CopyTo(buffer, 16);
        display.CopyTo(buffer, 16 + substitute.Length + 2);
        bool success = DeviceIoControl(handle, 0x900A4, buffer, (uint)buffer.Length, IntPtr.Zero, 0, out _, IntPtr.Zero);
        return (success, success ? 0 : Marshal.GetLastWin32Error());
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(SafeFileHandle handle, uint code, byte[] input, uint inputSize, IntPtr output, uint outputSize, out uint returned, IntPtr overlapped);

    private static void RemoveJunctions(string root)
    {
        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) Directory.Delete(directory);
            else RemoveJunctions(directory);
        }
    }

    private static async Task CreateJunctionAsync(string link, string target)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path $env:KB_RETENTION_LINK -Target $env:KB_RETENTION_TARGET | Out-Null" }) start.ArgumentList.Add(argument);
        start.Environment["KB_RETENTION_LINK"] = link;
        start.Environment["KB_RETENTION_TARGET"] = target;
        using var process = Process.Start(start) ?? throw new Exception("Junction fixture did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new Exception("Junction fixture timed out."); }
        await output;
        if (process.ExitCode != 0) throw new Exception("Junction fixture failed: " + await error);
    }
}
