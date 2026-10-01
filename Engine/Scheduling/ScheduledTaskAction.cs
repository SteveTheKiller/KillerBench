namespace KillerBench.Engine.Scheduling;

public sealed record ScheduledTaskAction(string Executable, string Arguments, string? WorkingDirectory = null)
{
    public ScheduledTaskAction Validate()
    {
        ValidateLocalPath(Executable, nameof(Executable));
        if (Executable.EndsWith('\\')) throw new ArgumentException("Choose an executable file, not a directory path.", nameof(Executable));
        if (Arguments is null || Arguments.Length > 32767 || Arguments.Any(char.IsControl))
            throw new ArgumentException("Task arguments must be a single line of at most 32767 characters.", nameof(Arguments));
        if (WorkingDirectory is not null) ValidateLocalPath(WorkingDirectory, nameof(WorkingDirectory));
        return this;
    }

    private static void ValidateLocalPath(string path, string parameter)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length is < 3 or > 32767 || path != path.Trim() ||
            !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\' ||
            path.Any(char.IsControl) || path.IndexOfAny(['"', '*', '?', '<', '>', '|', '/']) >= 0 ||
            path[2..].Contains(':') || path.Contains("\\\\", StringComparison.Ordinal) ||
            path[3..].Split('\\', StringSplitOptions.RemoveEmptyEntries).Any(part => part is "." or ".." || part != part.Trim() || part.EndsWith('.')))
            throw new ArgumentException("Choose an absolute local path without quotes, wildcards or relative segments.", parameter);
    }
}
