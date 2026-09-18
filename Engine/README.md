# The KillerBench.Engine

The KillerBench.Engine is the reusable Windows diagnostics and repair library behind KillerBench. It provides bounded evidence collection, deterministic analysis, guarded repair workflows, durable recovery records, and structured results for desktop, CLI, and embedded hosts.

## Facts

- Target: .NET 10
- Platform: Windows
- Namespace: `KillerBench.Engine`
- UI dependency: none
- Host surfaces: KillerBench desktop, `killerbench-cli`, and other .NET applications
- Default behavior: read-only diagnostics
- Repair behavior: explicit, evidence-gated, journaled, and verified
- Result formats: typed .NET records plus CLI text or JSON
- Status: in development

## Start here

- [Getting started](../bench-landing/engine/docs/getting-started.md)
- [Diagnostic workflows](../bench-landing/engine/docs/workflows.md)
- [Evidence and results](../bench-landing/engine/docs/evidence.md)
- [Repairs and safety](../bench-landing/engine/docs/repairs.md)
- [CLI guide](../bench-landing/engine/docs/cli.md)
- [API guide](../bench-landing/engine/docs/api.md)
- [Architecture](../bench-landing/engine/docs/architecture.md)
- [Knowledge packs](../bench-landing/engine/docs/knowledge-packs.md)
- [Detailed capability catalog](docs/CAPABILITY-CATALOG.md)

## Build

```powershell
dotnet build KillerBench.Engine.csproj
```

Reference the Engine directly from another .NET 10 project:

```xml
<ProjectReference Include="path\to\KillerBench\Engine\KillerBench.Engine.csproj" />
```

The Engine is not yet published as a package. Pin a repository revision when embedding it outside the KillerBench solution.

## License

KillerBench is licensed under GPLv3.
