# Your first diagnosis

A full system scan is the fastest way to learn what is wrong with a computer. It only reads, so it is safe to run on any machine.

## Run the scan

1. Open KillerBench. The **Diagnostics** section opens with **Full system scan** selected.
2. Leave **Auto-repair confirmed issues** turned off for your first run.
3. Press **Run diagnosis**. Progress appears in the console while each check runs.
4. When it finishes, KillerBench switches to the **Readable report**.

Run KillerBench as administrator when you can. Some Windows evidence, such as BitLocker protectors and protected services, is only readable with administrator rights. Without them those checks report that the evidence was unavailable instead of guessing.

## Read the report

The report starts with counts for **Needs review**, **Verified repairs**, **Incomplete** and **Checked**, then a recommended next step.

- **What needs attention** lists problems the evidence supports, most important first.
- **Collected information** shows what was read, including evidence that was incomplete or unavailable.
- Historical events are labeled as history. An error from last month is not treated as a current fault.

Missing evidence never counts as healthy. If a check could not run, the report says so.

## Choose checks yourself

Open **Choose checks (advanced)** to pick individual checks instead of the full scan. Each section in the app also has focused tools, described in the **Sections** pages of this guide.

## Fix what you found

When the report recommends a fix, it names the section and tool to open. Read [How changes stay safe](safety.html) before your first repair.
