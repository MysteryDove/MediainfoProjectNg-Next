# Deep Interview: Validation Findings UI

## Metadata

- Profile: Standard
- Context: brownfield
- Rounds: 5
- Threshold: `0.20`
- Final ambiguity: `0.071`
- Context snapshot:
  `.omx/context/validation-findings-ui-20260725T060954Z.md`

## Prompt-safe summary

Not needed; the initial prompt was bounded.

## Transcript summary

### Round 1: primary workflow

The user chose a hybrid workflow: rows summarize batch state while a side
surface shows all findings for the selected file.

### Round 2: row summary granularity

The user chose grouping by problem category rather than one segment per rule or
severity-only grouping. Each category carries a count and category-level visual
identity.

### Round 3: panel ownership pressure test

Given the 800px minimum window and existing 320px right summary panel, the user
rejected a second left pane. The existing right panel remains; findings are
inserted above raw MediaInfo when present, without tabs.

### Round 4: row color and filter interaction

The user kept the existing highest-priority full-row background. The grouped
category color surface becomes button-like filters. Hovering a problematic row
shows a delayed problem preview; selecting it shows the complete persistent
list in the right panel.

### Round 5: filter composition

The user chose multi-select OR: a file remains visible when it matches any
selected category. An all/clear command resets the filter.

## Pressure-pass finding

The initial idea could have produced a left findings pane plus the existing
right MediaInfo pane. Rechecking this against the 800px minimum and dense-grid
goal exposed unacceptable grid compression. The design therefore reuses one
right panel and moves batch navigation into compact filters above the grid.

## Decisions and non-goals

- Keep highest-priority row coloring.
- Use category buttons for filtering, not a per-row rainbow strip.
- Keep hover supplementary and persistent details selection-bound.
- No new left pane.
- No tabbed right panel.
- No per-rule color segment.
- No tooltip-only finding access.
- Do not remove the existing technical details window.
