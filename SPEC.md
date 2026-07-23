# MediainfoProjectNg-Next

## Product and Migration Specification

Status: approved requirements baseline  
Target framework: .NET 10  
Project type: standalone cross-platform desktop application  
Reference implementation: `../mpng`

## 1. Objective

Create a modern, standalone rewrite of the existing `MediainfoProjectNg` WPF application. The new project is named `MediainfoProjectNg-Next` and uses its own folder, Git repository, solution, build system, and release lifecycle.

The rewrite must preserve the original tool's intended media-inspection and validation behavior while replacing its Windows-only WPF implementation and manually supplied `MediaInfo.dll` dependency with:

- A shared .NET 10 desktop codebase.
- A modern but compact cross-platform UI.
- A pinned upstream MediaInfoLib source integration.
- Self-contained platform deliverables.
- Automated builds, tests, artifacts, and draft releases.

The original `../mpng` repository remains unchanged and is the behavioral reference implementation.

## 2. Product Principles

1. This is a technical desktop tool. Workflow efficiency, information density, correctness, and scanning speed take priority over decorative UI.
2. The interface may be modernized, but it must remain restrained, compact, platform-appropriate, and recognizable to existing users.
3. V1 is a feature-parity rewrite, not a general redesign or product expansion.
4. Preserve intended original behavior strictly unless this specification explicitly permits a change.
5. Users must not locate, install, or manage MediaInfo or a separate .NET runtime.
6. Use one shared application and UI codebase across all supported platforms.

## 3. Supported Platforms

V1 supports desktop operating systems only.

| Platform | Architectures | User-managed application |
| --- | --- | --- |
| Windows | x64, arm64 | Self-contained executable |
| Linux | x64, arm64 | Self-contained executable |
| macOS | Universal x64 + arm64 | Self-contained `.app` bundle |

Android, iOS, browsers, and other platforms are out of scope.

Minimum operating-system versions follow the supported intersection of .NET 10, Avalonia, MediaInfoLib, and standard GitHub-hosted runners at implementation time. Legacy operating systems outside that supported intersection are not required.

### Deliverable Meaning

- Each platform release contains one application that the user manages and launches.
- Windows and Linux should expose a single application executable.
- macOS uses one `.app` bundle. A transport archive may be used to preserve the bundle structure and executable permissions, but the extracted user-facing deliverable is one `.app`.
- The user must not manually place or install `.dll`, `.so`, `.dylib`, .NET runtime, or other support files.

## 4. Naming

Respect the original project's naming pattern.

| Surface | Name |
| --- | --- |
| Repository and product | `MediainfoProjectNg-Next` |
| Solution | `MediainfoProjectNg.Next` |
| Main assembly | `MediainfoProjectNg.Next` |
| Root C# namespace | `MediainfoProjectNg.Next` |
| Display title | `mediainfo project ng next` |

## 5. Language

V1 is Simplified Chinese only.

- Preserve original Chinese terminology and labels where applicable.
- Correct inconsistent legacy English text when necessary to produce a coherent Simplified Chinese interface.
- Localization infrastructure and English translation are not required.

## 6. Required Technology Stack

Use a deliberately minimal dependency set.

### Application

- .NET 10
- Avalonia desktop
- Avalonia Fluent theme
- `Avalonia.Controls.DataGrid`
- `CommunityToolkit.Mvvm`
- Source-generated `.NET LibraryImport` bindings over MediaInfo's C API
- Built-in .NET APIs for application composition, JSON, concurrency, and diagnostic logging

### Native Build

- `MediaArea/MediaInfoLib` as a pinned Git submodule
- CMake
- Ninja where appropriate
- MSVC toolchains on Windows
- Supported Clang/GCC toolchains on macOS and Linux

### Testing

- xUnit
- .NET test SDK
- Small synthetic or legally redistributable media fixtures

### CI and Distribution

- GitHub Actions
- `dotnet publish`
- Standard GitHub-hosted runners only

### Avoid Unless a Demonstrated Need Appears

- ReactiveUI
- General-purpose dependency-injection containers
- AutoMapper
- Additional styling frameworks
- Databases or persistence frameworks
- Large application frameworks unrelated to the required desktop workflow

## 7. Architecture

Use one shared UI and application codebase across Windows, Linux, and macOS. Platform-specific code is limited to:

- Application packaging and native artifact selection.
- Native MediaInfo loading.
- File-dialog integration.
- Drag-and-drop differences required by the operating system.
- Small platform-specific UI adjustments required for usable native behavior.

Recommended project boundaries:

1. **Desktop application**: Avalonia views, view models, commands, theming, dialogs, and platform startup.
2. **Application/core layer**: load workflow, status transitions, duplicate filtering, and orchestration.
3. **Domain layer**: media information models, validation inputs, and validation results without Avalonia/WPF types.
4. **MediaInfo adapter**: native bindings, lifetime management, field projection, native loading, and error translation.
5. **Tests**: legacy characterization, domain unit tests, native integration tests, and UI smoke tests.

