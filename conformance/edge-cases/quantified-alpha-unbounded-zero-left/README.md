# quantified-alpha-unbounded-zero-left: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# quantified-alpha-unbounded-zero-left

Synthetic alpha agreement with zero allowed repetitions at the word edge.
The zero-length environment permits a to e; mixed repeated features forbid it.
Oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`; all 16 rows
completed without caps or skips. FieldWorks: HCLoader.cs:2338-2344,2745-2770.
Divergence: docs/divergences/058-repeated-environment-alpha-agreement.md.
Upstream PR: none (Rust-only bug; network closed).
