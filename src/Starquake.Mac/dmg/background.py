#!/usr/bin/env python3
"""Draw background@2x.png, the picture behind the icons in the DMG window.

    python3 background.py [comic|title]      # default: comic

The window is 1000x480 points and the art is drawn at 2x for retina; build-dmg.sh tags the result
144 dpi so Finder puts it back to point size. Everything here is positioned around the three icon
slots build-dmg.sh gives Finder - the arrow runs from the app to Applications - so the two files
have to be changed together.

Two backdrops, same layout:

  comic   the website's headline panel, docs/assets/hero.png - what ships
  title   the game's own title screen, Content/Graphics/BootScreen2.png

Neither is cropped with an edge. The title screen is placed so its wordmark lands where this
wants it and the rest runs off the window, then dissolved into a starfield drawn underneath; the
comic panel is darkened under a gradient that goes almost black by the icon row. Both end with
the same thing: a quiet bottom half for the icons to sit on.

The comic variant takes its wordmark from the title screen too, and a rectangle of that screen's
own near-black comes with it. Matting the letters out of it does not work - the red half of the
gradient is too close in luminance to the backdrop, and comes away dimmed to nothing - so the
rectangle is kept and the ground it lands on is made to match it instead: two soft gradients, one
black to clear the comic art and one in the backdrop's own dark navy, neither with an edge
anywhere.

Needs rsvg-convert (`brew install librsvg`). Only run when the art should change; the PNG it
produces is committed.
"""
import os
import random
import shutil
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
TITLE_SRC = os.path.normpath(
    os.path.join(HERE, "..", "..", "Starquake.Shared", "Content", "Graphics", "BootScreen2.png"))
COMIC_SRC = os.path.normpath(os.path.join(HERE, "..", "..", "..", "docs", "assets", "hero.png"))
OUT = os.path.join(HERE, "background@2x.png")

VARIANT = (sys.argv[1] if len(sys.argv) > 1 else "comic").lower()
if VARIANT not in ("title", "comic"):
    sys.exit("usage: background.py [comic|title]")

# librsvg will only follow an <image> href that resolves inside the SVG's own directory, so the
# document and the art it references are assembled together in a scratch directory. Referring to
# the originals where they live renders a silently blank picture.
WORK = tempfile.mkdtemp()
TITLE, COMIC = "title.png", "comic.png"
shutil.copyfile(TITLE_SRC, os.path.join(WORK, TITLE))
shutil.copyfile(COMIC_SRC, os.path.join(WORK, COMIC))

W, H = 1000, 480          # window, in points
SCALE = 2                 # drawn at 2x

# Where the wordmark sits inside BootScreen2.png (which is 1280x800), and how wide it should be
# in the window. It is bigger on the title variant, where it is the picture rather than a caption
# over one.
MARK = (255, 288, 750, 227)
MARK_W = 620 if VARIANT == "title" else 540
MARK_X, MARK_Y = (W - MARK_W) / 2, 36 if VARIANT == "title" else 26

# The icon slots, from build-dmg.sh.
ICON_Y = 340
APP_X, APPS_X, README_X = 150, 500, 850

scale = MARK_W / MARK[2]
mark_h = MARK[3] * scale
mark_bottom = MARK_Y + mark_h
# The whole title screen, placed so its wordmark lands at MARK_X/MARK_Y.
img_x, img_y = MARK_X - MARK[0] * scale, MARK_Y - MARK[1] * scale
img_w, img_h = 1280 * scale, 800 * scale

out = []
a = out.append
a(f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
  f'width="{W}" height="{H}" viewBox="0 0 {W} {H}">')

a('<defs>')
a('<radialGradient id="seat"><stop offset="0" stop-color="#ffffff" stop-opacity=".055"/>'
  '<stop offset=".6" stop-color="#ffffff" stop-opacity=".02"/>'
  '<stop offset="1" stop-color="#ffffff" stop-opacity="0"/></radialGradient>')
a('<linearGradient id="arrow" x1="0" y1="0" x2="1" y2="0">'
  '<stop offset="0" stop-color="#28c0b0"/><stop offset=".55" stop-color="#a6ff3d"/>'
  '<stop offset="1" stop-color="#ffe94a"/></linearGradient>')
a('<filter id="glow" x="-50%" y="-300%" width="200%" height="700%">'
  '<feGaussianBlur stdDeviation="6"/></filter>')

