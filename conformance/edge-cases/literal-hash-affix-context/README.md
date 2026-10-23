# literal-hash-affix-context: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# Staged conformance case: literal hash boundary in affix rewrite contexts

## Why this fixture exists

This reproduces the context authored in Motif's `PrefixRootRewriteTraceTests`: its left and right
contexts point to a user-created `PhBdryMarker` whose code is `#`. HermitCrab treats that object as
a literal boundary marker in the shape. It is distinct from a word-edge anchor (`#` authored as a
word-boundary condition). Since the affix/root shape contains `+?`, not a literal `#` segment, the
rewrite does not produce `ta` or `at`.

The fixture covers the one-segment prefix, a root-initial underlying `r`, a two-segment prefix, an
internal `r` control, and the suffix-final mirror. The expected unchanged forms and absent rewritten
forms are the C# oracle behavior for this authored grammar.

## Founding-oracle evidence

`words.yaml` signatures and rule traces were checked by the C# HermitCrab conformance harness built
from the pinned Machine commit `18cf242f4b114b0eb9bac304b4b171ca2f499a39` on 2026-10-09. The
targeted self-check passed: 1 fixture, 0 failed. The harness accepted `ra`, `raa`, `ara`, `ar`,
and `a`; it rejected `ta`, `taa`, `ata`, and `at`.

The Motif report's object inspection confirms the authored left context has kind `boundary` and a
marker GUID, rather than the word-boundary context kind. That matches this fixture. The earlier
word-edge-anchor fixture passed both engines, so no PanGloss parser divergence is demonstrated.

## Graduation

Candidate destination: `machine/conformance/edge-cases/literal-hash-affix-context/`. No upstream PR
has been opened.

## Shared construct coverage note

The shared exercise tags record only positive affix analyses. The literal # boundary contexts
and missing rewrites remain described above as this fixture's focal behavior.
