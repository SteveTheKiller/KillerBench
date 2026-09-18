namespace KillerBench.Engine;

public sealed record ScheduledTaskIdentity
{
    public string Path { get; }
    public string Name { get; }
    public string FullName => Path + Name;

    public ScheduledTaskIdentity(string path, string name)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1024 || path[0] != '\\' || path[^1] != '\\' ||
            path.Any(char.IsControl) || path.Contains('/') || path.Contains("\\\\", StringComparison.Ordinal) ||
            path.Split('\\', StringSplitOptions.RemoveEmptyEntries).Any(part => part is "." or ".." || part != part.Trim() || part.IndexOfAny(['*', '?', ':', '"', '<', '>', '|']) >= 0))
            throw new ArgumentException("Choose an absolute task folder ending in a backslash.", nameof(path));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 238 || name != name.Trim() || name is "." or ".." ||
            name.Any(char.IsControl) || name.IndexOfAny(['\\', '/', '*', '?', ':', '"', '<', '>', '|']) >= 0)
            throw new ArgumentException("Choose an exact task name without folder separators or wildcards.", nameof(name));
        Path = path;
        Name = name;
    }

    public bool Matches(ScheduledTaskIdentity other) => other is not null &&
        string.Equals(FullName, other.FullName, StringComparison.OrdinalIgnoreCase);
}
