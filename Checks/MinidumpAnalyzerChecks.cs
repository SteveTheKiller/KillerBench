using System.Buffers.Binary;
using System.IO;
using System.Text;
using KillerBench.Engine;

internal static class MinidumpAnalyzerChecks
{
    internal static void Run(string scratch)
    {
        string folder = Path.Combine(scratch, "minidump-analyzer");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "fixture.dmp");
        byte[] fixture = BuildFixture();
        File.WriteAllBytes(path, fixture);
        var summary = MinidumpAnalyzer.AnalyzeFile(path);
        Require(summary is { Complete: true, Format: "Microsoft minidump", Architecture: "x64", WindowsVersion: "10.0.26200",
            ExceptionCode: "0xC0000005", FaultModule: "fixture.sys" } && summary.FileName == "fixture.dmp" &&
            !summary.Detail.Contains(folder, StringComparison.OrdinalIgnoreCase),
            "Valid minidump structure exposes bounded exception and module-basename evidence without full paths");
        var finding = MinidumpAnalyzer.Interpret(summary);
        Require(finding is { State: CheckState.Information, Repair: null, Evidence: not null } &&
            finding.Detail.Contains("Exception 0xC0000005", StringComparison.Ordinal) &&
            finding.Detail.Contains("Memory pages, registers, stack frames, symbols", StringComparison.Ordinal),
            "Minidump interpretation remains diagnostic and states its structural limits");

        string unsupported = Path.Combine(folder, "kernel.dmp");
        File.WriteAllBytes(unsupported, Encoding.ASCII.GetBytes("PAGEDUMP-FIXTURE-DATA-THAT-IS-LONG-ENOUGH"));
        Require(MinidumpAnalyzer.AnalyzeFile(unsupported) is { Complete: true, Format: "Unsupported", ExceptionCode: null },
            "Non-minidump crash formats are identified honestly without being parsed as minidumps");
        foreach (Action<byte[]> corrupt in new Action<byte[]>[]
        {
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 129),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), uint.MaxValue),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), uint.MaxValue),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(56), 7),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), 24),
            bytes => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(64), 68)
        })
        {
            byte[] bytes = BuildFixture();
            corrupt(bytes);
            string invalid = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".dmp");
            File.WriteAllBytes(invalid, bytes);
            var result = MinidumpAnalyzer.AnalyzeFile(invalid);
            bool safeBoundary = result.Detail.Contains("No memory page, symbol, stack, register context or system setting was read or changed.", StringComparison.Ordinal) ||
                result.Detail.Contains("Memory pages, registers, stack frames, symbols, command lines and full module paths were not read.", StringComparison.Ordinal);
            Require(!result.Complete && safeBoundary,
                "Malformed or duplicate minidump structures remain explicit incomplete evidence");
        }
        try { MinidumpAnalyzer.AnalyzeFile(path, new CancellationToken(true)); throw new Exception("Minidump cancellation ignored"); }
        catch (OperationCanceledException) { }
        Require(File.ReadAllBytes(path).SequenceEqual(fixture), "Minidump analysis does not modify its source file");
        Console.WriteLine("PASS: bounded minidump structure analysis retains exception identity and module basename without reading memory pages or changing files.");
    }

    private static byte[] BuildFixture()
    {
        byte[] name = Encoding.Unicode.GetBytes(@"C:\Windows\System32\drivers\fixture.sys");
        byte[] bytes = new byte[248 + name.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 0x504D444D);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x0000A793);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 3);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 32);
        WriteDirectory(bytes, 32, 7, 24, 68);
        WriteDirectory(bytes, 44, 6, 40, 92);
        WriteDirectory(bytes, 56, 4, 112, 132);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(68), 9);
        bytes[74] = 8;
        bytes[75] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(76), 10);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(84), 26200);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(100), 0xC0000005);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(116), 0x1010);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(132), 1);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(136), 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(144), 0x100);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(156), 244);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(244), (uint)name.Length);
        name.CopyTo(bytes.AsSpan(248));
        return bytes;
    }

    private static void WriteDirectory(byte[] bytes, int offset, uint type, uint size, uint rva)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), type);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), size);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), rva);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
