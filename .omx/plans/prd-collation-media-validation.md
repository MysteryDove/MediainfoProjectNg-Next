# PRD: Collation Media Validation

## Requirements Summary

Implement a separately approved post-V1 validation profile for files inspected by
MediaInfoProjectNg-Next. The profile must turn VCB-S collation requirements
that MediaInfo can determine into clear, read-only findings. It must preserve
current legacy findings, keep non-media and semantic validation out of scope,
and avoid false failures for known production variants.

### In scope

1. Filename/content matching for recognized VCB-S MKV release names:
   profile, resolution, video encoder, ordered audio encoder groups, and
   only Phase-0-qualified optional MKA/MP4 grammar/profile variants.
2. Per-file track-policy validation: video language/default, audio language
   presence and default cardinality, subtitle language/default, and
   container-purpose/track-count consistency.
3. Video metadata validation: progressive-scan expectation and presence of
   essential color metadata, with non-default HDR/DVD/source cases reported
   for review rather than rejected by a generic rule.
4. Chapter validation adds only missing/mixed language findings, with MP4
   `NotApplicable` under the pinned policy. Existing single/multi/first/end
   chapter warnings remain Legacy-only. Default-name, monotonicity, and
   duplicate rules are out of this increment unless a future plan defines
   explicit supersession and source-backed policy.
5. Golden, synthetic, and integration test coverage proving both findings and
   deliberate non-findings for documented exceptions.
6. A dense findings-discovery UI that preserves the existing ordered
   first-finding row background, adds category-based OR filtering above the
   DataGrid, previews row issues after a bounded hover delay, and shows every
   selected-file issue above raw MediaInfo in the existing right panel.

### Explicit non-goals

- File/directory naming syntax not tied to media claims, path length, magic
  headers, empty files, CUE/audio-image/archive checks, torrents, or tracker
  checks (ATI/integrity tools).
- Episode/title/PV/Menu/NCOP semantic correctness, visual quality, dropped
  frames, actual A/V synchronization, subtitle translation, fonts, and staff
  metadata (human review).
- Any mutation of media or paths.
- Directory-level relationship/anomaly checks. They remain a separately scoped
  project-inspection milestone because they need a tree inventory and waiver
  model.
- A second sidebar, findings/MediaInfo tabs, per-rule rainbow strips,
  tooltip-only discovery, AND filtering, or changes to validation severity,
  priority, applicability, and rule policy.

## Product Decisions

### Phase 0: normative policy gate

Before implementation, create a reviewed `CollationV1` policy matrix pinned to
VCB-S_Collation commit `2cb203644dd4a05335fe4551b1086304f9f623a9` (or one
explicitly reviewed replacement). One named local repository product
owner/maintainer is the release approver; the pinned upstream revision is
evidence only. Record approval and the policy-matrix hash in
`.omx/specs/collation-v1-policy-approval.md` (or a reviewed repository-owned
successor). Each matrix row must include: upstream clause URL,
`Normative` or `Advisory` status, applicability predicate, outcome/severity,
expected evidence, exceptions/waivers, and superseded legacy rule. No
hard-error validator ships for an unapproved or inferred row.

Every track/container applicability predicate must be a pure function of an
explicit profile flag and/or recognized frozen filename grammar, container,
and extension. Path, parent directory, title semantics, and inferred track
count are forbidden classifier inputs. Default hypothesis: MKV track rules
require a recognized Collation filename; MKA/MP4 rows stay disabled unless
Phase 0 freezes a dedicated grammar or explicit profile input with negatives.

The first implementation increment includes only exact grammar/rules frozen in
this matrix. AV1, HDR, mobile MP4, unusual crops, and menu PGS rules are
implemented only if Phase 0 provides exact claims and fixtures; otherwise they
remain documented follow-ups, not conditional acceptance gaps. Chapter
sequence/default-name rules require a separate future plan regardless of Phase
0 evidence because they also need a new supersession/architecture decision.

### Validation profile and outcomes

Preserve parameterless `MediaValidator.CheckFile(info)` as `LegacyV1`, with
exact findings, text, order, early-return behaviour, and color semantics.
Introduce a versioned `ValidationProfile.CollationV1` entry point. Core loading
accepts the profile explicitly; the post-V1 desktop composition root is the
only owner that activates `CollationV1`. Tests and any caller that omit the new
profile remain `LegacyV1`. No runtime auto-detection silently switches modes.

