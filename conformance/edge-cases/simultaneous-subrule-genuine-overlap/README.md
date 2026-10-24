# simultaneous-subrule-genuine-overlap: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# simultaneous-subrule-genuine-overlap

**Status:** `upstream_candidate`

## Why this fixture exists

It closes the only **NEEDS-ORACLE** verdict in the project. ADR 0001
(`docs/adr/0001-honest-capability-boundary.md`) names simultaneous-subrule overlap by hand as its
worked example of a configuration that is "unsupported by definition" *because the oracle itself is
unverified* for it — "never pinned against `hc.dll`".
`openspec/changes/plan-construct-coverage-completion` design.md row 6 records it as the one row whose
blocker is not construction quality but oracle trust, and tasks.md 5.2 tracks it.

Every other staged fixture in this repo is authored against `pangloss` per the `conformance-grammars`
skill's oracle-discipline note. That is exactly what ADR 0001 says is *insufficient here*. **This is
the first fixture in the repo whose ground truth comes from the C# founding oracle.**

## The grammar

Three `posV` roots — `pu` (PU), `pi` (PI), `pe` (PE) — and one `Simultaneous` phonological rule
(`prOverlapDemo`) whose focus is `ncStop` = {p}, carrying two subrules that share that focus:

| Subrule | Rewrites | Right environment | Class members |
|---|---|---|---|
| 1 (declared first) | p → b | `ncBackOrMid` | {u, e} |
| 2 (declared second) | p → d | `ncMidOrFront` | {e, i} |

The two right environments **genuinely intersect on the mid vowel `e`**. That is not asserted in
prose: `rust/crates/pg-foma/tests/simultaneous_overlap_capability_refuses.rs` checks it against the
real lowered-span intersection (`crate::lower`, via `is_fully_supported_shape`), so the fixture's
central claim is mechanically verified rather than believed.

## What the overlap actually verifies

Agreement on the underlying form alone would be weak evidence — a shared "no analysis" for `pe` is
equally consistent with both engines simply *failing* on overlap. The load-bearing evidence is the
**discriminating pair**:

- `be` **analyzes** — the overlapping position resolved by subrule 1
- `de` **does not** — the same position resolved by subrule 2

If subrule 2 had won the overlap, those two results would be exactly swapped. Both engines agree on
the pair, so what is verified is the **overlap resolution order** (first-declared subrule wins), not
merely that both engines are equally silent.

`bu` and `di` are single-subrule controls, one per subrule, so a regression in overlap handling cannot
silently take the ordinary case with it, and so subrule 2 is provably live rather than dead code that
overlap resolution happens to shadow.

## Verification

Founding oracle — `hc.dll`, built from the pinned `machine` submodule revision, invoked through the
repo's own protocol adapter (`machine/conformance/adapters/hc-dotnet-wrapper.sh`, PROTOCOL.md §7):

```
dotnet build machine/src/SIL.Machine.Morphology.HermitCrab.Tool/...   # produces hc.dll
bash machine/conformance/adapters/hc-dotnet-wrapper.sh batch \
     grammar.xml words.txt output.tsv
```

`hc.dll` loaded the grammar and compiled the rules without error or warning
(`SimultaneousSubruleGenuineOverlap loaded.`), then parsed all 9 words. Its output is kept verbatim
in `output.tsv`, with its console log in `hc_stderr.txt`, as the raw provenance for every signature in
`words.yaml`.

Cross-check — this repo's engine, same grammar, same wordlist:

```
pangloss batch grammar.xml words.txt out.tsv --threads 1 --engine=default
```

**Result: the `(word, signature)` projection is byte-identical across all 9 words.** Diffed
mechanically, not eyeballed.

## What this does and does not license

- **Does:** discharge ADR 0001's stated blocker for this configuration. The Rust confirm engine — the
  oracle every other fixture is authored against — is now verified against the founding oracle for
  genuinely overlapping simultaneous subrules, including on the case that discriminates resolution
  order. Row 6's verdict is no longer NEEDS-ORACLE.
- **Does NOT:** change the compiled proposer's disposition. The capability gate still refuses this
  grammar on the `--engine=foma` path (`simultaneous.subrule-overlap`, with a real span-intersection
  witness), and that refusal is about whether the *proposer* can faithfully represent overlap — a
  construction question, entirely separate from oracle trust. Row 6 becomes an ordinary open
  construct row rather than an oracle-blocked one.
- **Does NOT:** generalize beyond two subrules sharing one focus position with one shared class
  member. Deeper overlap shapes (three-way, or overlap in the left environment, or overlap combined
  with a feature-changing output) are unverified and would each need their own oracle run.

## Promotion note

`upstream_candidate`: it isolates one HermitCrab behavior, records the pinned `machine` revision and
the evidence method (a real `hc.dll` run through the documented adapter contract), and checks both the
Rust HermitCrab and the FST-plus-Rust outcomes. Unusually for this repo, upstream acceptance would
*not* need to re-derive ground truth against the founding oracle — it already is that.
