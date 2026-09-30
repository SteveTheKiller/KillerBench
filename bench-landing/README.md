# KillerBench landing shell

Static pre-release page at https://killerbench.net/. Default: Dark/Amber using the family Orange accent. No public download or release is claimed. Canonical and social URLs use the production domain.

`family.css` is the existing KillerScan landing stylesheet. `bench.css` adds only this shell's layout and Orange defaults. Wordmarks use the canonical family SVG generator; regenerate with `python generate-art.py` from this directory. The generated social image is 1200x630.

Deployed September 12, 2026 to the Cloudflare Pages project `killerbench-landing` (production branch `main`). Publish only HTML, CSS, JavaScript, image/font assets, and `_headers`; exclude this README and the artwork generator. `_headers` defines CSP, HSTS, and other browser security policies.

The user guide (`help/`) and Engine developer guide (`engine/`) are static pages generated from `help/docs/*.md` and `engine/docs/*.md`. Edit the Markdown, then run `python build-docs.py` from this directory; it needs only Python 3 and also rebuilds `docs-search-index.js`. Do not publish `build-docs.py` or the `docs` Markdown folders.
