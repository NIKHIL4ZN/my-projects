"""Regenerates the Google Play graphics: icon-512.png and feature-graphic-1024x500.png.
Run:  python3 store-assets/generate_store_graphics.py   (needs Pillow)"""
from PIL import Image, ImageDraw, ImageFont
import os

HERE = os.path.dirname(os.path.abspath(__file__))
NAVY, BLUE, WHITE = (20, 33, 61, 255), (29, 78, 216, 255), (255, 255, 255, 255)
DOTS = [((-0.34, -0.05), (29, 78, 216, 255)), ((0.34, -0.05), (245, 158, 11, 255)), ((0.0, 0.42), (22, 163, 74, 255))]

def pin_icon(size, rounded=True):
    s = size * 4
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if rounded:
        d.rounded_rectangle([0, 0, s, s], radius=int(s * 0.22), fill=NAVY)
    cx, cy, r = s / 2, s * 0.42, s * 0.30
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=BLUE)
    d.polygon([(cx - r * 0.62, cy + r * 0.62), (cx + r * 0.62, cy + r * 0.62), (cx, s * 0.86)], fill=BLUE)
    hr = r * 0.62
    d.ellipse([cx - hr, cy - hr, cx + hr, cy + hr], fill=WHITE)
    dr = hr * 0.30
    for (dx, dy), col in DOTS:
        x, y = cx + dx * hr, cy + dy * hr
        d.ellipse([x - dr, y - dr, x + dr, y + dr], fill=col)
    return img.resize((size, size), Image.LANCZOS)

def font(bold, size):
    name = "DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf"
    for base in ("/usr/share/fonts/truetype/dejavu/", "C:/Windows/Fonts/", ""):
        try:
            return ImageFont.truetype(base + name, size)
        except OSError:
            continue
    return ImageFont.load_default()

# 512x512 high-res icon (Play requires PNG, 32-bit, max 1 MB)
pin_icon(512).save(os.path.join(HERE, "icon-512.png"))

# 1024x500 feature graphic
W, H = 1024, 500
fg = Image.new("RGBA", (W, H), NAVY)
px = fg.load()
for y in range(H):                       # soft vertical gradient
    for x in range(W):
        t = (x / W) * 0.35 + (y / H) * 0.15
        px[x, y] = (int(20 + 18 * t), int(33 + 45 * t), int(61 + 110 * t), 255)
fg.alpha_composite(pin_icon(300, rounded=True), (70, 100))
d = ImageDraw.Draw(fg)
d.text((420, 135), "FamilyLink Live", font=font(True, 60), fill=WHITE)
d.text((422, 215), "Stay close, stay safe", font=font(False, 34), fill=(203, 213, 245, 255))

# Translucent chips are drawn on their own layer and composited, so the transparency really blends.
chips = Image.new("RGBA", (W, H), (0, 0, 0, 0))
cd = ImageDraw.Draw(chips)
x, positions = 422, []
for label in ("Live map", "Private chat", "SOS alerts"):
    w = cd.textlength(label, font=font(True, 24)) + 36
    cd.rounded_rectangle([x, 300, x + w, 348], radius=24, fill=(255, 255, 255, 38), outline=(255, 255, 255, 140), width=2)
    positions.append((x + 18, label))
    x += w + 16
fg = Image.alpha_composite(fg, chips)
d = ImageDraw.Draw(fg)
for tx, label in positions:
    d.text((tx, 310), label, font=font(True, 24), fill=WHITE)
fg.convert("RGB").save(os.path.join(HERE, "feature-graphic-1024x500.png"))
print("wrote icon-512.png and feature-graphic-1024x500.png")
