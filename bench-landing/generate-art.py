"""Generate KillerBench artwork with the existing family generators."""
import importlib.util
import io
import os
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image
from fontTools.ttLib import TTFont

HERE = Path(__file__).resolve().parent
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
face = svg.Face(svg.F_TYPE)
for variant, accents in svg.ACCENTS.items():
    for accent, color in accents.items():
        data = svg.build_svg("Killer", "Bench", color, variant, face).encode("utf-8")
        ET.fromstring(data)
        write(HERE / "brand" / f"killerbench-logo-{variant}-{accent}.svg", data)
for theme, color in svg.THEME_ACCENTS.items():
    data = svg.build_svg("Killer", "Bench", color, "dark", face).encode("utf-8")
    ET.fromstring(data)
    write(HERE / "brand" / f"killerbench-logo-{theme}.svg", data)

font = Path(svg.F_TYPE).read_bytes()
TTFont(io.BytesIO(font))
write(HERE / "brand" / "typewriter.ttf", font)
grain = (HERE.parents[1] / "KillerScan" / "scan-landing" / "grain.png").read_bytes()
Image.open(io.BytesIO(grain)).verify()
write(HERE / "grain.png", grain)

og = load("bench_og", "make-og-images.py")
og.mw.APPS["KillerBench"] = ("Killer", "Bench", (232, 150, 44), "Diagnostic Workbench")
original_line = og.punk_line
og.punk_line = lambda text, size, fill: original_line("IN DEVELOPMENT" if text == "KillerBench.net" else text, size, fill)
art = og.build("KillerBench")
buffer = io.BytesIO()
art.save(buffer, format="PNG")
write(HERE / "og-image.png", buffer.getvalue())
Image.open(HERE / "og-image.png").verify()
assert art.size == (1200, 630)
print("Generated family SVG wordmarks, validated font and grain, and 1200x630 social artwork.")

if "--scan" in __import__("sys").argv:
    for accent, color in svg.ACCENTS["black"].items():
        data = svg.build_svg("Killer", "Scan", color, "black", face).encode("utf-8")
        ET.fromstring(data)
        write(HERE.parents[1] / "KillerScan" / "scan-landing" / "brand" / f"killerscan-logo-black-{accent}.svg", data)
