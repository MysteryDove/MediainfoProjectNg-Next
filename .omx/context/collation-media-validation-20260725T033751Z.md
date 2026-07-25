# Collation Media Validation Context

## Task statement

Create a consensus implementation plan from Codex session
`019f919d-94d9-7572-a81b-dda4ca7d68cf` for MediaInfoProjectNg-Next validation
enhancements derived from the VCB-S collation workflow.

## Desired outcome

Deliver a post-V1, read-only media-validation milestone that catches
machine-verifiable release mistakes while retaining ATI and human review for
filesystem, semantic, and visual/content decisions.

## Known facts and evidence

- `MediaValidator.CheckFile` currently preserves legacy checks and emits one
  generic filename/content mismatch error. It compares profile, video codec,
  and aggregated audio encoders but not the regex-captured resolution.
  `src/MediainfoProjectNg.Next.Domain/Validation/MediaValidator.cs:15-185`
- The legacy matcher likewise captured resolution but did not compare it;
  therefore resolution matching is an intentional post-parity correction.
  `../mpng/MediainfoProjectNg/Utils.cs:101-160`
- `VideoInfo`, `AudioInfo`, and `SubInfo` already carry width/height,
  language, default flag, format, and basic color-space data.
  `src/MediainfoProjectNg.Next.Domain/Models/TrackInfos.cs:35-139`
- The MediaInfo projector reads those fields, but does not project scan type,
  color range/matrix/primaries/transfer, or text-track timing/resolution.
  `src/MediainfoProjectNg.Next.MediaInfo/Projection/MediaInfoProjector.cs:41-120`
- Existing V1 specification forbids new validation rules; this request must
  therefore be a separately versioned post-V1 milestone, not a claim of V1
  parity. `SPEC.md:222-239, 289, 488, 533`
- Prior workflow analysis distinguishes MediaInfo-derived single-file checks
  from ATI tree/integrity checks and manual semantic/visual review.

## Constraints

- Preserve legacy findings and their current severity/messages unless a new
  versioned rule explicitly supersedes them.
- Validation stays read-only and must never rename, remux, or alter media.
- Do not duplicate ATI checks (directory naming, file headers, paths, CUE,
  archives) or infer content/semantic correctness.
- Pin the first policy matrix to VCB-S_Collation commit
  `2cb203644dd4a05335fe4551b1086304f9f623a9`, or explicitly rebase it to a
  newer reviewed commit before implementation.
- Encode known release-profile exceptions as data/test fixtures; do not hide
  them in scattered conditionals.
- A mismatch must identify its field and expected/actual values; insufficient
  metadata must be distinguishable from a confirmed mismatch.
- Preserve `MediaValidator.CheckFile(info)` as the exact `LegacyV1` entry
  point. New rules require explicit `CollationV1` activation by the post-V1
  composition root and may not leak into legacy output.

## Open questions to resolve during implementation design

- Whether implementation should stay pinned to `2cb2036` or intentionally
  rebase to a newer reviewed upstream commit.
- Exact release-profile handling for HDR/color values, mobile MP4, and crop
  cases beyond the documented `1920x1072 -> 1080p` example.
- Which named local repository product owner/maintainer approves the pinned
  `CollationV1` interpretation for release. Upstream is evidence, not an
  approval actor. Hard-error implementation is blocked until approval is
  recorded in `.omx/specs/collation-v1-policy-approval.md` (or its accepted
  repository-owned successor).
- Chapter sequence/default-name checks are deferred unconditionally to a
  future plan because they need both policy evidence and a new supersession/
  architecture decision.

## Likely touchpoints

- `src/MediainfoProjectNg.Next.Domain/Validation/MediaValidator.cs`
- `src/MediainfoProjectNg.Next.Domain/Models/TrackInfos.cs`
- `src/MediainfoProjectNg.Next.MediaInfo/Projection/MediaInfoProjector.cs`
- `tests/MediainfoProjectNg.Next.Tests/Validation/MediaValidatorTests.cs`
- projector/integration tests and representative media fixtures
- selected presentation tests only if the findings view needs an explicit
  rule/detail rendering change
