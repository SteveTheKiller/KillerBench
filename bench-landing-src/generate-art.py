"""Generate KillerBench artwork with the existing family generators."""
import importlib.util
import io
import os
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image
from fontTools.ttLib import TTFont

HERE = Path(__file__).resolve().parent.parent / "bench-landing"  # public site output
BRANDING = HERE.parents[1] / "Killer Branding"

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, BRANDING / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as output:
        output.write(data)
        output.flush()
        os.fsync(output.fileno())
    assert path.read_bytes() == data

svg = load("bench_svg", "make-logo-svgs.py")
face = svg.load_faces()
for variant, accents in svg.ACCENTS.items():
    for accent, color in accents.items():
        data = svg.build_svg("Killer", "Bench", color, variant, face).encode("utf-8")
        ET.fromstring(data)
        write(HERE / "brand" / f"killerbench-logo-{variant}-{accent}.svg", data)
for theme, color in svg.THEME_ACCENTS.items():
    data = svg.build_svg("Killer", "Bench", color, "dark", face).encode("utf-8")
    ET.fromstring(data)
    write(HERE / "brand" / f"killerbench-logo-{theme}.svg", data)

# The family faces and their license texts, copied beside the site that loads them. They come
# from the app's own Fonts folder: its Punktype license text is the public one.
for name in ("KillerSans-Regular.ttf", "Punktype.ttf", "Lacquer-Regular.ttf",
             "KillerSans-OFL.txt", "Punktype-LICENSE.txt", "Lacquer-OFL.txt"):
    data = (HERE.parent / "Fonts" / name).read_bytes()
    if name.endswith(".ttf"):
        TTFont(io.BytesIO(data))
    write(HERE / "brand" / name, data)
grain = (HERE.parents[1] / "KillerScan" / "scan-landing" / "grain.png").read_bytes()
Image.open(io.BytesIO(grain)).verify()
write(HERE / "grain.png", grain)

og = load("bench_og", "make-og-images.py")
# Dark/Yellow is the default theme: Themes/Accents/Dark/Yellow.xaml PrimaryBrush.
og.mw.APPS["KillerBench"] = ("Killer", "Bench", (0xEA, 0xD9, 0x00), "Free Diagnostic Workbench")
og.mw.LOGO_LEFT["KillerBench"] = str(HERE / "kb-icon.png")
art = og.build("KillerBench")
buffer = io.BytesIO()
art.save(buffer, format="PNG")
write(HERE / "og-image.png", buffer.getvalue())
Image.open(HERE / "og-image.png").verify()
assert art.size == (1200, 630)
readme = og.mw.build("KillerBench", 1600, style="banner")
buffer = io.BytesIO()
readme.save(buffer, format="PNG")
write(HERE.parent / "docs" / "wordmark.png", buffer.getvalue())
write(HERE.parent / "docs" / "banner.png", (HERE / "og-image.png").read_bytes())
print("Generated family SVG wordmarks, validated font and grain, 1200x630 social artwork and README art.")

if "--scan" in __import__("sys").argv:
    for accent, color in svg.ACCENTS["black"].items():
        data = svg.build_svg("Killer", "Scan", color, "black", face).encode("utf-8")
        ET.fromstring(data)
        write(HERE.parents[1] / "KillerScan" / "scan-landing" / "brand" / f"killerscan-logo-black-{accent}.svg", data)
