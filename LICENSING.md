# Licensing and release requirements

KillerBench is licensed under GPLv3. [LICENSE.txt](LICENSE.txt) contains the unmodified GNU General Public License version 3, downloaded from [GNU](https://www.gnu.org/licenses/gpl-3.0.txt). Preserve the license document's existing Free Software Foundation copyright notice and all applicable contributor and third-party notices.

## License metadata

The existing product declaration says GPLv3 without a version-only or later-version qualifier. No qualifier is added here. Before assigning an SPDX expression or adding an application license header, confirm whether the intended expression is `GPL-3.0-only` or `GPL-3.0-or-later`. The example application notice in GNU's canonical license appendix is not a KillerBench declaration. No `PackageLicenseExpression` has been invented.

## Corresponding Source and binary delivery

Version 0.1.0 is Unreleased. The public repository currently supplies project information; the complete implementation is not publicly available there. Adding this license does not publish source or make the existing test executable ready for public distribution.

Before conveying a binary, select and implement a compliant source-delivery method from [GPLv3 section 6](https://www.gnu.org/licenses/gpl-3.0.en.html#section6). For a future website download, section 6(d) provides a route using equivalent access to Corresponding Source from the same designated place, at no further charge; a different source server requires clear directions next to the object code. Verify access using an account without private repository credentials before advertising it. This is a proposed distribution route, not an existing download or source offer.

Prepare the preferred source for the exact distributed revision, including the relevant desktop, Engine and CLI projects, resources, project files, dependency information and scripts needed to generate, install and run the covered work as required by the license. Record the matching binary/source hashes and build instructions. Review GPLv3's definition of Corresponding Source and its System Libraries exception against the actual package contents; do not assume every separately installed runtime or bundled asset is covered by that exception. If distribution involves a User Product, review the applicable Installation Information requirements. Do not include user logs, secrets or unrelated work in a source archive.

The desktop and CLI project publish items include `LICENSE.txt` as an accompanying file. Keep it with the executable. A single-file executable does not eliminate license, source or notice obligations. Run `python check-license.py` and inspect the actual final artifact inventory before release. There is no public Engine package yet; a future package also needs its reviewed license metadata, notices and source-delivery information.

## Third-party assets and notices

GPLv3 does not relicense third-party assets or supply missing permissions. The following inventory is a release review checklist, not proof that redistribution is cleared:

| Item | Current evidence | Required before distribution |
| --- | --- | --- |
| Typewriter - a602 (dead postman 2004) | The desktop `Fonts/Typewriter_a602_Dead_Postman.ttf` and website `brand/typewriter.ttf` match. Embedded notice: Copyright (c) MR.FISK (MikeLarsson), 2004. All rights reserved. No license grant or provenance receipt accompanies the files. The same font is used to generate outlined wordmarks. | Verify the actual desktop embedding, web font redistribution and artwork permissions and retain the required notice, or replace the affected assets with approved alternatives. |
| Punktype artwork input | The shared branding generator uses Punktype for the website social image. Its embedded license permits personal use and requires a paid license for commercial use. Existing authorization has not been verified. The font itself is not in the website export. | Verify applicable artwork permission and retain required evidence/notices, or regenerate with an approved font. |
| .NET build and host inputs | The desktop publish profile is framework-dependent Windows x64; its SDK-generated apphost is included in the output. Restored product project assets currently contain project references and no third-party NuGet packages. The external Desktop Runtime is installed separately. | Inventory the exact release inputs and retain their applicable licenses/notices. Follow the [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md) and [Windows component license information](https://github.com/dotnet/core/blob/main/license-information-windows.md). Do not describe all Windows .NET components as MIT or copy the entire SDK notice inventory as if all those components shipped. |
| Knowledge content | The checked-in Windows starter file contains original summaries with links to Microsoft documentation. Production knowledge packs are not bundled by the current publish items. | Recheck the final package and any new imports. Preserve provenance, license and notice requirements for material actually distributed. |

Font embedding flags alone do not establish redistribution permission; Microsoft's [OpenType OS/2 specification](https://learn.microsoft.com/en-us/typography/opentype/spec/os2#fstype) requires respect for the original license. Keep unresolved font and source-delivery status visible in the public setup/About copy until evidence supports changing it. Do not label the fonts GPLv3.

## Local verification

Run `python check-license.py` from the repository, then the documented site generation, localization, route and Engine-example checks from `bench-landing`. Confirm the license is retained in the clean site export and evaluated desktop/CLI publish items. These checks establish documentation and packaging behavior; they do not certify legal compliance or authorize publication.
