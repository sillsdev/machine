# ambiguous-disagree-repeated-minus-partial-focus: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# ambiguous-disagree-repeated-minus-partial-focus

Synthetic repeated minus alpha on a complete class and an agreeing focus on
a partial class. hc.dll requires ii from iu.
Oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`; 16 complete rows.
Analysis binding: docs/divergences/069-analysis-binds-overridden-alpha.md.
Confirmed FST proposal: docs/divergences/067-ambiguous-disagreement-proposals.md.
FieldWorks: HCLoader.cs:2338-2344,2745-2770,2799-2808.
Upstream PR: none (network closed).
