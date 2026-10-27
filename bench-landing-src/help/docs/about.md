# About KillerBench

KillerBench is a Windows diagnosis, repair and maintenance workbench by Steve the Killer, part of the [Killer Tools family](https://killertools.net). Version 1.0.0 is Unreleased. Its desktop app, Engine and CLI share diagnostic evidence and supported repair workflows.

## Availability and scope

No public KillerBench download or Engine package is available yet. [Download and setup](setup.html) describes the current Windows x64 target and Desktop Runtime requirements. Native repair validation is still in progress; the presence of a tool is not proof that every failure, interruption or Windows configuration has been tested.

## Source and distribution notices

KillerBench is licensed under **GPLv3**. Its distribution license is the [GNU General Public License version 3](../license.txt). The license describes the permissions and conditions for copying, modifying and distributing covered work.

The public [KillerBench repository](https://github.com/SteveTheKiller/KillerBench) currently provides project information. The complete implementation is not publicly available there. These developer guides describe an authorized source checkout; they do not provide a public source download.

Before distributing a binary, its publisher must provide Corresponding Source through a method allowed by [GPLv3 section 6](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), including the source and build material required by the license. The first public package's source-delivery instructions and bundled third-party notices remain pending. GPLv3 does not grant permission to redistribute third-party fonts; the current font permissions still need verification. Review the published package's license, source instructions and notices before redistributing it.

## Privacy and external services

Diagnostics and log analysis run locally without an AI service or KillerBench account. Journals and retained evidence use the local account's `%LOCALAPPDATA%\KillerBench` folders. Selected actions can contact Windows Update, software download services, directories or other named destinations. The startup update check contacts GitHub, and knowledge updates and the MCP installer page contact their configured services. An AI client using MCP may receive the returned reference result.

This website stores theme and accent choices in the browser's local storage. Its guides do not run a diagnosis on your computer. Raw logs, imported files and exported evidence can contain computer names, accounts, paths or sensitive source text. Review them before sharing; do not include recovery keys, credentials or customer data in a public report.

## Support

Project information is available in the public repository. Use the contact information published on [the author's website](https://thekiller.net) for support; include the app version, Windows build, exact tool, expected result and observed result. Review and redact evidence before sending it, and use an approved private channel for customer information. Keep journals for an uncertain repair outcome.

## Related guides

- [How changes stay safe](safety.html)
- [Reports and evidence](reports.html)
- [Engine developer guide](../engine/)
- [KillerMCP](../mcp.html)
