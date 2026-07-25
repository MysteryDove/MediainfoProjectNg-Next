# CollationV1 Policy Approval

## Authority

| Field | Value |
| --- | --- |
| Policy revision | `CollationV1@2cb203644dd4a05335fe4551b1086304f9f623a9` |
| Matrix path | `.omx/specs/collation-v1-policy-matrix.json` |
| Matrix SHA-256 | `60adfb54d3295e25dbad62ae8dc840335cee8677ff6536df8f0b3d762fd000c6` |
| Upstream repository | `vcb-s/VCB-S_Collation` |
| Upstream pin (evidence only) | `2cb203644dd4a05335fe4551b1086304f9f623a9` |
| Named local approver | Owen (repository product owner / maintainer) |
| Approved at (UTC) | 2026-07-25T13:30:00Z |
| Status | **APPROVED** for implementation (bugfix Stage 0 re-pin) |

## Semantic changelog (Stage 0 bugfix)

- Added `applicabilityPredicates` with multi-input Matroska/.mkv gates for TRACK/VIDEO/CH.
- Enabled TRACK/VIDEO rules use `RecognizedVcbsMkvFilenameAndContainer`.
- Enabled CH rules use `RecognizedVcbsMkvFilenameAndContainerWithChapters`.
- Added mandatory `filenameGrammarAllowlists` for `vcbs-mkv-release-v1`.
- Added `approvedColorReviewProfiles` (HDR/DVD waiver names).
- `signalSupersession.SIG.FpsReview` → empty (no ScanType supersession).
- Removed empty-string aliases from `sdrDefaults` transfer/primaries (exact SDR membership only).

## Scope approved for this increment

Enabled normative/advisory rows only as listed in the matrix with `"enabled": true`.

Explicitly **disabled**: MKA, mobile MP4, Menu-PGS flood exemption, chapter default-name/timestamp rules.

## Approval statement

I approve the pinned matrix hash above as the sole Phase 0 authority for
`ValidationProfile.CollationV1` hard rules in MediaInfoProjectNg-Next, including
bugfix Stage 0 freezes.

**Approver:** Owen  
**Signature meaning:** local product owner acceptance of the matrix content and hash.
