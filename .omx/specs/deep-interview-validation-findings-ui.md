# Deep Interview Specification: Validation Findings UI

> Implementation supersession: the consensus PRD's `IssueDisplayItem` contract
> refines "findings" below to include presentation-only `检查提示`. A signal-only
> selection therefore shows the 问题 section; such signals do not become Domain
> findings or acquire a validation severity.

## Metadata

- Profile: Standard
- Rounds: 5
- Final ambiguity: `0.071`
- Threshold: `0.20`
- Context type: brownfield
- Context snapshot:
  `.omx/context/validation-findings-ui-20260725T060954Z.md`
- Transcript:
  `.omx/interviews/validation-findings-ui-20260725T060954Z.md`

## Clarity Breakdown

| Dimension | Clarity |
| --- | ---: |
| Intent | 92% |
| Outcome | 97% |
| Scope | 94% |
| Constraints | 88% |
| Success criteria | 86% |
| Brownfield context | 97% |

## Intent

Let users discover all detected problems in one inspection pass without
memorizing a large legacy palette or repeatedly opening the technical window.
Keep MPNG's dense grid useful for batch triage.

## Desired Outcome

The grid communicates per-file priority and supports category-based batch
filtering. Selection exposes every finding persistently in the existing right
panel. Hover provides a fast preview but is never the only discovery path.

## In Scope

### Category filter bar

- Add a compact, unframed filter strip between the top open-file toolbar and
  the DataGrid.
- Show one toggle button per enabled finding category. Each button contains a
  color swatch, category label, and count of loaded files matching it.
- Buttons are independently selectable and combine with OR.
- With no category selected, show all files.
- Provide a compact all/clear command that deselects every category.
- Counts describe the full loaded set, not only the currently filtered rows,
  so toggling one category does not make other counts jump.
- Categories with count zero are disabled or omitted consistently; exact choice
  is an implementation detail but must not shift layout during filtering.

Initial categories are rule-based, not brush-based:

| Category | Legacy examples |
| --- | --- |
| Container/naming | extension-container mismatch, filename-content mismatch |
| Track | non-zero delay, excessive duration difference, multi-audio/subtitle indicators |
| Frame rate | VFR, NTSC-rate review, rounded-rate review, other-rate review |
| Video/color | unusual color format and approved video metadata findings |
| Chapter | chapter structure and chapter-language findings |

New Collation rules use stable `RuleId` category metadata. Legacy findings use
a compatibility map by known rule/description; raw color alone is not the
category source of truth.

### Row presentation

- Preserve the existing highest-priority finding as the row background signal.
- Preserve selection contrast and existing cell-specific signals.
- Filtering must not change a file's priority color or finding order.
- A problematic row exposes a delayed hover preview containing its finding
  categories and readable descriptions.
- Target tooltip-open delay is 500-700 ms. Exact platform-safe timing may be
  chosen during implementation.
- Keep tooltip content bounded: show category counts and the first several
  descriptions, then `另有 N 条，选中查看全部` when necessary.
- Hover is supplementary; keyboard selection and the right panel provide the
  complete accessible path.

### Right panel

- Reuse the existing hideable/resizable right panel; do not add a left pane or
  tabs.
- When the selected file has findings, render a compact findings section above
  the existing raw MediaInfo summary.
- The findings section shows every finding with category, severity, and full
  description. Structured evidence such as expected/actual may be shown when
  available without making legacy findings appear incomplete.
- When the selected file has no findings, omit the findings section and let raw
  MediaInfo use the full panel.
- When filtering removes the selected row from view, clear selection and its
  right-panel content to avoid showing an invisible file as current.
- Hiding and restoring the right panel preserves filter state and selection
  when the selected row is still visible.

## Out of Scope / Non-goals

- No new left-side findings pane.
- No tabbed findings/MediaInfo panel.
- No per-rule rainbow strip or requirement to memorize all legacy colors.
- No replacement of persistent details with tooltip-only interaction.
- No removal of the technical details window or property tree.
- No validation-rule, severity, priority, or policy changes in this UI work.
- No directory/project-level issue aggregation beyond the currently loaded
  file collection.

