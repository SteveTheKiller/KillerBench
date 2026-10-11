"""Validate generated public routes, assets, search links and pre-release copy.

Run: python test-site.py [path-to-clean-export]
No external requests, host diagnosis or third-party dependencies.
"""
import hashlib
import json
import re
import sys
import tempfile
import unittest
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parent
HELP = 'welcome setup first-run workspace safety fix-windows-update repair-system-files free-disk-space check-a-drive fix-network fix-printing encrypt-with-bitlocker investigate-crashes make-windows-private set-up-new-pc use-the-cli diagnostics repair updates devices network apps maintenance onboarding security bitlocker privacy storage reports cli troubleshooting'.split()
ENGINE = 'index getting-started workflows evidence repairs cli api architecture knowledge-packs'.split()
LOCALES = 'en-US bn cs-CZ de-DE es fr-FR hu-HU it-IT ja-JP kk-KZ nb-NO pl-PL pt-BR ru-RU tr-TR uk-UA vi-VN zh-CN zh-TW'.split()
EXCLUDED = {'templates', 'locales', 'docs', '__pycache__'}


class Page(HTMLParser):
    def __init__(self, text):
        super().__init__(convert_charrefs=True)
        self.refs, self.ids, self.canonical = [], [], []
        self.feed(text)

    def handle_starttag(self, tag, attrs):
        data = dict(attrs)
        if data.get('id'):
            self.ids.append(data['id'])
        if tag == 'link' and data.get('rel') == 'canonical':
            self.canonical.append(data.get('href', ''))
            return
        for name in ('href', 'src'):
            if data.get(name):
                self.refs.append(data[name])


def target(root, source, ref):
    url = urlsplit(ref)
    if url.scheme or url.netloc:
        return None, ''
    path = unquote(url.path)
    destination = root / path.lstrip('/') if path.startswith('/') else source.parent / path
    if not path:
        destination = source
    destination = destination.resolve()
    if not destination.is_relative_to(root.resolve()):
        raise ValueError(f'{source}: reference escapes public root: {ref}')
    if destination.is_dir() and (destination/'index.html').is_file():
        destination /= 'index.html'
    elif not destination.is_file() and not destination.suffix and not path.endswith('/'):
        destination = destination.with_suffix('.html')
    if not destination.is_file():
        raise ValueError(f'{source}: missing local reference: {ref}')
    return destination, unquote(url.fragment)


def validate_license(path):
    expected = '3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986'
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
        raise ValueError('Missing or modified canonical GNU GPLv3 license: '+str(path))


