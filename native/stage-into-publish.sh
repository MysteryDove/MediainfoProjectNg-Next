#!/usr/bin/env bash
# Copy MediaInfo native library for a RID into a publish directory with loader-friendly names.
#
# Usage:
#   ./native/stage-into-publish.sh <rid> <publish-dir>
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RID="${1:-}"
DEST="${2:-}"

die() { echo "error: $*" >&2; exit 1; }

[[ -n "$RID" && -n "$DEST" ]] || die "usage: $0 <rid> <publish-dir>"
[[ -d "$DEST" ]] || die "publish dir not found: $DEST"

SRC="$ROOT/native/out/$RID"
[[ -d "$SRC" ]] || die "native out missing for $RID: $SRC (run ./native/build-rid.sh $RID first)"

stage_one() {
  local from="$1" to="$2"
  cp -f "$from" "$to"
  echo "Staged $(basename "$from") -> $to"
}

case "$RID" in
  win-*)
    dll=""
    for c in "$SRC/MediaInfo.dll" "$SRC/mediainfo.dll" "$SRC/bin/MediaInfo.dll" "$SRC/bin/mediainfo.dll"; do
      if [[ -f "$c" ]]; then dll="$c"; break; fi
    done
    if [[ -z "$dll" ]]; then
      dll="$(find "$SRC" -iname 'mediainfo.dll' -type f 2>/dev/null | head -n1 || true)"
    fi
    [[ -n "$dll" ]] || die "MediaInfo.dll not found under $SRC"
    # Loader probes MediaInfo.dll; keep both names for safety.
    stage_one "$dll" "$DEST/MediaInfo.dll"
    stage_one "$dll" "$DEST/mediainfo.dll"
    ;;
  osx-*)
    dylib="$(find "$SRC" -name 'libmediainfo.dylib' -type f 2>/dev/null | head -n1 || true)"
    if [[ -z "$dylib" ]]; then
      dylib="$(find "$SRC" -name 'libmediainfo*.dylib' -type f 2>/dev/null | head -n1 || true)"
    fi
    [[ -n "$dylib" ]] || die "libmediainfo.dylib not found under $SRC"
    stage_one "$dylib" "$DEST/libmediainfo.dylib"
    # Also drop into native/ for secondary probe path.
    mkdir -p "$DEST/native"
    stage_one "$dylib" "$DEST/native/libmediainfo.dylib"
    ;;
  linux-*)
    # Prefer real .so files, stage soname family.
    if compgen -G "$SRC/lib/libmediainfo.so*" > /dev/null; then
      cp -a "$SRC"/lib/libmediainfo.so* "$DEST/"
    elif compgen -G "$SRC/libmediainfo.so*" > /dev/null; then
      cp -a "$SRC"/libmediainfo.so* "$DEST/"
    else
      so="$(find "$SRC" -name 'libmediainfo.so*' -type f 2>/dev/null | head -n1 || true)"
      [[ -n "$so" ]] || die "libmediainfo.so not found under $SRC"
      cp -f "$so" "$DEST/libmediainfo.so"
    fi
    if [[ ! -f "$DEST/libmediainfo.so" ]]; then
      # Pick any versioned so as the loader name.
      any="$(ls "$DEST"/libmediainfo.so* 2>/dev/null | head -n1 || true)"
      [[ -n "$any" ]] || die "failed to stage libmediainfo.so"
      cp -f "$any" "$DEST/libmediainfo.so"
    fi
    mkdir -p "$DEST/native"
    cp -f "$DEST/libmediainfo.so" "$DEST/native/libmediainfo.so"
    echo "Staged libmediainfo.so* into $DEST"
    ls -la "$DEST"/libmediainfo.so* || true
    ;;
  *)
    die "unsupported RID: $RID"
    ;;
esac

echo "OK: native MediaInfo staged for $RID into $DEST"