Do not carry WPF types such as `Brush`, `Color`, `Visibility`, or WPF converter interfaces into domain or validation logic. Validation results expose semantic values; the Avalonia UI maps those values to theme-aware presentation.

## 8. MediaInfoLib Integration

### Source Ownership

- Add `https://github.com/MediaArea/MediaInfoLib` as a Git submodule.
- Pin the submodule to a reviewed upstream tag or commit.
- Do not depend on a user-supplied `MediaInfo.dll`, `.so`, or `.dylib`.
- Do not use the legacy checked-in `MediaInfoDLL.cs` wrapper as the new integration boundary.
- Bind to MediaInfo's supported C API through source-generated `LibraryImport` declarations.

### Build and Packaging

- Build the required native MediaInfo targets in CI for every supported platform and architecture.
- Bundle the resulting native library into each self-contained application deliverable.
- Windows and Linux may extract bundled native components automatically to an internal runtime cache or temporary location before loading them.
- macOS stores native components inside the `.app` bundle.
- Users manage only the downloaded application and never manually install or locate MediaInfo.

Literal in-memory loading of native-library bytes is not required. NativeAOT and static linking are not required unless implementation evidence later proves them necessary.

### Compatibility

Preserve the application's metadata categories, projections, checks, and displayed workflow. Raw summary text and individual field values may differ when the pinned newer MediaInfoLib version provides different or improved upstream output.

## 9. Legacy Behavior to Preserve

The implementation in `../mpng` is the behavioral source of truth.

### Main Workflow

- Accept files and folders through drag-and-drop.
- Load media files sequentially using the original observable ordering.
- Display a dense metadata grid.
- Preserve multi-row selection.
- Delete selected rows with the Delete key.
- Suppress items according to the original duplicate-filtering behavior.
- Display current load progress and final elapsed time in the status area.
- Allow the right summary panel to be shown or hidden.
- Preserve clear behavior.
- Double-click a row to open its technical details window.

### Explicitly Allowed Compact Addition

Add compact **Open Files** and **Open Folder** commands. Drag-and-drop remains the fastest primary workflow. These controls must not expand the interface into a large toolbar or unnecessarily reduce the usable grid area.

### Main Metadata Grid

Preserve the original categories and practical density, including:

- Filename.
- Container.
- Video format, resolution, bit depth, frame rate, color format, language, and default state.
- First and second audio-track format, bit depth, bitrate, language, and default state.
- First subtitle format, language, and default state.
- Chapter state and language.
- Full path.
- Validation-driven row and cell colors.

The Avalonia implementation may replace WPF-specific bindings and converters, but observable content and semantics should remain equivalent.

### Summary Panel

- Keep a compact, hideable right-side panel.
- Display MediaInfo's raw summary output in a read-only monospaced text area.
- Preserve clear/show/hide behavior.

### Technical Details Window

- Display validation findings for the selected file.
- Display a hierarchical property tree for general, video, audio, subtitle, and chapter information.
- Preserve copying an individual value and copying a key/value pair.

## 10. Folder Processing

Follow the original behavior rather than redesigning folder traversal in V1.

- Traverse files and nested directories sequentially.
- Exclude directories named `CDs` and `Scans` according to the original rules.
- Exclude `.txt`, `.log`, and `.torrent` according to the original rules.
- Preserve original filtering, ordering, duplicate behavior, status updates, and error propagation as closely as cross-platform APIs permit.
- Do not add concurrency, cancellation, retry, symbolic-link-cycle handling, per-file error isolation, or continue-on-error behavior in V1.
- A filesystem or permission error may abort the batch when that matches the reference implementation.

These behaviors may be documented as known limitations but must not be silently redesigned.

## 11. Validation Engine

Port the original check engine strictly. The primary references are `Utils.CheckFile`, `FileNameContentMatched`, and their helper functions.

Preserve at least the following checks and their severity and presentation semantics:

- File extension versus container mismatch.
- Non-zero video or audio track delay.
- Excessive duration differences between tracks.
- Single-chapter warning.
- Multiple chapter-set warning.
- Useless final chapter warning.
- Non-zero first chapter timestamp warning.
- Filename description versus media-content mismatch.
- Informational multiple-audio-track result.
- Existing filename/profile/encoder generation logic and thresholds.

Characterization tests must lock the reference behavior before or alongside porting. Do not add new validation rules in V1.

## 12. UI and UX Direction

### General

- Modern, compact, restrained tool UI.
- Preserve information density and fast scanning.
- Avoid decorative or marketing-style composition.
- Avoid oversized headers, nested cards, and unnecessary whitespace.
- Use familiar icons for compact commands where appropriate.
- Keep the metadata grid as the dominant surface.
- Preserve efficient keyboard and pointer interactions.