The Collation evaluator produces structured `RuleEvaluation` values with
`RuleId`, `Outcome` (`Pass`, `Violation`, `Unverifiable`, `NotApplicable`),
policy revision, expected, actual, and evidence/provenance. Severity is a
separate presentation mapping: confirmed normative contradiction is `Error`;
confirmed absence of required metadata is `Warning`; adapter/source inability
is `Unverifiable`/`Info`; advisory deviations are `Warning` or `Info` per the
approved matrix. `Pass` and `NotApplicable` are retained for tests/reporting
but do not create visible findings.

Extend the existing `ValidationFinding` visible-list type compatibly with
optional `RuleId`, `Outcome`, `PolicyRevision`, `Expected`, `Actual`, and
evidence fields. Legacy constructor/callers retain their current values. Do not
create a parallel visible result list. Human-readable text is rendered from
structured data and is not the sole test oracle.

### Legacy supersession and order

Normative merge algorithm: first compute `LegacyV1` by calling the unchanged
parameterless `CheckFile(info)`, including its duration early return and every
result omission. Separately evaluate all Collation rules. For a recognized
supported filename claim, remove the generic legacy filename/content mismatch
from the legacy stream if present, then insert ordered field-specific filename
findings at that legacy slot; never duplicate it. If Legacy early-return or a
legacy match yields no generic filename slot, append approved field-specific
filename findings after the retained Legacy stream in filename matrix order.
Empty-duration legacy omissions remain unless an independent approved
Collation row applies.
Unrecognized/non-applicable names retain legacy behaviour and produce no new
filename-policy failure. Retained legacy findings keep their original slots;
field-specific filename evaluations occupy the superseded generic filename
finding's slot; all other visible Collation findings append afterward in Phase
0 matrix order. This preserves first-finding row-color behaviour unless the
superseded filename rule itself was first.

All field-specific filename `Error` rule IDs map to the legacy
`ColorToken.ErrorViolet`, independent of localized description. Multiple
filename failures use the Phase 0 filename-rule order; the first expanded
finding drives row background when the superseded slot is first. Update
`LegacyColorRules` to prefer structured rule identity and retain description
matching as the LegacyV1 fallback.
Phase 0 orders filename `Violation`/`Error` rows before
`Unverifiable`/`Info` siblings so a mixed result retains violet error emphasis.

### Filename matching

Keep the legacy matcher intact for `LegacyV1`. Add a separate Collation parser
whose exact recognized grammars are frozen by Phase 0, then produce field-level
comparisons. For names the supported grammar does not recognize, return
`NotApplicable` rather than failing. For recognized claims:

- Map supported resolutions (`1080p`, `720p`, `576p`, `480p`) from actual
  dimensions. Accept only exact dimensions plus policy-pinned crop cases;
  `1920x1072` is explicitly accepted as `1080p`. Do not invent a generic
  tolerance for undocumented crops.
- Compare recognized profile, video codec, and ordered audio groups. Preserve
  profile/codec capability gaps as `Info` (cannot verify), not a false match.
- Implement AV1/8-bit/HDR/mobile-MP4 claims only when their exact grammar and
  meaning are present in the approved Phase 0 matrix; otherwise exclude them
  from this increment.

### Track and container rules

Apply only when `CollationV1` is explicitly active and the approved policy's
release-purpose predicate matches. Extension alone is insufficient; unrelated
MKV/MP4 files must be `NotApplicable`:

- Video-bearing `.mkv`: at least one video and audio track; primary video
  language `UND` and `Default=yes`; exactly one audio track `Default=yes`.
  Multi-video cardinality remains a review case because upstream does not
  define a universal machine rule for it.
- Audio-only `.mka`: no video; all audio defaults disabled.
- Mobile `.mp4`: one video and one AAC audio track, no embedded subtitles,
  unless a documented policy profile exempts a release.
- PGS subtitle tracks: language present and `Default=no` under the pinned
  policy. Exempt tool-generated Menu-PGS track floods; require an explicit
  profile signal/waiver rather than guessing from a high track count alone.

### Video and chapter rules

- Warn when `ScanType` is non-progressive; VFR and 1 FPS menus remain review
  findings, not corruption errors. Preserve the documented short-clip
  denominator-1000 FPS representation as a non-finding.
