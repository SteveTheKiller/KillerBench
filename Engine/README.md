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

- [Getting started](../bench-landing/engine/getting-started.html)
- [Diagnostic workflows](../bench-landing/engine/workflows.html)
- [Evidence and results](../bench-landing/engine/evidence.html)
- [Repairs and safety](../bench-landing/engine/repairs.html)
- [CLI guide](../bench-landing/engine/cli.html)
- [API guide](../bench-landing/engine/api.html)
- [Architecture](../bench-landing/engine/architecture.html)
- [Knowledge packs](../bench-landing/engine/knowledge-packs.html)
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

KillerBench is licensed under GPLv3. See [LICENSE.txt](../LICENSE.txt) and the [licensing and release requirements](../LICENSING.md). A public Engine package and the complete public implementation remain pending.
