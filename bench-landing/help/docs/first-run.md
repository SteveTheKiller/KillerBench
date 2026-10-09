# Your first diagnosis

Start with [Download and setup](setup.html), then run a Full system scan. It collects diagnostic evidence and saves a local journal; it does not run repairs. A finding describes the inspected scope, not every possible fault on the computer.

## Run the scan

1. Open KillerBench. The **Diagnostics** section opens with **Full system scan** selected.
2. Leave **Auto-repair confirmed issues** turned off for your first run.
3. Press **Run diagnosis**. Progress appears in **Raw output** on the right while each check runs.
4. When it finishes, the output pane switches to the **Readable report**.

Start without elevation. Some Windows evidence, such as BitLocker protectors and protected services, needs administrator rights. Those checks report unavailable evidence under a standard account. If you need that evidence, finish the current run, keep the report, then deliberately reopen KillerBench with **Run as administrator**. Elevation does not select or approve a repair.

## Read the report

The report starts with counts for **Needs review**, **Verified repairs**, **Incomplete** and **Checked**, then a recommended next step.

- **What needs attention** lists problems the evidence supports, most important first.
- **Collected information** shows what was read, including evidence that was incomplete or unavailable.
- Historical events are labeled as history. An error from last month is not treated as a current fault.

Missing evidence never counts as healthy. If a check could not run, the report says so.

## Choose checks yourself

Open **Choose checks (advanced)** to pick individual checks instead of the full scan. Each section in the app also has focused tools, described in the **Sections** pages of this guide.

## Fix what you found

When the report recommends a fix, it names the section and tool to open. Read [How changes stay safe](safety.html) before your first repair. Native validation is still in progress; a successful diagnosis does not establish that every offered repair has been tested on your Windows build. Keep backups and review the exact target and consequences before any change.
