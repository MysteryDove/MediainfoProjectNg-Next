# PRD / Plan: CollationV1 Bugfix (Post-Review)

```yaml
status: pending approval
planning_mode: ralplan-consensus
interactive: true
execution_gate: user_execution_approval_required
architect_r1: ITERATE
critic_r1: ITERATE
architect_r2: APPROVE
critic_r2: ITERATE
architect_r3: APPROVE
critic_r3: APPROVE
consensus: complete
synthesis: Option B-prime fully frozen; ready for user execution approval
parent_prd: .omx/plans/prd-collation-media-validation.md
parent_test_spec: .omx/plans/test-spec-collation-media-validation.md
```

## Requirements Summary

Close consolidated **REQUEST CHANGES** findings so CollationV1 is release-ready.
**Bugfix only** — no new rule families, no MKA/MP4 enablement, no directory
collation, no Algorithm A redesign.

### In scope

1. Policy honesty — exact SDR; matrix-pinned waivers; no substring Pass.
2. Applicability — multi-input predicates encoded **in the matrix**, not plan-only.
3. Raw evidence — null≠empty; ParseFailed never throws on Collation numerics.
4. UI thread — **`IProgress<string>`** marshaled to caller context only.
5. Theme-aware colors + theme-switch refresh.
6. Native CI — audio/text/chapter fields.
7. Matrix authority — dual-write contract tests for all inventory rows.
8. Presentation — FPS supersession; app-level tooltip; clean rows; path de-dupe.
9. Regression — Legacy ordered corpus; Collation VM; filter/selection.
10. Manual acceptance recorded.

### Explicit non-goals

- MKA / mobile MP4 / Menu-PGS enablement.
- Chapter timestamp/default-name rules.
- Directory relationship checks.
- Changing Legacy 600 ms duration threshold.
- Algorithm A resolution-first violet redesign.
- Broad Avalonia redesign beyond colors / tooltip / thread safety.

## RALPLAN-DR Summary

### Principles

1. **Pinned matrix is the only hard authority** — SDR, waivers, supersession,
   applicability predicates, grammar allowlists; C# is projection under tests.
2. **Missing evidence is never a silent Pass.**
3. **Applicability is pure multi-input** (matrix-encoded).
4. **UI contracts are structural** (IProgress, theme rebind, popup tooltip, path).
5. **Proof before done.**

### Decision Drivers

1. False Pass / wrong applicability.
2. Crash-class load threading + dark-theme contrast.
3. Fail-closed CI/tests against drift.

### Viable Options

| Option | Status |
| --- | --- |
| **A. Serial waterfall** | Fallback if Stage 0 freezes thrash (>1 Raw API revision or Stage 0 not mergeable in one PR) |
| **B′. Stage 0 full freeze → Domain+Raw coupled → Core/UI/Tests parallel** | **Chosen** |
| **C. Minimal hotfix** | **Invalidated** — leaves REQUEST CHANGES |

## Frozen Stage 0 contracts (normative)

### Progress marshaling — **locked (no OR-pick)**

- **`MediaLoadService.LoadAsync` / `LoadFileAsync` take `IProgress<string>?` only.**
  Remove `Action<string>?` from Core load APIs. Do not keep Action as an
  alternate Core signature.
- UI constructs `Progress<string>` (or another `IProgress<string>`
  implementation) **on the UI thread**, capturing `SynchronizationContext`
  at construction, and passes that into Core. That UI construction is not a
  second Core API shape.
- Pure I/O / `Task.Run` may use `ConfigureAwait(false)`.
- **Forbidden:** invoking progress/`StatusString` updates on a thread-pool
  continuation after `ConfigureAwait(false)`.

### Applicability — matrix-encoded

Stage 0 **must** add a matrix section, e.g. `applicabilityPredicates`:

