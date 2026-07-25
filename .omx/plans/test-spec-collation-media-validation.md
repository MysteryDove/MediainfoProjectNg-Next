# Test Specification: Collation Media Validation

## Test Layers

| Layer | Scope | Required evidence |
| --- | --- | --- |
| Domain unit | Policy, filename parser, comparisons, findings | Table-driven fixtures with exact rule code, severity, and expected/actual text. |
| Projection unit/integration | Raw MediaInfo field normalization | Fixture or fake-native coverage for present, missing, and variant raw values. |
| Regression | Legacy `CheckFile` paths | Current tests remain unchanged and green; non-VCB-S matching remains a pass. |
| Native smoke | Real media metadata | Local runs may skip with explicit missing-library evidence; the CI `native-projection` job is required and fails closed. |
| Presentation unit | Category projection and view-model state | Stable identity, full-set counts, OR filtering, selection invalidation, tooltip/detail projection. |
| XAML contract | Dense main-window structure | Fixed filter placement/order, 600 ms tooltip delay, conditional right-panel findings above MediaInfo, keyboard metadata. |
| UI acceptance | Cross-platform interaction and Windows visuals | Mouse/keyboard filter and hover flows plus 800px/default-width, light/dark Windows evidence. |

The projector test seam is a pure immutable raw snapshot/value source. Tests
must not require direct calls to the current static P/Invoke API. CI also runs
at least one real native-fixture lane to validate adapter field names.

## Required Cases

1. Filename parser accepts supported VCB-S MKV form and returns profile,
   resolution, video claim, and ordered audio claims.
2. Filename parser rejects/non-applies unrecognized names without changing the
   legacy non-VCB-S result.
3. Resolution: `1920x1080 -> 1080p`, pinned exception `1920x1072 -> 1080p`,
   `1280x720 -> 720p`, plus adjacent undocumented crop cases that must remain
   unverifiable rather than being accepted by a generic tolerance.
4. A `720p` claim with 1080p media emits one error naming `720p` and actual
   dimensions/bucket.
5. Valid and invalid profile/video/audio group cases, including reordered
   audio groups, unknown codec/profile, empty-video input, and empty-audio
   input.
6. Recognized `.mkv` profiles cover valid and invalid track/default
   combinations, including PGS default-no and an explicit Menu-PGS exemption
   fixture when its matrix row is enabled. Independently: if Phase 0 enables
   MKA, run its exact cases; otherwise assert no MKA rule. If Phase 0 enables
   mobile MP4, run its exact pinned grammar/rule cases; otherwise assert no MP4
   rule.
7. Language cases cover `UND` video, valid audio/subtitle language, missing
   language, and policy-exempt container/profile.
8. Scan/color cases cover progressive, interlaced, missing scan, the short-clip
   denominator-1000 exception, missing range/matrix, SDR defaults, and a
   declared HDR/DVD review profile.
9. Chapter cases cover new language missing/mixed plus old
   one/multiple/final/first Legacy warnings. Assert the streams do not duplicate
   those Legacy warnings. MP4 is `NotApplicable`; default-name,
   duplicate/decreasing timestamp rules are absent from this increment.
10. Policy tests assert explicit non-goals: no validator result is added for
    file path, directory name, title semantics, visual quality, or mutation.
11. The existing 600 ms duration-delta tests remain unchanged; the new profile
    does not silently reinterpret them as a one-second threshold.
12. Parameterless `CheckFile(info)` locks exact Legacy findings, descriptions,
    order, early-return behaviour, and presentation/color tokens.
13. A Collation filename mismatch supersedes the generic legacy mismatch once;
    no duplicate result is emitted and all unrelated legacy findings keep
    their specified order. Structured filename RuleIds map to
    `ColorToken.ErrorViolet`; multiple filename failures follow matrix order.
14. Mixed outcome: one parsed claim violates policy while another metadata
    field is `Unverifiable`; both structured evaluations are preserved.
15. Raw projection distinguishes absent from explicit `No`, `UND`, zero, and
    malformed values for default, language, and numeric fields. Existing
    chapter timestamp parsing is characterized separately but enables no new
    Collation timestamp rule in this increment.
16. Unrelated MKV/MP4 files are `NotApplicable`, even when their track layout
    resembles a release profile.
17. Rule IDs are unique and order is deterministic across repeated runs.
18. Phase 0 matrix contract tests ensure every enabled rule has a pinned URL,
    normative/advisory class, applicability, exception, outcome/severity, and
    supersession entry.
19. Algorithm-A cases compute the locked parameterless Legacy stream first,
    including empty-duration early-return omissions, then merge a separate
    Collation stream according to the supersession and append rules. When no
    generic filename slot exists, filename findings append after the retained
    Legacy stream in filename matrix order.
20. Track/container applicability uses only explicit profile/recognized
    grammar, container, and extension. Path, directory, title, and track-count
    classifier inputs are rejected. MKA/MP4 rows are absent unless Phase 0
    enables dedicated pure predicates.