if VARIANT == "title":
    a('<radialGradient id="nebBlue"><stop offset="0" stop-color="#2b3fe0" stop-opacity=".16"/>'
      '<stop offset="1" stop-color="#2b3fe0" stop-opacity="0"/></radialGradient>')
    # Cropped by fading, never by an edge. Vertically it is solid down to the wordmark and gone by
    # two thirds; horizontally the two side fades are what swallow the title screen's bezel, which
    # would otherwise show as a dark bar down each side of the window.
    a('<linearGradient id="vFade" x1="0" y1="0" x2="0" y2="1">'
      '<stop offset="0" stop-color="#fff"/><stop offset=".46" stop-color="#fff"/>'
      '<stop offset=".54" stop-color="#9a9a9a"/><stop offset=".64" stop-color="#000"/></linearGradient>')
    a('<linearGradient id="lFade" x1="0" y1="0" x2="1" y2="0">'
      '<stop offset="0" stop-color="#000"/><stop offset=".3" stop-color="#000"/>'
      '<stop offset="1" stop-color="#000" stop-opacity="0"/></linearGradient>')
    a('<linearGradient id="rFade" x1="0" y1="0" x2="1" y2="0">'
      '<stop offset="0" stop-color="#000" stop-opacity="0"/><stop offset=".8" stop-color="#000"/>'
      '<stop offset="1" stop-color="#000"/></linearGradient>')
    a(f'<mask id="bootMask">'
      f'<rect width="{W}" height="{H}" fill="url(#vFade)"/>'
      f'<rect x="0" y="0" width="280" height="{H}" fill="url(#lFade)"/>'
      f'<rect x="{W - 280}" y="0" width="280" height="{H}" fill="url(#rFade)"/>'
      f'</mask>')
else:
    a('<linearGradient id="dark" x1="0" y1="0" x2="0" y2="1">'
      '<stop offset="0" stop-color="#05040d" stop-opacity=".45"/>'
      '<stop offset=".42" stop-color="#05040d" stop-opacity=".6"/>'
      '<stop offset=".60" stop-color="#05040d" stop-opacity=".93"/>'
      '<stop offset=".78" stop-color="#05040d" stop-opacity=".985"/>'
      '<stop offset="1" stop-color="#05040d" stop-opacity=".99"/></linearGradient>')
    # The cloud the wordmark is set into. It has to be solid across the whole rectangle of
    # near-black the wordmark arrives in, and then reach nothing by degrees - so it is a gradient
    # rather than a blurred shape: a blur is clipped by its filter region, and the clip shows up on
    # the comic art as exactly the straight edge the cloud exists to avoid.
    a('<radialGradient id="cloud">'
      '<stop offset="0" stop-color="#05040d" stop-opacity="1"/>'
      '<stop offset=".5" stop-color="#05040d" stop-opacity="1"/>'
      '<stop offset=".7" stop-color="#05040d" stop-opacity=".82"/>'
      '<stop offset=".85" stop-color="#05040d" stop-opacity=".45"/>'
      '<stop offset=".94" stop-color="#05040d" stop-opacity=".16"/>'
      '<stop offset="1" stop-color="#05040d" stop-opacity="0"/></radialGradient>')
    # The second layer is the colour the wordmark's own backdrop actually is - not black, a very
    # dark navy - so the rectangle it arrives in ends on ground of its own tone and has no edge to
    # show. The black layer underneath is what gets the comic art out of the way first.
    a('<radialGradient id="plate">'
      '<stop offset="0" stop-color="#0e1119" stop-opacity="1"/>'
      '<stop offset=".55" stop-color="#0e1119" stop-opacity="1"/>'
      '<stop offset=".78" stop-color="#0e1119" stop-opacity=".6"/>'
      '<stop offset=".9" stop-color="#0e1119" stop-opacity=".25"/>'
      '<stop offset="1" stop-color="#0e1119" stop-opacity="0"/></radialGradient>')

# A soft-edged window on the wordmark, used on both variants to take it off the title screen.
FEATHER = 18 if VARIANT == "title" else 26
a(f'<filter id="feather"><feGaussianBlur stdDeviation="{FEATHER}"/></filter>')
a(f'<mask id="markMask"><rect x="{MARK_X + FEATHER + 6}" y="{MARK_Y + FEATHER + 2}" '
  f'width="{MARK_W - 2 * (FEATHER + 6)}" height="{mark_h - 2 * (FEATHER + 2):.0f}" '
  f'fill="#fff" filter="url(#feather)"/></mask>')
a('</defs>')

a(f'<rect width="{W}" height="{H}" fill="#05040d"/>')