## Decision Boundaries

OMX may decide without further confirmation:

- Exact spacing, icon choice, button dimensions, tooltip delay within 500-700
  ms, and theme-adjusted color values.
- Whether zero-count categories are disabled or omitted, provided the decision
  is stable and tested.
- Internal collection-view/filter implementation and view-model structure.
- Exact truncation threshold for hover preview, provided the persistent panel
  always exposes all findings.

Require user confirmation before:

- Removing full-row validation backgrounds.
- Adding a second sidebar or changing the right panel into tabs.
- Changing category taxonomy materially or adding AND filtering.
- Changing rule severity, priority, or legacy/Collation semantics.

## Constraints

- Preserve the dense DataGrid as the dominant surface at the 800px minimum
  window size.
- Follow operating-system theme and retain readable selection/focus contrast.
- Simplified Chinese UI only.
- Controls must remain keyboard-reachable; hover cannot be required.
- Use stable rule/category identity rather than description/color parsing for
  new structured findings.
- Preserve `LegacyV1` presentation behavior except for the explicitly approved
  filter/detail additions.

## Testable Acceptance Criteria

1. With no selected category, the grid shows every loaded file.
2. Selecting one category shows exactly files containing that category.
3. Selecting two categories shows the union, with no duplicates; clearing
   categories restores all files.
4. Category counts remain based on the complete loaded set while filters are
   toggled.
5. The row background still reflects the established highest-priority finding
   and does not change because a filter is active.
6. Hovering a problematic row for the configured delay shows a readable
   problem preview; a clean row does not show an empty problem tooltip.
7. The preview clearly indicates when additional findings are omitted and
   directs the user to select the row for the complete list.
8. Selecting a problematic file displays every finding above raw MediaInfo in
   the right panel; selecting a clean file removes that section.
9. Filtering out the selected row clears selection and prevents stale detail.
10. Hiding/restoring the right panel preserves active category filters.
11. Keyboard users can focus/toggle category buttons, select rows, and inspect
    the complete right-panel findings without hover.
12. At 800px width, the filter bar and right panel do not overlap or obscure
    the DataGrid; horizontal DataGrid scrolling remains usable.
13. Light/dark themes keep filter state, category swatches, selection, row
    priority, and text mutually distinguishable.
14. Existing technical-window findings and property-tree behavior remain
    available and existing Legacy color tests remain green.

## Assumptions Exposed and Resolved

- A separate left pane was assumed to improve discoverability; it was rejected
  after testing against the dense 800px layout.
- A color strip was assumed to be the best multi-finding summary; it became an
  actionable category filter because passive colors would still demand legend
  memorization.
- Hover was considered useful but explicitly limited to preview status; the
  persistent right panel is authoritative for complete findings.

## Brownfield Evidence

- `MainWindow.axaml` already provides the dense grid and hideable right panel.
- `LegacyColorRules` currently chooses the first finding for row background.
- `TechnicalWindow` already demonstrates a complete findings list.
- The approved Collation PRD introduces stable rule IDs/evidence, enabling
  category mapping without description/color coupling for new rules.

## Docs / Terminology Ledger

- `SPEC.md`: grid-dominant, compact, theme-readable validation UI.
- `MainWindow.axaml`: current 800px minimum, 320px right panel, first-finding
  row color, cell-specific signals.
- `TechnicalWindow.axaml`: complete findings list already exists.
- `.omx/plans/prd-collation-media-validation.md`: structured finding identity
  and preserved Legacy priority semantics.
- Canonical term: `finding category`; avoid treating `ColorToken` as category
  identity.

## Handoff

This is a requirements artifact only. Recommended next step is `$ralplan` to
merge this UI contract into the approved Collation plan and review interaction,
view-model, and test architecture before implementation. `$ultragoal` is the
default durable execution follow-up after consensus; `$team` may implement
separate presentation/view-model/test lanes. `$ralph` remains an explicit
single-owner fallback. `$autoresearch-goal` and `$performance-goal` do not fit
this UI feature.
