# Licensing and release requirements

KillerBench is licensed under GPLv3. [LICENSE.txt](LICENSE.txt) contains the unmodified GNU General Public License version 3, downloaded from [GNU](https://www.gnu.org/licenses/gpl-3.0.txt). Preserve the license document's existing Free Software Foundation copyright notice and all applicable contributor and third-party notices.

## License metadata

The existing product declaration says GPLv3 without a version-only or later-version qualifier. No qualifier is added here. Before assigning an SPDX expression or adding an application license header, confirm whether the intended expression is `GPL-3.0-only` or `GPL-3.0-or-later`. The example application notice in GNU's canonical license appendix is not a KillerBench declaration. No `PackageLicenseExpression` has been invented.

## Corresponding Source and binary delivery

Source always ships with the app. Binaries are conveyed only as GitHub release assets, and every release carries the Corresponding Source from the same place at no charge, which follows [GPLv3 section 6(d)](https://www.gnu.org/licenses/gpl-3.0.en.html#section6).

`release.ps1` enforces this. It refuses to build unless the working tree is clean and the default branch matches `origin`, so the public repository holds the exact released revision. It then builds `KillerBench-<version>-src.zip` from the tagged commit with `git archive`, which includes the desktop, Engine, Core, Windows provider, CLI and Checks projects, resources, project files and scripts, and leaves out the separately deployed `bench-landing` site. The archive, both executables, `LICENSE.txt` and `SHA256SUMS.txt` are attached to the same GitHub release, and the README links the current archive. An existing archive is never overwritten.

Version 1.0.0 is Unreleased. Until the first release, the public repository supplies project information only and no binary is conveyed. Before the first release, push the complete history so `origin` matches the release commit, then confirm the release assets download from an account without private repository credentials. Review GPLv3's definition of Corresponding Source and its System Libraries exception against the actual package contents; do not assume every separately installed runtime or bundled asset is covered by that exception. If distribution involves a User Product, review the applicable Installation Information requirements. Do not include user logs, secrets or unrelated work in a source archive.

The desktop and CLI project publish items include `LICENSE.txt` as an accompanying file. Keep it with the executable. A single-file executable does not eliminate license, source or notice obligations. Run `python check-license.py` and inspect the actual final artifact inventory before release. There is no public Engine package yet; a future package also needs its reviewed license metadata, notices and source-delivery information.

## Third-party assets and notices

GPLv3 does not relicense third-party assets or supply missing permissions. The following inventory is a release review checklist, not proof that redistribution is cleared:

| Item | Current evidence | Required before distribution |
| --- | --- | --- |
| Killer Sans and Lacquer | The desktop ships `Fonts/KillerSans-Regular.ttf`, `Fonts/KillerSansUI-Regular.ttf` and `Fonts/Lacquer-Regular.ttf`. Killer Sans UI is another Modified Version of Play under the same notice. The publish output places each notice beside the executable, and `Checks/Test-PublishNotices.ps1` verifies them. They replaced the earlier Typewriter - a602 face, which had no redistribution license and is no longer shipped. Killer Sans is a Modified Version of Play, renamed because Play is a Reserved Font Name. Both are under the SIL Open Font License 1.1, with the license texts beside every copy (`KillerSans-OFL.txt`, `Lacquer-OFL.txt`). Killer Sans sets headings and footers (BrandSansFont) and Lacquer sets the app name in the wordmark (WordmarkAccentFont). | Keep the OFL texts with the fonts in every package and on the site, and do not sell the fonts by themselves. |
| Punktype | The desktop ships `Fonts/Punktype.ttf` to set "Killer" in the wordmark (WordmarkFont), and the shared branding generator uses it for wordmark art. Its embedded license permits personal use only. Steve Riley bought the commercial license from the designer, junkohanhero, on 2026-10-10; the license note is `Fonts/Punktype-LICENSE.txt` and the purchase record is kept outside the repository. | Keep `Punktype-LICENSE.txt` beside every copy, credit the designer, and do not offer the font for reuse outside this project. |
| .NET build and host inputs | The desktop publish profile is framework-dependent Windows x64; its SDK-generated apphost is included in the output. Restored product project assets currently contain project references and no third-party NuGet packages. The external Desktop Runtime is installed separately. | Inventory the exact release inputs and retain their applicable licenses/notices. Follow the [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md) and [Windows component license information](https://github.com/dotnet/core/blob/main/license-information-windows.md). Do not describe all Windows .NET components as MIT or copy the entire SDK notice inventory as if all those components shipped. |
| Knowledge content | The checked-in Windows starter file contains original summaries with links to Microsoft documentation. Production knowledge packs are not bundled by the current publish items. | Recheck the final package and any new imports. Preserve provenance, license and notice requirements for material actually distributed. |

Font embedding flags alone do not establish redistribution permission; Microsoft's [OpenType OS/2 specification](https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype) requires respect for the original license. Keep unresolved font and source-delivery status visible in the public setup/About copy until evidence supports changing it. Do not label the fonts GPLv3.

## Local verification

Run `python check-license.py` from the repository, then the documented site generation, localization, route and Engine-example checks from `bench-landing`. Confirm the license is retained in the clean site export and evaluated desktop/CLI publish items. These checks establish documentation and packaging behavior; they do not certify legal compliance or authorize publication.