### Native Appearance

Use Avalonia's Fluent theme as the shared base while allowing minor platform-specific adjustments. Do not build separate native frontends.

### Theme

- Follow the operating system's light/dark preference automatically.
- Preserve the semantic meaning of original validation colors.
- Adjust actual color values by theme when necessary for readable foreground/background contrast.
- Selection, focus, and validation colors must remain distinguishable.
- Theme support must not reduce grid readability or change validation meaning.

### Fidelity Boundary

Exact WPF pixel styling is not required. Preserve layout concepts, density, commands, labels, data categories, and interaction workflow. Modern improvements are acceptable when they do not disrupt established usage.

## 13. Feature Scope

### In Scope

- Cross-platform .NET 10 rewrite.
- Windows, Linux, and macOS target matrix.
- Direct pinned MediaInfoLib source integration.
- Self-contained application deliverables.
- Original behavior and validation parity.
- Compact Open Files and Open Folder commands.
- Automatic light/dark themes with readable contrast.
- Automated tests.
- Push/PR build artifacts.
- Manual draft-release automation.
- Documentation required to build, test, and release.

### Out of Scope / Non-goals

- Mobile support.
- Browser support.
- New media validation rules.
- Cloud services.
- User accounts.
- Telemetry or analytics.
- Database integration.
- Auto-update functionality.
- General folder-processing redesign.
- New concurrency or cancellation model.
- Installers or system packages.
- Microsoft Store or Mac App Store distribution.
- Code signing and macOS notarization in V1.
- Localization beyond Simplified Chinese.
- NativeAOT/static linking unless later shown necessary.
- Separate UI implementations per platform.

## 14. Bug-Fix Boundary

Preserve intended behavior rather than accidental WPF implementation details. Fixes are allowed when necessary to:

- Make the same intended behavior work on all supported platforms.
- Prevent obvious UI binding failures that do not define intended product behavior.
- Correct platform-specific packaging or native-loading problems.
- Make validation and media projection testable without changing results.

Folder traversal and its error behavior are explicitly required to follow the original implementation for V1. Do not use the general bug-fix allowance to redesign that workflow.

## 15. Testing Strategy

### Characterization Tests

- Encode original validation rules and thresholds.
- Cover every severity and message category.
- Cover filename/profile/encoder generation.
- Confirm ordering where observable.
- Record intentional legacy behavior before refactoring.

### Unit Tests

- Domain model projection.
- Validation engine.
- Theme-independent validation semantics.
- Duplicate filtering.
- Folder exclusions and traversal ordering.
- Status transitions.
- View-model commands.
- MediaInfo adapter error translation.

### Native Integration Tests

Use tiny synthetic or legally redistributable media fixtures covering:

- Video.
- Audio-only media.
- Subtitle tracks.
- Chapters.
- Multiple tracks.
- Malformed or unsupported content.
- Unicode filenames and paths.

Fixtures validate MediaInfo extraction and platform-native loading. They must not define new validation rules.

### UI Smoke Tests

Cover the critical workflow where practical:

- Application starts.
- Open Files and Open Folder are available.
- Drag-and-drop is wired.
- Grid renders expected columns.
- Right summary panel toggles.
- Selection and Delete behavior work.
- Technical details window opens.
- Copy commands work.
- Light/dark theme changes preserve readable contrast.

### Build Verification

- Restore, build, and test managed projects.
- Build the correct native MediaInfo artifact for every target.
- Publish every target deliverable.
- Verify required native components are bundled.
- Smoke-launch artifacts where the GitHub runner can execute the target architecture.

## 16. GitHub Actions

Use standard GitHub-hosted runners only. Prefer native runners when available and supported; otherwise use supported cross-compilation toolchains.

### Build Check Workflow

Triggers:

- Every push.
- Every pull request.

Responsibilities:

1. Initialize the MediaInfoLib submodule.
2. Restore and safely cache managed/native dependencies.
3. Build MediaInfoLib for the target platform and architecture.
4. Build the .NET solution.
5. Run unit and characterization tests.
6. Run native integration tests where executable on the runner.
7. Run applicable UI smoke checks.
8. Publish the self-contained application.
9. Upload downloadable artifacts for all targets.

Required artifacts:

- Windows x64.
- Windows arm64.
- Linux x64.
- Linux arm64.
- macOS Universal x64 + arm64.

Do not expose repository secrets to untrusted pull-request workflows.

### Manual Draft Release Workflow

Trigger: `workflow_dispatch`.

Inputs include at least:

- Release version.
- Source branch or commit, defaulting to the selected workflow ref.

Responsibilities:

