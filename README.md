<p align="center">
  <a href="https://killerbench.net"><img src="docs/wordmark.png" height="180" alt="KillerBench - Free Diagnostic Workbench"></a>
</p>

Free and open-source Windows diagnosis, repair and maintenance workbench for technicians. KillerBench checks the computer first, explains what it found with the evidence behind each finding, and changes Windows only when you pick a specific fix and confirm it.

Full how-tos live in the [Help guide](https://killerbench.net/help.html), and the [Technical](https://killerbench.net/technical.html) page explains how KillerBench works. The Engine, CLI and architecture are covered in the [developer guide](https://killerbench.net/engine/).

## Features

- Full system scan reads 49 diagnostic areas, ranks what needs attention and saves a local journal without changing anything
- Twelve sections group every tool: Diagnostics, Repair, Updates, Devices, Network, Apps, Maintenance, Onboarding, Security, BitLocker, Privacy and Storage
- Guarded repairs recheck fresh evidence immediately before they act, write a pending record, verify the result and reconcile an interrupted request without repeating it
- Missing, failed or timed-out evidence stays Incomplete and never counts as healthy
- Windows Update, servicing, DISM and SFC, services, networking, DHCP and DNS, printing, storage, Storage Spaces, BitLocker, Defender, firewall, certificates, crashes and hardware errors
- BitLocker encryption, protectors, recovery backup to a file, Active Directory or Microsoft Entra ID, and decryption, with the recovery password never shown or logged
- Cleanup with previews, age and file limits, and per-file rechecks; profile review; scheduled-task review; declutter review
- Privacy controls for app permissions, advertising, clipboard, cloud search and suggestions, with policy checks and exact undo
- Readable reports beside the raw evidence, with HTML and text export and the next safe step first
- `killerbench-cli` runs the same Engine from PowerShell, RMM and KillerMCP, with JSON output, previews by default and distinct exit codes
- Thirteen themes and twenty languages, with the family About card, shortcut list and keyboard map
- Diagnostics and report analysis run locally, with no account and no AI service

## Requirements

- Windows x64
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for Windows x64
- Administrator rights for repairs and for the protected evidence some checks need; diagnostics also run as a standard user

KillerBench ships as `KillerBench.exe` and `killerbench-cli.exe`, each a single portable EXE. Each release lists their SHA-256 values in `SHA256SUMS.txt`, and both are signed.

## Safety

KillerBench is free and you use it at your own risk. Read [How changes stay safe](https://killerbench.net/help.html#safety) before your first repair and keep a current backup before any change.

## License and source

KillerBench is licensed under GPLv3. See the complete [GNU General Public License version 3](LICENSE.txt). Every release attaches the complete source for that exact version, and the current release's source is [KillerBench-1.0.0-src.zip](https://github.com/SteveTheKiller/KillerBench/releases/download/v1.0.0/KillerBench-1.0.0-src.zip). See [licensing and release requirements](LICENSING.md).

Part of [killertools.net](https://killertools.net).
