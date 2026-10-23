# circumfix-reduplication-precedence: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: circumfix-reduplication-precedence

## Why this fixture exists

Pins census **C2** (`docs/conformance/circumfix-structural-composite-census.md`,
`openspec/changes/plan-construct-coverage-completion` task 4.3c): `crate::emit::classify_affix`'s
reduplication test used to be an unconditional EARLY RETURN — computed and consulted before
`first_copy`/`last_copy` even existed — so an RHS that is SIMULTANEOUSLY circumfixing (an
`InsertSegments` before the first `CopyFromInput`, another after the last) AND reduplicating (the
SAME LHS part echoed by >= 2 `CopyFromInput` actions) always classified `Role::Reduplication`,
before the leading/trailing test further down ever ran. This is the one census item explicitly
flagged as coupled to `openspec/changes/plan-construct-coverage-completion` design.md row 11's
`Reduplication` carve-out: whichever role wins decides which MECHANISM claims the allomorph
(`Role::Reduplication` → `crate::peel::ReduplicationPeeler`; `Role::CircumfixPrefix` →
`build_structural_composites`), so it was deliberately left unscheduled until row 11's own boundary
could be re-checked alongside it (C1 and C3, the other two census items, closed independently
first).

## The joint decision (task 4.3c's own obligation)

**Reachability, checked first, not assumed.** `MorphologicalOutput` is declared
`(CopyFromInput | InsertSimpleContext | ModifyFromInput | InsertSegments)*`
(`machine/src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd:420`) — an unconstrained
repeated-choice group, so `<CopyFromInput index="p1"/>` may legally appear any number of times, in
any position, alongside any number of `<InsertSegments>`/other actions. `pg_grammar::load`
(`load.rs:1896-1901`) places no additional uniqueness constraint on `index` either — it only checks
that the referenced part exists. So an RHS that is simultaneously circumfix-shaped (leading AND
trailing insert) and reduplication-shaped (the same part `Copy`d twice) is fully DTD-legal and
loader-legal; this grammar is exactly that shape, not a vacuous or unreachable construction.

**Decision: `CircumfixPrefix` wins (mirrors C3's own resolution), and this is NOT merely a
relabeling — a genuine recall gap existed.** The other two options considered and rejected:

1. ~~Reduplication keeps winning, documented as a permanent boundary.~~ REJECTED: this would require
   showing the peel genuinely handles the circumfixing part, or that the combination is
   unreachable. Neither holds. `crate::peel::ReduplicationPeeler`'s four scan kinds (that module's
   own doc: prefix-copy, suffix-copy, separator+tail-copy, separator+suffix-peel) are each a
   ONE-SIDED surface-string match — none of them searches for a repeated span with independent
   material wrapping it on BOTH sides. Before this fix, an `AffixProcessRule` with this exact shape
   (peel-eligible per rule kind) was claimed by the peel but the peel's scans cannot actually find
   the wrapped reduplicated span — a real, silent recall gap dressed up as `ConfirmOnly`, not an
   honest refusal.
2. ~~Both mechanisms are wrong; refuse with a named witness.~~ REJECTED: `build_structural_composites`
   handles this shape correctly (see below), so refusing would be needlessly conservative — the
   real compiler already has a construction that works.
3. **`CircumfixPrefix` wins.** `build_structural_composites`'s `struct_extend` calls
   `pg_rules::morph::synthesize` directly (`emit.rs`), which replays every `OutputAction` in RHS
   document order with NO reference to `Role` and no assumption that a `Copy` run is contiguous or
   occurs only once per part — confirmed by reading `pg_rules::morph::synth_process_allomorph`'s own
   per-action loop over `allo.rhs`, plus that crate's own "Tier-2 #8 (reduplication morph
   attribution)" handling of a repeated `Input` part. A rule reaching `build_structural_composites`
   with this shape is therefore resynthesized faithfully — reduplicated copies AND wrapping inserts
   together — not merely "accepted." This is the SAME argument C1/C3 already established for the
   mechanism in general, extended here to the specific combined shape.

## Row 11's carve-out, re-checked against current code (not assumed still valid)

`crate::peel::is_reduplication_rule` (`peel.rs`) only ever classifies a rule
`AffixProcess`-vs-`Realizational` — a `RealizationalRule` allomorph is never peel-eligible "even if
one of its allomorphs would classify as `Role::Reduplication`" (that function's own doc, citing
`ReduplicationProposer.IsReduplication`, `ReduplicationProposer.cs:233-247`). **This carve-out is
UNCHANGED by both C3's and C2's reordering, and remains the faithful C# behavior**: it is a rule-KIND
distinction (checked BEFORE `classify_affix` is even consulted — the `_ => false` arm), completely
orthogonal to the Role-shape distinction C2 resolves, which applies identically regardless of rule
kind. No C# citation needed beyond the one `is_reduplication_rule`'s own doc already carries, because
no code in that function changed at all — see the next paragraph for why.

**No code change was needed in `peel.rs`.** `is_reduplication_rule`'s `.any()` scan calls
`crate::emit::classify_affix` directly, per allomorph — the SAME function C2's fix reorders. Once
`classify_affix` stops returning `Role::Reduplication` for a simultaneously-circumfixing-and-
reduplicating RHS (returning `CircumfixPrefix` instead), that allomorph silently drops out of THIS
scan too, automatically, with zero lines changed in `peel.rs` — mechanically identical to how C3
closed the `crate::preexpand` handoff by only touching `classify_affix`. Only `peel.rs`'s DOC comment
was updated, to record this interaction and the reasoning above (`is_reduplication_rule`'s doc
comment, `peel.rs`).

## What it pins

- `tam` (bare root): a plain control.
- `ketamtaman` (root + `mrCircRedup`): `classify_affix` classified `subCircRedup`'s RHS
  `Role::Reduplication` before the fix (the unconditional early return), routing `mrCircRedup` to
  `crate::peel::ReduplicationPeeler` — which cannot recall this exact surface (see above: a REAL
  recall gap, not a relabeling). After the fix, `classify_affix` reads this RHS as
  `Role::CircumfixPrefix` and `mrCircRedup` is admitted into `build_structural_composites` instead,
  which resynthesizes it correctly.
- A companion Rust test (`rust/crates/pg-foma/tests/circumfix_candidate_selection.rs`, C2 section)
  proves: (1) full proposer-to-confirm containment — every analysis `pg_parse::Morpher` finds for
  `ketamtaman` is reachable in `emit::emit`'s compiled net; (2) the ownership handoff is clean in the
  OTHER direction from C1/C3: `crate::peel::ReduplicationPeeler::new(&g).has_redup_rules()` is
  `false` for this grammar (the peel relinquishes the ONLY rule that could have made it `true`), so
  the peel is never even consulted for this word; (3) C1's and C3's own pinned behavior is unchanged
  by this fix (their existing tests re-verified passing, unmodified).

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; no `dotnet`/C# toolchain available in this environment. Per
`docs/conformance-staging-plan.md`'s oracle-discipline note, this must be treated as `pangloss`-only
ground truth until independently re-verified against the C# founding oracle.

## Verification

Signatures captured by running `cargo test -p pg-parse --test conformance_fixtures_gate --
--nocapture`, which discovers this staged fixture automatically and replays every word against
`pg_parse::Morpher` — the mismatch panic message on each pass reported the engine's own actual
signature, transcribed verbatim into `words.yaml` above (not hand-derived). Final run: `376 words
checked across 35 fixtures (2 skipped)`, this fixture's 2 words included, all passing.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/circumfix-reduplication-precedence/`. On acceptance, delete this
staged copy in the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
