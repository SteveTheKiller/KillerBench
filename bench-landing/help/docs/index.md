# KillerBench help

KillerBench is a Windows diagnostic and repair workbench for technicians. It checks a computer first, explains what it found with the evidence behind each finding, and only changes Windows when you pick a specific fix and confirm it.

KillerBench is in development. There is no public download yet. These pages describe the current build.

## What it does

- Runs read-only diagnostics across Windows Update, devices, storage, networking, printing, security, apps and profiles, then writes a readable report.
- Offers guarded repairs that run only when fresh evidence proves the exact problem, and verifies the result afterwards.
- Covers setup and hardening work such as BitLocker, privacy settings, decluttering Windows and onboarding a new computer.
- Runs diagnostics and report analysis locally without AI or a KillerBench account. Selected actions, such as downloads and recovery-key backup to Entra ID, can contact external services.
- Uses the active app theme for the console, reports, confirmations, and Open, Save, and Folder dialogs.

## Where to start

- [Download and setup](setup.html)
- [About KillerBench](about.html)

- [Your first diagnosis](first-run.html) walks through a full system scan.
- [The workspace](workspace.html) explains the sections, panels and shortcuts.
- [How changes stay safe](safety.html) explains what KillerBench checks before and after any change.
- The **Sections** pages describe every tool, grouped the same way as the app.
- [Command line](cli.html) lists every `killerbench-cli` command.
- [Troubleshooting](troubleshooting.html) covers blocked actions, restarts and interrupted requests.

Developers who want to embed the diagnostics in their own tools can read the [Engine developer guide](../engine/index.html).