- Report absent color range/matrix as missing metadata. Values outside SDR
  defaults are review findings unless a profile declares an exact expected
  HDR/DVD value.
- Promote chapter missing/mixed language to findings for applicable MKV files.
  Exempt MP4. Do not re-emit or supersede existing first/end/single/multi
  chapter warnings in this increment.

### Truthful projection boundary

Do not normalize missing values into `UND`, `No`, or numeric zero before
validation. Introduce an immutable raw MediaInfo snapshot/value-source record
that preserves raw text and presence/parse status. A pure snapshot projector
maps it to validation evidence and existing display models; P/Invoke stays
behind the native adapter. Existing display properties may continue showing
legacy-friendly values, but new rules consume the evidence model.

### Findings discovery and category identity

Add a presentation projection, `IssueDisplayItem`, rather than teaching the UI
to infer issue identity from brushes or localized text. Each display item has a
stable key, one `IssueCategory`, a Chinese label/description, and either a
finding severity or the explicit display kind `LegacyReviewSignal`.

The initial fixed categories are `ContainerNaming`, `Track`, `FrameRate`,
`VideoColor`, and `Chapter`. Structured Collation findings map by stable
`RuleId` through an exhaustive rule-category registry. Existing Legacy findings
reuse the same pure constants and match predicates as
`LegacyColorRules.TokenForFinding`: prefix matching for the dynamic extension
mismatch and exact matching for delay, duration, chapter, filename, and
multi-audio descriptions. Category and color registries must call shared
predicates rather than duplicate strings. Existing colored cell review signals
that are not
`ValidationFinding` values (FPS, color-space, chapter-language, and the
multi-subtitle foreground signal) receive fixed presentation-only signal IDs
derived from the current pure `LegacyColorRules` predicates. They may appear as
`LegacyReviewSignal`/"检查提示", but must not acquire a fabricated policy
severity or alter the Domain validation stream. The registry is exhaustive for
every shipped RuleId. A runtime-unknown future RuleId remains visible in hover
and detail as non-filterable `Uncategorized`/"未分类"; it contributes to none of
the five category counts and does not create a sixth button. This is a safety
fallback, not an extension of the user-visible category taxonomy. A Legacy
description not recognized by the shared compatibility predicates follows the
same non-filterable `Uncategorized` path. Raw `ColorToken` is never an identity
input.

Projection order is deterministic: emit actual `ValidationFinding` display
items in their existing Domain order, then emit remaining legacy review signals
in a fixed signal-ID order. Maintain an explicit
`LegacySignalId -> frozen set<RuleId>` supersession registry. Every edge must
preserve category co-membership: `category(signal) == category(rule)`. Presence
of any mapped actual structured finding suppresses that signal display item;
unmapped sibling signals remain, and existing cell colors remain unchanged.
Category membership and counts use the deduplicated display list.

`MediaFileRowViewModel` owns the immutable ordered `IssueDisplayItem` projection
for its model. Row background continues to use the first item of the ordered
visible `ValidationFinding` stream exactly as today; filtering and category
projection do not reorder findings or choose a new background. In product copy,
"最高优先级" therefore means the established first-finding order, not a new
severity sort.

### Filter and selection state

Keep a canonical full loaded collection and a separate observable visible
collection bound to `FileGrid`. Category buttons are independent toggles and
combine with OR. No active categories shows all loaded rows. Each category
count is the number of distinct files in the full loaded collection that have
at least one matching display item; counts do not change when filters toggle.
Use a fixed-order compact button strip in the left content column, directly
above `FileGrid`; it does not span or reduce the right panel. Place both in a
left-column host while retaining the current splitter/right-panel columns.
Zero-count categories remain present but disabled so filtering cannot shift
the layout. Each button exposes a swatch, Chinese label, count, selected state,
accessible name, and keyboard activation. A compact "全部" command clears every
active category.

Reapplying a filter is one coordinated selection/view-model operation. The view
model computes the new visible identity set and clears an excluded
`SelectedFile` before publishing new detail state. Because `FileGrid` uses
`SelectionMode=Extended`, the view selection boundary then intersects
`FileGrid.SelectedItems` with the visible set before Delete can observe it. If
the primary `SelectedFile` is excluded, clear all selected rows; if it remains,
drop only hidden secondary selections. A small pure selection-reconciliation
helper makes both branches testable without UI timing assumptions. Clear,
removal, and load operations update the canonical collection, counts, filtered collection, and
selection without duplicates or stale detail. Hiding/restoring the existing
right panel changes only panel visibility/width; it does not reset filters or a
still-visible selection.

