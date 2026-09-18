using System.Text;

namespace KillerBench.Engine;

public sealed record TextEvidence(string Source, string[] Lines, bool Truncated)
{
    public const int MaximumCharacters = 1_000_000;

    public static async Task<TextEvidence> ReadAsync(string path, CancellationToken cancellationToken)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[MaximumCharacters + 1];
        int count = 0;
        while (count < buffer.Length)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(count), cancellationToken);
            if (read == 0) break;
            count += read;
        }
        var text = new string(buffer, 0, Math.Min(count, MaximumCharacters));
        if (text.Contains('\0')) throw new InvalidDataException("This viewer accepts text logs, not binary files or EVTX containers.");
        return new TextEvidence(Path.GetFullPath(path), text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'),
            count > MaximumCharacters);
    }

    public static async Task<TextEvidence> ReadTailAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.RandomAccess);
        long snapshotLength = stream.Length;
        byte[] prefix = new byte[4];
        int prefixCount = await stream.ReadAsync(prefix, cancellationToken).ConfigureAwait(false);
        var (encoding, preamble, alignment) = DetectEncoding(prefix.AsSpan(0, prefixCount));
        long byteWindow = Math.Min(snapshotLength, (long)alignment * (MaximumCharacters + 1));
        long start = Math.Max(preamble, snapshotLength - byteWindow);
        if (alignment > 1)
        {
            long remainder = (start - preamble) % alignment;
            if (remainder != 0) start += alignment - remainder;
        }
        stream.Seek(start, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, encoding, false, 4096, leaveOpen: true);
        var buffer = new char[MaximumCharacters + 1];
        int count = 0;
        while (count < buffer.Length)
        {
            int read = await reader.ReadAsync(buffer.AsMemory(count), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }
        string text = new(buffer, 0, count);
        bool truncated = start > preamble || count > MaximumCharacters || stream.Position < snapshotLength;
        if (count > MaximumCharacters) text = text[^MaximumCharacters..];
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (truncated)
        {
            int firstBreak = text.IndexOf('\n');
            text = firstBreak >= 0 ? text[(firstBreak + 1)..] : "";
        }
        if (text.Contains('\0')) throw new InvalidDataException("This viewer accepts text logs, not binary files or EVTX containers.");
        return new(fullPath, text.Split('\n'), truncated);
    }

    private static (Encoding Encoding, int Preamble, int Alignment) DetectEncoding(ReadOnlySpan<byte> prefix)
    {
        if (prefix.StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 })) return (Encoding.UTF32, 4, 4);
        if (prefix.StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF })) return (new UTF32Encoding(true, true), 4, 4);
        if (prefix.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) return (new UTF8Encoding(false, false), 3, 1);
        if (prefix.StartsWith(new byte[] { 0xFF, 0xFE })) return (Encoding.Unicode, 2, 2);
        if (prefix.StartsWith(new byte[] { 0xFE, 0xFF })) return (Encoding.BigEndianUnicode, 2, 2);
        return (new UTF8Encoding(false, false), 0, 1);
    }

    public string Excerpt(string query, out int matches)
    {
        const int maximumDisplayedLines = 2000;
        var result = new StringBuilder();
        matches = 0;
        for (int i = 0; i < Lines.Length; i++)
        {
            if (query.Length != 0 && !Lines[i].Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            matches++;
            if (matches <= maximumDisplayedLines)
                result.Append(i + 1).Append("  ").AppendLine(Lines[i]);
        }
        if (matches > maximumDisplayedLines)
            result.AppendLine("[Display limited to 2,000 matching lines. Refine the search.]");
        return result.ToString();
    }
}