21. `ValidationFinding` legacy construction remains source-compatible while
    Collation instances expose structured rule/evidence fields through the
    same visible findings list.
22. Every structured visible Collation RuleId maps to exactly one category;
    every known Legacy finding maps through the same shared predicate/constants
    used by `LegacyColorRules.TokenForFinding`, including a dynamic extension-
    mismatch fixture that must match the existing prefix predicate and map to
    `ContainerNaming`. Unknown RuleIds remain visible as non-filterable "未分类", add no sixth
    button, and affect none of the five category counts. Mutating a brush or
    `ColorToken` does not change category membership.
23. Existing FPS, color-space, chapter-language, and multi-subtitle visual
    predicates project fixed `LegacyReviewSignal` IDs without changing the
    Domain `ValidationFinding` list or inventing a policy severity.
24. With zero active categories, visible rows equal the canonical loaded set.
    One selected category returns its exact distinct-file set; two categories
    return the union without duplicates; clearing restores the full set.
25. Category counts are distinct-file counts from the canonical loaded set and
    do not change while filters toggle. Zero-count categories remain present,
    fixed in order, and disabled.
    An unknown-only RuleId or unmapped Legacy-description file appears only
    when no category is active, matches no single/union filter, and contributes
    zero to all five counts. A Track-plus-unknown file matches Track while
    retaining `未分类` in hover/detail.
26. Loading, deletion, `Clear!`, cancellation generation, and duplicate-path
    suppression update the canonical set, visible set, category counts, and
    file-count text consistently under active and inactive filters.
27. When a filter excludes `SelectedFile`, selection is null before selected
    issue/detail properties notify; no stale findings or MediaInfo remain. A
    selected row that still matches remains selected.
28. Row background remains the token of the first ordered visible
    `ValidationFinding`; toggling filters does not reorder findings or alter
    row/cell color tokens.
29. A row with one to four display items produces a complete tooltip preview.
    A row with more produces four ordered entries plus the exact overflow count
    and `另有 N 条，选中查看全部`; a clean row has no tooltip content.
30. XAML sets the row tooltip opening delay to 600 ms and binds only bounded
    row preview content. The five category toggles and "全部" command have
    Chinese accessible names and keyboard activation.
31. Selected problematic rows expose every display item above raw MediaInfo in
    the existing right panel. Findings show category/severity/description and
    structured evidence when present; legacy signals show "检查提示". Clean or
    null selection hides the entire issue section.
32. Hiding/restoring the right panel preserves active filters and a still-visible
    selection. Filtering while hidden still clears an excluded selection and
    stale details.
33. `MainWindowLayoutTests` assert the filter strip is between the open toolbar
    and DataGrid, the right panel orders findings before MediaInfo, controls do
    not create a left pane/tab/per-rule strip, and the grid retains its compact
    row/header/cell contract.
34. Manual Windows acceptance at 800px and default width, light and dark themes,
    records readable selection/focus contrast, non-overlap, bounded tooltip,
    splitter resizing, and hide/restore behavior. macOS/Linux smoke remains
    required for functional filtering and tooltip activation; Windows evidence
    is required before claiming cross-platform visual completion.
35. A fixture where any RuleId from a signal's frozen supersession set is
    present emits the actual finding once, omits the duplicate display signal,
    asserts signal/rule category co-membership, preserves the legacy cell color,
    and contributes one distinct file to that shared category count.
    Non-mapped sibling signals remain after findings in fixed signal-ID order.
36. Localized descriptions and one unbroken value longer than 120 Unicode text
    elements, measured with `StringInfo`, are ellipsized/wrapped inside a hard-
    clipped tooltip no larger than 360x240px with no inner scroll. The full text
    remains present in the selected-file issue list, and Windows acceptance
    checks placement remains inside the current screen work area.
37. A selected file with enough issues/evidence to overflow the 350px minimum
    window height keeps the findings list and raw MediaInfo in separate scroll
    regions; keyboard scrolling can reach the last issue and the end of the raw
    summary without resizing the window.
38. With Extended selection, filtering out the primary selected row clears all
    selected rows before detail notification. If the primary remains, only
    hidden secondary rows are removed. Delete after either branch receives only
    visible selected rows.
39. XAML layout tests assert the filter strip belongs to the left/grid column
    above `FileGrid`, does not span the splitter/right panel, and the 45% finding
    cap is measured inside the right content host below `Clear!`.

## Verification Commands

```bash
dotnet test tests/MediainfoProjectNg.Next.Tests/MediainfoProjectNg.Next.Tests.csproj
dotnet build MediainfoProjectNg.Next.sln
```

Local runs may skip native-fixture selection only with explicit missing-library
evidence. The designated CI native adapter lane is non-skippable and must fail
when its native dependency, fixture, or expected field projection is missing.
Its concrete target is `MediaInfoProjectionIntegrationTests`, run by a
`native-projection` job in `.github/workflows/publish-bundled.yml` on
`linux-x64` after the workflow builds the native library.