### Hover preview and right-panel detail

Attach one bounded tooltip to problematic DataGrid rows. Use a 600 ms opening
delay (within the approved 500-700 ms range). The tooltip groups by category,
shows counts and the first four ordered descriptions/signals, and ends with
`另有 N 条，选中查看全部` when more remain. Cap preview descriptions at 120
Unicode text elements measured with `System.Globalization.StringInfo` and add
an ellipsis. Set a 360px maximum width and 240px maximum height, wrap text, hard
clip any residual overflow with no inner tooltip scroll, and clamp placement to
the current screen work area. Clean rows
have no empty tooltip. Tooltip content comes from the row projection and is
supplemental; it never owns selection or complete-detail state.

Replace the right panel's single raw-summary TextBox with an unframed vertical
layout. When `SelectedFile` has display items, show a compact "问题" section
above MediaInfo containing every item in deterministic order. Actual findings
show category, Chinese severity, description, and available expected/actual
evidence. Presentation-only signals are labeled "检查提示". Hide the entire
section for a clean selection, leaving raw MediaInfo to use the available panel
height. For problematic selections, findings and raw MediaInfo use separate
vertical scroll containers inside the right-panel content host below `Clear!`;
cap findings at 45% of that host's available height so both remain reachable at
the 350px minimum window height. The left-column filter strip does not consume
right-panel height. Preserve the technical window and existing
resizable/hideable panel.

## Acceptance Criteria

1. Phase 0 produces an approved, commit-pinned policy matrix; every enabled
   hard rule cites an exact normative clause, applicability predicate,
   exception, severity mapping, and supersession behaviour.
2. Parameterless `CheckFile(info)` returns exactly the legacy findings,
   descriptions, order, early-return and colors for the locked corpus.
3. A recognized VCB-S `720p` claim against a `1920x1080` video under
   `CollationV1` returns a
   field-specific `Error`; a `1920x1072` video against `1080p` does not.
4. Supported profile, video codec, and ordered audio claims yield separate,
   deterministic findings; unsupported metadata yields an `Info` and never a
   successful silent match.
5. A recognized Collation filename mismatch emits field-specific evaluations
   and no duplicate generic legacy mismatch; unrelated MKV/MP4 input is
   `NotApplicable` under the new profile.
   Its first visible successor error retains `ColorToken.ErrorViolet` by
   structured rule identity and filename rule order is deterministic.
6. Recognized `.mkv` and only Phase-0-qualified `.mka`/`.mp4` fixture cases
   enforce the agreed track/default/subtitle invariants without applying them
   to unrelated containers.
7. Tests distinguish absent, explicit `No`, explicit `UND`, valid zero,
   malformed, and adapter-unavailable values without null/empty-track errors.
8. Non-progressive scan and confirmed-absent required range/matrix are warnings;
   source/adapter inability is `Unverifiable`/`Info`;
   documented HDR/DVD and VFR fixture cases do not fail a false SDR/CFR rule.
9. Chapter missing/mixed-language findings are emitted only from complete,
   projected source data for applicable MKV files; MP4 is `NotApplicable`.
10. Rule IDs are unique, rule order is deterministic, and mixed evaluation can
    report one field mismatch while another field is unverifiable.
11. Tests demonstrate the ATI/manual boundaries: no rule is added for paths,
   directory names, title/episode semantics, visual review, or file mutation.
12. `dotnet test tests/MediainfoProjectNg.Next.Tests/MediainfoProjectNg.Next.Tests.csproj`
   passes, and selected native projection tests pass when MediaInfo fixtures
   are available locally. CI must run at least one required native fixture lane
   rather than allowing projection coverage to be optional everywhere.
13. Chapter language findings never duplicate Legacy single/multi/first/end
    warnings, and the normative merge algorithm preserves the exact legacy
    stream for empty-duration inputs before Collation-only evaluation.
14. With no active category, the visible grid equals the full loaded set;
    selecting one category returns exactly its files, selecting two returns the
    distinct union, and "全部" restores every loaded file.
15. Category counts are distinct-file counts over the full loaded set and stay
    unchanged while filters toggle. Zero-count buttons remain in their fixed
    positions and are disabled.
