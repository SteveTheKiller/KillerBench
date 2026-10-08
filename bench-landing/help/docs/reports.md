# Reports and evidence

Every run produces a report you can read, filter and export. The report always separates what was proven from what was only observed.

## States you will see

| State | Meaning |
| --- | --- |
| Needs review | The evidence supports a problem that deserves attention. |
| Information | Evidence was collected and is shown for context. |
| Incomplete or Unknown | A check could not finish or the evidence was unavailable. It is never counted as healthy. |
| Historical | An event from the past. It explains history but does not prove a current fault. |
| Verified | A repair or change was confirmed by reading the result back. |

## Raw output and the readable report

**Raw output** keeps the technical evidence with original line numbers so you can trace any finding back to its source. The **Readable report** summarizes the same run in plain language, with the next safe step first.

## Saved logs

KillerBench can also read saved Windows logs, such as CBS and Setup logs, without running anything on the machine. Findings link to the exact line that produced them. **Load sample** in Session sources opens a synthetic example you can explore safely.

## Exporting

**Export report** saves the report with its source paths, parsed entries and evidence excerpts, for review before attaching it to a ticket. Raw logs and evidence excerpts can contain computer names, accounts, paths or sensitive source text. Guarded collectors restrict what they retain, but imported evidence is not guaranteed to be redacted. Inspect and remove sensitive information before sharing.