```json
"applicabilityPredicates": {
  "RecognizedVcbsMkvFilename": {
    "grammarId": "vcbs-mkv-release-v1",
    "requireMatroska": false,
    "requireExtension": ".mkv"
  },
  "RecognizedVcbsMkvFilenameAndContainer": {
    "grammarId": "vcbs-mkv-release-v1",
    "requireMatroska": true,
    "containerFormatEquals": ["Matroska"],
    "containerFormatComparer": "OrdinalIgnoreCase",
    "requireExtension": ".mkv"
  },
  "RecognizedVcbsMkvFilenameAndContainerWithChapters": {
    "grammarId": "vcbs-mkv-release-v1",
    "requireMatroska": true,
    "containerFormatEquals": ["Matroska"],
    "containerFormatComparer": "OrdinalIgnoreCase",
    "requireExtension": ".mkv",
    "requireChaptersPresent": true
  }
}
```

Update each enabled rule's `applicability` string to one of the above ids:

| Group | Predicate id |
| --- | --- |
| **FN.\*** | `RecognizedVcbsMkvFilename` (grammar + `.mkv` in grammar; **no** Matroska container require) |
| **TRACK.\* / VIDEO.\*** | `RecognizedVcbsMkvFilenameAndContainer` |
| **CH.\*** | `RecognizedVcbsMkvFilenameAndContainerWithChapters` |

**Chapters present (exact):**  
`requireChaptersPresent` is true iff `(ChapterCount > 0 || Chapters.Count > 0)`.  
When `ChapterCount > 0` but `Chapters` empty → rules still **applicable** and
outcomes remain **Unverifiable** (not NA), matching current intent.

Domain: pure `CollationApplicability.IsApplicable(ruleApplicabilityId, info)`
implemented from matrix-projected constants; contract test equality with matrix.

### Grammar allowlists — **mandatory in matrix**

Stage 0 adds e.g.:

```json
"filenameGrammarAllowlists": {
  "vcbs-mkv-release-v1": {
    "profiles": ["", "Ma10p", "Ma444-10p", "Hi444pp", "Hi10p"],
    "videoEncoders": ["x264", "x265"],
    "resolutions": ["1080p", "720p", "576p", "480p"]
  }
}
```

`CollationFilenameParser` sets **must** equal these; dual-write tests mandatory.
Remove any plan language that treat allowlists as optional.

### Raw presence

| Input | RawField |
| --- | --- |
| Null pointer | Absent |
| `""` | PresentEmpty |
| Whitespace-only | Trim → PresentEmpty |
| Other text | Present, raw preserved |

### Numeric parse (Collation evidence fields) — **locked with AC5**

For **Width, Height, BitDepth** (and any other numeric Collation evidence):

- Non-parseable → `ParseFailed`, parsed null, **never throw**.
- Overflow / out-of-range for `long` → `ParseFailed`, **never throw**.
- **Nonzero fractional part** → `ParseFailed` (no silent truncate on Collation path).

Non-Collation display-only numerics (e.g. legacy duration filled after projection
in native adapter) may keep existing behavior; named list only:
`Duration`, `Delay`, `Bitrate` display fields on VideoInfo/AudioInfo — not
Collation evidence. Document that list in code comment; no open “prefer”.

### Exact SDR

Set membership only against matrix `sdrDefaults` with OrdinalIgnoreCase.
**Empty string `""` must not appear in runtime SDR Pass sets** (strip on load
from matrix if present in JSON for documentation). Banned Pass-path
`Contains("709"|"601"|"Limited"|"Full")`.

### Waivers

`approvedColorReviewProfiles` array in matrix; runtime set equals matrix.

### Signal supersession

`SIG.FpsReview` → `[]`. Matrix authority; registry is mirror only.

### Path equality

Windows: `OrdinalIgnoreCase`. Non-Windows: `Ordinal`.

### Dual-write inventory

