#!/usr/bin/python3
"""Rasterize the existing Android gorilla vector for Apple's AppIcon catalog.
Requires distro python3-gi, python3-pil and gir1.2-rsvg-2.0. No image generation.
"""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
import gi
from PIL import Image
gi.require_version('Rsvg', '2.0')
from gi.repository import Rsvg
ROOT = Path(__file__).resolve().parents[2]
ANDROID = '{http://schemas.android.com/apk/res/android}'
vector = ET.parse(ROOT / 'Companion/android/app/src/main/res/drawable/gorilla_mark.xml').getroot()
svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 140 140">' + ''.join(
    '<path fill="' + p.attrib[ANDROID+'fillColor'] + '" d="' + p.attrib[ANDROID+'pathData'] + '"/>'
    for p in vector) + '</svg>\n'
(ROOT / 'Companion/assets/branding/gorilla-mark.svg').write_text(svg)
catalog = ROOT / 'Companion/ios/Runner/Assets.xcassets/AppIcon.appiconset'
images = json.loads((catalog / 'Contents.json').read_text())['images']
written = set()
for item in images:
    name = item['filename']
    size = round(float(item['size'].split('x')[0]) * float(item['scale'].removesuffix('x')))
    if name in written:
        continue
    sized = svg.replace('viewBox="0 0 140 140"', f'width="{size}" height="{size}" viewBox="0 0 140 140"')
    pixbuf = Rsvg.Handle.new_from_data(sized.encode()).get_pixbuf()
    mode = 'RGBA' if pixbuf.get_has_alpha() else 'RGB'
    image = Image.frombytes(mode, (size, size), pixbuf.get_pixels(), 'raw', mode, pixbuf.get_rowstride())
    image.convert('RGB').save(catalog / name)
    written.add(name)
print(f'Exported {len(written)} opaque iOS icons from the existing gorilla vector')
