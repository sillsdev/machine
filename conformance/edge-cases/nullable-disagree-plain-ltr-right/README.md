# nullable-disagree-plain-ltr-right: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# nullable-disagree-plain-ltr-right

Synthetic back/round disagreement over a four-member class. The unchanged
feature at each position distinguishes the required surface ia from other
two-segment words; a nullable environment at the edge must permit the rewrite.
Oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`; 16 complete rows.
Divergence: docs/divergences/065-nullable-rewrite-environment.md.
FieldWorks: HCLoader.cs:2338-2344,2745-2770.
Upstream PR: none (Rust-only defect, network closed).

FST proposal lowering is recorded in entry 067. The owning and templated pipelines
check all sixteen oracle rows under unchanged apply limits, with measured HC confirmation.
