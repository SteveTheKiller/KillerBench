"""Build the static KillerBench help and Engine guide pages from Markdown.

Run from this directory with any Python 3.9 or later:  python build-docs.py
Sources:  help/docs/*.md and engine/docs/*.md
Output:   help/*.html, engine/*.html and docs-search-index.js
No third-party packages are required. Do not publish this script or the Markdown sources.
"""
import html
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
VERSION = "v0.1.0"
ASSET = "5"

SITES = [
    {
        "key": "help", "label": "User guide", "folder": "help", "nav": "Help",
        "description": "How to use the KillerBench Windows diagnostic and repair workbench.",
        "pages": [
            ("index", "Welcome", "Start here"),
            ("first-run", "Your first diagnosis", "Start here"),
            ("workspace", "The workspace", "Start here"),
            ("safety", "How changes stay safe", "Start here"),
            ("diagnostics", "Diagnostics", "Sections"),
            ("repair", "Repair", "Sections"),
            ("updates", "Updates", "Sections"),
            ("devices", "Devices", "Sections"),
            ("network", "Network", "Sections"),
            ("apps", "Apps", "Sections"),
            ("maintenance", "Maintenance", "Sections"),
            ("onboarding", "Onboarding", "Sections"),
            ("security", "Security", "Sections"),
            ("bitlocker", "BitLocker", "Sections"),
            ("privacy", "Privacy", "Sections"),
            ("reports", "Reports and evidence", "Reference"),
            ("cli", "Command line", "Reference"),
            ("troubleshooting", "Troubleshooting", "Reference"),
        ],
    },
    {
        "key": "engine", "label": "Developer guide", "folder": "engine", "nav": "Engine",
        "description": "The KillerBench.Engine developer guide for Windows diagnostics, evidence, guarded repairs, and embedded hosts.",
        "pages": [
            ("overview", "Overview", "Engine"),
            ("getting-started", "Getting started", "Start here"),
            ("workflows", "Diagnostic workflows", "Use the Engine"),
            ("evidence", "Evidence and results", "Use the Engine"),
            ("repairs", "Repairs and safety", "Use the Engine"),
            ("cli", "CLI guide", "Reference"),
            ("api", "API guide", "Reference"),
            ("architecture", "Architecture", "Reference"),
            ("knowledge-packs", "Knowledge packs", "Reference"),
        ],
    },
]

THEMES = ["dark", "light", "hc", "blood", "greed", "cyanotic", "ectoplasm", "decay", "malaise", "sepulchre", "delirium", "mourning"]
ACCENTS = [("red", "#DD504B"), ("orange", "#E8962C"), ("yellow", "#EAD900"), ("green", "#1EA54C"), ("teal", "#1FB8A8"), ("blue", "#50AEE8"), ("purple", "#B982E3"), ("magenta", "#FF52C9")]


def esc(value):
    return html.escape(value, quote=True)