| Surface | Source | Projection | Test |
| --- | --- | --- | --- |
| Matrix hash | JSON file | `ExpectedMatrixSha256` | PolicyMatrixTests |
| Enabled rules | `rules[].enabled` | EnabledOrder | PolicyMatrixTests |
| Applicability predicates | `applicabilityPredicates` + `rules[].applicability` | `CollationApplicability` | PolicyMatrixTests + evaluator NA fixtures |
| SDR sets | `sdrDefaults` (no empty aliases) | `Sdr*` | PolicyMatrixTests |
| Waivers | `approvedColorReviewProfiles` | approved set | PolicyMatrixTests |
| Resolution buckets | `resolutionBuckets` | buckets | PolicyMatrixTests |
| Grammar allowlists | `filenameGrammarAllowlists` | parser sets | PolicyMatrixTests |
| Supersession | `signalSupersession` | IssueCategoryRegistry | PolicyMatrixTests or IssueDisplayProjectorTests |

## Acceptance Criteria

1. **SDR exactness:** No Pass-path substring bypass. Tests: `Unlimited`,
   `foo709bar`, and values not in exact sets → ColorReview does not Pass as SDR.
2. **Waiver pin:** Runtime set ≡ matrix; hash + approval updated on change.
3. **Applicability:** Matrix predicates as Stage 0. Fixture grammar-valid name +
   `ContainerFormat=MPEG-4` → TRACK/VIDEO/CH all NA; FN still evaluates when
   grammar matched (not forced NA solely by wrong container).
4. **Raw tri-state:** Absent vs PresentEmpty proven unit + native/integration.
5. **Numeric:** Width/Height/BitDepth overflow and nonzero fraction → ParseFailed;
   load never throws from these parses.
6. **Thread:** `IProgress<string>` only on Core load APIs. Test proves
   `IProgress.Report` runs on the captured/UI context, not the thread-pool
   worker after `Task.Run` / `ConfigureAwait(false)`.
7. **Theme:** All ColorTokens via theme resources (`Val.*` + dark contrast).
   Theme switch refreshes visible validation colors (DynamicResource preferred;
   or rebuild rows on `ActualThemeVariantChanged`).
8. **Native CI:** Fixture: video + audio + ≥1 text/PGS + chapter/menu with
   language. Assert audio/text language+default+format and chapter language.
   Fail-closed under `MPNG_NATIVE_PROJECTION_REQUIRED=1`.
9. **Matrix contracts:** Dual-write inventory all green; hash matches approval.
10. **FPS supersession:** ScanType finding does not suppress `SIG.FpsReview`.
11. **Path de-dupe:** Platform comparer; Windows case variants load once.
12. **Tooltip:** App/window-level ToolTip style 360×240; bind only when
    `HasTooltip`/non-null; clean rows no empty popup.
13. **Regression:**
    - Extend `MediaValidatorTests` (or dedicated `LegacyCorpusTests`) with
      **ordered** level+description sequences for locked fixtures.
    - `MainWindowViewModelTests` use `MediaLoadService(reader, CollationV1)`.
    - Cover filter OR, stable counts, clear, delete, cancel generation,
      Extended selection reconcile.
14. **Manual:** `docs/collation-v1-findings-discovery.md` checklist executed with notes.
15. **Build/test:** Release build + managed tests green; CI native-projection green.

## Implementation Steps

### Stage 0 — Full freeze (serial, one PR preferred)

1. Matrix edits (all required):
   - `applicabilityPredicates` + update each enabled `rules[].applicability`
   - `filenameGrammarAllowlists`
   - `approvedColorReviewProfiles`
   - `signalSupersession.SIG.FpsReview`: `[]`
   - Ensure runtime SDR sets exclude empty string aliases
2. Semantic changelog + new SHA-256 in approval md + `ExpectedMatrixSha256`
3. Land C# projections + dual-write tests (may same PR)
4. Dual-write tests green. (Core `IProgress` signature change may land in
   Stage 1b Core but is **required** before release; no Action residual.)

**Gate:** dual-write tests green; approval record updated; no product rule
behavior PR merges before this hash lands. Stage 0 green ≠ AC3 behavioral green
(AC3 exits Stage 1a).

### Stage 1a — Domain + Raw (coupled)

