"""Build the flat KillerBench site in ../bench-landing, the same shape as the other family sites.

The Markdown guides, page templates and locale catalogs live here, outside the public folder.
This script renders them with build-docs.py and build-locales.py in a temporary staging copy
(which still produces one folder per language, and runs their own checks there), then flattens
the result: one English page per route, every translation moved into kb-i18n.js and applied in
the browser by kb.js, one combined search index, and the language menu as buttons.

Run: python build-site.py            build ../bench-landing and check it
     python build-site.py --check    only check ../bench-landing
Needs only Python 3.
"""
import difflib
import hashlib
import html
import json
import os
import posixpath
import re
import shutil
import subprocess
import sys
import tempfile
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit

sys.dont_write_bytecode = True
SRC = Path(__file__).resolve().parent
SITE = SRC.parent / 'bench-landing'
# Locale code in the catalogs -> short code used by the language menu, localStorage and ?lang=.
SHORT = {'en-US': 'en', 'bn': 'bn', 'cs-CZ': 'cs', 'de-DE': 'de', 'es': 'es', 'fr-FR': 'fr', 'hu-HU': 'hu', 'it-IT': 'it',
         'ja-JP': 'ja', 'kk-KZ': 'kk', 'nb-NO': 'nb', 'pl-PL': 'pl', 'pt-BR': 'pt', 'ru-RU': 'ru', 'sl-SI': 'sl',
         'tr-TR': 'tr', 'uk-UA': 'uk', 'vi-VN': 'vi', 'zh-CN': 'zh-cn', 'zh-TW': 'zh'}