16. Structured findings are categorized by `RuleId`; known Legacy findings and
    legacy cell signals use explicit compatibility keys/predicates. No category
    identity depends on a brush, `ColorToken`, or arbitrary localized parsing,
    and an unknown RuleId or unmapped Legacy description remains visible as
    non-filterable "未分类" without adding a sixth button or changing the five
    category counts. An unknown-only file appears with no active filter, matches
    no active category, and a mixed known/unknown file matches by its known item
    while still showing the unknown item in hover/detail.
17. Filtering never changes finding order, first-finding row background, or
    cell-specific signals. Filtering out the selected row clears selection and
    both findings/MediaInfo detail; a still-visible selection is preserved.
18. A problematic row opens a bounded preview after 600 ms, including overflow
    wording when applicable. A clean row exposes no empty tooltip, and all
    filter controls remain keyboard reachable.
19. Selecting a problematic file shows every projected item above raw MediaInfo;
    a clean selection hides the problem section. Hiding/restoring the right
    panel preserves active filters and any still-visible selection.
20. At the 800px minimum window width, the compact filter strip and right-panel
    detail do not overlap, clip button text, or reduce the DataGrid below its
    existing minimum column. Windows visual acceptance confirms theme,
    selection/focus contrast, tooltip bounds, and resize behavior.
21. Actual findings precede presentation-only signals. An explicit
    signal-to-frozen-RuleId-set supersession table removes a semantically
    duplicate display signal when any mapped finding is present, requires
    category co-membership, and does not remove the existing cell color; counts
    and detail use the deduplicated list.
22. Tooltip content remains within 360x240px for long localized or unbroken
    descriptions. At the 350px minimum height, separate findings and MediaInfo
    scroll regions keep every selected issue and the raw summary reachable.
23. Extended selection is restricted to visible rows after every filter. If the
    primary selection is filtered out, all selection clears; otherwise hidden
    secondary selections are dropped, and Delete cannot remove a hidden row.

## Implementation Steps

1. Complete Phase 0 and land the approved policy matrix/spec amendment before
   validator implementation. Freeze grammar, applicability, outcome/severity,
   exceptions, rule order, and legacy supersession.
2. Define `ValidationProfile.LegacyV1` and `CollationV1`, structured
   `RuleEvaluation`, and a backward-compatible finding/presentation mapping.
   Preserve parameterless legacy entry points exactly.
3. Add a raw immutable MediaInfo snapshot or injectable value-source seam that
   preserves missing/raw/parse state; map it through a pure projector to new
   validation evidence and existing display models.
4. Extend the native adapter to collect scan type, color range/matrix/
   primaries/transfer, and only the approved subtitle/chapter evidence. Add
   pure projector tests plus a required CI native fixture lane.
5. Implement a separate Collation filename parser/evaluator and the explicit
   supersession/order matrix; do not rewrite the legacy Boolean matcher.
6. Add profile-gated track/container, scan/color, subtitle, and chapter-language
   validators. Use declarative policy helpers rather than extension-specific
   conditionals scattered through `CheckFile`.
7. Thread explicit profile selection through `MediaLoadService`; leave omitted
   callers/tests on Legacy and activate `CollationV1` only in the post-V1
   composition root after the policy gate.
8. Add a table-driven unit corpus covering every rule, pinned crop case,
   missing-metadata state, exception, and legacy regression. Add native
   integration fixtures only for fields whose MediaInfo projection is not
   faithfully testable from domain models.
9. Add the rule/category registry and presentation-only compatibility signal
   projector. Extend the row view model with deterministic display items,
   tooltip preview text/items, and category membership without changing the
   underlying Legacy/Collation finding stream or row-background rule.
10. Refactor `MainWindowViewModel` to retain a canonical loaded set plus a
    filtered observable set, fixed category-toggle/count models, OR filtering,
    and primary selection/detail cleanup. Add a pure reconciliation helper at
    the view-selection boundary for Extended `SelectedItems`. Preserve
    duplicate-load, delete, clear, cancellation-generation, and panel-toggle behavior.
11. Update `MainWindow.axaml` with the compact fixed-order filter strip in the
    left/grid column only,
    600 ms row tooltip, and conditional findings section above raw MediaInfo.
    Keep the DataGrid dominant, the existing right-panel splitter, keyboard
    access, selection contrast, and the technical window.
