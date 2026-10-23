using System.IO;

internal static class CheckSource
{
    internal static string Find(string relativePath)
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                if (!File.Exists(Path.Combine(directory.FullName, "KillerBench.csproj"))) continue;
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
            }
        throw new FileNotFoundException("Run source checks from the KillerBench checkout or one of its subdirectories.", relativePath);
    }
}
