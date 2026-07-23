# Native MediaInfo build

Host spike and packaging helpers for **MediaInfoLib** (+ **ZenLib**). Managed code lives under `src/`; this directory only builds and stages native shared libraries.

## Dependencies

| Tool | Role | macOS install hint |
| --- | --- | --- |
| CMake (≥ 3.5) | Configure MediaInfoLib `Project/CMake` | `brew install cmake ninja` |
| Ninja | Fast generator used by `build-host.sh` | `brew install cmake ninja` |
| C/C++ toolchain | Compile natives | Xcode CLT: `xcode-select --install` |
| zlib | Compression dependency | System / Xcode CLT on macOS (`BUILD_ZLIB=OFF`) |
| Git submodules | Source pins | `git submodule update --init --recursive` |

Pinned commits: [`native/PINS.md`](PINS.md)

Submodules:

- `external/MediaInfoLib` → https://github.com/MediaArea/MediaInfoLib
- `external/ZenLib` → https://github.com/MediaArea/ZenLib

If `cmake` or `ninja` is missing, `./native/build-host.sh` prints the install hints above and exits non-zero.

## Source layout (important for CMake)

MediaInfoLib's CMake project lives at:

```text
external/MediaInfoLib/Project/CMake/CMakeLists.txt
```

With `-DBUILD_ZENLIB=ON` it pulls ZenLib via a relative path:

```text
external/MediaInfoLib/Project/CMake/../../../ZenLib
  → external/ZenLib
```

Keep both trees as siblings under `external/`. Do not nest ZenLib inside MediaInfoLib.

Alternative build systems (documented by MediaArea, not used by the host spike script):

- `external/MediaInfoLib/Project/GNU` — autotools-style GNU build
- `external/MediaInfoLib/Project/CMake` — **preferred** for this repo

## Host / RID builds

```bash
# From repo root:
git submodule update --init --recursive

# Host RID (detect OS/arch → build + mirror to native/out/host):
./native/build-host.sh

# Explicit RID (used by CI for every platform artifact):
./native/build-rid.sh osx-arm64
./native/build-rid.sh osx-x64      # cross from Apple Silicon via CMAKE_OSX_ARCHITECTURES
./native/build-rid.sh linux-x64
./native/build-rid.sh win-x64      # MSVC + Ninja on Windows

# Copy into a publish folder (loader-friendly names):
./native/stage-into-publish.sh osx-arm64 publish/osx-arm64
```

What `build-rid.sh` does:

1. Verifies tools (`cmake`, `ninja` / MSVC) and submodules.
2. Configures MediaInfoLib:
   - `-DBUILD_SHARED_LIBS=ON`
   - `-DBUILD_ZENLIB=ON`
   - `-DBUILD_ZLIB=OFF` on Unix (system zlib); `ON` on Windows (bundled)
   - arch flags for `osx-x64` / MSVC `-A` when using VS generator
3. Installs into `native/out/<rid>/` and stages flat copies (`libmediainfo.dylib` / `.so` / `MediaInfo.dll`).

Example configure equivalent (for CI or debugging):

```bash
cmake -G Ninja \
  -S external/MediaInfoLib/Project/CMake \
  -B native/build/host \
  -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON \
  -DBUILD_ZENLIB=ON \
  -DBUILD_ZLIB=OFF \
  -DCMAKE_INSTALL_PREFIX="$PWD/native/out/host"
cmake --build native/build/host --parallel
cmake --install native/build/host
```

On **Windows**, MediaInfoLib defaults `BUILD_ZLIB` / `BUILD_ZENLIB` to ON and may need MSVC + bundled zlib; the host script is optimized for darwin/Linux first.

## Output

| Path | Content |
| --- | --- |
| `native/build/host/` | CMake build tree (gitignored) |
| `native/out/host/` | Installed shared library + headers (gitignored) |

Library target name: **`mediainfo`** → `libmediainfo.dylib` (macOS), `libmediainfo.so` (Linux), `mediainfo.dll` (Windows).

## Notes

- V1 default publish is self-contained **non-single-file**.
- macOS Universal packaging is handled later in Phase 5 (lipo / multi-arch matrix).
- Domain/Core unit tests do **not** require natives; only MediaInfo adapter / integration tests will load `libmediainfo`.
- Keep third-party license notices in [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) when shipping binaries.
