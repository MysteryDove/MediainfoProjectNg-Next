#!/usr/bin/env bash
# Build MediaInfoLib shared library for a specific .NET RID and stage under native/out/<rid>/.
#
# Usage:
#   ./native/build-rid.sh osx-arm64
#   ./native/build-rid.sh osx-x64
#   ./native/build-rid.sh linux-x64
#   ./native/build-rid.sh win-x64
#
# Env:
#   GENERATOR   CMake generator (default: Ninja)
#   BUILD_TYPE  Release (default)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RID="${1:-}"
GENERATOR="${GENERATOR:-Ninja}"
BUILD_TYPE="${BUILD_TYPE:-Release}"
MIL_CMAKE="$ROOT/external/MediaInfoLib/Project/CMake"
ZEN_DIR="$ROOT/external/ZenLib"

die() { echo "error: $*" >&2; exit 1; }

if [[ -z "$RID" ]]; then
  die "usage: $0 <rid>   e.g. osx-arm64 | osx-x64 | linux-x64 | linux-arm64 | win-x64 | win-arm64"
fi

case "$RID" in
  osx-arm64|osx-x64|linux-x64|linux-arm64|win-x64|win-arm64) ;;
  *) die "unsupported RID: $RID" ;;
esac

OUT="$ROOT/native/out/$RID"
BUILD="$ROOT/native/build/$RID"
mkdir -p "$OUT" "$BUILD"

need_cmd() {
  command -v "$1" >/dev/null 2>&1 || die "'$1' not found on PATH"
}

need_cmd cmake

if [[ ! -f "$MIL_CMAKE/CMakeLists.txt" ]]; then
  die "MediaInfoLib CMake missing at $MIL_CMAKE (git submodule update --init --recursive)"
fi
if [[ ! -f "$ZEN_DIR/Project/CMake/CMakeLists.txt" ]]; then
  die "ZenLib missing at $ZEN_DIR (git submodule update --init --recursive)"
fi

HOST_OS="$(uname -s | tr '[:upper:]' '[:lower:]')"
HOST_ARCH="$(uname -m)"

# ---- platform / arch validation ----
case "$RID" in
  osx-*)
    [[ "$HOST_OS" == "darwin" ]] || die "RID $RID requires macOS host (got $HOST_OS)"
    ;;
  linux-*)
    [[ "$HOST_OS" == "linux" ]] || die "RID $RID requires Linux host (got $HOST_OS)"
    ;;
  win-*)
    # GitHub windows runners report MINGW/MSYS/CYGWIN via uname; allow all.
    case "$HOST_OS" in
      mingw*|msys*|cygwin*|windows*|windows_nt) ;;
      *)
        # uname -s on pure cmd is not used; bash on GHA windows is MINGW64_NT-...
        if [[ "$HOST_OS" != *"mingw"* && "$HOST_OS" != *"msys"* && "$OSTYPE" != *"msys"* && "$OSTYPE" != *"win"* ]]; then
          # Still allow if cmake works (MSYS2 / Git Bash)
          :
        fi
        ;;
    esac
    ;;
esac

USE_MULTI_CONFIG=0
case "$RID" in
  win-*)
    # Prefer Ninja + MSVC env (GitHub: ilammy/msvc-dev-cmd). Fall back to VS generator.
    if ! command -v ninja >/dev/null 2>&1 && [[ "$GENERATOR" == "Ninja" ]]; then
      GENERATOR="Visual Studio 17 2022"
    fi
    if [[ "$GENERATOR" == "Visual Studio"* ]]; then
      USE_MULTI_CONFIG=1
    fi
    ;;
esac

if [[ "$GENERATOR" == "Ninja" ]]; then
  need_cmd ninja
fi

CMAKE_ARGS=(
  -S "$MIL_CMAKE"
  -B "$BUILD"
  -DBUILD_SHARED_LIBS=ON
  -DBUILD_ZENLIB=ON
  -DCMAKE_INSTALL_PREFIX="$OUT"
)

if [[ "$USE_MULTI_CONFIG" -eq 0 ]]; then
  CMAKE_ARGS+=(-DCMAKE_BUILD_TYPE="$BUILD_TYPE")
fi

