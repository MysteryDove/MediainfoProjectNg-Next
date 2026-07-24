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

Windows x64/arm64, Linux x64/arm64, macOS. macOS: first launch may require **System Settings → Privacy & Security → Open Anyway**.

### CI

| Trigger | Workflow | What runs |
| --- | --- | --- |
| **Push** to `main` | `build-check` | Quick **managed** restore / build / unit tests only (no native MediaInfo, no multi-RID publish) |
| **Pull request** | `build-check` → `publish-bundled` | Tests, then full multi-RID publish with MediaInfo native |
| **draft-release** (manual) | `draft-release` → `publish-bundled` | Same packages; `dry_run=false` attaches zips to a GitHub **draft** release |
| Manual | `publish-bundled` or `build-check` with *publish_bundled* | Full packages on demand |

Package artifacts (PR / draft-release / manual) — **Native AOT is never used**:

| Artifact | Platform | .NET packaging |
| --- | --- | --- |
| `publish-win-x64` / `publish-win-arm64` | Windows | **Framework-dependent** (no runtime/AOT in the zip; install [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)) + `MediaInfo.dll` |
| `publish-linux-*` / `publish-osx-*` | Linux / macOS | **Self-contained** (runtime included) + MediaInfo native |
| `publish-zip-*` | all | Zip of each publish folder |
| `app-bundle-osx-*` | macOS | Unsigned `.app.zip` (self-contained) |

Native MediaInfo is built per RID via `./native/build-rid.sh`.

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
