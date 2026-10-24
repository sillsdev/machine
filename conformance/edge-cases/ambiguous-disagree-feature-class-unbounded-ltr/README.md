# ambiguous-disagree-feature-class-unbounded-ltr: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# ambiguous-disagree-feature-class-unbounded-ltr

Synthetic feature-defined vowel class with an ambiguous minus-alpha rewrite context and an unbounded left-to-right optional environment. This fixture is a negative witness for the still-refused lowerability shape; the authored class is a FeatureNaturalClass, not an explicit SegmentNaturalClass.

Oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`; 16 complete rows, 0 skipped. The oracle accepts only `ia` as `AU|[iy]a`. Evidence: `docs/divergences/evidence/079-partial-explicit-class/refusal-witnesses/`.
FieldWorks/Machine: `HCLoader.cs:2338-2344, 2745-2770`; Machine `XmlLanguageLoader.cs:696-719`.
Ledger: `docs/divergences/079-partial-explicit-class-disagreement.md`.
Upstream PR: none (network closed).