LOCALES = list(SHORT)
SOURCES = ['templates', 'locales', 'help', 'engine/docs', 'build-docs.py', 'build-locales.py', 'test-site.py']
PAGES = ['index.html', 'help.html', 'technical.html', 'about.html', 'mcp.html', 'engine/index.html']
VOID = {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr'}
LEAF = {'p', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'td', 'th', 'dt', 'dd', 'figcaption', 'summary', 'caption', 'title', 'button', 'label', 'option', 'legend'}
BLOCK = {'div', 'p', 'ul', 'ol', 'li', 'table', 'section', 'article', 'nav', 'aside', 'header', 'footer', 'figure', 'pre', 'dl', 'blockquote', 'details', 'tr', 'tbody', 'thead', 'form', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'}
ATTRS = ('title', 'aria-label', 'alt', 'placeholder')
META = {'description', 'og:title', 'og:description', 'twitter:title', 'twitter:description'}
REQUIRED = {
    'index.html': ['Twelve sections, one habit', 'Privacy and Storage', 'Public downloads are not available yet.'],
    'help.html': ['Unreleased', 'GPLv3', 'href="license.txt"', 'Corresponding Source', '.NET Desktop Runtime 10 for Windows x64', 'framework-dependent', 'Native repair validation is still in progress.'],
    'about.html': ['GPLv3', 'href="license.txt"', 'complete implementation is not publicly available', 'distribution license', 'third-party notices', 'font permissions still need verification'],
    'mcp.html': ['Installing KillerMCP does not supply KillerBench.'],
}
DASHES = ('–', '—')


class Node:
    def __init__(self, tag, attrs=None, parent=None):
        self.tag, self.attrs, self.kids, self.parent = tag, [list(a) for a in (attrs or [])], [], parent

    def get(self, name):
        for k, v in self.attrs:
            if k == name:
                return v
        return None

    def set(self, name, value):
        for pair in self.attrs:
            if pair[0] == name:
                pair[1] = value
                return
        self.attrs.append([name, value])

    def elements(self):
        return [k for k in self.kids if isinstance(k, Node)]


class Raw(str):
    """Markup or script text written out exactly."""


class Comment(str):
    pass


class Tree(HTMLParser):
    def __init__(self, source):
        super().__init__(convert_charrefs=True)
        self.root = self.cur = Node('#root')
        self.feed(source)
        self.close()

    def handle_decl(self, decl):
        self.cur.kids.append(Raw('<!' + decl + '>'))

    def handle_starttag(self, tag, attrs):
        node = Node(tag, attrs, self.cur)
        self.cur.kids.append(node)
        if tag not in VOID:
            self.cur = node

    def handle_startendtag(self, tag, attrs):
        self.cur.kids.append(Node(tag, attrs, self.cur))

    def handle_endtag(self, tag):
        node = self.cur
        while node is not self.root and node.tag != tag:
            node = node.parent
        if node is not self.root:
            self.cur = node.parent

    def handle_data(self, data):
        self.cur.kids.append(Raw(data) if self.cur.tag in ('script', 'style') else data)

    def handle_comment(self, data):
        self.cur.kids.append(Comment(data))


def serialize(node, remap=None):
    out = []
    for kid in node.kids:
        if isinstance(kid, Node):
            attrs = ''
            for k, v in kid.attrs:
                if v is not None and remap and k in ('href', 'src'):
                    v = remap(v)
                attrs += ' ' + k + ('' if v is None else '="' + html.escape(v, quote=True) + '"')
            out.append('<' + kid.tag + attrs + '>')
            if kid.tag not in VOID:
                out.append(serialize(kid, remap) + '</' + kid.tag + '>')
        elif isinstance(kid, Comment):
            out.append('<!--' + kid + '-->')
        elif isinstance(kid, Raw):
            out.append(kid)
        else:
            out.append(html.escape(kid, quote=False))
    return ''.join(out)


def norm(value):
    return ' '.join(value.split())


def has_text(node):
    return any(isinstance(k, str) and not isinstance(k, (Raw, Comment)) and k.strip() for k in node.kids)


def any_text(node):
    return has_text(node) or any(any_text(k) for k in node.elements() if k.tag not in ('script', 'style'))


def any_block(node):
    return any(k.tag in BLOCK or any_block(k) for k in node.elements())


def skipped(node):
    return node.tag in ('script', 'style', 'svg') or 'lang-switch' in (node.get('class') or '').split()


def dropped(node):
    return node.tag == 'link' and node.get('rel') == 'alternate'


def units(node, found):
    for kid in node.elements():
        if skipped(kid):
            continue
        if has_text(kid) or (kid.tag in LEAF and any_text(kid) and not any_block(kid)):
            found.append(kid)
        else:
            units(kid, found)
    return found


def signature(node):
    return node.tag + '.' + (node.get('class') or '') + '#' + (node.get('id') or '')


def align(en, lo, mapping):
    mapping[id(en)] = lo
    ek = [k for k in en.elements() if not dropped(k)]
    lk = [k for k in lo.elements() if not dropped(k)]
    if [k.tag for k in ek] == [k.tag for k in lk]:
        pairs = zip(ek, lk)
    else:
        matcher = difflib.SequenceMatcher(None, [signature(k) for k in ek], [signature(k) for k in lk], autojunk=False)
        pairs = [(ek[a + i], lk[b + i]) for a, b, size in matcher.get_matching_blocks() for i in range(size)]
    for e, l in pairs:
        if e.tag == l.tag:
            align(e, l, mapping)


def remapper(from_page, to_page, locale):
    """Rewrite a locale page's local link so it works from the flat English page."""
    def remap(value):
        u = urlsplit(value)
        if u.scheme or u.netloc or not u.path or value.startswith('#'):
            return value
        target = posixpath.normpath(posixpath.join(posixpath.dirname(from_page), unquote(u.path)))
        if target.startswith(locale + '/'):
            target = target[len(locale) + 1:]
        result = posixpath.relpath(target, posixpath.dirname(to_page) or '.')
        return result + ('?' + u.query if u.query else '') + ('#' + u.fragment if u.fragment else '')
    return remap


def walk(node):
    yield node
    for kid in node.elements():
        yield from walk(kid)


class Keys:
    def __init__(self):
        self.text, self.attr, self.values = {}, {}, {SHORT[l]: {} for l in LOCALES[1:]}

    def key(self, table, english, prefix):
        if english not in table:
            table[english] = prefix + str(len(self.text) + len(self.attr) + 1)
        return table[english]

    def record(self, key, translations):
        for lang, value in translations.items():
            self.values[lang].setdefault(key, value)


def language_menu(catalogs, keys):
    label = {SHORT[l]: c['strings'].get('Choose language', 'Choose language') for l, c in catalogs.items() if l != 'en-US'}
    key = keys.key(keys.attr, 'Choose language', 'a')
    keys.record(key, {k: v for k, v in label.items() if v != 'Choose language'})
    items = ''.join('<button type="button" class="lang-item" data-lang="%s" lang="%s" aria-pressed="%s">%s</button>' % (
        SHORT[l], l, 'true' if l == 'en-US' else 'false', html.escape(catalogs[l]['language'])) for l in LOCALES)
    return Raw('<div class="lang-switch"><button class="lang-toggle" id="languageToggle" aria-haspopup="true" aria-expanded="false" '
               'aria-label="Choose language" data-i18n-attr="aria-label:%s">EN</button><div class="lang-menu" id="languageMenu" hidden>%s</div></div>' % (key, items))


def flatten_page(stage, page, keys, menu, learn):
    tree = Tree((stage / page).read_text(encoding='utf-8')).root
    mappings = {}
    if learn:
        for locale in LOCALES[1:]:
            path = stage / locale / page
            if path.is_file():
                mapping = {}
                align(tree, Tree(path.read_text(encoding='utf-8')).root, mapping)
                mappings[locale] = mapping
    for unit in units(tree, []):
        english = serialize(unit)
        translations = {}
        for locale, mapping in mappings.items():
            other = mapping.get(id(unit))
            if other is not None and other.tag == unit.tag:
                value = serialize(other, remapper(locale + '/' + page, page, locale))
                if norm(value) != norm(english):
                    translations[SHORT[locale]] = value.strip()
        if translations or norm(english) in keys.text:
            key = keys.key(keys.text, norm(english), 't')
            keys.record(key, translations)
            unit.set('data-i18n', key)
    for node in walk(tree):
        names = [a for a in ATTRS if node.get(a)] + [k for k, v in node.attrs if k.startswith('data-ui-') and v]
        if node.tag == 'meta' and (node.get('name') in META or node.get('property') in META):
            names.append('content')
        pairs = []
        for name in names:
            english = node.get(name)
            translations = {}
            for locale, mapping in mappings.items():
                other = mapping.get(id(node))
                value = other.get(name) if other is not None and other.tag == node.tag else None
                if value and norm(value) != norm(english):
                    translations[SHORT[locale]] = value
            if translations or norm(english) in keys.attr:
                key = keys.key(keys.attr, norm(english), 'a')
                keys.record(key, translations)
                pairs.append(name + ':' + key)
        if pairs and not node.get('data-i18n-attr'):
            node.set('data-i18n-attr', ';'.join(pairs))
    prefix = '../' * page.count('/')
    for node in walk(tree):
        kids = []
        for kid in node.kids:
            if isinstance(kid, Node) and dropped(kid):
                continue
            if isinstance(kid, Node) and 'lang-switch' in (kid.get('class') or '').split():
                kid = menu
            if isinstance(kid, Comment) and kid.strip() == 'language-menu':
                kid = menu
            if isinstance(kid, Node) and kid.tag == 'script' and re.match(r'(\.\./)*kb\.js', kid.get('src') or ''):
                kids.append(Raw('<script defer src="%skb-i18n.js?v=%s"></script>' % (prefix, BUILD)))
            kids.append(kid)
        node.kids = kids
        if node.tag == 'html':
            node.set('lang', 'en-US')
    return serialize(tree).rstrip() + '\n'


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8', newline='\n')


def run(stage, *args):
    env = dict(os.environ, PYTHONIOENCODING='utf-8', PYTHONDONTWRITEBYTECODE='1')
    result = subprocess.run([sys.executable, '-B', *args], cwd=stage, env=env, capture_output=True, text=True, encoding='utf-8')
    print((result.stdout + result.stderr).strip().splitlines()[-1] if (result.stdout + result.stderr).strip() else args[0])
    if result.returncode:
        raise SystemExit(result.stdout + result.stderr)


def search_index(text):
    return json.loads(text.strip().removeprefix('window.KillerBenchSearch=').removesuffix(';'))


BUILD = '1'


def build():
    global BUILD
    catalogs = {l: json.loads((SRC / 'locales' / (l + '.json')).read_text(encoding='utf-8')) for l in LOCALES if (SRC / 'locales' / (l + '.json')).is_file()}
    with tempfile.TemporaryDirectory(prefix='kb-site-') as temp:
        stage = Path(temp)
        for item in SITE.iterdir():
            if item.name in LOCALES:
                continue
            (shutil.copytree if item.is_dir() else shutil.copyfile)(item, stage / item.name)
        for name in SOURCES:
            source = SRC / name
            (shutil.copytree if source.is_dir() else shutil.copyfile)(source, stage / name)
        run(stage, 'build-locales.py')
        run(stage, 'build-locales.py', '--check')
        run(stage, 'test-site.py')
        catalogs = {l: json.loads((stage / 'locales' / (l + '.json')).read_text(encoding='utf-8')) for l in catalogs}
        keys = Keys()
        menu = language_menu(catalogs, keys)
        guides = sorted(p.relative_to(stage).as_posix() for p in (stage / 'engine').glob('*.html') if p.name != 'index.html')
        pages = {}
        for page in PAGES + guides:
            pages[page] = flatten_page(stage, page, keys, menu, page in PAGES)
        searches = {'en': search_index((stage / 'docs-search-index.js').read_text(encoding='utf-8'))}
        for locale in LOCALES[1:]:
            entries = search_index((stage / locale / 'docs-search-index.js').read_text(encoding='utf-8'))
            for entry in entries:
                entry['u'] = entry['u'][3:] if entry['u'].startswith('../') else entry['u']
            searches[SHORT[locale]] = entries
    values = {lang: dict(sorted(v.items(), key=lambda kv: int(kv[0][1:]))) for lang, v in keys.values.items()}
    BUILD = hashlib.sha256(json.dumps(values, sort_keys=True).encode('utf-8')).hexdigest()[:8]
    for page, text in pages.items():
        write(SITE / page, re.sub(r'kb-i18n\.js\?v=\w+', 'kb-i18n.js?v=' + BUILD, text))
    write(SITE / 'kb-i18n.js', '/* Generated by bench-landing-src/build-site.py from locales/*.json and help/docs/i18n. Do not edit. */\n'
          'window.KB_I18N=' + json.dumps(values, ensure_ascii=False, separators=(',', ':')) + ';\n')
    write(SITE / 'docs-search-index.js', 'window.KillerBenchSearch=' + json.dumps(searches, ensure_ascii=False, separators=(',', ':')) + ';\n')
    for locale in LOCALES[1:]:
        if (SITE / locale).is_dir():
            shutil.rmtree(SITE / locale)
    redirects = ['# Cloudflare Pages redirects. Generated by bench-landing-src/build-site.py.',
                 '# The site used to publish one folder per language. Those routes now open the single page',
                 '# with ?lang=, which kb.js applies and remembers.']
    for locale in LOCALES[1:]:
        redirects += ['/%s  /?lang=%s  301' % (locale, SHORT[locale]), '/%s/*  /:splat?lang=%s  301' % (locale, SHORT[locale])]
    write(SITE / '_redirects', '\n'.join(redirects) + '\n')
    counts = {lang: len(v) for lang, v in values.items()}
    print('Flattened %d pages; %d translation keys; per language %d to %d values.' % (len(pages), len(keys.text) + len(keys.attr), min(counts.values()), max(counts.values())))


class Refs(HTMLParser):
    def __init__(self, text):
        super().__init__(convert_charrefs=True)
        self.refs, self.ids, self.keys, self.langs = [], set(), [], 0
        self.feed(text)

    def handle_starttag(self, tag, attrs):
        d = dict(attrs)
        for name in ('href', 'src'):
            if d.get(name) and not (tag == 'link' and d.get('rel') == 'canonical'):
                self.refs.append(d[name])
        if d.get('id'):
            self.ids.add(d['id'])
        if d.get('name') and tag == 'a':
            self.ids.add(d['name'])
        if d.get('data-i18n'):
            self.keys.append(d['data-i18n'])
        for pair in (d.get('data-i18n-attr') or '').split(';'):
            if ':' in pair:
                self.keys.append(pair.split(':', 1)[1])
        if tag == 'button' and 'lang-item' in (d.get('class') or ''):
            self.langs += 1


def check():
    problems, checked = [], 0
    values = json.loads((SITE / 'kb-i18n.js').read_text(encoding='utf-8').split('window.KB_I18N=', 1)[1].strip().removesuffix(';'))
    if set(values) != set(SHORT.values()) - {'en'}:
        problems.append('kb-i18n.js languages differ: ' + str(sorted(values)))
    parsed = {p.relative_to(SITE).as_posix(): Refs(p.read_text(encoding='utf-8')) for p in SITE.rglob('*.html')}
    used = set()

    def resolve(page, ref):
        u = urlsplit(html.unescape(ref))
        if u.scheme or u.netloc:
            return None
        if u.path.startswith('/'):
            target = u.path.lstrip('/')
        else:
            target = posixpath.normpath(posixpath.join(posixpath.dirname(page), unquote(u.path))) if u.path else page
        if target in ('', '.') or (SITE / target).is_dir():
            target = posixpath.join(target if target not in ('', '.') else '', 'index.html').lstrip('/')
        return target, u.fragment

    def verify(page, ref, where):
        nonlocal checked
        resolved = resolve(page, ref)
        if not resolved:
            return
        target, fragment = resolved
        checked += 1
        if target.startswith('..') or not (SITE / target).is_file():
            problems.append('%s: missing %s (%s)' % (page, ref, where))
        elif fragment and target.endswith('.html') and fragment not in parsed[target].ids:
            problems.append('%s: missing fragment %s (%s)' % (page, ref, where))

    for page, info in parsed.items():
        for ref in info.refs:
            verify(page, ref, 'page')
        for key in info.keys:
            used.add(key)
            for lang, table in values.items():
                if key in table:
                    for ref in re.findall(r'(?:href|src)="([^"]+)"', table[key]):
                        verify(page, ref, lang + ' ' + key)
        if page != '404.html' and info.langs != len(SHORT):
            problems.append('%s: language menu has %d entries' % (page, info.langs))
    for css in SITE.rglob('*.css'):
        for ref in re.findall(r'url\(\s*["\']?([^"\')]+)', css.read_text(encoding='utf-8')):
            if not ref.startswith('data:'):
                verify(css.relative_to(SITE).as_posix(), ref, 'css')
    unused = {k for table in values.values() for k in table} - used
    if unused:
        problems.append('translation keys not used on any page: %d' % len(unused))
    search = json.loads((SITE / 'docs-search-index.js').read_text(encoding='utf-8').removeprefix('window.KillerBenchSearch=').strip().removesuffix(';'))
    for lang, entries in search.items():
        for entry in entries:
            verify('engine/index.html', '../' + entry['u'], 'search ' + lang)
    for path in list(SITE.rglob('*.html')) + list(SITE.rglob('*.js')) + list(SITE.rglob('*.css')):
        text = path.read_text(encoding='utf-8')
        if any(d in text for d in DASHES):
            problems.append('%s: en or em dash' % path.relative_to(SITE))
    for page, phrases in REQUIRED.items():
        text = (SITE / page).read_text(encoding='utf-8')
        for phrase in phrases:
            if phrase not in text:
                problems.append('%s: required pre-release statement absent: %s' % (page, phrase))
    for junk in ('templates', 'locales', '__pycache__', 'help', 'engine/docs') + tuple(LOCALES[1:]):
        if (SITE / junk).exists():
            problems.append('source or generated folder in the public site: ' + junk)
    if problems:
        raise SystemExit('Site check failed:\n  ' + '\n  '.join(problems[:60]) + ('\n  ...' if len(problems) > 60 else ''))
    print('Site check passed: %d pages, %d languages, %d local references (including translated markup and search).' % (len(parsed), len(values) + 1, checked))


if __name__ == '__main__':
    if '--check' not in sys.argv:
        build()
    check()
