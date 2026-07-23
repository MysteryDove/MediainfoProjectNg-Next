# External source pins

Pinned commits for MediaInfoLib and ZenLib. Prefer git submodules (see `.gitmodules`); this file documents the exact SHAs used for reproducible native builds.

| Path | Upstream | Pin (full SHA) | Notes |
| --- | --- | --- | --- |
| `external/MediaInfoLib` | https://github.com/MediaArea/MediaInfoLib | `dd11d7971107e1b554e41ed446387d22cb3198e9` | master @ 2026-06-21 (PR #2643) |
| `external/ZenLib` | https://github.com/MediaArea/ZenLib | `2ddc277fe7ecfcbfe45616bb9cd9e23079113ecd` | master @ 2026-06-12 (PR #199) |

## Initialize / update

```bash
# After clone (submodules present in .gitmodules):
git submodule update --init --recursive

# Or pin explicitly (offline-friendly once objects exist):
git -C external/MediaInfoLib checkout dd11d7971107e1b554e41ed446387d22cb3198e9
git -C external/ZenLib checkout 2ddc277fe7ecfcbfe45616bb9cd9e23079113ecd
```

## Layout expectation for MediaInfoLib CMake

MediaInfoLib `Project/CMake` with `-DBUILD_ZENLIB=ON` expects ZenLib at:

`external/MediaInfoLib/Project/CMake/../../../ZenLib` → `external/ZenLib`

That matches this repository layout. System **zlib** is used on non-Windows (`-DBUILD_ZLIB=OFF`); install zlib headers if the toolchain does not ship them (on macOS, Xcode CLT provides them).

## Bumping pins

1. Update submodule commits: `git -C external/<name> fetch && git -C external/<name> checkout <new-sha>`
2. Record the new SHAs in this file and stage the submodule gitlinks + this file together.
3. Re-run `./native/build-host.sh` and host smoke before merging.
