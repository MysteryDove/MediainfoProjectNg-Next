# CollationV1 and Findings Discovery

## Policy

- Profile: `ValidationProfile.CollationV1`
- Authority: `.omx/specs/collation-v1-policy-matrix.json`
- Approval: `.omx/specs/collation-v1-policy-approval.md`
- Upstream evidence pin: `vcb-s/VCB-S_Collation@2cb203644dd4a05335fe4551b1086304f9f623a9`

Parameterless `MediaValidator.CheckFile(info)` remains **LegacyV1** exactly.
The post-V1 desktop composition root activates `CollationV1` via
`MediaLoadService(reader, ValidationProfile.CollationV1)`.
Omitted callers and unit tests that do not pass a profile stay on LegacyV1.

## Enabled rule groups (Phase 0)

| Group | Rules |
| --- | --- |
| Filename | Resolution, Profile, VideoEncoder, AudioEncoders |
| Track (recognized VCB-S MKV) | Video/audio presence, video UND+default, audio language + one default, PGS language + default-no |
| Video | ScanType progressive, colour range/matrix presence, non-SDR advisory review |
| Chapter | Missing / mixed language (MKV only) |

Disabled in this increment: MKA, mobile MP4, Menu-PGS flood exemption profile, chapter default-name/timestamps.

## Findings discovery UI

- Five fixed category OR filters above the DataGrid (left column only): 容器命名, 轨道, 帧率, 视频色彩, 章节
- Counts are distinct-file counts over the full loaded set
- 600 ms row tooltip (bounded 360×240, 120 Unicode text elements, overflow wording)
- Selected-file 问题 section above raw MediaInfo with separate scroll regions
- Unknown RuleIds remain visible as 未分类 without a sixth filter button

## Bugfix Stage 0–1 notes (2026-07-25)

- Multi-input applicability: TRACK/VIDEO/CH require Matroska + `.mkv` (not filename alone)
- Exact SDR membership (no substring Pass); HDR/DVD waivers pinned in matrix
- Native raw: null ptr → Absent, empty → PresentEmpty; `IProgress<string>` load progress
- `SIG.FpsReview` no longer superseded by `VIDEO.ScanType`
- Theme `Val.*` dark overrides; window-level tooltip bounds

## Manual gates remaining

- Windows acceptance at 800px and default width, light/dark themes (see checklist below) — **not yet executed**
- macOS/Linux interaction smoke for filter/tooltip (functional) — **not yet executed**
- Local native projection: skipped via `Xunit.Sdk.SkipException` with explicit message when library/fixture missing; CI `native-projection` fails closed
- Directory-level relationship checks are a separate future milestone
- Chapter/PGS native CI fixture expansion still desirable beyond video+audio

## Windows acceptance checklist

- [ ] 800px minimum width: filter strip and right panel do not overlap; button text readable
- [ ] Default width: DataGrid remains dominant; splitter resizes cleanly
- [ ] Light theme: selection/focus contrast readable
- [ ] Dark theme: selection/focus contrast readable
- [ ] Hover 600 ms preview bounded; clean rows have no empty tooltip
- [ ] Hide/restore right panel preserves filters and still-visible selection
- [ ] Filter out selected row clears findings + MediaInfo detail
- [ ] Extended multi-select: Delete only removes visible selected rows
- [ ] Structured filename error retains violet row background
