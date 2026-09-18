# Saved workflow review

`WorkflowJournalReview.ReadAsync(path, token)` reads a workflow journal or an exported review. It returns the recorded machine, workflow kind, status, timestamp, check and repair counts, and the retained JSON record. The returned source is the file actually opened. Paths inside the record are not followed.

The desktop exposes this through **Open workflow** in advanced controls. **Export report** saves the historical review, which can be reopened. Filtering and switching these sources does not rerun diagnostic rules against their text.

The CLI command is `killerbench-cli workflow-journal FILE.json`. Exit 0 means the file was read successfully, including when the recorded workflow was running or stopped. It does not mean the machine is healthy or the recorded repair succeeded. Exit 1 indicates an unreadable or invalid file, 2 invalid arguments, and 130 cancellation or deadline expiration.

The reader has a 16 MiB input limit, a 15-second read deadline and a JSON depth limit of 32. It validates the root metadata and required arrays. Nested check and repair contents remain historical claims, not validated repair authority. A review wrapper cannot override metadata in the retained record.

No diagnostics, repairs, retries or journal reconciliation run during review. Engine and CLI fixtures cover malformed inputs, round trips, source preservation, oversized input and cancellation. Desktop picker, filtering, export and Stop/close interactions still require hands-on validation.
