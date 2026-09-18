# The KillerBench.Engine

The KillerBench.Engine is a Windows diagnostics and repair library for applications that need structured evidence instead of a collection of opaque scripts. It powers the KillerBench desktop and command-line tools, and it can be embedded in another .NET host.

## What it provides

- Bounded diagnostic workflows for Windows servicing, devices, storage, networking, printing, security, applications, profiles, and other system areas
- Typed evidence with explicit complete, incomplete, unknown, and historical states
- Repair recipes that require exact current evidence before they can run
- Durable journals for repair verification and interrupted-run recovery
- Progress callbacks for live displays such as servicing activity
- Text, JSON, HTML, and structured .NET results for reports and integrations
- Versioned knowledge packs for error codes, hardware identifiers, vendors, and support references

## Designed for traceability

A finding is tied to the evidence that produced it. Historical events stay historical. Missing data does not become a healthy verdict. A failed provider does not become an empty result. The Engine reports the boundary of what it inspected so a host can explain what is known and what remains unknown.

## Diagnostics first

Most Engine operations are read-only. Repair support is deliberately narrower than diagnostic coverage. A repair must have a supported recipe, fresh eligibility evidence, explicit host authorization, a durable preparation record, a bounded native action, and post-action verification.

## Current status

The Engine targets .NET 10 and Windows. It is in active development and is currently consumed through a project reference inside the KillerBench repository. A public package has not been released.

## Choose a guide

- [Getting started](getting-started.html) shows a first read-only workflow.
- [Diagnostic workflows](workflows.html) explains focused checks and the full system scan.
- [Evidence and results](evidence.html) explains states, journals, progress, and reports.
- [Repairs and safety](repairs.html) describes the evidence gates and recovery model.
- [CLI guide](cli.html) covers command discovery, text output, and JSON output.
- [API guide](api.html) maps the main public entry points.
- [Architecture](architecture.html) explains the Engine boundary and internal layers.
- [Knowledge packs](knowledge-packs.html) covers updateable reference data.