12. Add projection/view-model tests for category identity, distinct counts,
    union filtering, load/remove/clear updates, stale selection cleanup,
    tooltip truncation, and complete detail. Extend XAML contract tests for
    placement, delay, accessibility, stable controls, and panel hierarchy; run
    manual Windows acceptance at minimum/default widths and light/dark themes.
13. Update the post-V1 policy documentation and release notes with the enabled
    profile, findings-discovery behavior, and remaining manual gates.
14. Add `MediaInfoProjectionIntegrationTests` and a non-skippable
    `native-projection` job to `.github/workflows/publish-bundled.yml` for the
    `linux-x64` native build. The job runs the exact integration-test filter and
    fails red when the native library, fixture, or projected field is missing.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Guidance differs from legacy behaviour or other tools | Pin a policy revision, encode exceptions in fixtures, and make disputed rules warnings until resolved. |
| MediaInfo field names/normalization vary by media | Test raw projector values against fixtures and preserve unknown/missing separately. |
| Crop tolerance hides wrong resolution | Use only named buckets and exact documented tolerance cases; add boundary-negative tests. |
| Rule growth creates false-positive fatigue | Profile-gate new checks, prefer `Info`/`Warning` for uncertain cases, and retain field-level evidence. |
| MPNG begins duplicating ATI/manual work | Enforce the explicit non-goals in policy tests and documentation review. |
| Dual evaluators drift | Lock the legacy stream exactly, define a narrow supersession table, and run both profiles over the same golden corpus. |
| Profile applies to arbitrary media | Require explicit activation plus a policy applicability predicate; test unrelated MKV/MP4 negatives. |
| Collation produces false positives after release | Roll back by deactivating `CollationV1` at the composition root; omitted callers remain `LegacyV1` and no data migration exists. |
| UI categories drift from rules or colors become accidental identity | Use an exhaustive RuleId registry, explicit Legacy compatibility keys, presentation-only signal IDs, an unknown fallback, and registry tests; never classify from brushes. |
| Filtering mutates the source list or leaves invisible selection/details | Separate canonical and visible collections and apply filter/selection changes atomically in the view model. |
| Tooltip and filter chrome crowd the 800px dense layout | Keep fixed compact controls, cap tooltip content, lock XAML contracts, and require Windows minimum-width/theme acceptance. |
| Cell-only legacy colors are mislabeled as policy findings | Project them as `LegacyReviewSignal` with "检查提示" and no fabricated Domain severity. |

The existing 600 ms duration-delta warning stays unchanged in this milestone.
The upstream operator guide uses one second and the production spec is
qualitative; changing that threshold requires a separate compatibility and
policy decision.

## Verification Plan

- Targeted Domain tests for parser, policy, validator, and legacy regressions.
- Pure snapshot projection tests for present, missing, malformed, and explicit
  values, plus native adapter integration for all newly requested raw fields.
- Full managed test project and solution build.
- Native fixture smoke test when the local native dependency is available.
- Presentation/view-model tests for stable category mapping, full-set counts,
  OR filtering, tooltip truncation, selection invalidation, and complete
  right-panel detail; XAML contract tests for the filter/tooltip/panel layout.
- Manual Windows desktop smoke at 800px and default size in light/dark themes:
  load multi-category fixtures, exercise mouse and keyboard filters, wait for
  the 600 ms preview, filter out the selection, resize/hide/restore the panel,
  and confirm the structured filename rule retains `ErrorViolet` while every
  issue remains available above raw MediaInfo and in the technical window.

## RALPLAN-DR Summary

### Principles

1. Media facts are validated automatically; semantic and visual judgements are
   left to humans.
2. Preserve V1 parity while versioning new post-V1 policy explicitly.
3. Missing evidence is not a match; it must be visible as unverifiable.
4. Rules are commit-pinned, declarative, profile-gated, and corpus-tested to
   control drift.
5. Validation is read-only and never a disguised media-repair action.

### Decision Drivers

1. Catch high-value release mistakes already visible in MediaInfo.
2. Avoid false positives from legal crop, HDR/DVD, VFR, and release variants.
3. Keep policy ownership clear across MPNG, ATI, and manual review.

### Viable Options

| Option | Pros | Cons |
| --- | --- | --- |
| A. Implement only resolution matching | Lowest risk and effort; fixes the clear legacy defect. | Leaves default/language and metadata checks manual despite available data. |
| B. Add all suggested checks directly to `CheckFile` | Fast visible coverage. | Mixes policy with mechanics, risks unreviewable false positives and V1 drift. |
| C. Add a versioned, profile-gated collation validator (chosen) | Supports phased rules, clear evidence, exceptions, and safe evolution. | Requires policy/corpus work before individual checks. |

