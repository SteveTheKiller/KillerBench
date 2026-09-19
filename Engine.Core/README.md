# The KillerBench.Engine.Core

The platform-neutral core of The KillerBench.Engine. It has no Windows dependency and no reference to the existing Engine. It defines the declarative recipe model, the primitive library contract that each platform implements, the expectation evaluator, the recipe runner and the journal contract.

## Facts

- Target: .NET 10, no package references, warnings are errors
- Namespace: `KillerBench.Engine.Core`
- Platform code: none. Platforms register `IProbePrimitive` and `IActionPrimitive` implementations in a `PrimitiveLibrary`.
- Default behavior: detect only. An action runs only when the caller passes `AuthorizeRepair`.

## Recipe packs

A pack is UTF-8 JSON (`RecipePackJson`) holding one platform's recipes. Each recipe declares:

- `Detect`: a probe (primitive plus parameters) and an expectation that maps the observed value to Clear, Repairable, Attention, Information or Unknown.
- `Preconditions`: probes that must all be Clear before the action runs.
- `Action`: one whitelisted action primitive. Absent means diagnostic only.
- `Verify`: the probe evaluated after the action. Absent means Detect is reused.
- `Rollback`: an undo action, present only when a real undo exists.
- `Risk`, `RequiresElevation`, `RequiresExplicitOptions`, `MayRequireRestart`, timeouts, `VerificationScope` and `Limitations`.

`RecipePackValidation` rejects malformed ids, duplicate ids, dependency cycles, unknown operators, invalid regular expressions, unknown primitives, parameters outside a primitive's declared pattern, and recipes whose declared risk or elevation is lower than the primitive they call.

## Runner

`RecipeRunner` runs one recipe: detect; when authorized, check preconditions, detect again immediately before the change, journal the prepared change, dispatch the action once, verify and report. Incomplete, timed-out or truncated evidence is Unknown. A dispatched change whose verification does not confirm the expected state is reported with `ExecutionUncertain`. The runner never retries and never elevates.

## Relationship to the Engine

The existing `KillerBench.Engine` keeps its workflows and recipes unchanged. It references Core so its Windows collectors can be exposed as primitives and its bespoke recipes can be expressed as packs over time. See `Engine/docs/PLATFORM-BOUNDARY.md`.
