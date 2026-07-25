# Verified Grok Research Handover: Collation Policy

- Task ID: `f1e0e402-cd8b-4bbe-8962-7ea889a35699`
- Lane: `general` with research tools, read-only
- Result: completed; Codex inspected `handover.md`, `result.json`, and confirmed
  the target checkout changed only through leader-owned `.omx` artifacts.
- Upstream evidence pin: `vcb-s/VCB-S_Collation`
  `2cb203644dd4a05335fe4551b1086304f9f623a9`

## Accepted evidence

- Production/mux rules:
  `https://github.com/vcb-s/VCB-S_Collation/blob/2cb203644dd4a05335fe4551b1086304f9f623a9/%E5%8E%8B%E5%88%B6%E6%88%90%E5%93%81%E6%96%87%E4%BB%B6%E8%A7%84%E6%A0%BC.md`
- Filename technical tags and explicit `1920x1072 -> 1080p` crop example:
  `https://github.com/vcb-s/VCB-S_Collation/blob/2cb203644dd4a05335fe4551b1086304f9f623a9/%E8%A7%86%E9%A2%91%E5%92%8C%E5%AD%97%E5%B9%95%E6%95%B4%E7%90%86%E8%A7%84%E8%8C%83.md`
- MPNG operator thresholds and review semantics:
  `https://github.com/vcb-s/VCB-S_Collation/blob/2cb203644dd4a05335fe4551b1086304f9f623a9/guidance/mp_ng.md`
- Explicit rules at this pin: video language `und` and default yes; audio
  language plus one main default yes, external MKA audio defaults no; PGS
  language plus default no; mobile MP4 has no embedded subtitles; menu-PGS
  floods are exempt; progressive/CFR expected; range and matrix are minimum
  color annotations; MKV chapter language applies and MP4 is exempt.
- Values outside common SDR/CFR cases require review profiles rather than a
  universal hard error. A short clip's denominator-1000 FPS representation is
  an explicit non-failure case.

## Planning constraints

- Do not invent a generic crop tolerance from one `1920x1072` example.
- Do not infer exact multi-video cardinality or semantic primary-audio identity
  beyond evidence available from track metadata.
- Keep the legacy duration delta threshold at 600 ms in this milestone. The
  upstream tool guidance says over one second while production language is
  qualitative; changing it needs a separate policy decision and regression
  plan.
- The current upstream pin supports PGS default-no; replace the draft's vague
  disagreement with the specific menu-PGS exemption.

## Remaining risks

- Evidence came from a local shallow clone last synchronized 2026-07-23; pin
  the commit for deterministic implementation or re-fetch before choosing a
  newer revision.
- Other crop sizes, main-audio semantics, and unusual multi-video releases are
  not sufficiently specified for unconditional errors.

## External artifacts

- Directory:
  `/var/folders/x6/4p2_1vfj3ts2gt84yq5m5szm0000gn/T/codex-grok-build/20260725-114110-f1e0e402`
- `handover.md` SHA-256:
  `a925f354f40b54e66e0cd80c9c3313b6ed694645be688b95d205d3f1e9f56b44`
- `result.json` SHA-256:
  `a9f17426c56b905978c53e63fd1898bb8fba9933f0bb069e312453591de2ec86`
