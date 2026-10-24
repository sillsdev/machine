# partial-class-disagree-bounded-ltr-left: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# partial-class-disagree-bounded-ltr-left

Synthetic explicit vowel class containing only `i`, `a`; it omits `u` and `y` from the four-member vowel table. The ambiguous disagree rule has a bounded LTR rewrite with a left-side environment. hc.dll accepts `ia` as `AU|ia` and rejects the other fifteen words.

Oracle: hc.dll, Machine `18cf242f4b114b0eb9bac304b4b171ca2f499a39`; sixteen complete rows. Evidence: `docs/divergences/evidence/079-partial-explicit-class/`.
FieldWorks: HCLoader.cs:2338-2344, 2745-2770, 2799-2808.
Ledger: docs/divergences/079-partial-explicit-class-disagreement.md.
Upstream PR: none (network closed).
