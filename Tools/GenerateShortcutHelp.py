"""Check or print an atomic patch for Help shortcuts from the app binding table."""
import argparse
import difflib
import html
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
KEY = '{http://schemas.microsoft.com/winfx/2006/xaml}Key'
REQUIRED = {'Ctrl+Shift+O', 'Ctrl+S', 'Ctrl+E', 'Ctrl+Shift+E', 'Ctrl+F'}


def bindings():
    # The app's own rows (Shell/ShortcutsOverlay.Rows.cs), then the kit's family rows
    # (Shell/ShortcutsOverlay.cs): Film Grain, F1, F12 and Esc close the Help table.
    family = (ROOT / 'Shell/ShortcutsOverlay.cs').read_text(encoding='utf-8')
    app = (ROOT / 'Shell/ShortcutsOverlay.Rows.cs').read_text(encoding='utf-8')
    blocks = [app.split('KsBinding[] KsAppRows() =>', 1)[1].split('];', 1)[0],
              family.split('KsBinding[] KsFamilyRows =', 1)[1].split('];', 1)[0]]
    result = []
    for block in blocks:
        for keys, label, category, caps in re.findall(r'new\("([^"]+)",\s*"([^"]+)",\s*KsCat\.(\w+)([^\n]*)\)', block):
            layer = 'ctrlshift' if keys.startswith('Ctrl+Shift+') else 'altshift' if keys.startswith('Alt+Shift+') else 'ctrl' if keys.startswith('Ctrl+') else 'base'
            # The overlay names WPF keys; the Help board names the = key's cap Plus.
            caps = ['Plus' if cap == 'Equals' else cap for cap in re.findall(r'"([^"]+)"', caps)]
            result.append((keys, label, category.lower(), layer, caps))
    return result


def validate(items):
    assert REQUIRED <= {item[0] for item in items}, 'Missing practical action shortcuts'
    mapped = [(item[3], cap) for item in items for cap in item[4]]
    assert len(set(mapped)) == len(mapped), 'Keyboard map layer collision'


def expected(items):
    locales = {file.stem: {node.attrib[KEY]: node.text or '' for node in ET.parse(file).getroot() if KEY in node.attrib} for file in (ROOT / 'Strings').glob('*.xaml')}
    english = locales['en-US']
    rows = ''.join('<tr><td>' + html.escape(keys) + '</td><td data-i18n="shortcut_' + label + '">' + html.escape(english[label]) + '</td></tr>' for keys, label, *_ in items)
    page = ROOT / 'bench-landing/help.html'
    original = page.read_text(encoding='utf-8')
    updated = re.sub(r'(<table class="spec-table"><thead><tr><th data-i18n="t198">.*?</thead><tbody>).*?(</tbody></table>)', lambda m: m[1] + rows + m[2], original, count=1, flags=re.S)
    keymap = {re.sub(r'\s+', '', keys): [caps, layer, category] for keys, label, category, layer, caps in items}
    updated = re.sub(r'var KEYS=.*?;\n', 'var KEYS=' + json.dumps(keymap, ensure_ascii=False, separators=(',', ':')) + ';\n', updated, count=1)
    translations = ROOT / 'bench-landing/kb-i18n.js'
    script = translations.read_text(encoding='utf-8')
    changed = re.sub(r'^  "shortcut_[^"]+":.*\n', '', script, flags=re.M)
    for locale, values in locales.items():
        if locale == 'en-US':
            continue
        generated = ['  ' + json.dumps('shortcut_' + label) + ': ' + json.dumps(values[label], ensure_ascii=False) + ',' for _, label, *_ in items]
        # Insert after the exact locale opening without changing its existing content.
        site_locale = {'cs-CZ': 'cs', 'de-DE': 'de', 'fr-FR': 'fr', 'hu-HU': 'hu', 'it-IT': 'it', 'ja-JP': 'ja', 'kk-KZ': 'kk', 'nb-NO': 'nb', 'pl-PL': 'pl', 'pt-BR': 'pt', 'ru-RU': 'ru', 'tr-TR': 'tr', 'uk-UA': 'uk', 'vi-VN': 'vi', 'sl-SI': 'sl', 'zh-CN': 'zh-cn', 'zh-TW': 'zh'}.get(locale, locale)
        opening = ' "' + site_locale + '": {'
        assert opening in changed, locale
        changed = changed.replace(opening, opening + '\n' + '\n'.join(dict.fromkeys(generated)), 1)
    return [(page, original, updated), (translations, script, changed)]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--patch', action='store_true')
    options = parser.parse_args()
    items = bindings()
    validate(items)
    try:
        validate(items + [items[0]])
    except AssertionError:
        pass
    else:
        raise AssertionError('Duplicate map negative control accepted')
    files = expected(items)
    if options.patch:
        print('*** Begin Patch')
        for file, before, after in files:
            if before == after:
                continue
            print('*** Update File: ' + file.as_posix())
            for line in list(difflib.unified_diff(before.splitlines(), after.splitlines(), n=2))[2:]:
                print('@@' if line.startswith('@@') else line)
        print('*** End Patch')
    else:
        for file, before, after in files:
            assert before == after, str(file.relative_to(ROOT)) + ': generated shortcuts are stale'
        print(f'PASS shortcuts: {len(items)} authoritative app bindings, 20 translated Help tables, map-layer negative control rejected')


if __name__ == '__main__':
    try:
        main()
    except AssertionError as error:
        print('FAIL: ' + str(error), file=sys.stderr)
        sys.exit(1)
