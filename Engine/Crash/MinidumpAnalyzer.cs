using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace KillerBench.Engine;

public sealed record MinidumpSummary(string FileName, string Format, string? Architecture, string? WindowsVersion,
    string? ExceptionCode, string? FaultModule, bool Complete, string Detail);

public static class MinidumpAnalyzer
{
    private const uint Signature = 0x504D444D;
    private const uint ModuleListStream = 4;
    private const uint ExceptionStream = 6;
    private const uint SystemInfoStream = 7;
    private const int MaximumStreams = 128;
    private const int MaximumModules = 4096;
    private const int ModuleSize = 108;

    public static async Task<WorkflowCheck> CheckAsync(string path, CancellationToken token = default)
        => Interpret(await AnalyzeFileAsync(path, token).ConfigureAwait(false));

    public static async Task<MinidumpSummary> AnalyzeFileAsync(string path, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Select one local dump file.", nameof(path));
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Minidump analysis requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            return await Task.Run(() => AnalyzeFile(path, deadline.Token), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            string fileName;
            try { fileName = Path.GetFileName(path); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) { fileName = "selected dump"; }
            return Unknown(string.IsNullOrWhiteSpace(fileName) ? "selected dump" : fileName,
                "Minidump structure analysis exceeded its 20-second deadline.");
        }
    }

    public static MinidumpSummary AnalyzeFile(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        string fileName;
        try { fileName = Path.GetFileName(path); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        { return Unknown("selected dump", "The selected dump path is invalid."); }
        try { return AnalyzeFileCore(path, token); }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is InvalidDataException or EndOfStreamException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        { return Unknown(string.IsNullOrWhiteSpace(fileName) ? "selected dump" : fileName, "The dump structure or file could not be read completely: " + error.GetType().Name + "."); }
    }

    private static MinidumpSummary AnalyzeFileCore(string path, CancellationToken token)
    {
        string full = Path.GetFullPath(path);
        if (!CrashDumpInventory.LocalUnlinked(full, File.GetAttributes, drive => new DriveInfo(drive).DriveType, token))
            return Unknown(Path.GetFileName(full), "The selected path is not a fixed local unlinked file.");
        var info = new FileInfo(full);
        if (!info.Exists || (info.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            return Unknown(info.Name, "The selected dump file is unavailable or linked.");
        using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.RandomAccess);
        if (stream.Length < 32) return Unknown(info.Name, "The file is too small to contain a minidump header.");
        Span<byte> header = stackalloc byte[32];
        ReadExact(stream, 0, header, token);
        if (BinaryPrimitives.ReadUInt32LittleEndian(header) != Signature)
            return new(info.Name, "Unsupported", null, null, null, null, true,
                "The file is not a Microsoft minidump. Full kernel, active-memory and other dump formats require an external debugger. No memory page, symbol or stack data was read.");
        uint streamCount = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        uint directoryRva = BinaryPrimitives.ReadUInt32LittleEndian(header[12..]);
        if (streamCount == 0 || streamCount > MaximumStreams || !Range(directoryRva, checked((long)streamCount * 12), stream.Length))
            return Unknown(info.Name, "The minidump stream directory is missing, excessive or outside the file.");
        var directories = new Dictionary<uint, (uint Size, uint Rva)>();
        byte[] directoryEntry = new byte[12];
        for (int index = 0; index < streamCount; index++)
        {
            token.ThrowIfCancellationRequested();
            Span<byte> entry = directoryEntry;
            ReadExact(stream, directoryRva + index * 12L, entry, token);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(entry);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]);
            uint rva = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            if (!Range(rva, size, stream.Length) || !directories.TryAdd(type, (size, rva)))
                return Unknown(info.Name, "The minidump contains an invalid or duplicate stream directory entry.");
        }
        string? architecture = null;
        string? windowsVersion = null;
        if (directories.TryGetValue(SystemInfoStream, out var system))
        {
            if (system.Size < 24) return Unknown(info.Name, "The minidump system-information stream is incomplete.");
            Span<byte> data = stackalloc byte[24];
            ReadExact(stream, system.Rva, data, token);
            architecture = Architecture(BinaryPrimitives.ReadUInt16LittleEndian(data));
            uint major = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
            uint minor = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
            uint build = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
            windowsVersion = major <= 99 && minor <= 99 && build <= 999999 ? $"{major}.{minor}.{build}" : null;
        }
        string? exceptionCode = null;
        ulong exceptionAddress = 0;
        if (directories.TryGetValue(ExceptionStream, out var exception))
        {
            if (exception.Size < 40) return Unknown(info.Name, "The minidump exception stream is incomplete.");
            Span<byte> data = stackalloc byte[40];
            ReadExact(stream, exception.Rva, data, token);
            exceptionCode = "0x" + BinaryPrimitives.ReadUInt32LittleEndian(data[8..]).ToString("X8", CultureInfo.InvariantCulture);
            exceptionAddress = BinaryPrimitives.ReadUInt64LittleEndian(data[24..]);
        }
        string? faultModule = null;
        if (exceptionAddress != 0 && directories.TryGetValue(ModuleListStream, out var modules))
            faultModule = ReadFaultModule(stream, modules, exceptionAddress, token, out bool modulesComplete, out string? moduleError)
                ?? (modulesComplete ? null : throw new InvalidDataException(moduleError));
        bool complete = architecture is not null && windowsVersion is not null && exceptionCode is not null;
        string detail = "Validated Microsoft minidump structure. " +
            (exceptionCode is null ? "No exception stream was present. " : $"Exception {exceptionCode}. ") +
            (faultModule is null ? "No containing module basename was resolved. " : $"Containing module: {faultModule}. ") +
            "Only header, directory, system-information, exception and module-range metadata was read. Memory pages, registers, stack frames, symbols, command lines and full module paths were not read. This evidence does not establish root cause or authorize repair.";
        return new(info.Name, "Microsoft minidump", architecture, windowsVersion, exceptionCode, faultModule, complete, detail);
    }

