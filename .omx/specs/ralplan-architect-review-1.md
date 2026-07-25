# Ralplan Architect Review 1

- Verdict: `ITERATE`
- Sequence: Architect review completed before any Critic review.

## Blocking findings

1. A post-V1 label is insufficient without an approved policy owner/revision.
2. The proposed profile has no executable activation/default contract.
3. Collation filename findings do not say how they supersede the legacy
   generic mismatch without duplicate results.
4. Missing, unknown, not-applicable, and violated states are collapsed into
   severity and strings.
5. Current projection turns missing language/default/timestamps into ordinary
   values, preventing truthful validation.
6. Direct static native calls provide no pure projection test seam.
7. Applicability and unpinned MKV/MP4 grammar are underdefined.

## Required revision

Add Phase 0 policy approval, explicit `LegacyV1`/`CollationV1` activation,
supersession and ordering, structured rule evaluation, presence-preserving raw
metadata, a pure snapshot projector, applicability negatives, and mandatory
CI projection evidence.

## Antithesis and synthesis

The strongest alternative is a narrow resolution-only amendment, which may be
more proportionate than a policy framework for one caller. The synthesis is to
preserve the legacy API/stream unchanged while layering a separately approved
Collation evaluator and starting only with deterministic ratified rules.
