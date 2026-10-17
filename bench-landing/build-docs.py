"""Build the static KillerBench Help, Technical, About and Engine guide pages from Markdown.

Run from this directory with any Python 3.9 or later:  python build-docs.py
Sources:  help/docs/*.md and engine/docs/*.md
Output:   help.html, technical.html, about.html, engine/*.html and docs-search-index.js
No third-party packages are required. Do not publish this script or the Markdown sources.
"""
import html
import json
import re
from pathlib import Path
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parent
VERSION = "v1.0.0"
ASSET = "8"

SITES = [
    {
        "key": "help", "label": "User guide", "folder": "help", "nav": "Help",
        "description": "How to use the KillerBench Windows diagnostic and repair workbench.",
        "pages": [
            ("index", "Welcome", "Start here"),
            ("setup", "Download and setup", "Start here"),
            ("first-run", "Your first diagnosis", "Start here"),
            ("workspace", "The workspace", "Start here"),
            ("safety", "How changes stay safe", "Start here"),
            ("fix-windows-update", "Fix Windows Update", "How-to guides"),
            ("repair-system-files", "Repair system files", "How-to guides"),
            ("free-disk-space", "Free up disk space", "How-to guides"),
            ("check-a-drive", "Check and repair a drive", "How-to guides"),
            ("fix-network", "Fix network and internet problems", "How-to guides"),
            ("fix-printing", "Fix printing problems", "How-to guides"),
            ("encrypt-with-bitlocker", "Encrypt a drive with BitLocker", "How-to guides"),
            ("investigate-crashes", "Investigate crashes and blue screens", "How-to guides"),
            ("make-windows-private", "Make Windows more private", "How-to guides"),
            ("set-up-new-pc", "Set up a new computer", "How-to guides"),
            ("use-the-cli", "Run KillerBench from scripts", "How-to guides"),
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
            ("storage", "Storage", "Sections"),
            ("reports", "Reports and evidence", "Reference"),
            ("cli", "Command line", "Reference"),
            ("troubleshooting", "Troubleshooting", "Reference"),
            ("about", "About KillerBench", "Reference"),
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


# Localized Help, Technical and About pages. Each locale keeps translated Markdown with the same
# heading structure as English in help/docs/i18n/<locale>/, plus ui.json for page chrome. The
# folder sits under docs/, so it is never part of the public export.
I18N = ROOT / "help" / "docs" / "i18n"
LOCALE_ORDER = ["en-US", "bn", "cs-CZ", "de-DE", "es", "fr-FR", "hu-HU", "it-IT", "ja-JP", "kk-KZ", "nb-NO", "pl-PL", "pt-BR", "ru-RU", "tr-TR", "uk-UA", "vi-VN", "zh-CN", "zh-TW"]
DOC_PAGES = ("help.html", "technical.html", "about.html")
LOCAL_ROUTES = set(DOC_PAGES) | {"index.html", "engine/index.html"}
DOC_LOCALES = [c for c in LOCALE_ORDER[1:] if (I18N / c / "ui.json").is_file()]
AVAILABLE = ["en-US"] + DOC_LOCALES


class Loc:
    def __init__(self, code):
        self.code = code
        catalog = ROOT / "locales" / (code + ".json")
        data = json.loads(catalog.read_text(encoding="utf-8")) if catalog.is_file() else {"language": "English", "strings": {}}
        self.language, self.catalog = data["language"], data["strings"]
        self.ui = json.loads((I18N / code / "ui.json").read_text(encoding="utf-8")) if code != "en-US" else {}
        self.sub = "" if code == "en-US" else code + "/"
        self.up = "" if code == "en-US" else "../"

    def t(self, text):
        if self.code == "en-US":
            return text
        if text in self.ui:
            return self.ui[text]
        if text in self.catalog:
            return self.catalog[text]
        raise ValueError("%s: missing docs UI translation: %s" % (self.code, text))

    def rebase(self, markup):
        """Root-relative references in localized pages point one folder up unless the route is localized."""
        if self.code == "en-US":
            return markup
        def fix(match):
            ref = match.group(2)
            url = urlsplit(html.unescape(ref))
            if url.scheme or url.netloc or ref.startswith("#") or not url.path or url.path in LOCAL_ROUTES:
                return match.group(0)
            return '%s="../%s"' % (match.group(1), ref)
        return re.sub(r'(href|src)="([^"]*)"', fix, markup)


EN = Loc("en-US")


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
    anchors = set()

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
            base = slug(text) or "section"
            anchor, suffix = base, 2
            while anchor in anchors:
                anchor = "%s-%d" % (base, suffix)
                suffix += 1
            anchors.add(anchor)
            if level == 1 and title is None:
                title = text
            if level in (2, 3):
                headings.append((anchor, text, level))
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
                command = re.match(r"^`([a-z0-9][a-z0-9-]*)", match.group(1)) if bullet else None
                item_id = ""
                if command:
                    base = "cmd-" + command.group(1)
                    anchor, suffix = base, 2
                    while anchor in anchors:
                        anchor = "%s-%d" % (base, suffix)
                        suffix += 1
                    anchors.add(anchor)
                    item_id = ' id="%s"' % anchor
                items.append("<li%s>%s</li>" % (item_id, inline(match.group(1))))
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


NAV = [("help.html", "Help", "help"), ("technical.html", "Technical", "technical"), ("engine/index.html", "Engine", "engine"), ("about.html", "About", "about")]


def topbar(prefix, active, loc=EN, pages=None):
    """prefix reaches shared assets; pages reaches the localized routes (defaults to prefix)."""
    pages = prefix if pages is None else pages
    t = loc.t
    nav = "".join('<a%s href="%s%s">%s</a>' % (' class="on"' if key == active else "", pages, href, esc(t(text))) for href, text, key in NAV)
    themes = "".join('<button class="swatch sw-%s" data-theme="%s" title="%s"></button>' % (th, th, esc(t("Black" if th == "hc" else th.capitalize()))) for th in THEMES)
    accents = "".join('<button class="acc" data-accent="%s" style="background:%s;color:%s" title="%s"></button>' % (n, c, c, esc(t(n.capitalize()))) for n, c in ACCENTS)
    return ('<header class="topbar"><a class="tb-home" href="%sindex.html" title="KillerBench"><img class="tb-icon" src="%sapp-icon.png" alt="KillerBench" width="44" height="44"><img class="wm-logo tb-wm" src="%sbrand/killerbench-logo-dark-yellow.svg" alt="KillerBench"></a>'
            '<a class="tb-mcp-link" href="%smcp.html" aria-label="KillerMCP"><img src="%smcp.png" alt="" aria-hidden="true"><span class="tb-mcp-wordmark"><span>Killer</span><span class="tb-mcp-accent">MCP</span></span></a>'
            '<span class="tb-spacer"></span><nav class="tb-nav">' % (pages, prefix, prefix, prefix, prefix) + nav + '</nav>'
            '<div class="tgrp" role="group" aria-label="%s">' % esc(t("Theme")) + themes + '</div>'
            '<div class="acc-fly accent-switch" id="accentSwitch"><button class="acc-toggle" id="accentToggle" aria-haspopup="true" aria-expanded="false" title="%s"></button>' % esc(t("Accent color")) +
            '<div class="acc-pop" id="accentPop" hidden role="group" aria-label="%s"><span class="acc-pop-label">%s</span>' % (esc(t("Accent color")), esc(t("accent:"))) + accents + '</div></div></header>')


def lang_switch(file, loc):
    """The same language menu build-locales.py adds to the entry pages."""
    items = []
    for code in AVAILABLE:
        other = Loc(code)
        items.append('<a class="lang-item" hreflang="%s" lang="%s" href="%s" aria-pressed="%s">%s</a>' % (code, code, loc.up + other.sub + file, str(code == loc.code).lower(), esc(other.language)))
    return ('<div class="lang-switch"><button class="lang-toggle" id="languageToggle" aria-haspopup="true" aria-expanded="false" aria-label="%s">%s</button>'
            '<div class="lang-menu" id="languageMenu" hidden>%s</div></div>') % (esc(loc.t("Choose language")), esc(loc.code.split("-")[0].upper()), "".join(items))


def header(site):
    return topbar("../", site["key"])


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
                outline = "".join('<a href="#%s"%s>%s</a>' % (a, ' class="sub"' if lvl == 3 else "", inline(t)) for a, t, lvl in headings)
                items.append('<a href="%s" class="on" aria-current="page">%s</a><div class="page-outline">%s</div>' % (file, esc(title), outline))
            else:
                items.append('<a href="%s">%s</a>' % (file, esc(title)))
        parts.append('<div class="docs-nav-group"><span class="docs-nav-label">%s</span>%s</div>' % (esc(group), "".join(items)))
    tabs = '<div class="sb-tabs"><span class="on">%s</span><a href="../help.html">Help</a></div>' % esc(site["label"])
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


HELP_PAGES = [p for p in SITES[0]["pages"] if p[0] != "about"]
HELP_IDS = {p[0] for p in SITES[0]["pages"]}
ANCHOR = {"index": "welcome"}
VERSION_NOTE = "In development &middot; Part of <a href=\"https://killertools.net\">killertools.net</a>"


def help_anchor(page_id, fragment=""):
    base = ANCHOR.get(page_id, page_id)
    return base + ("_" + fragment if fragment else "")


def relink(body, on_help):
    """Point Markdown links written for the old help/ folder at the single-page routes."""
    def fix(match):
        href = html.unescape(match.group(1))
        url = urlsplit(href)
        if url.scheme or url.netloc or href.startswith("#"):
            return match.group(0)
        frag = "#" + url.fragment if url.fragment else ""
        if url.path.startswith("../"):
            rest = url.path[3:]
            if rest in ("engine", "engine/"):
                rest = "engine/index.html"
            return 'href="%s%s"' % (esc(rest), esc(frag))
        name = url.path[:-5] if url.path.endswith(".html") else None
        if name == "about":
            return 'href="about.html%s"' % esc(frag)
        if name == "technical":
            return 'href="technical.html%s"' % esc(frag)
        if name in HELP_IDS:
            return 'href="%s#%s"' % ("" if on_help else "help.html", esc(help_anchor(name, url.fragment)))
        raise ValueError("Unmapped guide link: " + href)
    return re.sub(r'href="([^"]*)"', fix, body)


def prefix_ids(body, page_id):
    return re.sub(r' id="([^"]+)"', lambda m: ' id="%s"' % help_anchor(page_id, m.group(1)), body)


def without_title(body):
    return re.sub(r"^<h1 id=\"[^\"]*\">.*?</h1>\n?", "", body, count=1)


def tables(body):
    return body.replace("<table>", '<table class="spec-table">')


def page_shell(file, title, description, active, style, aside, inner, script="", loc=EN):
    canonical = "https://killerbench.net/" + loc.sub + file
    alternates = "".join('<link rel="alternate" hreflang="%s" href="https://killerbench.net/%s%s">' % (c, Loc(c).sub, file) for c in AVAILABLE)
    alternates += '<link rel="alternate" hreflang="x-default" href="https://killerbench.net/%s">' % file
    ui = "".join(' data-ui-%s="%s"' % (k, esc(loc.t(v))) for k, v in (("theme", "Theme"), ("choose-theme", "Choose theme"), ("accent", "Accent color"), ("accent-label", "accent:"), ("close", "Close"), ("no-results", "No matching pages."))) if loc.code != "en-US" else ""
    note = VERSION_NOTE if loc.code == "en-US" else '%s <a href="https://killertools.net">killertools.net</a>' % esc(loc.t("In development · Part of")).replace("·", "&middot;")
    if loc.code != "en-US":
        style = style.replace("url('grain.png", "url('../grain.png")
    return ('<!doctype html>\n<html lang="%s" data-theme="dark"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width,initial-scale=1">'
            '<meta name="description" content="%s"><link rel="canonical" href="%s">%s'
            '<meta property="og:title" content="%s"><meta property="og:description" content="%s">'
            '<meta property="og:image" content="https://killerbench.net/og-image.png"><meta property="og:url" content="%s">'
            '<meta property="og:type" content="website"><meta name="twitter:card" content="summary_large_image">'
            '<title>%s</title><link rel="stylesheet" href="%sfamily.css?v=13"><link rel="stylesheet" href="%sbench.css?v=7">'
            '<style>%s</style><script defer src="%sbench.js?v=7"></script></head>'
            '<body data-brand-base="%sbrand"%s>%s%s<div class="shell">%s<main class="content"><div class="content-scroll"><div class="content-inner">%s</div></div></main></div>'
            '<footer class="statusbar"><span class="left">%s</span><span class="right">%s &middot; &copy; 2026 <b><a href="https://thekiller.net">Steve the Killer</a></b></span></footer>'
            '%s</body></html>\n') % (loc.code, esc(description), canonical, alternates, esc(title), esc(description), canonical, esc(title), loc.up, loc.up, style, loc.up,
                                     loc.up, ui, topbar(loc.up, active, loc, "")[:-len("</header>")], lang_switch(file, loc) + "</header>", loc.rebase(aside), loc.rebase(inner), note, VERSION, script)


def render_page(page_id, loc=EN):
    """Render one guide page. A translation must keep the English heading structure, and it
    takes the English anchors so links and search results stay identical across languages."""
    english = (ROOT / "help" / "docs" / (page_id + ".md")).read_text(encoding="utf-8")
    body, headings, title = render(english)
    if loc.code == "en-US":
        return body, headings, title, english
    markdown = (I18N / loc.code / (page_id + ".md")).read_text(encoding="utf-8")
    local, local_headings, local_title = render(markdown)
    pattern = r'<h([1-3]) id="([^"]+)">'
    ids, local_ids = re.findall(pattern, body), re.findall(pattern, local)
    if [level for level, _ in ids] != [level for level, _ in local_ids] or len(headings) != len(local_headings):
        raise ValueError("%s/%s.md: heading structure differs from English" % (loc.code, page_id))
    order = iter(ids)
    local = re.sub(pattern, lambda m: '<h%s id="%s">' % (m.group(1), next(order)[1]), local)
    if sorted(re.findall(r'<li id="([^"]+)"', local)) != sorted(re.findall(r'<li id="([^"]+)"', body)):
        raise ValueError("%s/%s.md: command entries differ from English" % (loc.code, page_id))
    return local, [(a, t, lvl) for (a, _, lvl), (_, t, _) in zip(headings, local_headings)], local_title, markdown


def output(loc, file, page):
    path = ROOT / loc.sub / file
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        path.read_text(encoding="utf-8")
    path.write_text(page, encoding="utf-8", newline="\n")


OUTLINE_CSS = (".sb-tabs .on{border-bottom:0}.outline{padding:8px 0;overflow-y:auto;flex:1;min-height:0}"
               ".outline a{display:block;padding:8px 16px 8px 18px;font-size:13px;color:var(--text2);border-left:2px solid transparent}"
               ".outline a:hover{color:var(--text);background:color-mix(in srgb,var(--accent) 8%,transparent);filter:none}"
               ".outline a.on{color:var(--accent-text);border-left-color:var(--accent);background:var(--accent)}"
               ".outline a.sub{padding:6px 16px 6px 32px;font-size:12px;opacity:.82}"
               "@media (min-width:901px){.shell{grid-template-columns:268px 1fr}.content{margin:8px 8px 0 2px}}"
               "@media (max-width:900px){.outline{display:flex;flex-direction:row;flex-wrap:wrap;padding:8px;gap:4px}.outline a{border-left:0;border-radius:5px;padding:6px 10px}.outline a.on{border-left:0}.outline a.sub{display:none}}"
               ".page-hero,.help-hero{text-align:center;padding:44px 16px 8px}"
               ".page-hero .wm-logo,.help-wm{height:clamp(55px,9.5vw,91px);width:auto;margin:0 auto 8px;display:block;filter:drop-shadow(0 4px 8px rgba(0,0,0,.55))}"
               ".page-hero p,.help-hero p{color:var(--muted);font-size:15px;margin:4px auto 0;max-width:620px}"
               ".help-hero .legend{font-size:13px;color:var(--text2)}"
               ".spec-table code,.acc-body code,.tech-sec code,.about-card code{font-family:Consolas,monospace;font-size:12.5px;background:var(--panel);padding:1px 6px;border-radius:4px;color:var(--text);overflow-wrap:anywhere}"
               ".acc-body pre,.tech-sec pre{margin:10px 0 14px;padding:12px 14px;overflow:auto;background:var(--panel);border:1px solid var(--pane-border);border-radius:6px;font:12.5px/1.55 Consolas,monospace;color:var(--text)}"
               ".acc-body pre code,.tech-sec pre code{padding:0;background:none}")

OUTLINE_SPY = ('<script>(function(){var links=[].slice.call(document.querySelectorAll(".outline a"));'
               'var sc=document.querySelector(".content-scroll");'
               'function spy(){var secs=links.map(function(a){return document.getElementById(a.getAttribute("href").slice(1));});'
               'var y=sc.scrollTop+110,cur=null;secs.forEach(function(s){if(s&&s.offsetParent!==null&&s.getBoundingClientRect().top-sc.getBoundingClientRect().top+sc.scrollTop<=y)cur=s;});'
               'links.forEach(function(a){a.classList.toggle("on",!!cur&&a.getAttribute("href")==="#"+cur.id);});}'
               'sc.addEventListener("scroll",spy,{passive:true});window.addEventListener("load",spy);spy();})();</script>')


def build_help(loc=EN, write=True):
    groups, entries = [], []
    for page_id, title, group in HELP_PAGES:
        title, group = loc.t(title), loc.t(group)
        body, headings, _, markdown = render_page(page_id, loc)
        body = tables(relink(prefix_ids(without_title(body), page_id), True))
        anchor = help_anchor(page_id)
        if not groups or groups[-1][0] != group:
            groups.append((group, []))
        groups[-1][1].append((anchor, title, body))
        entries.append({"u": "help.html", "a": anchor, "g": loc.t("Help"), "t": title,
                        "h": [[anchor, title]] + [[help_anchor(page_id, a), re.sub(r"[`*]", "", t)] for a, t, _ in headings],
                        "x": plain(markdown)[:6000]})
    outline, inner = [], []
    for group, items in groups:
        gid = slug(group)
        outline.append('<a href="#%s">%s</a>' % (gid, esc(group)))
        outline.extend('<a class="sub" href="#%s">%s</a>' % (a, esc(t)) for a, t, _ in items)
        inner.append('<h2 class="help-cat" id="%s">%s</h2>' % (gid, esc(group)))
        for anchor, title, body in items:
            inner.append('<div class="acc-item" id="%s"><button class="acc-head" type="button" aria-expanded="false"><span>%s</span><span class="chev" aria-hidden="true">&#9656;</span></button>'
                         '<div class="acc-body"><div class="acc-body-inner">%s</div></div></div>' % (anchor, esc(title), body))
    legend = esc(loc.t("Click a row to open it. For how KillerBench works underneath, see the {technical} page.")).replace("{technical}", '<a href="technical.html">%s</a>' % esc(loc.t("Technical")))
    hero = ('<section class="help-hero"><span class="page-title">%s</span>'
            '<img class="help-wm wm-logo" src="brand/killerbench-logo-dark-yellow.svg" alt="KillerBench">'
            '<p>%s</p><p class="legend">%s</p></section>') % (esc(loc.t("How to use")), esc(loc.t("Every section and tool, what it does, and how to work through common jobs step by step.")), legend)
    style = OUTLINE_CSS + (
        ".content-inner{max-width:880px;margin:0 auto;padding:0 28px 56px}"
        ".help-cat{font-family:var(--kt-font-section);font-size:18px;font-weight:normal;letter-spacing:.5px;color:var(--accent);margin:30px 2px 12px;padding-top:6px;scroll-margin-top:20px;text-shadow:0 2px 5px rgba(0,0,0,.55),0 1px 2px rgba(0,0,0,.5)}"
        "html[data-theme=\"light\"] .help-cat{text-shadow:none}"
        ".acc-item{border:1px solid var(--dragline);border-radius:8px;margin-bottom:10px;background:var(--modal) url('grain.png') repeat;background-size:256px 256px;overflow:hidden;scroll-margin-top:16px}"
        ".acc-head{width:100%;text-align:left;background:none;border:0;color:var(--text);font:inherit;font-size:15px;font-weight:600;padding:15px 18px;cursor:pointer;display:flex;align-items:center;justify-content:space-between;gap:12px}"
        ".acc-head:hover{color:var(--accent)}.acc-head .chev{color:var(--muted);transition:transform .25s ease;flex-shrink:0;font-size:13px}"
        ".acc-item.open .acc-head{color:var(--accent)}.acc-item.open .acc-head .chev{transform:rotate(90deg);color:var(--accent)}"
        ".acc-body{max-height:0;overflow:hidden;transition:max-height .3s ease;color:var(--muted);font-size:14px;line-height:1.6}"
        ".acc-body-inner{padding:2px 18px 16px}.acc-body p{margin-bottom:9px}.acc-body b,.acc-body strong{color:var(--text);font-weight:600}"
        ".acc-body h2{font-size:15px;font-weight:600;color:var(--text);margin:16px 0 6px;scroll-margin-top:16px}.acc-body h3{font-size:14px;font-weight:600;color:var(--text2);margin:12px 0 5px;scroll-margin-top:16px}"
        ".acc-body ul,.acc-body ol{margin:6px 0 10px;padding-left:22px}.acc-body li{margin:3px 0;scroll-margin-top:16px}.acc-body .spec-table{margin:10px 0 12px}"
        ".acc-body a{color:var(--accent)}")
    script = ('<script>(function(){var items=[].slice.call(document.querySelectorAll(".acc-item"));'
              'function shut(o){o.classList.remove("open");o.querySelector(".acc-head").setAttribute("aria-expanded","false");o.querySelector(".acc-body").style.maxHeight=null;}'
              'function show(it){items.forEach(function(o){if(o!==it)shut(o);});it.classList.add("open");it.querySelector(".acc-head").setAttribute("aria-expanded","true");var b=it.querySelector(".acc-body");b.style.maxHeight=b.scrollHeight+"px";}'
              'items.forEach(function(it){it.querySelector(".acc-head").addEventListener("click",function(){if(it.classList.contains("open"))shut(it);else show(it);});});'
              'function go(id,smooth){var el=id&&document.getElementById(id);if(!el)return;var it=el.closest(".acc-item");if(it&&!it.classList.contains("open"))show(it);'
              'setTimeout(function(){el.scrollIntoView({block:"start",behavior:smooth?"smooth":"auto"});},it?320:0);}'
              'document.addEventListener("click",function(e){var a=e.target.closest("a[href^=\'#\']");if(!a)return;var id=a.getAttribute("href").slice(1);if(!document.getElementById(id))return;e.preventDefault();history.pushState(null,"","#"+id);go(id,true);});'
              'window.addEventListener("hashchange",function(){go(location.hash.slice(1),true);});'
              'window.addEventListener("resize",function(){var o=document.querySelector(".acc-item.open .acc-body");if(o)o.style.maxHeight=o.scrollHeight+"px";});'
              'if(location.hash)go(decodeURIComponent(location.hash.slice(1)),false);})();</script>' + OUTLINE_SPY)
    aside = '<aside class="sidebar"><div class="sb-tabs"><span class="on">%s</span></div><nav class="outline" aria-label="%s">%s</nav></aside>' % (esc(loc.t("Help")), esc(loc.t("Help")), "".join(outline))
    page = page_shell("help.html", "KillerBench | " + loc.t("Help"), loc.t("How to use the KillerBench Windows diagnostic and repair workbench."), "help", style, aside, hero + "".join(inner), script, loc)
    if write:
        output(loc, "help.html", page)
    return entries


def sections(body):
    """Split rendered Markdown at each h2 into (anchor, title, html, subsections)."""
    parts = re.split(r'(?=<h2 id=")', body)
    lede, out = parts[0], []
    for part in parts[1:]:
        m = re.match(r'<h2 id="([^"]+)">(.*?)</h2>', part)
        subs = re.findall(r'<h3 id="([^"]+)">(.*?)</h3>', part)
        out.append((m.group(1), m.group(2), part, subs))
    return lede, out


def lede_text(lede):
    return re.sub(r"<[^>]+>", "", lede).strip()


def build_technical(loc=EN, write=True):
    body, headings, _, markdown = render_page("technical", loc)
    lede, secs = sections(tables(relink(without_title(body), False)))
    columns, weights = [[], []], [0, 0]
    for sec in secs:
        col = 0 if weights[0] <= weights[1] else 1
        columns[col].append(sec)
        weights[col] += len(re.sub(r"<[^>]+>", "", sec[2]))
    grid = "".join('<div class="tech-column">%s</div>' % "".join('<div class="tech-sec" id="%s">%s</div>' % (a, h.replace(' id="%s"' % a, "", 1)) for a, _, h, _ in col) for col in columns)
    outline = []
    for a, t, _, subs in secs:
        outline.append('<a href="#%s">%s</a>' % (a, t))
        outline.extend('<a class="sub" href="#%s">%s</a>' % (sa, st) for sa, st in subs)
    hero = ('<section class="page-hero"><span class="page-title">%s</span>'
            '<img class="wm-logo" src="brand/killerbench-logo-dark-yellow.svg" alt="KillerBench"><p>%s</p></section>' % (esc(loc.t("The Tech of")), esc(lede_text(lede))))
    style = OUTLINE_CSS + (
        ".content-inner{max-width:1440px;margin:0 auto;padding:0 28px 56px}"
        ".tech-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px;align-items:start}.tech-column{display:grid;gap:18px;min-width:0}"
        ".tech-sec{min-width:0;margin:0;padding:20px 22px;border:1px solid var(--pane-border);border-top:2px solid var(--sel,var(--accent));border-radius:10px;background:var(--modal) url('grain.png?v=2') repeat;background-size:256px 256px;color:var(--muted);font-size:14px;line-height:1.65}"
        ".tech-sec,.tech-sec h3{scroll-margin-top:20px}.tech-sec h2{font-family:var(--kt-font-card-title);font-weight:normal;font-size:21px;color:var(--accent);margin:0 0 10px}"
        ".tech-sec h3{font-size:15px;font-weight:600;color:var(--text);margin:16px 0 6px}.tech-sec p{margin:0 0 10px}.tech-sec b,.tech-sec strong{color:var(--text)}"
        ".tech-sec ul,.tech-sec ol{margin:6px 0 12px;padding-left:22px}.tech-sec li{margin:3px 0}.tech-sec table{width:100%;margin:8px 0 12px}.tech-sec a{color:var(--accent)}"
        ".tech-sec>:last-child{margin-bottom:0}@media (max-width:1050px){.tech-grid{grid-template-columns:1fr}}")
    aside = '<aside class="sidebar"><div class="sb-tabs"><span class="on">%s</span></div><nav class="outline" aria-label="%s">%s</nav></aside>' % (esc(loc.t("Technical")), esc(loc.t("Technical")), "".join(outline))
    page = page_shell("technical.html", "KillerBench | " + loc.t("Technical"), lede_text(lede), "technical", style, aside, hero + '<div class="tech-grid">%s</div>' % grid, OUTLINE_SPY, loc)
    if write:
        output(loc, "technical.html", page)
    return [{"u": "technical.html", "a": a, "g": loc.t("Technical"), "t": re.sub(r"<[^>]+>", "", t),
             "h": [[a, re.sub(r"<[^>]+>", "", t)]] + [[sa, re.sub(r"<[^>]+>", "", st)] for sa, st in subs],
             "x": re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", h))).strip()[:6000]} for a, t, h, subs in secs]


def build_about(loc=EN):
    body, _, _, _ = render_page("about", loc)
    lede, secs = sections(tables(relink(without_title(body), False)))
    cards = "".join('<div class="about-card" id="%s">%s</div>' % (a, h.replace(' id="%s"' % a, "", 1)) for a, _, h, _ in secs)
    outline = "".join('<a href="#%s">%s</a>' % (a, t) for a, t, _, _ in secs)
    hero = ('<section class="page-hero"><span class="page-title">%s</span>'
            '<img class="wm-logo" src="brand/killerbench-logo-dark-yellow.svg" alt="KillerBench"></section>'
            '<div class="about-card" id="overview">%s</div>' % (esc(loc.t("About")), lede.replace(' class="doc-lede"', "")))
    style = OUTLINE_CSS + (".content-inner{max-width:880px;margin:0 auto;padding:0 28px 56px}.about-card{scroll-margin-top:20px}"
                           ".about-card ul{margin:6px 0 0;padding-left:22px;color:var(--muted);font-size:14.5px;line-height:1.7}")
    aside = '<aside class="sidebar"><div class="sb-tabs"><span class="on">%s</span></div><nav class="outline" aria-label="%s"><a href="#overview">KillerBench</a>%s</nav></aside>' % (esc(loc.t("About")), esc(loc.t("About")), outline)
    page = page_shell("about.html", "KillerBench | " + loc.t("About"), lede_text(lede), "about", style, aside, hero + cards, OUTLINE_SPY, loc)
    output(loc, "about.html", page)


def build_pages():
    entries = build_help() + build_technical()
    build_about()
    for code in DOC_LOCALES:
        loc = Loc(code)
        build_help(loc)
        build_technical(loc)
        build_about(loc)
    return entries


def search_entries(code):
    """Localized Help and Technical search entries for build-locales.py, relative to the locale folder."""
    loc = Loc(code)
    return build_help(loc, write=False) + build_technical(loc, write=False)


def build():
    index = build_pages()
    for site in SITES[1:]:
        folder = ROOT / site["folder"]
        for page_id, nav_title, _ in site["pages"]:
            source = folder / "docs" / (page_id + ".md")
            markdown = source.read_text(encoding="utf-8")
            body, headings, title = render(markdown)
            title = title or nav_title
            file = page_file(page_id)
            first = next((line.strip() for line in markdown.splitlines() if line.strip() and not line.startswith("#")), "")
            summary = re.sub(r"[`*\[\]]|\([^)]*\.html[^)]*\)", "", first)
            canonical = "https://killerbench.net/%s/%s" % (site["folder"], "" if file == "index.html" else file)
            page = ('<!doctype html>\n<html lang="en-US" data-theme="dark"><head><meta charset="utf-8">'
                    '<meta name="viewport" content="width=device-width,initial-scale=1">'
                    '<meta name="description" content="%s"><link rel="canonical" href="%s">'
                    '<title>%s | KillerBench %s</title>'
                    '<link rel="stylesheet" href="../family.css?v=13"><link rel="stylesheet" href="../bench.css?v=7">'
                    '<link rel="stylesheet" href="../engine/engine-docs.css?v=%s">'
                    '<script defer src="../bench.js?v=7"></script><script defer src="../docs-search-index.js?v=%s"></script>'
                    '<script defer src="../docs-search.js?v=%s"></script></head>'
                    '<body data-brand-base="../brand">%s<div class="shell engine-shell">%s'
                    '<main class="content"><div class="content-scroll"><div class="engine-doc-wrap">'
                    '<article class="engine-doc" id="engineDoc">%s</article>%s</div></div></main></div>'
                    '<footer class="statusbar"><span class="left">%s &middot; In development &middot; No public download yet</span>'
                    '<span class="right">%s &middot; &copy; 2026 <b><a href="https://thekiller.net">Steve the Killer</a></b></span></footer>'
                    '</body></html>\n') % (
                esc(summary or site["description"]), canonical, esc(title), esc(site["label"].lower()), ASSET, ASSET, ASSET,
                header(site), sidebar(site, page_id, headings), body, pager(site, page_id), esc(site["label"]), VERSION)
            output = folder / file
            if output.exists():
                output.read_text(encoding="utf-8")
            output.write_text(page, encoding="utf-8", newline="\n")
            index.append({"u": "%s/%s" % (site["folder"], file), "g": site["label"], "t": title,
                          "h": [[a, re.sub(r"[`*]", "", t)] for a, t, _ in headings], "x": plain(markdown)[:6000]})
    search_index = ROOT / "docs-search-index.js"
    if search_index.exists():
        search_index.read_text(encoding="utf-8")
    search_index.write_text(
        "window.KillerBenchSearch=" + json.dumps(index, ensure_ascii=False, separators=(",", ":")) + ";\n", encoding="utf-8", newline="\n")
    print("Built %d pages." % len(index))


if __name__ == "__main__":
    build()
    import runpy
    runpy.run_path(str(ROOT / 'build-locales.py'))['build']()
