"""Make a fresh, checked static export. Does not publish or delete anything."""
import argparse
import importlib.util
import shutil
import sys

sys.dont_write_bytecode = True
from pathlib import Path

ROOT = Path(__file__).resolve().parent
ALLOWED = {'.html', '.css', '.js', '.svg', '.png', '.jpg', '.jpeg', '.webp', '.ico', '.ttf', '.woff', '.woff2'}
EXCLUDED = {'templates', 'locales', 'docs', '__pycache__'}


def export(destination):
    destination = destination.resolve()
    if destination.is_relative_to(ROOT) or ROOT.is_relative_to(destination):
        raise ValueError('Export must be outside the site source and its parent directories.')
    if destination.exists():
        raise ValueError('Choose a new output directory. Existing output is never overwritten or removed.')
    for parent in destination.parents:
        if parent.is_symlink():
            raise ValueError('Output may not traverse a symbolic link.')
    files = []
    for path in ROOT.rglob('*'):
        relative = path.relative_to(ROOT)
        if EXCLUDED.intersection(relative.parts):
            continue
        if path.is_symlink():
            raise ValueError('Source contains a symbolic link: '+str(relative))
        if path.is_file() and (path.suffix.lower() in ALLOWED or relative.as_posix() in {'_headers', 'license.txt', 'robots.txt', 'sitemap.xml'}):
            files.append((path, relative))
    spec = importlib.util.spec_from_file_location('site_checks', ROOT/'test-site.py')
    checks = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(checks)
    checks.validate(ROOT)
    destination.mkdir(parents=True)
    for source, relative in files:
        target = destination/relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    checks.validate(destination, clean=True)
    print(f'Prepared {len(files)} static files at {destination}. Nothing published.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('destination', type=Path)
    export(parser.parse_args().destination)