def inline(value):
    code = []

    def keep(match):
        code.append("<code>" + match.group(1) + "</code>")
        return "\u0000%d\u0000" % (len(code) - 1)

    text = re.sub(r"`([^`]+)`", keep, esc(value).replace("&#x27;", "'"))
    text = re.sub(r"\[([^\]]+)\]\(([^\s)]+)\)", r'<a href="\2">\1</a>', text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    return re.sub("\u0000(\\d+)\u0000", lambda m: code[int(m.group(1))], text)


def slug(value):
    value = re.sub(r"<[^>]*>", "", value.lower())
    return re.sub(r"[^a-z0-9]+", "-", value).strip("-")


def cells(line):
    return [cell.strip() for cell in line.strip().strip("|").split("|")]


def render(markdown):
    lines = markdown.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    out, paragraph, headings, i = [], [], [], 0
    title = None

    def flush():
        if paragraph:
            lede = "" if len(out) > 1 else ' class="doc-lede"'
            out.append("<p%s>%s</p>" % (lede, inline(" ".join(paragraph))))
            paragraph.clear()

    while i < len(lines):
        line = lines[i]
        if line.startswith("```"):
            flush()
            language, body = line[3:].strip(), []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                body.append(lines[i])
                i += 1
            out.append('<pre data-language="%s"><code>%s</code></pre>' % (esc(language), esc("\n".join(body))))
            i += 1
            continue
        heading = re.match(r"^(#{1,3})\s+(.+)$", line)
        if heading:
            flush()
            level, text = len(heading.group(1)), heading.group(2)
            anchor = slug(text)
            if level == 1 and title is None:
                title = text
            if level == 2:
                headings.append((anchor, text))
            out.append('<h%d id="%s">%s</h%d>' % (level, anchor, inline(text), level))
            i += 1
            continue
        if "|" in line and i + 1 < len(lines) and re.match(r"^\s*\|?\s*:?-+", lines[i + 1]):
            flush()
            head, rows = cells(line), []
            i += 2
            while i < len(lines) and lines[i].strip() and "|" in lines[i]:
                rows.append(cells(lines[i]))
                i += 1
            out.append("<table><thead><tr>%s</tr></thead><tbody>%s</tbody></table>" % (
                "".join("<th>%s</th>" % inline(c) for c in head),
                "".join("<tr>%s</tr>" % "".join("<td>%s</td>" % inline(c) for c in row) for row in rows)))
            continue
        bullet = re.match(r"^\s*[-+]\s+(.+)$", line)
        number = re.match(r"^\s*\d+\.\s+(.+)$", line)
        if bullet or number:
            flush()
            tag = "ul" if bullet else "ol"
            pattern = r"^\s*[-+]\s+(.+)$" if bullet else r"^\s*\d+\.\s+(.+)$"
            items = []
            while i < len(lines):
                match = re.match(pattern, lines[i])
                if not match:
                    break
                items.append("<li>%s</li>" % inline(match.group(1)))
                i += 1
            out.append("<%s>%s</%s>" % (tag, "".join(items), tag))
            continue
        if not line.strip():
            flush()
        else:
            paragraph.append(line.strip())
        i += 1
    flush()
    return "\n".join(out), headings, title


def plain(markdown):
    text = re.sub(r"```.*?```", " ", markdown, flags=re.S)
    text = re.sub(r"[#*`|>\[\]]", " ", text)
    text = re.sub(r"\(([^)\s]+\.html[^)\s]*)\)", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def header(site):
    links = [("../index.html#workbench", "Workbench", False), ("../index.html#features", "Features", False),
             ("../help/index.html", "Help", site["key"] == "help"), ("../engine/index.html", "Engine", site["key"] == "engine"),
             ("../index.html#about", "About", False)]
    nav = "".join('<a%s href="%s">%s</a>' % (' class="on"' if on else "", href, text) for href, text, on in links)
    themes = "".join('<button class="swatch sw-%s" data-theme="%s" title="%s"></button>' % (t, t, "Black" if t == "hc" else t.capitalize()) for t in THEMES)
    accents = "".join('<button class="acc" data-accent="%s" style="background:%s;color:%s" title="%s"></button>' % (n, c, c, n.capitalize()) for n, c in ACCENTS)
    return ('<header class="topbar"><a class="tb-home" href="../index.html"><img class="wm-logo tb-wm" src="../brand/killerbench-logo-dark-yellow.svg" alt="KillerBench"></a>'
            '<span class="tb-spacer"></span><nav class="tb-nav">' + nav + '</nav>'
            '<div class="tgrp" role="group" aria-label="Theme">' + themes + '</div>'
            '<div class="acc-fly accent-switch" id="accentSwitch"><button class="acc-toggle" id="accentToggle" aria-haspopup="true" aria-expanded="false" title="Accent color"></button>'
            '<div class="acc-pop" id="accentPop" hidden role="group" aria-label="Accent color"><span class="acc-pop-label">accent:</span>' + accents + '</div></div></header>')


def sidebar(site, current, headings):
    groups = []
    for page_id, title, group in site["pages"]:
        if not groups or groups[-1][0] != group:
            groups.append((group, []))
        groups[-1][1].append((page_id, title))
    parts = []
    for group, pages in groups:
        items = []
        for page_id, title in pages:
            file = page_file(page_id)
            if page_id == current:
                outline = "".join('<a href="#%s">%s</a>' % (a, inline(t)) for a, t in headings)
                items.append('<a href="%s" class="on" aria-current="page">%s</a><div class="page-outline">%s</div>' % (file, esc(title), outline))
            else:
                items.append('<a href="%s">%s</a>' % (file, esc(title)))
        parts.append('<div class="docs-nav-group"><span class="docs-nav-label">%s</span>%s</div>' % (esc(group), "".join(items)))
    other = [s for s in SITES if s is not site][0]
    tabs = '<div class="sb-tabs"><span class="on">%s</span><a href="../%s/index.html">%s</a></div>' % (esc(site["label"]), other["folder"], esc(other["label"]))
    search = ('<div class="docs-search" role="search"><input type="search" id="docsSearch" placeholder="Search help and Engine guides" '
              'aria-label="Search help and Engine guides" autocomplete="off"><div class="docs-search-results" id="docsSearchResults" hidden></div></div>')
    return '<aside class="sidebar engine-sidebar">' + tabs + search + '<nav class="docs-nav" aria-label="%s">%s</nav></aside>' % (esc(site["label"]), "".join(parts))


def page_file(page_id):
    return "index.html" if page_id in ("index", "overview") else page_id + ".html"


def pager(site, current):
    ids = [p[0] for p in site["pages"]]
    index = ids.index(current)
    def link(offset, label):
        j = index + offset
        if 0 <= j < len(ids):
            page = site["pages"][j]
            return '<a href="%s"><small>%s</small><span>%s</span></a>' % (page_file(page[0]), label, esc(page[1]))
        return "<span></span>"
    return '<nav class="doc-pager" aria-label="Documentation pages">%s%s</nav>' % (link(-1, "Previous"), link(1, "Next"))


def build():
    index = []
    for site in SITES:
        folder = ROOT / site["folder"]
        for page_id, nav_title, _ in site["pages"]:
            source = folder / "docs" / (page_id + ".md")
            markdown = source.read_text(encoding="utf-8")
            body, headings, title = render(markdown)
            title = title or nav_title
            file = page_file(page_id)
            first = next((line.strip() for line in markdown.splitlines() if line.strip() and not line.startswith("#")), "")
            summary = re.sub(r"[`*\[\]]|\([^)]*\.html[^)]*\)", "", first)[:240]
            canonical = "https://killerbench.net/%s/%s" % (site["folder"], "" if file == "index.html" else file)
            page = ('<!doctype html>\n<html lang="en" data-theme="dark"><head><meta charset="utf-8">'
                    '<meta name="viewport" content="width=device-width,initial-scale=1">'
                    '<meta name="description" content="%s"><link rel="canonical" href="%s">'
                    '<title>%s | KillerBench %s</title>'
                    '<link rel="stylesheet" href="../family.css?v=4"><link rel="stylesheet" href="../bench.css?v=3">'
                    '<link rel="stylesheet" href="../engine/engine-docs.css?v=%s">'
                    '<script defer src="../bench.js?v=4"></script><script defer src="../docs-search-index.js?v=%s"></script>'
                    '<script defer src="../docs-search.js?v=%s"></script></head>'
                    '<body data-brand-base="../brand">%s<div class="shell engine-shell">%s'
                    '<main class="content"><div class="content-scroll"><div class="engine-doc-wrap">'
                    '<article class="engine-doc" id="engineDoc">%s</article>%s</div></div></main></div>'
                    '<footer class="statusbar"><span class="left">%s &middot; In development &middot; No public download yet</span>'
                    '<span class="right">%s &middot; &copy; 2026 <b><a href="https://thekiller.net">Steve the Killer</a></b></span></footer>'
                    '</body></html>\n') % (
                esc(summary or site["description"]), canonical, esc(title), esc(site["label"].lower()), ASSET, ASSET, ASSET,
                header(site), sidebar(site, page_id, headings), body, pager(site, page_id), esc(site["label"]), VERSION)
            (folder / file).write_text(page, encoding="utf-8", newline="\n")
            index.append({"u": "%s/%s" % (site["folder"], file), "g": site["label"], "t": title,
                          "h": [[a, re.sub(r"[`*]", "", t)] for a, t in headings], "x": plain(markdown)[:6000]})
    (ROOT / "docs-search-index.js").write_text(
        "window.KillerBenchSearch=" + json.dumps(index, ensure_ascii=False, separators=(",", ":")) + ";\n", encoding="utf-8", newline="\n")
    print("Built %d pages." % len(index))


if __name__ == "__main__":
    build()
