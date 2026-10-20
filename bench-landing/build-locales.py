"""Generate the three localized entry pages and validate their local links."""
import html
import json
import posixpath
import re
import sys
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlsplit, unquote

ROOT = Path(__file__).resolve().parent
LOCALES = ['en-US', 'bn', 'cs-CZ', 'de-DE', 'es', 'fr-FR', 'hu-HU', 'it-IT', 'ja-JP', 'kk-KZ', 'nb-NO', 'pl-PL', 'pt-BR', 'ru-RU', 'tr-TR', 'uk-UA', 'vi-VN', 'zh-CN', 'zh-TW']
PAGES = ['index.html', 'engine/index.html']
# Help, Technical and About are rendered per locale by build-docs.py from translated Markdown.
DOC_PAGES = ['help.html', 'technical.html', 'about.html']

def doc_locales():
    return [loc for loc in LOCALES[1:] if (ROOT / 'help' / 'docs' / 'i18n' / loc / 'ui.json').is_file()]
VOID = {'area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'param', 'source', 'track', 'wbr'}

def route(locale, page):
    return '/' + (locale + '/' if locale != 'en-US' else '') + (page[:-10] if page.endswith('index.html') else page)

def out_path(locale, page):
    return (locale + '/' if locale != 'en-US' else '') + page

# Local references stay relative, like the other family landing pages, so the site works from
# disk as well as from killerbench.net. Only canonical, og:url and hreflang links are absolute.
def rel(locale, page, dest):
    return posixpath.relpath(dest, posixpath.dirname(out_path(locale, page)) or '.')

class Localized(HTMLParser):
    def __init__(self, source, page, locale, catalog, available, collect=False):
        super().__init__(convert_charrefs=True)
        self.page, self.locale, self.catalog = page, locale, catalog
        self.available, self.collect = available, collect
        self.out, self.keys, self.skip = [], {}, 0
        self.feed(source)

    def text(self, value):
        key = value.strip()
        if not key:
            return value
        self.keys[key] = key
        translated = self.catalog.get(key, key)
        return value[:len(value) - len(value.lstrip())] + translated + value[len(value.rstrip()):]

    def href(self, value):
        u = urlsplit(value)
        if u.scheme or u.netloc or not u.path:
            return value
        target = (ROOT / unquote(u.path).lstrip('/')).resolve() if u.path.startswith('/') else (ROOT / self.page).parent.joinpath(unquote(u.path)).resolve()
        if target.is_dir():
            target = target / 'index.html'
        relative = target.relative_to(ROOT).as_posix()
        if relative in PAGES or (relative in DOC_PAGES and self.locale in doc_locales()):
            result = rel(self.locale, self.page, out_path(self.locale, relative))
        else:
            result = rel(self.locale, self.page, relative)
        return result + ('?' + u.query if u.query else '') + ('#' + u.fragment if u.fragment else '')

    def handle_decl(self, decl):
        self.out.append('<!' + decl + '>')

    def handle_starttag(self, tag, attrs):
        d = dict(attrs)
        if tag == 'html':
            d['lang'] = self.locale
        if tag == 'body':
            d['data-brand-base'] = rel(self.locale, self.page, 'brand')
            for key, value in {'theme': 'Theme', 'choose-theme': 'Choose theme', 'accent': 'Accent color', 'accent-label': 'accent:', 'close': 'Close', 'more': 'More', 'no-results': 'No matching pages.'}.items():
                d['data-ui-' + key] = self.text(value)
        if tag == 'meta' and (d.get('name') == 'description' or d.get('property') in ('og:title', 'og:description')):
            d['content'] = self.text(d['content'])
        if tag == 'meta' and d.get('property') == 'og:url':
            d['content'] = 'https://killerbench.net' + route(self.locale, self.page)
        for key in ('title', 'aria-label', 'alt', 'placeholder'):
            if key in d:
                d[key] = self.text(d[key])
        for key in ('href', 'src'):
            if key in d:
                d[key] = self.href(d[key])
        if tag == 'script' and posixpath.basename(urlsplit(d.get('src', '')).path) == 'docs-search-index.js' and self.locale != 'en-US':
            query = urlsplit(d['src']).query
            d['src'] = rel(self.locale, self.page, self.locale + '/docs-search-index.js') + ('?' + query if query else '')
        if tag == 'link' and d.get('rel') == 'canonical':
            d['href'] = 'https://killerbench.net' + route(self.locale, self.page)
        self.out.append('<' + tag + ''.join(' ' + k + ('="' + html.escape(v, quote=True) + '"' if v is not None else '') for k, v in d.items()) + '>')
        if tag in ('script', 'style'):
            self.skip += 1
        if tag == 'head':
            for locale in self.available:
                self.out.append('<link rel="alternate" hreflang="' + locale + '" href="https://killerbench.net' + route(locale, self.page) + '">')
            self.out.append('<link rel="alternate" hreflang="x-default" href="https://killerbench.net' + route('en-US', self.page) + '">')

    def handle_endtag(self, tag):
        if tag in ('script', 'style'):
            self.skip -= 1
        if tag == 'header':
            label = self.text('Choose language')
            self.out.append('<div class="lang-switch"><button class="lang-toggle" id="languageToggle" aria-haspopup="true" aria-expanded="false" aria-label="' + html.escape(label, quote=True) + '">' + html.escape(self.locale.split('-')[0].upper()) + '</button><div class="lang-menu" id="languageMenu" hidden>')
            for locale, data in self.available.items():
                self.out.append('<a class="lang-item" hreflang="' + locale + '" lang="' + locale + '" href="' + rel(self.locale, self.page, out_path(locale, self.page)) + '" aria-pressed="' + str(locale == self.locale).lower() + '">' + html.escape(data['language']) + '</a>')
            self.out.append('</div></div>')
        self.out.append('</' + tag + '>')
        if tag == 'h1' and self.page == 'engine/index.html':
            note = self.text('Detailed guides linked from this page are in English.')
            if self.locale != 'en-US':
                self.out.append('<p class="caption">' + html.escape(note) + '</p>')

    def handle_data(self, data):
        self.out.append(data if self.skip else html.escape(self.text(data)))

    def handle_comment(self, data):
        self.out.append('<!--' + data + '-->')

def write(path, text):
    if path.exists():
        path.read_text(encoding='utf-8')
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8', newline='\n')

def runpy_docs():
    import runpy
    return runpy.run_path(str(ROOT / 'build-docs.py'))

def build():
    sources = {p: (ROOT / ('templates/index.en.html' if p == 'index.html' else p)).read_text(encoding='utf-8') for p in PAGES}
    english = {}
    for page, source in sources.items():
        english.update(Localized(source, page, 'en-US', {}, {}, True).keys)
    catalog = {'language': 'English', 'strings': dict(sorted(english.items()))}
    write(ROOT / 'locales/en-US.json', json.dumps(catalog, ensure_ascii=False, indent=2) + '\n')
    available = {'en-US': catalog}
    for locale in LOCALES[1:]:
        path = ROOT / 'locales' / (locale + '.json')
        if path.exists():
            data = json.loads(path.read_text(encoding='utf-8'))
            if set(data['strings']) != set(english):
                raise ValueError(locale + ': locale keys differ from English catalog')
            if not data.get('language') or any(not isinstance(v, str) or not v.strip() for v in data['strings'].values()):
                raise ValueError(locale + ': empty or invalid translation')
            if any(chr(c) in v for v in data['strings'].values() for c in (0x2013, 0x2014)):
                raise ValueError(locale + ': prohibited dash')
            available[locale] = data
    for locale, data in available.items():
        localized_search = []
        for page, source in sources.items():
            result = Localized(source, page, locale, data['strings'], available)
            output = ROOT / (locale if locale != 'en-US' else '') / page
            write(output, ''.join(result.out).rstrip() + '\n')
            if page != 'index.html':
                rendered = ''.join(result.out)
                title = html.unescape(re.search(r'<title>(.*?)</title>', rendered, re.S).group(1)).split(' | ')[0]
                article = re.search(r'<article[^>]*>(.*?)</article>', rendered, re.S).group(1)
                headings = [[a, html.unescape(re.sub('<[^>]+>', '', t))] for a, t in re.findall(r'<h2 id="([^"]+)">(.*?)</h2>', article, re.S)]
                localized_search.append({'u': page, 'g': data['strings'].get('User guide' if page.startswith('help/') else 'Developer guide', 'User guide' if page.startswith('help/') else 'Developer guide'), 't': title, 'h': headings, 'x': html.unescape(re.sub('<[^>]+>', ' ', article))})
        if locale != 'en-US':
            english_index = (ROOT / 'docs-search-index.js').read_text(encoding='utf-8')
            details = json.loads(english_index.removeprefix('window.KillerBenchSearch=').strip().removesuffix(';'))
            localized_docs = []
            if locale in doc_locales():
                localized_docs = runpy_docs()['search_entries'](locale)
            details = [dict(p, u='../' + p['u']) for p in details if p['u'] not in PAGES and not (localized_docs and p['u'] in DOC_PAGES)]
            details = localized_docs + details
            write(ROOT / locale / 'docs-search-index.js', 'window.KillerBenchSearch=' + json.dumps(localized_search + details, ensure_ascii=False, separators=(',', ':')) + ';\n')
    print('Built localized entry pages for ' + str(len(available)) + ' locales; English catalog has ' + str(len(english)) + ' keys.')

def check():
    locales = [loc for loc in LOCALES if (ROOT / 'locales' / (loc + '.json')).exists()]
    missing = [loc for loc in LOCALES if loc not in locales]
    if missing and '--partial' not in sys.argv:
        raise ValueError('Missing locales: ' + ', '.join(missing))
    english = json.loads((ROOT / 'locales/en-US.json').read_text(encoding='utf-8'))['strings']
    checked = 0
    for locale in locales:
        catalog = json.loads((ROOT / 'locales' / (locale + '.json')).read_text(encoding='utf-8'))['strings']
        assert set(catalog) == set(english), locale
        assert all(isinstance(value, str) and value.strip() and not any(chr(code) in value for code in (0x2013, 0x2014)) for value in catalog.values()), locale
        for page in PAGES:
            path = ROOT / (locale if locale != 'en-US' else '') / page
            source = path.read_text(encoding='utf-8')
            assert '<html lang="' + locale + '"' in source, path
            assert '<link rel="canonical" href="https://killerbench.net' + route(locale, page) + '"' in source, path
            for target in locales:
                assert '<link rel="alternate" hreflang="' + target + '" href="https://killerbench.net' + route(target, page) + '"' in source, (path, target)
            assert source.count('id="languageToggle"') == 1, path
            for entry in PAGES:
                assert 'href="' + rel(locale, page, out_path(locale, entry)) + '"' in source, (path, entry)
            for ref in re.findall(r'(?:href|src)="([^"]+)"', source):
                u = urlsplit(html.unescape(ref))
                if u.scheme or u.netloc:
                    continue
                assert not u.path.startswith('/'), ('root-absolute reference breaks local viewing', path, ref)
                target = path.parent / unquote(u.path)
                if not u.path:
                    target = path
                if target.is_dir():
                    target = target / 'index.html'
                assert target.exists(), (path, ref)
                if u.fragment and target.suffix == '.html':
                    assert 'id="' + u.fragment + '"' in target.read_text(encoding='utf-8'), (path, ref)
                checked += 1
        if locale in doc_locales():
            for page in DOC_PAGES:
                path = ROOT / locale / page
                source = path.read_text(encoding='utf-8')
                assert '<html lang="' + locale + '"' in source, path
                assert '<link rel="canonical" href="https://killerbench.net/' + locale + '/' + page + '"' in source, path
                assert source.count('id="languageToggle"') == 1, path
                for target in ['en-US'] + doc_locales():
                    assert '<link rel="alternate" hreflang="' + target + '" href="https://killerbench.net/' + ('' if target == 'en-US' else target + '/') + page + '"' in source, (path, target)
            home = (ROOT / locale / 'index.html').read_text(encoding='utf-8')
            assert 'href="help.html' in home and 'href="technical.html"' in home, (locale, 'home does not link localized guides')
        if locale != 'en-US':
            index = (ROOT / locale / 'docs-search-index.js').read_text(encoding='utf-8')
            for result in json.loads(index.removeprefix('window.KillerBenchSearch=').strip().removesuffix(';')):
                target = (ROOT / locale / 'help' / ('../' + result['u'])).resolve()
                assert target.exists(), (locale, result['u'])
    print('Locale coverage passed: ' + str(len(locales)) + ' locales, ' + str(len(locales) * len(PAGES)) + ' entry pages, ' + str(checked) + ' local references.')

if __name__ == '__main__':
    if '--check' in sys.argv:
        check()
    else:
        import runpy
        runpy.run_path(str(ROOT / 'build-docs.py'))['build']()
        build()