Findings-discovery UI options:

| Option | Pros | Cons |
| --- | --- | --- |
| UI-A. Per-rule row color segments | Exposes many hits at once. | Recreates the unscalable palette and requires memorizing colors. |
| UI-B. New left findings pane or right-panel tabs | Persistent structure. | Crowds the 800px grid or hides MediaInfo behind navigation. |
| UI-C. Priority row color + category filters + hover + stacked right detail (chosen) | Supports batch triage and complete inspection while reusing the dense layout. | Requires explicit category projection and synchronized filter/selection state. |

## ADR

### Decision

Choose corrected Option C: a post-V1, versioned, explicitly activated,
profile-gated collation validation
layer, starting with the five first-priority rule groups described above. For
findings discovery, choose UI-C: preserve the established first-finding row
background, add category OR filters and delayed preview, and place complete
selected-file issues above raw MediaInfo in the existing right panel.

### Drivers

MediaInfo already exposes most required facts, but both legacy code and
workflow guidance contain known policy gaps and exceptions.

### Alternatives considered

Option A is retained only as a minimum fallback if policy ownership cannot be
resolved. Option B is rejected because it silently changes the V1 contract and
hard-codes contested rules.

### Why chosen

It fixes the concrete resolution defect while creating a testable boundary for
new rules, uncertain metadata, and future policy revision.

### Consequences

Initial implementation spans the normative policy document, Domain, raw
MediaInfo snapshot/projection, Core activation, tests, fixtures,
and presentation. It must not be represented as strict legacy parity. The UI
adds presentation-only legacy signals but does not promote them to Domain
validation findings or change their policy severity.

### Follow-ups

Directory-level relationship checks require a separate project-inventory
design. Waiver/report interoperability may extend the structured evidence
contract later without making rule identity an afterthought now.

## Initial Planner Changelog

- Elevated the V1-spec conflict to an explicit post-V1 profile decision.
- Limited the release milestone to single-file, MediaInfo-verifiable checks.
- Pinned initial evidence to VCB-S_Collation `2cb2036`, made PGS default-no
  explicit with the Menu-PGS exemption, and removed generic crop tolerance.
- Preserved the legacy 600 ms duration finding pending a separate policy
  decision.
- Architect iteration 1: made Phase 0 policy approval mandatory; defined
  explicit Legacy/Collation activation and supersession; separated outcome
  from severity; required presence-preserving raw projection, structured
  evidence, applicability negatives, and CI native coverage.
- Critic iteration 1: limited new chapter scope to language, locked algorithm A
  for Legacy+Collation merging, preserved filename `ErrorViolet` by RuleId,
  made applicability inputs pure, chose one compatible finding type, named the
  native CI job/test class, and documented rollback.
- Architect iteration 4: removed stale chapter timestamp requirements, defined
  the no-legacy-slot filename append fallback, and made MKA acceptance
  conditional on Phase 0.
- Architect iteration 5: made chapter sequence unconditionally future-plan
  scope and normalized MKA test coverage to Phase-0-qualified only.
- Architect iteration 6: split optional MKA and MP4 enablement/test gates so
  each Phase 0 row can be enabled independently.
- Critic iteration 2 approved. Final polish clarified optional MKA/MP4 scope,
  made chapter sequence explicitly future-plan-only in context, and required
  filename error rows before unverifiable rows for legacy first-finding color.
- UI extension draft: incorporated the completed findings-UI interview. Added
  stable category/display projection, full-set OR filtering, atomic selection
  cleanup, 600 ms bounded hover preview, conditional complete right-panel
  detail, accessibility/layout acceptance, and explicit treatment of legacy
  cell colors as presentation signals rather than new validation policy.
- UI Architect iteration 1: defined the non-filterable runtime-unknown fallback
  without adding a sixth taxonomy button; added finding-first semantic
  deduplication; bounded tooltip text/geometry; and required separately
  scrollable findings/MediaInfo regions at minimum height.
- UI Architect iteration 2 approved the revised fallback, deduplication,
  tooltip geometry, and scroll/reachability contracts with no required edits.
