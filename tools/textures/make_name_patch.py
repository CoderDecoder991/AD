"""Make the "SUNIL" name patch that replaces the national patch on the full-gear soldier (user request 2026-11-24).

Draws a khaki patch with a dark border and black block letters (no flag: project rule R-14), sampled to match the
original patch colours, then writes two tiles already transformed for the two places the patch occupies in the
soldier's gear texture (its UVs are mirrored there, and one copy is rotated):
  patch_right.png - 103 x 64 px, mirrored left-right       (texture box x 921..1024, y 644..708, top-left origin)
  patch_left.png  -  64 x 107 px, mirrored, rotated 90° CCW (texture box x 72..136,  y 870..977)
tools/blender/process_characters.py pastes them into the texture.

Usage: python -I tools/textures/make_name_patch.py <out_dir> [text] [source_texture.png]
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps

out = sys.argv[1]
text = sys.argv[2] if len(sys.argv) > 2 else "SUNIL"
src = sys.argv[3] if len(sys.argv) > 3 else None

# Colours sampled from the original patch (fallbacks if no source texture is given).
khaki, ink, border = (150, 146, 126), (18, 18, 16), (40, 40, 34)
if src and os.path.exists(src):
    im = Image.open(src).convert("RGB")
    region = im.crop((925, 648, 1020, 660))  # light area left of/around the letters
    px = sorted(region.getdata(), key=sum)
    khaki = px[int(len(px) * 0.8)]            # the light fabric, not the letters
    print("[patch] sampled khaki", khaki)

# Design at 4x resolution, in reading orientation (wide).
W, H = 103 * 4, 64 * 4
img = Image.new("RGB", (W, H), khaki)
d = ImageDraw.Draw(img)
d.rectangle([0, 0, W - 1, H - 1], outline=border, width=12)       # stitched dark border
d.rectangle([14, 14, W - 15, H - 15], outline=(120, 117, 100), width=3)
font = None
for f in ("C:/Windows/Fonts/bahnschrift.ttf", "C:/Windows/Fonts/arialbd.ttf"):
    if os.path.exists(f):
        font = ImageFont.truetype(f, 150)
        try:
            font.set_variation_by_name("Bold")
        except Exception:  # noqa: BLE001 - non-variable font
            pass
        break
box = d.textbbox((0, 0), text, font=font)
tw, th = box[2] - box[0], box[3] - box[1]
scale = min((W - 70) / tw, (H - 80) / th)
font = ImageFont.truetype(font.path, int(150 * scale))
try:
    font.set_variation_by_name("Bold")
except Exception:  # noqa: BLE001
    pass
box = d.textbbox((0, 0), text, font=font)
tw, th = box[2] - box[0], box[3] - box[1]
d.text(((W - tw) / 2 - box[0], (H - th) / 2 - box[1]), text, font=font, fill=ink)
# Fabric: slight blur and grain so it matches the rest of the 1K texture.
img = img.filter(ImageFilter.GaussianBlur(1.2))
grain = Image.effect_noise((W, H), 18).convert("RGB")
img = Image.blend(img, ImageOps.autocontrast(grain), 0.06)
design = img.resize((103, 64), Image.LANCZOS)

os.makedirs(out, exist_ok=True)
design.save(os.path.join(out, "patch_design.png"))
right = ImageOps.mirror(design)
right.save(os.path.join(out, "patch_right.png"))
left = ImageOps.mirror(design).rotate(90, expand=True)  # 90° counter-clockwise: text column on the left
left.save(os.path.join(out, "patch_left.png"))
print("[patch] wrote", right.size, left.size)
