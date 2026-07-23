#!/usr/bin/env bash
# Build MediaInfoLib shared library for the host RID (Phase 1B spike).
# macOS (darwin) is the primary host target for local development.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/native/out/host"
BUILD="$ROOT/native/build/host"
MIL_CMAKE="$ROOT/external/MediaInfoLib/Project/CMake"
ZEN_DIR="$ROOT/external/ZenLib"

mkdir -p "$OUT" "$BUILD"

die() {
  echo "error: $*" >&2
  exit 1
}

hint_macos_tools() {
  if [[ "$(uname -s)" == "Darwin" ]]; then
    cat >&2 <<'EOF'
Install build tools on macOS (Homebrew):
  brew install cmake ninja
Also ensure Xcode Command Line Tools are installed:
  xcode-select --install
EOF
  else
    cat >&2 <<'EOF'
Install build tools for your platform, for example:
  # Debian/Ubuntu
  sudo apt-get install -y cmake ninja-build g++ zlib1g-dev
  # Fedora
  sudo dnf install -y cmake ninja-build gcc-c++ zlib-devel
EOF
  fi
}

need_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "error: '$cmd' not found on PATH." >&2
    hint_macos_tools
    exit 1
  fi
}

need_cmd cmake
need_cmd ninja

if ! command -v c++ >/dev/null 2>&1 && ! command -v clang++ >/dev/null 2>&1 && ! command -v g++ >/dev/null 2>&1; then
  echo "error: no C++ compiler (c++/clang++/g++) found." >&2
  hint_macos_tools
  exit 1
fi

if [[ ! -f "$MIL_CMAKE/CMakeLists.txt" ]]; then
  die "MediaInfoLib CMake project missing at $MIL_CMAKE
Initialize submodules:
  git submodule update --init --recursive
See native/PINS.md for pinned commits."
fi

if [[ ! -f "$ZEN_DIR/Project/CMake/CMakeLists.txt" ]]; then
  die "ZenLib missing at $ZEN_DIR (required for -DBUILD_ZENLIB=ON).
Initialize submodules:
  git submodule update --init --recursive
See native/PINS.md."
fi

# MediaInfoLib Project/CMake with BUILD_ZENLIB=ON resolves:
#   ${CMAKE_CURRENT_SOURCE_DIR}/../../../ZenLib  ->  external/ZenLib
# Shared library target name: mediainfo  ->  libmediainfo.dylib / .so / mediainfo.dll
echo "==> Configure MediaInfoLib shared library (host)"
cmake -G Ninja \
  -S "$MIL_CMAKE" \
  -B "$BUILD" \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DBUILD_ZENLIB=ON \
  -DBUILD_ZLIB=OFF \
  -DCMAKE_INSTALL_PREFIX="$OUT"

echo "==> Build"
cmake --build "$BUILD" --parallel

echo "==> Install into $OUT"
cmake --install "$BUILD"

# Also stage copies at out root for simple loader probes (in addition to lib/).
shopt -s nullglob
for f in "$OUT"/lib/libmediainfo* "$OUT"/bin/mediainfo* "$OUT"/lib/mediainfo*; do
  if [[ -f "$f" ]]; then
    cp -f "$f" "$OUT/"
  fi
done
shopt -u nullglob

echo "==> Host shared library artifacts:"
ls -la "$OUT" || true
if [[ -d "$OUT/lib" ]]; then
  ls -la "$OUT/lib" || true
fi

# Sanity: expect a shared library somewhere under OUT
if ! find "$OUT" \( -name 'libmediainfo*.dylib' -o -name 'libmediainfo*.so*' -o -name 'mediainfo.dll' \) 2>/dev/null | grep -q .; then
  echo "warning: no libmediainfo shared library found under $OUT — check CMake install layout." >&2
  exit 1
fi

echo "OK: MediaInfoLib host shared library ready under $OUT"
echo "Pins: see native/PINS.md"