def validate(root, clean=False):
    root = root.resolve()
    required = ['index.html', 'mcp.html', '404.html', '_headers', 'license.txt']
    validate_license(root/'license.txt')
    required += ['help.html', 'technical.html', 'about.html'] + ['engine/'+p+'.html' for p in ENGINE]
    for locale in LOCALES[1:]:
        required += [locale+'/'+p for p in ('index.html', 'engine/index.html', 'docs-search-index.js')]
    for route in required:
        if not (root/route).is_file():
            raise ValueError('Missing public route or asset: '+route)
    if clean:
        for path in root.rglob('*'):
            relative = path.relative_to(root)
            if path.is_symlink() or EXCLUDED.intersection(relative.parts) or (path.is_file() and (path.suffix in {'.py', '.md', '.json', '.pyc'})):
                raise ValueError('Source file in public output: '+str(relative))
    pages = {}
    for path in root.rglob('*.html'):
        if not EXCLUDED.intersection(path.relative_to(root).parts):
            text = path.read_text(encoding='utf-8')
            page = Page(text)
            if len(page.ids) != len(set(page.ids)):
                raise ValueError('Duplicate HTML IDs: '+str(path))
            if path.name != '404.html' and (len(page.canonical) != 1 or not page.canonical[0].startswith('https://killerbench.net/')):
                raise ValueError('Missing or invalid canonical URL: '+str(path))
            if page.canonical:
                canonical_target, _ = target(root, path, urlsplit(page.canonical[0]).path)
                if canonical_target != path.resolve():
                    raise ValueError("Canonical points to a different route: "+str(path))
            pages[path.resolve()] = page
    count = 0
    for path, page in pages.items():
        for ref in page.refs:
            destination, fragment = target(root, path, ref)
            if destination and fragment and destination.suffix == '.html' and fragment not in pages[destination].ids:
                raise ValueError(f'{path}: missing fragment: {ref}')
            count += destination is not None
    for css in root.rglob('*.css'):
        for ref in re.findall(r'url\(\s*[\'"]?([^\'"\s)]+)', css.read_text(encoding='utf-8')):
            target(root, css, ref)
            count += 1
    for index in root.rglob('docs-search-index.js'):
        entries = json.loads(index.read_text(encoding='utf-8').removeprefix('window.KillerBenchSearch=').strip().removesuffix(';'))
        for entry in entries:
            destination, _ = target(root, index.parent/'engine/index.html', '../'+entry['u'])
            for fragment in [h[0] for h in entry['h']] + ([entry['a']] if entry.get('a') else []):
                if fragment not in pages[destination].ids:
                    raise ValueError(f'{index}: missing search heading: {entry["u"]}#{fragment}')
        if not any(p['u'].endswith('help.html') and p.get('a') == 'setup' for p in entries):
            raise ValueError('Setup missing from search: '+str(index))
    home = (root/'index.html').read_text(encoding='utf-8')
    setup = (root/'help.html').read_text(encoding='utf-8')
    about = (root/'about.html').read_text(encoding='utf-8')
    mcp = (root/'mcp.html').read_text(encoding='utf-8')
    for text, needed in [(home, ['Twelve sections, one habit', 'Privacy and Storage', 'Public downloads are not available yet.', 'Illustrative output, not a scan of your computer.']), (setup, ['Unreleased', 'GPLv3', 'href="license.txt"', 'Corresponding Source', '.NET Desktop Runtime 10 for Windows x64', 'framework-dependent', 'Native repair validation is still in progress.']), (about, ['GPLv3', 'href="license.txt"', 'complete implementation is not publicly available', 'distribution license', 'third-party notices', 'font permissions still need verification']), (mcp, ['Installing KillerMCP does not supply KillerBench.'])]:
        for phrase in needed:
            if phrase not in text:
                raise ValueError('Required pre-release statement absent: '+phrase)
    help_page = (root/'help.html').read_text(encoding='utf-8')
    for anchor in HELP:
        if 'href="#'+anchor+'"' not in help_page or 'id="'+anchor+'"' not in help_page:
            raise ValueError('Help section missing from the page or its outline: '+anchor)
    guide = (root/'engine/getting-started.html').read_text(encoding='utf-8')
    for phrase in ['Engine.Windows', 'WindowsEngine.Register();', 'KillerBench.Engine.Workflows', 'token: CancellationToken.None']:
        if phrase not in guide:
            raise ValueError('Engine prerequisite absent: '+phrase)
    print(f'Site checks passed: {len(pages)} HTML pages, {len(LOCALES)} language routes, {count} local HTML/CSS references; search routes and pre-release copy verified.')


class BrokenReferenceTests(unittest.TestCase):
    def test_missing_or_modified_license(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder)/'license.txt'
            with self.assertRaises(ValueError):
                validate_license(path)
            path.write_text('GNU GENERAL PUBLIC LICENSE\nIncomplete copy', encoding='utf-8')
            with self.assertRaises(ValueError):
                validate_license(path)

    def test_missing_asset_and_route(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for ref in ('missing.svg', '/missing/route', '../../outside'):
                with self.assertRaises(ValueError):
                    target(root, root/'index.html', ref)

    def test_duplicate_heading_generation(self):
        import runpy
        source = ROOT/'build-docs.py'
        render = runpy.run_path(str(source))['render']
        body, headings, _ = render('# Storage\n\n## Storage\n\n### Storage\n\n## Storage\n')
        self.assertEqual(Page(body).ids, ['storage', 'storage-2', 'storage-3', 'storage-4'])
        self.assertEqual([(h[0], h[2]) for h in headings], [('storage-2', 2), ('storage-3', 3), ('storage-4', 2)])

    def test_nested_and_extensionless_routes(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'mcp').mkdir()
            (root/'mcp.html').write_text('MCP', encoding='utf-8')
            self.assertEqual(target(root, root/'index.html', '/mcp')[0], (root/'mcp.html').resolve())
            (root/'help').mkdir()
            (root/'help/setup.html').write_text('<h1 id="setup">Setup</h1>', encoding='utf-8')
            destination, fragment = target(root, root/'help/index.html', '/help/setup#setup')
            self.assertEqual(destination, (root/'help/setup.html').resolve())
            self.assertEqual(fragment, 'setup')


if __name__ == '__main__':
    result = unittest.TextTestRunner().run(unittest.defaultTestLoader.loadTestsFromTestCase(BrokenReferenceTests))
    if not result.wasSuccessful():
        raise SystemExit(1)
    public = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT
    validate(public, clean=len(sys.argv) > 1)