if VARIANT == "title":
    a('<ellipse cx="500" cy="450" rx="640" ry="220" fill="url(#nebBlue)"/>')

    # A fixed seed, so re-running this does not reshuffle the sky and produce a pointless diff.
    random.seed(1985)
    stars = []
    for _ in range(340):
        x, y = random.uniform(0, W), random.uniform(0, H)
        r = random.choice([.5, .5, .6, .7, .7, .9, 1.1, 1.3])
        # Dimmer behind the title screen, which is bright enough there without them.
        o = random.uniform(.16, .8) * (1 if y > 230 else .35)
        tint = random.choice(["#ffffff"] * 7 + ["#bdb7d6", "#a6ff3d", "#ff3fd8", "#ffe94a"])
        stars.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r}" fill="{tint}" opacity="{o:.2f}"/>')
    a("".join(stars))
    for _ in range(6):
        x, y = random.uniform(40, W - 40), random.uniform(250, H - 50)
        k = random.uniform(5, 8)
        a(f'<g opacity=".45" stroke="#fff" stroke-width=".8" stroke-linecap="round">'
          f'<line x1="{x - k:.1f}" y1="{y:.1f}" x2="{x + k:.1f}" y2="{y:.1f}"/>'
          f'<line x1="{x:.1f}" y1="{y - k:.1f}" x2="{x:.1f}" y2="{y + k:.1f}"/></g>')

    a(f'<g mask="url(#bootMask)" opacity=".9"><image xlink:href="{TITLE}" '
      f'x="{img_x:.2f}" y="{img_y:.2f}" width="{img_w:.2f}" height="{img_h:.2f}"/></g>')
else:
    # hero.png is 1376x768; slice it to the window's shape rather than squashing it.
    a(f'<svg x="0" y="0" width="{W}" height="{H}" viewBox="0 90 1376 660" '
      f'preserveAspectRatio="xMidYMid slice">'
      f'<image xlink:href="{COMIC}" x="0" y="0" width="1376" height="768"/></svg>')
    a(f'<rect width="{W}" height="{H}" fill="url(#dark)"/>')
    a(f'<ellipse cx="{W / 2}" cy="{MARK_Y + mark_h / 2:.0f}" rx="440" ry="190" '
      f'fill="url(#cloud)"/>')
    a(f'<ellipse cx="{W / 2}" cy="{MARK_Y + mark_h / 2:.0f}" rx="370" ry="150" '
      f'fill="url(#plate)"/>')

a(f'<g mask="url(#markMask)"><image xlink:href="{TITLE}" '
  f'x="{img_x:.2f}" y="{img_y:.2f}" width="{img_w:.2f}" height="{img_h:.2f}"/></g>')

# Futura rather than the game's own Elektora: librsvg goes through CoreText on macOS, which only
# sees installed fonts, and a build should not depend on anyone having installed one.
a(f'<text x="{W / 2}" y="{mark_bottom + 50:.0f}" text-anchor="middle" font-family="Futura" '
  f'font-size="15" letter-spacing="6" fill="#a6ff3d">'
  f'DRAG STARQUAKE INTO YOUR APPLICATIONS FOLDER</text>')

# A barely-there lift under each icon, so they read as seated rather than floating.
for cx in (APP_X, APPS_X, README_X):
    a(f'<ellipse cx="{cx}" cy="{ICON_Y - 8}" rx="88" ry="80" fill="url(#seat)"/>')

x0, x1, y = APP_X + 84, APPS_X - 88, ICON_Y - 8
path = f'M {x0} {y + 12} Q {(x0 + x1) / 2} {y - 30} {x1 - 6} {y + 8}'
head = f'M {x1 - 22} {y - 4} L {x1 + 2} {y + 9} L {x1 - 19} {y + 19}'
for extra in ('filter="url(#glow)" opacity=".9"', ''):   # the blurred pass is the glow
    a(f'<g fill="none" stroke="url(#arrow)" stroke-width="6" stroke-linecap="round" '
      f'stroke-linejoin="round" {extra}><path d="{path}"/><path d="{head}"/></g>')
a(f'<circle cx="{x0}" cy="{y + 12}" r="4.5" fill="#28c0b0"/>')

a('</svg>')

svg = os.path.join(WORK, "background.svg")
with open(svg, "w") as f:
    f.write("\n".join(out))
try:
    subprocess.run(["rsvg-convert", "-w", str(W * SCALE), "-h", str(H * SCALE), svg, "-o", OUT],
                   check=True)
finally:
    shutil.rmtree(WORK)
print(f"Wrote {OUT} ({VARIANT})")