- UI Critic iteration 1: changed supersession to a category-preserving
  one-to-many map, reused exact Legacy matching predicates, specified unknown
  filter behavior, covered Extended selection, defined Unicode truncation and
  tooltip clipping/work-area checks, fixed filter-strip column ownership, and
  reconciled signal-only right-panel wording.
- UI Architect iteration 3 approved the complete Critic revision set with no
  required edits.
- UI Critic iteration 2 approved the final PRD/test/deep-interview contract;
  planning is complete, while product implementation remains Phase-0-gated.

## Evidence Sources

The source-backed policy synthesis is recorded in
`.omx/specs/grok-build/collation-policy-research-handover.md`. Implementation
must cite the pinned upstream commit in its policy fixtures/documentation.

## Available Agent Types

Installed roles available to a follow-up workflow include `explore`,
`analyst`, `planner`, `architect`, `debugger`, `executor`, `team-executor`,
`verifier`, `code-reviewer`, `dependency-expert`, `test-engineer`, `designer`,
`writer`, `git-master`, `code-simplifier`, `researcher`, `critic`,
`scholastic`, and `vision`, plus the installed Prometheus planning roles.

Workspace routing applies: listed implementation/research/review capabilities
use the bounded Grok collaboration lane when a worker is warranted; native
`architect`, `verifier`, `git-master`, and other unlisted authority roles remain
available. Team-runtime `worker` is reserved for an active `$team` session.

## Follow-up Staffing Guidance

### Default durable path: `$ultragoal` with `$team`

Use `$ultragoal` as ledger/goal owner. Goal 1 is sequential: Phase 0 policy
matrix, local approval record, and contract tests. Only after that gate may a
four-lane `$team` implement in parallel:

| Lane | Role shape | Suggested reasoning | Ownership/result |
| --- | --- | --- | --- |
| Domain policy/evaluator | `executor` / team execution lane | medium | Profiles, structured outcomes, supersession/order, validator unit tests. |
| Raw projection/adapter | `executor` plus `test-engineer` | high | Presence-preserving snapshot seam, native adapter fields, projector/native fixtures. |
| Core activation/state | `executor` | medium | Explicit profile activation, canonical/filtered collections, selection invariants. |
| Findings UI/tests/docs | `executor` plus `test-engineer`/`writer` | high | Category projection, filter strip, hover/detail panel, XAML/view-model tests, Windows acceptance checklist. |

After integration, a native `verifier` at high reasoning validates the policy
approval hash, changed-path ownership, legacy corpus, managed build/tests, and
the non-skippable CI native lane. Use `architect` only for a material boundary
change; do not reopen settled design for routine residuals.

### Team launch hints

```text
$ultragoal .omx/plans/prd-collation-media-validation.md
$team 4 "Implement the approved CollationV1 and findings-discovery UI plan after Phase 0 is approved; preserve LegacyV1 and return lane-specific test evidence"
```

Equivalent OMX CLI launch from an attached runtime:

```text
omx team 4 "Implement .omx/plans/prd-collation-media-validation.md after the Phase 0 approval artifact exists"
```

The execution leader must pass the approved policy-matrix hash, enabled-row
list, owned paths, and test-spec path to every lane. No team lane may create or
self-approve the Phase 0 authority record.

### Team verification path

Before Team shutdown, prove: owned-path compliance; no product edits before
Phase 0 approval; exact parameterless LegacyV1 corpus; Collation supersession
and ordering; raw presence/parse cases; full managed tests/build; a green
designated native adapter CI lane; category/filter/selection/tooltip/detail
contracts; and Windows layout/theme acceptance. Team returns command output, fixture/rule
coverage, and changed-file evidence. Ultragoal checkpoints the approval hash,
each integrated lane, the verifier verdict, and any explicit validation gap as
durable completion evidence.

## Goal-Mode Follow-up Suggestions

- `$ultragoal` is the default: it preserves the Phase 0 dependency and durable
  completion checkpoints.
- `$ultragoal` + `$team` is recommended after Phase 0 because Domain,
  projection, and Core/presentation work can proceed in bounded parallel lanes.
- `$autoresearch-goal` is not appropriate; the task has bounded upstream
  evidence rather than an open-ended research deliverable.
- `$performance-goal` is not appropriate; no performance metric is the target.
- `$ralph` is only an explicit fallback for a deliberately selected
  single-owner sequential implementation/verification loop; it is not the
  recommended durable path.
