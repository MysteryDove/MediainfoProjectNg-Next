# Ralplan Consensus Handoff: CollationV1 Bugfix

```yaml
planning_state: complete
plan: .omx/plans/prd-collation-bugfix.md
status: pending approval
required_order: Architect -> Critic (closed loop until Critic APPROVE)
iterations:
  architect: 3
  critic: 3
final_verdicts:
  architect: APPROVE
  critic: APPROVE
chosen_option: B-prime
execution_gate:
  ready_for_stage_0: true
  product_release_blocked_until: all AC1-15 including manual
```

## Decision

Execute **Option B′** from `.omx/plans/prd-collation-bugfix.md`:

1. **Stage 0** — matrix full freeze (applicabilityPredicates, grammar allowlists,
   waivers, SIG.FpsReview=[], empty SDR strip) + dual-write tests + re-approval hash
2. **Stage 1a** — Domain + Raw coupled (applicability, exact SDR, raw tri-state, ParseLong)
3. **Stage 1b** — Core (`IProgress<string>?` only, path de-dupe) / UI (theme, tooltip) / Tests parallel
4. **Stage 2** — managed + native CI + manual Windows/macOS checklist

## Explicit freezes (do not re-open)

- Core load API: `IProgress<string>?` only (no Action)
- TRACK/VIDEO: grammar ∩ Matroska ∩ .mkv; CH + chapters present; FN grammar-only
- Width/Height/BitDepth: fraction/overflow → ParseFailed, never throw
- No SDR Pass-path substring Contains
- Matrix is dual-write authority for allowlists, supersession, waivers, SDR

## Next

User chooses execution path via interactive approval UI (team recommended / ralph / request changes / reject).
No implementation started in the ralplan session.
