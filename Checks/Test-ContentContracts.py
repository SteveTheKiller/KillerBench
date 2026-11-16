"""Parsed product-content contracts. Run each repair slice with --only NAME."""
import argparse
import csv
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from html.parser import HTMLParser
from functools import cache
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
KEY = '{http://schemas.microsoft.com/winfx/2006/xaml}Key'


class Markup(HTMLParser):
    def __init__(self, text):
        super().__init__()
        self.references = []
        self.keys = []
        self.ids = []
        self.feed(text)

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        self.references.extend(attrs[name] for name in ('href', 'src') if name in attrs)
        self.keys.extend(value for name, value in attrs.items() if name.startswith('data-i18n'))
        if 'id' in attrs:
            self.ids.append(attrs['id'])


@cache
def page_ids(page):
    return Markup(page.read_text(encoding='utf-8')).ids


def local_links(markup, page):
    for reference in Markup(markup).references:
        if re.search(r'/(?:blob|raw)/main/license\.txt$', reference, re.I):
            assert reference.endswith('/LICENSE.txt') and (ROOT / 'LICENSE.txt').is_file(), 'Canonical license destination: ' + reference
        if re.match(r'^(https?:|mailto:|data:|javascript:)', reference, re.I):
            continue
        target, _, fragment = reference.partition('#')
        resolved = (page.parent / target.split('?')[0]).resolve() if target else page
        assert resolved.exists(), f'{page.name}: missing {reference}'
        if fragment and resolved.suffix == '.html':
            assert fragment in page_ids(resolved), reference


def links():
    script = "const fs=require('fs'),vm=require('vm');const c={};vm.createContext(c);vm.runInContext(fs.readFileSync(process.argv[1],'utf8'),c);process.stdout.write(JSON.stringify(c.I18N));"
    dictionaries = json.loads(subprocess.check_output(['node', '-e', script, str(ROOT / 'bench-landing/kb-i18n.js')], text=True, encoding='utf-8'))
    count = 0
    for page in (ROOT / 'bench-landing').rglob('*.html'):
        html = page.read_text(encoding='utf-8')
        local_links(html, page)
        keys = Markup(html).keys
        for locale, dictionary in dictionaries.items():
            for key in keys:
                if key in dictionary:
                    local_links(dictionary[key], page)
                    count += 1
    try:
        local_links('<a href="missing-contract-negative-control.txt">License</a>', ROOT / 'bench-landing/about.html')
    except AssertionError:
        pass
    else:
        raise AssertionError('Missing-link negative control was accepted')
    print(f'PASS links: {count} applied localized fragments; missing-link negative control rejected')


def resources():
    return {file.stem: {node.attrib[KEY]: node.text or '' for node in ET.parse(file).getroot() if KEY in node.attrib} for file in (ROOT / 'Strings').glob('*.xaml')}


def store():
    with (ROOT / 'store/KillerBench-Store-listing.csv').open(encoding='utf-8-sig', newline='') as stream:
        rows = list(csv.reader(stream))
    assert all(len(row) == 22 for row in rows), 'CSV field count'
    fields = {row[0]: row[2:] for row in rows[1:]}
    locales = ['en-US', 'bn', 'cs-CZ', 'de-DE', 'es', 'fr-FR', 'hu-HU', 'it-IT', 'ja-JP', 'kk-KZ', 'nb-NO', 'pl-PL', 'pt-BR', 'ru-RU', 'tr-TR', 'uk-UA', 'vi-VN', 'zh-CN', 'zh-TW', 'sl-SI']
    strings = resources()
    for index, locale in enumerate(locales):
        for field in ('Description', 'ShortDescription'):
            text = fields[field][index]
            assert not re.search(r'\bGO\b', text), f'{locale} {field}: obsolete GO action'
            assert strings[locale]['Str_Main_RunDiagnosis'] in text, f'{locale} {field}: missing diagnosis action'
            assert len(text) <= (10000 if field == 'Description' else 1000), f'{locale} {field}: field limit'
        assert '.NET' not in fields['RequirementsMinimum1'][index], f'{locale}: Store prerequisite still requires .NET'
    for row in rows[1:]:
        if row[2] and row[1] == 'Text':
            assert all(cell.strip() for cell in row[2:]), row[0]
        for cell in row[2:]:
            if re.search(r'\.(png|jpg|jpeg)$', cell, re.I):
                assert (ROOT / 'store' / cell).exists(), cell
    print(f'PASS store: {len(rows)-1} rows, 22 fields, 20 localized actions and runtime requirements')


def package():
    strings = resources()
    base = strings['en-US']
    keys = [key for key in base if key.startswith('Str_PackageReview_')]
    assert len(keys) >= 10, 'Package review text is not resourced'
    def tokens(text):
        return sorted(re.findall(r'\{\d+(?:[^}]*)\}', text))
    def validate(locale, values):
        assert values.keys() == base.keys(), f'{locale}: resource key parity'
        for key in keys:
            assert values[key].strip() and tokens(values[key]) == tokens(base[key]), f'{locale}: {key}'
    for locale, values in strings.items():
        validate(locale, values)
    malformed = dict(strings['de-DE'])
    malformed['Str_PackageReview_Refresh'] = malformed['Str_PackageReview_Refresh'].replace('{0}', '{1}')
    try:
        validate('malformed-negative-control', malformed)
    except AssertionError:
        pass
    else:
        raise AssertionError('Malformed-placeholder negative control accepted')
    print(f'PASS package: {len(keys)} package-review strings across {len(strings)} locales; malformed-placeholder negative control rejected')


def distribution():
    text = (ROOT / 'README.md').read_text(encoding='utf-8')
    assert '`KillerBench-Portable.exe`' in text, 'Portable artifact missing from distribution guidance'
    assert '`KillerBench.exe`' in text and 'installer' in text
    assert '.NET 10 Runtime' in text and 'Desktop Runtime' in text, 'CLI/runtime distinction missing'
    assert 'v1.0.0/KillerBench-1.0.0-src.zip' in text, 'Public source release changed'
    print('PASS distribution: installer, portable and CLI guidance; public source link retained')


def language_boundary():
    text = (ROOT / 'bench-landing/technical.html').read_text(encoding='utf-8')
    assert 'Raw evidence keeps its original text and language.' in text, 'Raw evidence language boundary is inaccurate'
    assert 'UI-owned headings' in text, 'Open-report language promise is too broad'
    assert 'unknown result' in text, 'Localized Windows output limitation missing'
    print('PASS language boundary: interface headings, report prose, original evidence and unknown-result limitation')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--only', choices=['links', 'store', 'package', 'distribution', 'language_boundary'])
    selection = parser.parse_args().only
    try:
        for name in ([selection] if selection else ['links', 'store', 'package', 'distribution', 'language_boundary']):
            globals()[name]()
    except (AssertionError, ET.ParseError) as error:
        print('FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
