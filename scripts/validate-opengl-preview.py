#!/usr/bin/env python3
import sys
from pathlib import Path
from PIL import Image

if len(sys.argv) != 5:
    raise SystemExit("usage: validate-opengl-preview.py window.png preview.png win_w win_h")

window_png = Path(sys.argv[1])
preview_png = Path(sys.argv[2])
win_w, win_h = map(int, sys.argv[3:5])

image = Image.open(window_png).convert("RGB")

content_x = 320 + 14
content_y = 48 + 14 + 36
content_w = max(1, win_w - 320 - 28)
content_h = max(1, win_h - 48 - 120 - 28 - 36 - 170)

crop_x = content_x + content_w // 2
crop_y = content_y
crop_w = content_w // 2
crop_h = content_h

crop = image.crop((crop_x, crop_y, crop_x + crop_w, crop_y + crop_h))
crop.save(preview_png)

pixels = list(crop.getdata())
total = len(pixels)
histogram = {}
for pixel in pixels:
    histogram[pixel] = histogram.get(pixel, 0) + 1
unique = len(histogram)

background = pixels[0]
non_background = sum(1 for pixel in pixels if pixel != background)
dominant = max(histogram.values())
non_dominant_ratio = (total - dominant) / total
bright = sum(1 for r, g, b in pixels if max(r, g, b) > 185)
colored = sum(1 for r, g, b in pixels if max(r, g, b) - min(r, g, b) > 35)
dark = sum(1 for r, g, b in pixels if r + g + b < 120)

non_background_ratio = non_background / total
colored_ratio = colored / total
bright_ratio = bright / total
dark_ratio = dark / total

passed = (
    unique >= 10
    and non_dominant_ratio >= 0.01
    and colored_ratio >= 0.001
    and bright_ratio >= 0.01
)

print(f"window={win_w}x{win_h}")
print(f"preview_crop={crop_w}x{crop_h}+{crop_x}+{crop_y}")
print(f"unique_colors={unique}")
print(f"non_background_ratio={non_background_ratio:.6f}")
print(f"non_dominant_ratio={non_dominant_ratio:.6f}")
print(f"colored_ratio={colored_ratio:.6f}")
print(f"bright_ratio={bright_ratio:.6f}")
print(f"dark_ratio={dark_ratio:.6f}")
print(f"status={'pass' if passed else 'fail'}")

if not passed:
    raise SystemExit(1)
