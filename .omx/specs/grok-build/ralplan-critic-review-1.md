# Verified Ralplan Critic Review 1

- Task ID: `a577d7f7-ec4f-4b92-88a8-fd676d5dca3a`
- Verdict: `ITERATE`
- Sequence: started only after Architect review 3 returned `APPROVE`.
- Lane: bounded Grok `critic`, read-only; Codex inspected its handover/result
  and confirmed it did not change the checkout.

## Required changes accepted by Planner

1. Limit new chapter scope to language missing/mixed; do not duplicate legacy
   first/end/single/multi findings or invent a default-name rule.
2. Preserve the legacy filename mismatch `ErrorViolet` token for structured
   successor rule IDs, with deterministic filename-field order.
3. Specify merge algorithm A: compute the legacy stream unchanged, compute
   Collation separately, remove only a superseded generic filename finding,
   then merge in defined slots/order.
4. Require pure Phase 0 applicability predicates; default MKV to recognized
   Collation grammar and disable MKA/MP4 unless a dedicated row is frozen.
5. Extend existing `ValidationFinding` with optional structured fields rather
   than introducing a parallel visible result list.
6. Name the mandatory CI native projection test/job and explicit rollback.

## External artifacts

- Directory:
  `/var/folders/x6/4p2_1vfj3ts2gt84yq5m5szm0000gn/T/codex-grok-build/20260725-135301-a577d7f7`
- `handover.md` SHA-256:
  `da588cb1cfd2ee4d4f445107625cf8dee2f88a6111fe2eabdd41d433d5836eee`
- `result.json` SHA-256:
  `668e141dfed8e7081a20fcfdb27ade2e12593d6473dd09c6c1f51e6ad87adceb`
