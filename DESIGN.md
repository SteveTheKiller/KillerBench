# KillerBench

Windows Diagnostic Workbench

Status: initial design and implementation proposal. The name is a working product name; availability has not been checked.

## Purpose

Turn repeatable Windows troubleshooting into an evidence-based local workflow without requiring AI. Collect or open diagnostic evidence, build a timeline, evaluate explicit rules, explain findings, and eventually offer selected repairs with verification.

The application runs on demand. It does not require a continuously installed monitoring agent or an external service.

## First useful investigation

Windows update and upgrade failures are the proposed first investigation. Sources include exported Windows events, CBS text logs, and Panther setup logs. Begin with opening saved evidence and a bounded read-only local collection. Additional investigation types can follow once the first is validated against real captures.

A scan is not a promise to find every problem. An unavailable source, incomplete capture, or failed parser must remain visible. No findings means no supported rule matched the available evidence, not that the machine is healthy.

## Instrument panel

Use cassette futurism: recessed displays, smoked surfaces, tactile controls, small labeled status lights, and clear typography. Adapt the KillerUI reference into app-owned resources and controls. Do not add a build or runtime dependency on the sibling KillerUI directory.

Suggested layout:

1. Session strip: machine, OS, investigation, elevation, capture time, and coverage.
2. Control bank: Scan, Stop, Open Capture, and Export Report.
3. Collection console: bounded, selectable live output and collector status.
4. Findings display: structured findings with severity and certainty shown separately.
5. Evidence display: source excerpts, event properties, timeline, or comparison for the selected finding.
6. Family footer: concise session status and progress.

The displays are views of structured data, not separate command shells. Console presentation must not require executing arbitrary text.

## Console appearance

Use one smoked-glass console pane that follows the active application theme. Keep raw operational output separate from the readable workflow report.

- Keep the glass and shadow treatment inside the console surface.
- Preserve crisp text selection, copying, searching, and scrolling.
- Provide readable scaling.
- Do not animate flicker, distort text geometry, or delay real output to imitate old hardware.
- Show severity through labels and symbols as well as color.
- Use reports to lead with specific next actions and limit the visible triage list without losing exported detail.

Use semantic product resources for console colors and effects without redefining shared family tokens. Preserve F1 for shortcuts and F12 for About. Use one binding table for shortcuts and their displayed labels.

## Diagnostic engine

Keep the engine independent of WPF. Use platform-specific collectors behind interfaces and parsers that can operate on saved evidence.

Pipeline: collect or import, preserve source metadata, parse, normalize time, evaluate rules, present findings.

Each evidence record retains its source, location or event identity, original timestamp, time-zone information when available, and raw excerpt. Unknown time zones and ambiguous timestamps must remain explicit.

Each finding includes a stable rule ID and version, title, severity, certainty, evidence references, explanation, limitations, and supported next steps. A log error or temporal coincidence alone must not be treated as a confirmed root cause.

Capture sessions track per-source outcomes: collected, unavailable, access denied, failed, canceled, or truncated. Bound collection duration, input sizes, displayed output, and memory use. Preserve partial results on cancellation.

## Repairs

The first increment has no executable repairs. Add repairs individually after diagnostic rules are validated.

Each supported repair needs prerequisites, a concrete preview of its actions and effects, required elevation, cancellation behavior, an execution record, and a post-repair check. Describe rollback only when a real rollback exists. Scanning never silently applies a repair.

## Initial implementation sequence

1. Create a Windows WPF shell and a separate diagnostic core, using app-owned family resources.
2. Implement the instrument layout and theme-aware console output with clearly labeled sample data.
3. Open saved text evidence and provide source selection, search, and line references.
4. Add source-specific parsers and meaningful tests using synthetic or explicitly approved sanitized fixtures.
5. Add bounded read-only collectors and cancellation, requesting elevation only for operations that need it.
6. Validate a small set of diagnostic rules against positive, negative, incomplete, and unrelated-error cases.
7. Export a capture and reopen it with the same evidence and findings.
8. Add selected repairs as separately verified features.

Do not present sample output as a scan of the current machine. Do not commit real customer captures or machine-specific logs as fixtures. Export must explain which evidence is included.

## First milestone acceptance

- The shell builds and opens with the intended family conventions.
- Console appearance can change without altering the rest of the app theme.
- Text remains readable and selectable with effects enabled or disabled.
- A saved text log can be opened, searched, and inspected with stable source references.
- Sample mode, live collection, and imported evidence are unambiguous.
- UI output and parsing remain bounded for large input.

## Future possibilities

Additional investigation modules, an application black box viewer, and benchmarking may fit the workbench later. They are outside the first milestone. A custom database is not a prerequisite for proving the diagnostic workflow.
