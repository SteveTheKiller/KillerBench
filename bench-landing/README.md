# KillerBench landing shell

Static pre-release page at https://killerbench.net/. Default: Dark with the family Yellow accent. No public download or release is claimed. Canonical and social URLs use the production domain.

`family.css` is the existing KillerScan landing stylesheet. `bench.css` adds only this shell's layout and Yellow defaults. Wordmarks use the canonical family SVG generator; regenerate with `python generate-art.py` from this directory. The generated social image is 1200x630.

Deployed September 12, 2026 to the Cloudflare Pages project `killerbench-landing` (production branch `main`). Publish only HTML, CSS, JavaScript, image/font assets, and `_headers`; exclude this README and the artwork generator. `_headers` defines CSP, HSTS, and other browser security policies.

The user guide (`help/`) and Engine developer guide (`engine/`) are static pages generated from `help/docs/*.md` and `engine/docs/*.md`. Edit the Markdown, then run `python build-docs.py` from this directory; it needs only Python 3 and also rebuilds `docs-search-index.js`.

The landing, Help overview, and Engine overview have 19 language routes. Edit `templates/index.en.html` or the overview Markdown for English copy, then update the matching values in every `locales/*.json` catalog. Run `python build-docs.py` to regenerate all pages and localized search indexes. Run `python build-locales.py --check` to verify translation keys, local routes, links, and anchors. The detailed Help and Engine guides remain in English; their localized overview pages say so.

Publish only HTML, CSS, JavaScript, image/font assets, and `_headers`. Do not publish this README, the artwork generator, the page builders, the locale catalogs, `templates`, or the `docs` Markdown folders.
