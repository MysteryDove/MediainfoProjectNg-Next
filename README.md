# MediainfoProjectNg-Next

Cross-platform rewrite of MediainfoProjectNg for .NET 10 + Avalonia.

**Status:** V1 implementation in progress (team execution from approved plan).

## Spec and plan

- Product requirements: [`SPEC.md`](SPEC.md)
- Implementation plan: [`.omc/plans/ralplan-mediainfoprojectng-next-v1.md`](.omc/plans/ralplan-mediainfoprojectng-next-v1.md)
- Behavioral reference (unchanged): `../mpng`

## Solution layout

| Project | Role |
| --- | --- |
| `MediainfoProjectNg.Next` | Avalonia desktop host + composition root |
| `MediainfoProjectNg.Next.Domain` | Models, validation, folder rules |
| `MediainfoProjectNg.Next.Core` | Load orchestration, `IMediaMetadataReader` |
| `MediainfoProjectNg.Next.MediaInfo` | Native bindings + projection |
| `MediainfoProjectNg.Next.Tests` | Unit + characterization tests |

## Prerequisites

- .NET 10 SDK
- Git
- For native MediaInfo builds: CMake, Ninja, C++ toolchain; MediaInfoLib + ZenLib sources

## Managed build and test

```bash
dotnet restore MediainfoProjectNg.Next.sln
dotnet build MediainfoProjectNg.Next.sln
dotnet test tests/MediainfoProjectNg.Next.Tests/MediainfoProjectNg.Next.Tests.csproj
```

Domain and Core tests do not require MediaInfo natives.

## Native

See [`native/README.md`](native/README.md). Submodules are added in Phase 1B.

## Platforms (V1)

Windows x64/arm64, Linux x64/arm64, macOS — self-contained, unsigned.

macOS: first launch may require **System Settings → Privacy & Security → Open Anyway**.

### CI publish artifacts

Every `build-check` run (push / PR / manual) uploads **self-contained** publish folders (**.NET 10 runtime included**) after tests pass. Each folder also bundles **MediaInfo native**:

| Artifact name | Contents |
| --- | --- |
| `publish-win-x64` / `publish-win-arm64` | Self-contained Windows + `MediaInfo.dll` |
| `publish-linux-x64` / `publish-linux-arm64` | Self-contained Linux + `libmediainfo.so` |
| `publish-osx-arm64` / `publish-osx-x64` | Self-contained macOS + `libmediainfo.dylib` |
| `app-bundle-osx-arm64` / `app-bundle-osx-x64` | Unsigned `.app.zip` (includes dylib) |

Native is built per RID via `./native/build-rid.sh <rid>` (osx-x64 cross-builds on Apple Silicon; win-arm64 cross-builds with MSVC). Download from the Actions run summary.

### macOS `.app` (unsigned)

Self-contained publish is a folder of binaries; wrap it for double-click:

```bash
# 1) Publish (non-single-file)
dotnet publish src/MediainfoProjectNg.Next/MediainfoProjectNg.Next.csproj \
  -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=false \
  -o publish/osx-arm64

# Ensure MediaInfo is next to the app binary (if not already copied)
# cp native/out/host/lib/libmediainfo.dylib publish/osx-arm64/

# 2) Pack unsigned .app (no codesign)
./scripts/pack-macos-app.sh --rid osx-arm64 --version 1.0.0

# 3) Launch
open publish/MediainfoProjectNg.Next.app
```

Output: `publish/MediainfoProjectNg.Next.app` (unsigned). See `./scripts/pack-macos-app.sh --help`.

## License

MIT for new code. See [`LICENSE`](LICENSE) and [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).
