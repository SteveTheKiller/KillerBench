# Evidence and results

The Engine separates provider output, interpreted checks, workflow results, and human-readable reports. Each layer keeps uncertainty visible.

## Provider evidence

Native commands and Windows providers run behind fixed time, record, and size bounds. `ToolResult` records exit status, timeout, truncation, standard output, and error output. A provider failure or truncated result cannot be treated as a clean empty response.

Collectors minimize retained data. Many diagnostic results keep names, categories, counts, states, and timestamps while excluding credentials, command lines, full paths, account identities, network endpoints, and unrelated event payloads.

## Workflow checks

`WorkflowCheck` adds a name, interpreted state, explanation, optional repair kind, and optional provider evidence. Consumers should display the state and detail together. The state alone does not communicate the scope or limitations of the check.

## Workflow results

`WorkflowResult` contains:

- The workflow kind and machine
- Start time and final status
- Every completed check
- Any repair results
- Recipe results
- The saved journal path

Use the typed result for UI decisions. Use reports for people and JSON for interoperable automation.

## Reports

KillerBench can generate diagnostic-only or diagnostic-and-repair reports. The report separates:

1. What was inspected
2. What problem was found
3. What evidence supports the finding
4. What action was proposed or performed
5. How the result was verified
6. What remains unknown or needs a restart

HTML reports use the Killer Tools theme system and remain readable without the desktop app. Text reports favor portability. JSON retains structured fields for another program.

## Historical evidence

Event records and imported logs describe what was recorded at a past time. They can identify useful patterns, error codes, and the next diagnostic scope. They do not prove that the fault is still active.

## Unknown is a result

`Unknown` is not an exception to hide. It tells the host that access was denied, a source was missing, a provider failed, output was malformed, the bound was exceeded, or evidence disagreed. A useful host shows this limitation and offers the relevant next step.