# zlib: system on Unix, bundled on Windows (MediaInfoLib default ON for WIN32)
case "$RID" in
  win-*)
    CMAKE_ARGS+=(-DBUILD_ZLIB=ON)
    if [[ "$GENERATOR" == "Visual Studio"* ]]; then
      if [[ "$RID" == "win-x64" ]]; then
        CMAKE_ARGS+=(-A x64)
      else
        CMAKE_ARGS+=(-A ARM64)
      fi
    fi
    ;;
  *)
    CMAKE_ARGS+=(-DBUILD_ZLIB=OFF)
    ;;
esac

# Architecture targeting (Unix)
case "$RID" in
  osx-arm64)
    CMAKE_ARGS+=(-DCMAKE_OSX_ARCHITECTURES=arm64)
    ;;
  osx-x64)
    # Cross-compile from Apple Silicon is supported by Xcode.
    CMAKE_ARGS+=(-DCMAKE_OSX_ARCHITECTURES=x86_64)
    ;;
  linux-x64)
    if [[ "$HOST_ARCH" != "x86_64" ]]; then
      die "linux-x64 requires x86_64 host (got $HOST_ARCH)"
    fi
    ;;
  linux-arm64)
    if [[ "$HOST_ARCH" != "aarch64" && "$HOST_ARCH" != "arm64" ]]; then
      die "linux-arm64 requires aarch64 host (got $HOST_ARCH)"
    fi
    ;;
esac

echo "==> Configure MediaInfoLib for RID=$RID (generator=$GENERATOR)"
cmake -G "$GENERATOR" "${CMAKE_ARGS[@]}"

echo "==> Build"
if [[ "$USE_MULTI_CONFIG" -eq 1 ]]; then
  cmake --build "$BUILD" --config "$BUILD_TYPE" --parallel
  echo "==> Install"
  cmake --install "$BUILD" --config "$BUILD_TYPE"
else
  cmake --build "$BUILD" --parallel
  echo "==> Install"
  cmake --install "$BUILD"
fi

# Stage flat copies at OUT root for simple loader probes.
shopt -s nullglob
for f in \
  "$OUT"/lib/libmediainfo* \
  "$OUT"/bin/libmediainfo* \
  "$OUT"/bin/mediainfo* \
  "$OUT"/bin/MediaInfo* \
  "$OUT"/lib/mediainfo* \
  "$OUT"/lib/MediaInfo*
do
  if [[ -f "$f" ]]; then
    cp -f "$f" "$OUT/" 2>/dev/null || cp -f "$f" "$OUT/"
  fi
done
shopt -u nullglob

# Windows CMake target is "mediainfo" → often mediainfo.dll; app probes MediaInfo.dll.
if [[ "$RID" == win-* ]]; then
  if [[ -f "$OUT/mediainfo.dll" && ! -f "$OUT/MediaInfo.dll" ]]; then
    cp -f "$OUT/mediainfo.dll" "$OUT/MediaInfo.dll"
  fi
  if [[ -f "$OUT/bin/mediainfo.dll" && ! -f "$OUT/MediaInfo.dll" ]]; then
    cp -f "$OUT/bin/mediainfo.dll" "$OUT/MediaInfo.dll"
  fi
  # Also search build tree if install layout is sparse.
  if [[ ! -f "$OUT/MediaInfo.dll" ]]; then
    found="$(find "$BUILD" "$OUT" -iname 'mediainfo.dll' -type f 2>/dev/null | head -n1 || true)"
    if [[ -n "$found" ]]; then
      cp -f "$found" "$OUT/MediaInfo.dll"
      cp -f "$found" "$OUT/mediainfo.dll"
    fi
  fi
fi

echo "==> Artifacts under $OUT:"
ls -la "$OUT" || true
[[ -d "$OUT/lib" ]] && ls -la "$OUT/lib" || true
[[ -d "$OUT/bin" ]] && ls -la "$OUT/bin" || true

# Require the name our managed loader looks for (plus Linux soname variants).
ok=0
case "$RID" in
  win-*)
    [[ -f "$OUT/MediaInfo.dll" || -f "$OUT/mediainfo.dll" ]] && ok=1
    ;;
  osx-*)
    find "$OUT" -name 'libmediainfo*.dylib' -type f | grep -q . && ok=1
    ;;
  linux-*)
    find "$OUT" -name 'libmediainfo.so*' -type f | grep -q . && ok=1
    ;;
esac

if [[ "$ok" -ne 1 ]]; then
  die "no MediaInfo shared library found for $RID under $OUT"
fi

echo "OK: MediaInfoLib for $RID ready under $OUT"
