"""Check canonical license copies and desktop/CLI license publish items."""
import hashlib
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent
GNU_SHA256 = '3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986'


def check_publish_item(project, repository):
    expected = 'LICENSE.txt' if project.parent == repository else '../LICENSE.txt'
    items = [item for item in ET.parse(project).getroot().iter('None')
             if item.get('Update') == expected or item.get('Include') == expected]
    if len(items) != 1 or items[0].get('CopyToPublishDirectory') != 'PreserveNewest':
        raise ValueError('Canonical license missing from publish items: '+str(project))
    if project.parent != repository and items[0].get('Link') != 'LICENSE.txt':
        raise ValueError('CLI license has an incorrect output name.')
    if not (project.parent/expected).is_file():
        raise ValueError('Publish license source is missing.')


class PublishItemTests(unittest.TestCase):
    def test_missing_or_disabled_publish_item(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'LICENSE.txt').write_text('fixture', encoding='utf-8')
            project = root/'KillerBench.csproj'
            for item in ['', '<None Update="LICENSE.txt" CopyToPublishDirectory="Never" />']:
                project.write_text('<Project><ItemGroup>'+item+'</ItemGroup></Project>', encoding='utf-8')
                with self.assertRaises(ValueError):
                    check_publish_item(project, root)

    def test_wrong_cli_output_name(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root/'LICENSE.txt').write_text('fixture', encoding='utf-8')
            (root/'Cli').mkdir()
            project = root/'Cli/KillerBench.Cli.csproj'
            project.write_text('<Project><ItemGroup><None Include="../LICENSE.txt" Link="wrong.txt" CopyToPublishDirectory="PreserveNewest" /></ItemGroup></Project>', encoding='utf-8')
            with self.assertRaises(ValueError):
                check_publish_item(project, root)


if __name__ == '__main__':
    result = unittest.TextTestRunner().run(unittest.defaultTestLoader.loadTestsFromTestCase(PublishItemTests))
    if not result.wasSuccessful():
        sys.exit(1)
    for name in ['LICENSE.txt']:
        if hashlib.sha256((ROOT/name).read_bytes()).hexdigest() != GNU_SHA256:
            raise ValueError('Missing or modified GNU GPLv3 text: '+name)
    for name in ['KillerBench.csproj', 'Cli/KillerBench.Cli.csproj']:
        check_publish_item(ROOT/name, ROOT)
    print('Canonical GNU GPLv3 copies and desktop/CLI publish declarations passed.')
