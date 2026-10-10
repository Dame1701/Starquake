#!/usr/bin/env bash
#
# Wrap a published Starquake.app in a drag-to-Applications disk image.
#
#   build-dmg.sh <path/to/Starquake.app> <path/to/output.dmg>
#
# A plain `hdiutil create -srcfolder Starquake.app` produces a working image and a joyless one:
# a single icon in a default Finder window, with nothing to say that the app is meant to be
# copied to Applications rather than run from the mount. Everything that makes a DMG look
# deliberate - window size, icon positions, a background picture - lives in a .DS_Store that only
# Finder knows how to write, so there is no way to set it without a Finder session driving the
# mounted volume over AppleScript. create-dmg (`brew install create-dmg`) is that driver; the
# layout numbers below are what it hands to Finder.
#
# The bundle arrives here already ad-hoc signed by CreateAotAppBundle in Starquake.Mac.csproj,
# which is what lets Apple Silicon start it at all. Copying it into the image preserves that, so
# nothing is signed again here. See "macOS: it is signed, and that is not the same as being
# allowed to run" in plans/BUILD.md for what the signature does and does not buy.

set -euo pipefail

APP="${1:?usage: build-dmg.sh <Starquake.app> <output.dmg>}"
DMG="${2:?usage: build-dmg.sh <Starquake.app> <output.dmg>}"

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKGROUND="$HERE/background@2x.png"
VOLICON="$HERE/../Starquake.icns"
README_SRC="$HERE/ReadMe.txt"
README_NAME="Read Me - First Launch.txt"

[[ -d "$APP" ]] || { echo "No app bundle at $APP" >&2; exit 1; }
command -v create-dmg >/dev/null || { echo "create-dmg not found - brew install create-dmg" >&2; exit 1; }

# The window, in points, and where the three icons sit in it. These are the coordinates the
# background art was drawn to: background.py lays out the wordmark, the instruction line and the
# arrow around exactly these positions, so moving one without the other pulls the picture apart.
WIN_W=1000; WIN_H=480; ICON=128
APP_X=150;    APP_Y=340
APPS_X=500;   APPS_Y=340
README_X=850; README_Y=340

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
STAGE="$WORK/stage"
mkdir -p "$STAGE"

# ditto rather than cp: it is the copy that keeps extended attributes and the code signature
# intact, and a bundle whose seal is broken in transit is worse than an unsigned one.
ditto "$APP" "$STAGE/Starquake.app"
cp "$README_SRC" "$STAGE/$README_NAME"

# Tag the @2x art 144 dpi so Finder draws it at half its pixel size - 1000x480 points, retina
# crisp - instead of filling the window with the top-left quarter of it. Done on a copy, so the
# committed asset stays a plain 72 dpi file.
BG="$WORK/background.png"
cp "$BACKGROUND" "$BG"
sips -s dpiHeight 144 -s dpiWidth 144 "$BG" >/dev/null

rm -f "$DMG"
create-dmg \
    --volname Starquake \
    --volicon "$VOLICON" \
    --background "$BG" \
    --window-pos 200 120 \
    --window-size "$WIN_W" "$WIN_H" \
    --icon-size "$ICON" \
    --text-size 13 \
    --icon Starquake.app "$APP_X" "$APP_Y" \
    --app-drop-link "$APPS_X" "$APPS_Y" \
    --icon "$README_NAME" "$README_X" "$README_Y" \
    --hide-extension Starquake.app \
    --no-internet-enable \
    --format UDZO \
    "$DMG" "$STAGE"

echo "Built $DMG ($(du -sh "$DMG" | cut -f1))"
