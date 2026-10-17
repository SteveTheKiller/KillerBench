# KillerBench landing shell

Static pre-release page at https://killerbench.net/. Default: Dark with the family Yellow accent. No public download or release is claimed. Canonical and social URLs use the production domain.

`family.css` is the existing KillerScan landing stylesheet. `bench.css` adds only this shell's layout and Yellow defaults. Wordmarks use the canonical family SVG generator; regenerate with `python generate-art.py` from this directory. The generated social image is 1200x630.

Deployed September 12, 2026 to the Cloudflare Pages project `killerbench-landing` (production branch `main`). Publish only HTML, CSS, JavaScript, image/font assets, `license.txt`, and `_headers`; exclude this README and the artwork generator. `_headers` defines CSP, HSTS, and other browser security policies.

The Help page (`help.html`), Technical page (`technical.html`), About page (`about.html`) and Engine developer guide (`engine/`) are static pages generated from `help/docs/*.md` and `engine/docs/*.md`. `help.html` combines every Help guide as one collapsible page, and `technical.html` and `about.html` come from `help/docs/technical.md` and `help/docs/about.md`. Edit the Markdown, then run `python build-docs.py` from this directory; it needs only Python 3 and also rebuilds `docs-search-index.js`.

The landing and Engine overview have 19 language routes. Edit `templates/index.en.html` or the overview Markdown for English copy, then update the matching values in every `locales/*.json` catalog. The home page screenshots live in `screenshots/` (01 to 12), and their captions are template text, so each caption is a catalog key. Run `python build-docs.py` to regenerate all pages and localized search indexes. Run `python build-locales.py --check` to verify translation keys and entry pages, then `python test-site.py` to check every generated route, local asset, fragment and search link. Run `python check-engine-examples.py` with the .NET 10 SDK and authorized full checkout to compile the two getting-started examples without executing diagnostics. The Help, Technical and About pages and the detailed Engine guides remain in English; the localized Engine overview says so.

Publish only HTML, CSS, JavaScript, image/font assets, `license.txt`, and `_headers`. Do not publish this README, the artwork generator, the page builders, the locale catalogs, `templates`, or the `docs` Markdown folders.

`license.txt` is a byte-for-byte copy of the root GNU GPLv3 license. Run `python ../check-license.py` to check both copies and the desktop/CLI publish items. GPLv3 is confirmed; complete public source delivery and third-party font permissions remain pending. The detailed release requirements are in `../LICENSING.md`. A checked export is not permission to publish unresolved third-party assets.

## Prepare and review publication

This work prepares local output only. Do not publish until the reviewed output and package claims have explicit publication approval. No deploy automation is checked into this repository.

1. From this directory, run `python build-docs.py`, `python build-locales.py --check`, `python test-site.py` and `python check-engine-examples.py`.
2. Run `python prepare-publish.py C:\path\to\new-empty-output`. Choose a new directory outside this site source and its parent directories. The exporter refuses existing output, copies only public HTML/CSS/JS/images/fonts, the canonical `license.txt` and `_headers`, then checks the exported site again. It never deletes files or publishes.
3. Serve that export on loopback for local review, for example `python -m http.server 8765 --bind 127.0.0.1 --directory C:\path\to\new-empty-output`. Check Home, Help, Technical, About, Engine and MCP at desktop and narrow widths. Use the keyboard for navigation, search and the theme/language menus. Stop the local server when finished.
4. Keep the pending-download message until an official package actually exists. Before changing it, include the confirmed GPLv3 license and resolve compliant Corresponding Source delivery and bundled third-party/font notices; check the Windows x64 package and Desktop Runtime requirement, signature/checksum information and declared repair validation scope. The landing console is explicitly illustrative.
5. After explicit publication authorization, the site's operator should upload the checked export to the existing Cloudflare Pages project `killerbench-landing`, following that project's current controls. Do not upload source folders or the whole repository. Do not change host, firewall, DNS or Pages security settings as part of this preparation.
6. Verify the resulting live routes and assets, including `/`, `/help`, `/technical`, `/about`, `/engine/`, `/engine/getting-started`, `/mcp`, `/license.txt` and a nonexistent nested path. Check all 19 language entry routes and documentation search. Record the deployment identity and results. A local check is not a production deployment result.

The top-level `404.html` has absolute asset and navigation paths so missing nested routes do not break its layout. [Cloudflare Pages route matching](https://developers.cloudflare.com/pages/configuration/serving-pages/) documents directory index routes, extensionless HTML redirects and the top-level 404 behavior; including this file prevents Pages' implicit home-page fallback for unknown routes. The simple local Python server verifies files and missing-route status but does not emulate Pages' extensionless redirects or custom 404 rendering.

Detailed guides remain English. The 19 localized entry pages link to those pages and explicitly identify that boundary. New translations have structural checks; native-speaker review remains advisable before launch.