1. Native: `IntPtr.Zero` → null → Absent; empty string → PresentEmpty (stop
   collapsing in `PtrToString`/`GetRaw`); ParseLong AC5 for W/H/BitDepth
2. Domain applicability helper; TRACK/VIDEO/CH NA on wrong container
3. Exact SDR ColorReview; waiver from matrix; supersession already mirrored

### Stage 1b — Parallel

| Lane | Work |
| --- | --- |
| Core | Finish IProgress marshaling; platform path comparer |
| UI | Val.* + dark; rebind; app-level tooltip; HasTooltip; registry mirror only |
| Tests/docs | Corpus; Collation VM; filter matrix; expand CI fixture; checklist |

**Suggested CI fixture expansion** (ffmpeg-level intent): generate mkv with
video+audio, add subtitle stream (or burn-in separate PGS if toolchain allows),
and chapters with language metadata; assert projected fields. If PGS cannot be
synthesized cheaply, assert **Text** track Format/Language/Default projection
plus a documented PGS synthetic unit path, but **chapter language** and
**audio default/language** are non-negotiable in the native lane.

### Stage 2

Full Release test; native CI; manual checklist; verifier re-check ACs.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Stage 0 false-complete | Gate = dual-write tests + approval hash + predicate map present |
| Alias noise from exact SDR | Only via matrix re-approval |
| Theme converter-only | AC7 rebind mandatory |
| IProgress forgotten in Core | AC6 test fails closed |
| Path comparer on Linux | OS-gated |
| PGS hard to ffmpeg | Text track + unit PGS; chapter+audio native required |
| Freeze thrash | Fallback to Option A |

## Verification Plan

```bash
dotnet build MediainfoProjectNg.Next.sln -c Release
dotnet test tests/MediainfoProjectNg.Next.Tests/MediainfoProjectNg.Next.Tests.csproj -c Release
MPNG_NATIVE_PROJECTION_REQUIRED=1 dotnet test tests/MediainfoProjectNg.Next.Tests/MediainfoProjectNg.Next.Tests.csproj -c Release --filter FullyQualifiedName~MediaInfoProjectionIntegrationTests
```

Named test themes: Unlimited-not-SDR; substring-709-not-SDR; MPEG-4 container NA;
empty vs absent; ParseLong overflow/fraction; IProgress thread affinity;
Windows path case; FPS+ScanType both visible; matrix dual-write; HasTooltip;
all ColorToken resource keys; unapproved DeclaredColorReviewProfile does not
suppress.

## ADR

### Decision

**Option B′** with fully locked Stage 0 (matrix-encoded applicability, mandatory
grammar allowlists, IProgress-only progress, AC5-aligned ParseLong). Stage 1a
Domain+Raw coupled; Stage 1b Core/UI/Tests parallel; AC14 manual is release gate.

### Drivers

Policy false Pass; wrong applicability; UI thread; theme; CI proof.

### Alternatives considered

A fallback; C invalidated; first-draft B rejected (open AC3 / thin Stage 0).

### Why chosen

Single-writer freezes then safe parallel; preserves Phase 0 authority.

### Consequences

Matrix re-approval; stricter ColorReview; richer fixtures; theme maintenance.

### Follow-ups

Source-gen matrix; optional DeclaredColorReviewProfile UI; separate PRD for
row-color emphasis.

## Suggested execution

```text
$team 4 "Execute .omx/plans/prd-collation-bugfix.md after Stage 0 full freeze; Stage 1a Domain+Raw; then Core/UI/Tests; preserve LegacyV1"
```

## Planner changelog

- Draft Option B → Architect ITERATE → B′ freezes.
- Critic r1 ITERATE → lock IProgress; matrix-encoded applicability; mandatory
  grammar allowlists; ParseLong ≡ AC5; CH predicate exact; Stage 0 concrete
  matrix edit list; empty SDR aliases stripped.
- Architect r2 APPROVE.
- Critic r2 ITERATE → remove residual Action/`or equivalent` from Progress + AC6;
  Core APIs `IProgress<string>?` only; native null note; Stage 0 ≠ AC3 note.
