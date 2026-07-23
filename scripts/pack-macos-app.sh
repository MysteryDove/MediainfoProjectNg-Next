#!/usr/bin/env bash
# Pack a self-contained osx publish folder into an unsigned .app bundle.
# No codesign / notarization — double-click may require Privacy & Security → Open Anyway.
#
# Usage (from repo root):
#   ./scripts/pack-macos-app.sh
#   ./scripts/pack-macos-app.sh --publish-dir publish/osx-arm64 --out publish/MediainfoProjectNg.Next.app
#   ./scripts/pack-macos-app.sh --rid osx-arm64 --version 1.0.0
#
# Options:
#   --publish-dir DIR   Publish output to wrap (default: publish/<rid>)
#   --out PATH          .app path (default: publish/MediainfoProjectNg.Next.app)
#   --rid RID           Used for default publish-dir (default: osx-arm64)
#   --exe NAME          Executable name inside publish (default: MediainfoProjectNg.Next)
#   --name DISPLAY      CFBundleDisplayName (default: MediainfoProjectNg Next)
#   --bundle-id ID      CFBundleIdentifier (default: local.mediainfoprojectng.next)
#   --version VER       CFBundleShortVersionString (default: 0.0.0)
#   --dylib PATH        Copy libmediainfo.dylib from PATH into MacOS/ if missing
#   --help              Show this help

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RID="osx-arm64"
PUBLISH_DIR=""
OUT_APP=""
EXE_NAME="MediainfoProjectNg.Next"
DISPLAY_NAME="MediainfoProjectNg Next"
BUNDLE_ID="local.mediainfoprojectng.next"
VERSION="0.0.0"
DYLIB_SRC=""

usage() {
  sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --publish-dir) PUBLISH_DIR="$2"; shift 2 ;;
    --out) OUT_APP="$2"; shift 2 ;;
    --rid) RID="$2"; shift 2 ;;
    --exe) EXE_NAME="$2"; shift 2 ;;
    --name) DISPLAY_NAME="$2"; shift 2 ;;
    --bundle-id) BUNDLE_ID="$2"; shift 2 ;;
    --version) VERSION="$2"; shift 2 ;;
    --dylib) DYLIB_SRC="$2"; shift 2 ;;
    --help|-h) usage; exit 0 ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$PUBLISH_DIR" ]]; then
  PUBLISH_DIR="$ROOT/publish/$RID"
fi
if [[ -z "$OUT_APP" ]]; then
  OUT_APP="$ROOT/publish/MediainfoProjectNg.Next.app"
fi

# Resolve relative paths against repo root when not absolute
[[ "$PUBLISH_DIR" = /* ]] || PUBLISH_DIR="$ROOT/$PUBLISH_DIR"
[[ "$OUT_APP" = /* ]] || OUT_APP="$ROOT/$OUT_APP"
if [[ -n "$DYLIB_SRC" && "$DYLIB_SRC" != /* ]]; then
  DYLIB_SRC="$ROOT/$DYLIB_SRC"
fi

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Publish directory not found: $PUBLISH_DIR" >&2
  echo "Publish first, e.g.:" >&2
  echo "  dotnet publish src/MediainfoProjectNg.Next/MediainfoProjectNg.Next.csproj \\" >&2
  echo "    -c Release -r $RID --self-contained true -p:PublishSingleFile=false \\" >&2
  echo "    -o publish/$RID" >&2
  exit 1
fi

EXE_PATH="$PUBLISH_DIR/$EXE_NAME"
if [[ ! -f "$EXE_PATH" ]]; then
  echo "Executable not found: $EXE_PATH" >&2
  exit 1
fi

# Stage MediaInfo native if requested or discover host build
if [[ -z "$DYLIB_SRC" ]]; then
  for cand in \
    "$PUBLISH_DIR/libmediainfo.dylib" \
    "$ROOT/native/out/host/lib/libmediainfo.dylib" \
    "$ROOT/native/out/host/libmediainfo.dylib"
  do
    if [[ -f "$cand" && "$cand" != "$PUBLISH_DIR/libmediainfo.dylib" ]]; then
      DYLIB_SRC="$cand"
      break
    fi
  done
fi

MACOS_DIR="$OUT_APP/Contents/MacOS"
RESOURCES_DIR="$OUT_APP/Contents/Resources"
PLIST="$OUT_APP/Contents/Info.plist"

echo "Packing unsigned .app"
echo "  publish : $PUBLISH_DIR"
echo "  out     : $OUT_APP"
echo "  exe     : $EXE_NAME"
echo "  version : $VERSION"
echo "  sign    : none (unsigned)"

rm -rf "$OUT_APP"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"

# Copy entire self-contained publish tree into Contents/MacOS
# so AppContext.BaseDirectory resolves next to managed + native libs.
cp -a "$PUBLISH_DIR"/. "$MACOS_DIR/"

if [[ -n "$DYLIB_SRC" && -f "$DYLIB_SRC" ]]; then
  if [[ ! -f "$MACOS_DIR/libmediainfo.dylib" ]]; then
    echo "  dylib   : copying $DYLIB_SRC"
    cp -f "$DYLIB_SRC" "$MACOS_DIR/libmediainfo.dylib"
  fi
fi

if [[ ! -f "$MACOS_DIR/libmediainfo.dylib" ]]; then
  echo "WARNING: libmediainfo.dylib missing under MacOS/ — MediaInfo will be Unavailable." >&2
  echo "  Copy it into the publish dir or pass --dylib path/to/libmediainfo.dylib" >&2
fi

chmod +x "$MACOS_DIR/$EXE_NAME"

# Minimal Info.plist — no CFBundleIconFile unless you add Resources/AppIcon.icns later
cat > "$PLIST" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>${DISPLAY_NAME}</string>
  <key>CFBundleDisplayName</key>
  <string>${DISPLAY_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>${BUNDLE_ID}</string>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleExecutable</key>
  <string>${EXE_NAME}</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>NSPrincipalClass</key>
  <string>NSApplication</string>
</dict>
</plist>
EOF

# Drop Finder quarantine on the bundle if present (does not sign).
if command -v xattr >/dev/null 2>&1; then
  xattr -cr "$OUT_APP" 2>/dev/null || true
fi

echo "Done (unsigned): $OUT_APP"
echo "Open with: open \"$OUT_APP\""
echo "If macOS blocks launch: right-click → Open, or System Settings → Privacy & Security → Open Anyway."
