namespace KillerBench.Engine.Services;

public sealed record ServiceDependencyNode(int? Startup, IReadOnlyList<string> Dependencies, bool Complete, bool DelayedAutoStart = false);
public sealed record ServiceDependencyGraphResult(IReadOnlyList<ServiceRecovery.Dependency> Dependencies, bool Complete)
{
    public IReadOnlyList<string> Issues { get; init; } = [];
    public int? TargetStartup { get; init; }
    public bool TargetDelayedAutoStart { get; init; }
    public bool DelayedStartWindowElapsed { get; init; } = true;
}

public static class ServiceDependencyGraph
{
    public static ServiceDependencyGraphResult Read(string target, Func<string, ServiceDependencyNode> read, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(read);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dependencies = new List<ServiceRecovery.Dependency>();
        bool complete = true;
        int? targetStartup = null;
        bool targetDelayedAutoStart = false;
        var issues = new List<string>();
        void Incomplete(string detail)
        {
            complete = false;
            if (issues.Count < 16) issues.Add(detail);
        }
        void Visit(string name, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 256 || name != name.Trim() ||
                name.Any(c => char.IsControl(c) || c is '\\' or '/'))
            { Incomplete("Invalid dependency identity."); return; }
            if (depth > 16) { Incomplete("Dependency depth limit reached at " + name + "."); return; }
            if (active.Contains(name)) { Incomplete("Dependency cycle reaches " + name + "."); return; }
            if (visited.Contains(name)) return;
            if (visited.Count >= 65) { Incomplete("Dependency node limit reached."); return; }
            visited.Add(name);
            active.Add(name);
            try
            {
                var node = read(name);
                token.ThrowIfCancellationRequested();
                if (node is null || !node.Complete || node.Dependencies is null || node.Dependencies.Count > 64 ||
                    node.Startup is not (0 or 1 or 2 or 3 or 4)) { Incomplete("Incomplete or unsupported dependency configuration: " + name + "."); return; }
                if (depth == 0)
                {
                    targetStartup = node.Startup;
                    targetDelayedAutoStart = node.DelayedAutoStart;
                }
                else dependencies.Add(new(name, node.Startup));
                var edges = node.Dependencies.ToArray();
                if (edges.Distinct(StringComparer.OrdinalIgnoreCase).Count() != edges.Length) Incomplete("Duplicate dependency identities under " + name + ".");
                foreach (var dependency in edges) Visit(dependency, depth + 1);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { Incomplete("Dependency configuration could not be read: " + name + "."); }
            finally { active.Remove(name); }
        }
        Visit(target, 0);
        return new(dependencies.AsReadOnly(), complete)
        {
            Issues = issues.AsReadOnly(),
            TargetStartup = targetStartup,
            TargetDelayedAutoStart = targetDelayedAutoStart
        };
    }
}
