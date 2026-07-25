# Validation Findings UI Context

## Task statement

Clarify a post-V1 UI design for displaying multiple simultaneous validation
findings without forcing a single row color to carry every meaning.

## Desired outcome

Make one inspection pass sufficient to discover all detected problems across
loaded files, while preserving the compact, dense MPNG workflow and avoiding a
palette with too many unrelated semantic colors.

## Stated solution ideas

- Add a hideable left-side area listing matched color-bearing rules.
- Add a narrow multi-color strip to each file row.
- Show finding text in a delayed pointer-hover tooltip.
- Collapse many legacy colors into broader categories such as frame-rate,
  container, and track problems.
- Reuse or extend the right-side/technical-detail findings presentation.

## Probable intent hypothesis

The user wants fast batch triage: identify every problematic file and every
problem without opening the technical window repeatedly, while retaining
enough visual priority to scan a dense table quickly.

## Known facts and evidence

- The main grid is the dominant surface and its row background is derived from
  only the first validation finding.
  `src/MediainfoProjectNg.Next/Views/MainWindow.axaml:82-144`
  `src/MediainfoProjectNg.Next.Core/Presentation/LegacyColorRules.cs:22-31`
- The grid also has cell-specific color signals for frame rate, color format,
  and chapter language, so current color semantics already mix row- and
  cell-level encodings.
  `src/MediainfoProjectNg.Next/Views/MainWindow.axaml:151-218`
- The current palette has thirteen non-neutral `ColorToken` values, which is
  too large to expect users to recall as an unlabelled categorical legend.
  `src/MediainfoProjectNg.Next.Domain/Models/ColorToken.cs`
  `src/MediainfoProjectNg.Next/Converters/ColorTokenToBrushConverter.cs:28-43`
- The hideable 320px right panel currently shows raw MediaInfo summary, not
  findings. `src/MediainfoProjectNg.Next/Views/MainWindow.axaml:223-252`
- Double-click opens `TechnicalWindow`, whose top grid already lists every
  finding with level and description.
  `src/MediainfoProjectNg.Next/Views/TechnicalWindow.axaml:18-41`
- The approved validation plan adds stable rule IDs and structured outcomes,
  providing a better grouping/filtering key than description strings.
  `.omx/plans/prd-collation-media-validation.md:79-94`
- Product UI guidance requires a compact, restrained, information-dense tool;
  the metadata grid remains dominant and theme/selection/validation states
  must remain distinguishable. `SPEC.md:241-267`

## Constraints

- Requirements discussion only; no product implementation in deep-interview.
- Preserve dense grid scanning and cross-platform Avalonia behavior.
- Do not make hover the only way to discover a problem; keyboard and touchpad
  users need a persistent path.
- Preserve LegacyV1 observable presentation unless an explicit post-V1 UI
  decision supersedes it.
- Avoid using raw colors as the only semantic label; future rules need stable
  category/rule identities and readable text.

## Unknowns/open questions

- Primary workflow: batch-level overview across all files, selected-file
  inspection, or both with different information density.
- Whether a side panel lists findings for the current selection or aggregates
  all files and supports navigation/filtering.
- Whether legacy colors remain per-rule, collapse to category/severity, or are
  replaced on rows by a smaller visual vocabulary.
- Required interaction for multi-selection, no selection, hidden panel,
  keyboard navigation, and long finding lists.
- Tooltip content and timing, and whether it is supplementary only.

## Interview decisions

- The grid is the batch overview; the existing right panel is the persistent
  selected-file detail surface.
- Preserve the highest-priority full-row validation background.
- Replace the passive category-strip idea with compact colored category filter
  buttons above the grid.
- Category filters are independently toggleable and combine with OR; an
  explicit all/clear command resets them.
- On a problematic selected file, insert a complete findings section above the
  existing raw MediaInfo summary. Hide the section when there are no findings.
- Row hover is supplementary and delayed; it previews the row's problems but
  never replaces the persistent right-panel list.
- Do not add a left pane, tabs, per-rule color segments, or tooltip-only access.

## Decision-boundary unknowns

- Which surface is authoritative for complete findings.
- How much post-V1 departure from legacy row colors is acceptable.
- Whether the first iteration may add a new panel, or should reuse the existing
  right panel/technical window and add only a compact row indicator.

## Likely touchpoints

- `src/MediainfoProjectNg.Next/Views/MainWindow.axaml`
- `src/MediainfoProjectNg.Next/Views/MainWindow.axaml.cs`
- `src/MediainfoProjectNg.Next/ViewModels/MainWindowViewModel.cs`
- `src/MediainfoProjectNg.Next/ViewModels/MediaFileRowViewModel.cs`
- `src/MediainfoProjectNg.Next.Core/Presentation/LegacyColorRules.cs`
- `src/MediainfoProjectNg.Next/Views/TechnicalWindow.axaml`
- presentation/layout tests

## Relevant sources inspected

- `README.md`
- `SPEC.md`
- `.omx/context/collation-media-validation-20260725T033751Z.md`
- `.omx/plans/prd-collation-media-validation.md`
- `.omx/specs/ralplan-consensus-handoff.md`
- current MainWindow, TechnicalWindow, color rules, view models, and tests

## Terminology conflicts

- "左边区域" may mean selected-file details or an all-files issue navigator;
  these are different products and must not be conflated.
- "有颜色规则" should use stable rule/category identity, not only whether a
  legacy brush happens to exist.
- "一次检查知道所有问题" may mean all problems for one selected file or all
  problems in the loaded batch; this is the first decision to clarify.

## Prompt-safe initial-context summary status

`not_needed`