    public static WorkflowCheck Interpret(MinidumpSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        string facts = $"File: {summary.FileName}. Format: {summary.Format}." +
            (summary.Architecture is null ? "" : $" Architecture: {summary.Architecture}.") +
            (summary.WindowsVersion is null ? "" : $" Windows: {summary.WindowsVersion}.");
        return new("Minidump structure", summary.Complete ? CheckState.Information : CheckState.Unknown,
            facts + " " + summary.Detail,
            Evidence: new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(summary, Capture.JsonOptions), "", false));
    }

    private static string? ReadFaultModule(FileStream stream, (uint Size, uint Rva) location, ulong address,
        CancellationToken token, out bool complete, out string? error)
    {
        complete = false;
        error = null;
        if (location.Size < 4) { error = "The module list is incomplete."; return null; }
        Span<byte> countBytes = stackalloc byte[4];
        ReadExact(stream, location.Rva, countBytes, token);
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(countBytes);
        if (count > MaximumModules || 4L + count * ModuleSize > location.Size) { error = "The module list is excessive or incomplete."; return null; }
        string? match = null;
        byte[] moduleEntry = new byte[24];
        for (int index = 0; index < count; index++)
        {
            token.ThrowIfCancellationRequested();
            Span<byte> module = moduleEntry;
            ReadExact(stream, location.Rva + 4L + index * ModuleSize, module, token);
            ulong start = BinaryPrimitives.ReadUInt64LittleEndian(module);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(module[8..]);
            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(module[20..]);
            ulong end = start + size;
            if (end < start) { error = "A module address range overflowed."; return null; }
            if (address >= start && address < end)
            {
                string? name = ReadName(stream, nameRva, token);
                if (name is null || match is not null) { error = "The containing module identity is invalid or ambiguous."; return null; }
                match = name;
            }
        }
        complete = true;
        return match;
    }

    private static string? ReadName(FileStream stream, uint rva, CancellationToken token)
    {
        Span<byte> lengthBytes = stackalloc byte[4];
        if (!Range(rva, 4, stream.Length)) return null;
        ReadExact(stream, rva, lengthBytes, token);
        uint length = BinaryPrimitives.ReadUInt32LittleEndian(lengthBytes);
        if (length == 0 || length > 4096 || (length & 1) != 0 || !Range(rva + 4L, length, stream.Length)) return null;
        byte[] bytes = new byte[length];
        ReadExact(stream, rva + 4L, bytes, token);
        string value = Encoding.Unicode.GetString(bytes);
        if (value.Any(char.IsControl)) return null;
        string name = value.Replace('/', '\\').Split('\\').LastOrDefault() ?? "";
        return string.IsNullOrWhiteSpace(name) || name.Length > 260 ? null : name;
    }

    private static string Architecture(ushort value) => value switch
    {
        0 => "x86", 5 => "ARM", 6 => "IA64", 9 => "x64", 12 => "ARM64", _ => "Unknown (" + value + ")"
    };

    private static void ReadExact(FileStream stream, long offset, Span<byte> destination, CancellationToken token)
    {
        if (!Range(offset, destination.Length, stream.Length)) throw new InvalidDataException("A minidump field points outside the file.");
        stream.Position = offset;
        int read = 0;
        while (read < destination.Length)
        {
            token.ThrowIfCancellationRequested();
            int count = stream.Read(destination[read..]);
            if (count == 0) throw new EndOfStreamException();
            read += count;
        }
    }

    private static bool Range(long offset, long length, long fileLength) => offset >= 0 && length >= 0 && offset <= fileLength && length <= fileLength - offset;
    private static MinidumpSummary Unknown(string fileName, string detail) => new(fileName, "Unknown", null, null, null, null, false,
        detail + " No memory page, symbol, stack, register context or system setting was read or changed.");
}
