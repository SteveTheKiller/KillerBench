using System.Security.Cryptography;
using System.Reflection.PortableExecutable;

namespace KillerBench.Engine.Scheduling;

public sealed record ScheduledTaskExecutableResult(string Path, long? Bytes, string? Sha256, string Detail)
{
    public bool HasContentIdentity => Sha256 is not null;
    public bool? HasPeHeader { get; init; }
    public bool? IsDll { get; init; }
    public string? Machine { get; init; }
    public bool RequiresReview => !HasContentIdentity || HasPeHeader != true || IsDll != false;
    public string ImageReview => !HasContentIdentity ? "File contents could not be inspected completely." :
        HasPeHeader != true ? "No executable PE image was established. Review the selected program and its interpreter, if applicable." :
        IsDll != false ? "The selected image is a DLL or its image type is unknown. Review the task's executable selection." :
        "Executable image headers observed. Launchability, trust and account access remain unverified.";
}

public static class ScheduledTaskExecutable
{
    public const long MaximumBytes = 64 * 1024 * 1024;

    public static async Task<ScheduledTaskExecutableResult> InspectAsync(ScheduledTaskAction action, CancellationToken token = default)
    {
        action.Validate();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(15));
        stop.Token.ThrowIfCancellationRequested();
        try
        {
            await using var file = new FileStream(action.Executable, FileMode.Open, FileAccess.Read, FileShare.Read,
                65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            long length = file.Length;
            if (length == 0 || length > MaximumBytes)
                return new(action.Executable, length, null, "Empty file or file exceeds the 64 MiB inspection limit. No content identity established.");
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[65536];
            long total = 0;
            int read;
            while ((read = await file.ReadAsync(buffer, stop.Token).ConfigureAwait(false)) != 0)
            {
                total += read;
                if (total > MaximumBytes) return new(action.Executable, total, null, "File exceeded the inspection limit while reading.");
                hash.AppendData(buffer, 0, read);
            }
            if (total != length) return new(action.Executable, total, null, "File length changed during inspection.");
            bool hasPeHeader = false;
            bool? isDll = null;
            string? machine = null;
            try
            {
                stop.Token.ThrowIfCancellationRequested();
                file.Position = 0;
                using var image = new PEReader(file, PEStreamOptions.LeaveOpen);
                hasPeHeader = image.PEHeaders.PEHeader is not null;
                if (hasPeHeader)
                {
                    isDll = (image.PEHeaders.CoffHeader.Characteristics & Characteristics.Dll) != 0;
                    machine = image.PEHeaders.CoffHeader.Machine.ToString();
                }
            }
            catch (BadImageFormatException) { }
            stop.Token.ThrowIfCancellationRequested();
            return new(action.Executable, total, Convert.ToHexString(hash.GetHashAndReset()),
                (hasPeHeader ? $"PE headers report {machine}, DLL: {isDll}. " : "No readable PE image header. ") +
                "Content and header inspection only. Complete image validity, runtime compatibility, signature, account permissions and path stability are not verified. No file was executed.")
            { HasPeHeader = hasPeHeader, IsDll = isDll, Machine = machine };
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        { return new(action.Executable, null, null, "File inspection unavailable: " + error.Message); }
    }
}