1. Validate and normalize the version.
2. Build and test the selected commit using the release matrix.
3. Create and push the version tag chosen during release.
4. Produce the same artifact set as the build workflow.
5. Create a GitHub draft release.
6. Attach all platform artifacts to the draft release.
7. Leave publication of the draft as an explicit human action.

Do not create or move a tag until required build and test gates pass.

## 17. Signing and Notarization

Code signing is deferred from V1.

- Windows artifacts may trigger SmartScreen warnings.
- macOS artifacts may require users to attempt launch and then approve the app through **System Settings > Privacy & Security > Open Anyway**.
- Avalonia does not sign applications automatically.
- Future signing should remain compatible with credential-driven CI steps.
- Push/PR artifacts should remain unsigned even if later official releases are signed.

Apple Developer membership, Developer ID signing, notarization, Windows Authenticode certificates, Microsoft Trusted Signing, and related credentials are not required for V1 completion.

## 18. Licensing

Use MIT for newly written project code.

Required repository files during implementation:

- Root `LICENSE` containing the MIT license.
- `THIRD_PARTY_NOTICES.md` containing required notices and attribution for the original BSD-licensed MediainfoProjectNg code and MediaInfoLib.

Retain original BSD notices in substantially copied or closely ported source where required. Do not remove upstream MediaInfoLib license files or notices from the submodule or distributed notices.

## 19. Documentation

The repository must document:

- Supported platforms and architectures.
- Minimum supported OS policy.
- Submodule initialization.
- Developer prerequisites.
- Managed build and test commands.
- Native MediaInfo build process.
- Local publish commands.
- Artifact layout.
- CI workflow behavior.
- Manual draft-release procedure.
- Unsigned macOS launch warning and approval steps.
- Third-party licensing.
- Known parity limitations inherited from original folder processing.

## 20. Decision Boundaries

Implementation may choose without further confirmation:

- Exact stable dependency versions compatible with .NET 10.
- Internal project and folder layout.
- CMake options and toolchain files.
- Native artifact naming and internal extraction paths.
- CI job decomposition and caching details.
- Test fixture generation approach.
- Minor platform-specific UI adjustments.
- Exact light/dark validation colors, subject to semantic fidelity and contrast.
- Internal abstractions separating domain logic from Avalonia.

Implementation must not independently change:

- Supported platform and architecture matrix.
- Product and namespace naming pattern.
- Simplified-Chinese-only scope.
- Feature-parity boundary.
- Original folder-processing behavior.
- Validation rules or thresholds.
- Self-contained distribution requirement.
- GitHub Actions trigger and release behavior.
- Signing deferral.
- MIT plus third-party notice model.

## 21. Completion Criteria

V1 is complete when all of the following are true:

1. This standalone repository contains the new project and `../mpng` remains unchanged.
2. The application targets .NET 10 and uses one shared Avalonia UI codebase.
3. Windows x64/arm64, Linux x64/arm64, and macOS Universal artifacts build successfully.
4. Each platform receives one self-contained, user-managed application deliverable.
5. MediaInfoLib is pinned as a Git submodule, built by CI, bundled, and loaded without user installation.
6. Original inspection, validation, folder traversal, status, grid, summary, and technical-detail workflows are reproduced.
7. Compact Open Files and Open Folder commands are available alongside drag-and-drop.
8. Simplified Chinese UI terminology is preserved.
9. Light and dark themes follow the OS and keep validation states readable.
10. Characterization tests prove legacy validation rules.
11. Unit, native integration, and applicable UI smoke tests pass.
12. Every push and pull request builds/tests and uploads the complete artifact matrix.
13. The manual workflow accepts a release version, creates the tag after gates pass, and creates a draft GitHub release containing all artifacts.
14. The repository uses MIT and preserves required third-party notices.
15. Documentation covers development, publishing, releases, unsigned macOS launch, and inherited limitations.
16. No V1 non-goal has been introduced without explicit scope approval.

## 22. Known Risks

- Standard GitHub runner availability and cross-compilation support may constrain native ARM execution tests.
- MediaInfoLib and its native dependency graph require correct shared-library build settings for every target.
- .NET single-file publishing may extract native libraries internally on Windows and Linux.
- macOS Universal packaging requires correctly combining compatible managed/native slices even though signing is deferred.
- Avalonia DataGrid behavior is not identical to WPF DataGrid; parity requires interaction testing.
- Original validation colors require theme-aware adjustment to remain readable.
- Strict preservation of folder error behavior intentionally retains known resilience limitations.
- Unsigned macOS and Windows artifacts create user-facing security warnings.

## 23. Deferred Follow-ups

The following require separate approval after V1:

- Windows code signing.
- Apple Developer ID signing and notarization.
- Resilient, cancellable, or concurrent folder scanning.
- Additional validation rules.
- Additional languages.
- Installers or store distribution.
- Automatic updates.
- Mobile or browser clients.
