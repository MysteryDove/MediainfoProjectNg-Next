#!/usr/bin/env bash
# Build MediaInfoLib shared library for the host RID (delegates to build-rid.sh).
# macOS (darwin) is the primary host target for local development.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

die() { echo "error: $*" >&2; exit 1; }

detect_host_rid() {
  local os arch
  os="$(uname -s | tr '[:upper:]' '[:lower:]')"
  arch="$(uname -m)"
  case "$arch" in
    x86_64|amd64) arch="x64" ;;
    aarch64|arm64) arch="arm64" ;;
    *) die "unsupported host arch: $arch" ;;
  esac
  case "$os" in
    darwin) echo "osx-${arch}" ;;
    linux) echo "linux-${arch}" ;;
    mingw*|msys*|cygwin*) echo "win-${arch}" ;;
    *) die "unsupported host OS: $os (use ./native/build-rid.sh <rid> explicitly)" ;;
  esac
}

RID="$(detect_host_rid)"
echo "==> Host RID: $RID"
chmod +x "$ROOT/native/build-rid.sh"
"$ROOT/native/build-rid.sh" "$RID"

# Keep legacy path native/out/host as a symlink/copy of the RID output for older docs.
HOST_OUT="$ROOT/native/out/host"
RID_OUT="$ROOT/native/out/$RID"
rm -rf "$HOST_OUT"
mkdir -p "$HOST_OUT"
# Copy so tools that expect out/host keep working without following symlinks on Windows.
cp -a "$RID_OUT"/. "$HOST_OUT/"
echo "OK: also mirrored to $HOST_OUT"
echo "Pins: see native/PINS.md"
