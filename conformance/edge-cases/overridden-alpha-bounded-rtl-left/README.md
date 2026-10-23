# overridden-alpha-bounded-rtl-left: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# overridden-alpha-bounded-rtl-left

Synthetic neutralization of an alpha-governed feature. hc.dll requires ii from iu.
Founding oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`.
Recorded XML and all 16 oracle rows: docs/divergences/evidence/069-overridden-alpha/overridden-alpha-bounded-rtl-left/.
Owner regression: docs/divergences/069-analysis-binds-overridden-alpha.md.
FieldWorks: HCLoader.cs:2033-2067,2338-2344,2745-2770,2799-2808.
Upstream PR: none (network closed).

Confirmed FST proof: docs/divergences/067-ambiguous-disagreement-proposals.md;
owning and templated pipelines, all 16 oracle rows, unchanged 128-path/32-candidate limits.
