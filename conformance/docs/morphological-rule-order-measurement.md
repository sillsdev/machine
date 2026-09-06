# The `morphologicalRuleOrder` dependence measurement

`fieldworks-producibility.tsv`'s `morphologicalRuleOrder` loader-gap row states the check this way:
"a fixture is HC-engine-only for this reason only if [flipping its `Stratum` to `unordered`] changes
some word's found-analysis set... Six fixtures were measured this way and found genuinely dependent."
Until this document existed, nothing named which six, so the claim could not be checked and could not
be reconciled with what a reader auditing this repo alone can find. This is that ledger.

## What "measured" means

Per fixture: copy its `grammar.xml`/`words.yaml` pair into a scratch directory outside any checkout
(never edit the checked-in fixture in place), flip every `<Stratum>` in the copy from
`morphologicalRuleOrder="linear"` to `"unordered"` (a `Stratum` already `unordered` is left alone —
nothing to flip), and re-run the C# founding oracle in self-check mode against the *unchanged*
checked-in `words.yaml` as ground truth:

```
hc-conformance.exe --fixtures <scratch-root> --propose --include-pathological
```

`--propose` prints, per mismatched word, the signature set the mutated grammar actually produced. A
fixture is **dependent** when at least one word's found-analysis set changes under the flip — a
spurious extra parse, a lost one, or (per `PROTOCOL.md` section 4 rule 3, which treats a duplicate-
count divergence in an otherwise-matching signature multiset as a genuine mismatch, not noise) the
*same* signature produced an extra time. A fixture is **inert** when every word's signature set is
byte-for-byte identical under the flip — the ordinary case, since most fixtures never exercise the
difference at all. A `Stratum` that measures inert is not evidence the fixture's `linear` declaration
is pointless; it only says this particular corpus of words never manages to tell the two orderings
apart.

## Scope of this ledger

The founding measurement covered 27 fixtures across both the upstream root (`machine/conformance`,
this repo) and the PanGloss-side staged root (`conformance-staging`, a separate repository) and found
21 inert and 6 dependent. This document records the six dependent fixtures in full — root, fixture,
whether `linear` was written explicitly or came from the DTD default, and the measured outcome — since
those are the only rows the "six" claim needs to be checkable against. It does not re-enumerate the 21
inert fixtures; nothing about them is in dispute, and re-running the full 27-fixture sweep was outside
what this task asked for. If that fuller enumeration is ever needed, it is the same procedure above run
once per fixture whose `Stratum` declares (or defaults to) `linear`.

**Two of the six live in the other repository, and that split is exactly why the count looked wrong.**
A reader who only ever looks inside `machine/conformance` — grepping fixture notes for "rule order" or
scanning `grammar.xml` files for `morphologicalRuleOrder` — can at most find the four upstream rows
below; the other two are staged fixtures under `conformance-staging/filter-passes/` in the PanGloss
repository, invisible to any search confined to this checkout. Nothing here makes this repo depend on
that one: the two PanGloss rows are recorded for completeness and were independently reproduced during
this session (see "Reproduction," below), but `machine`'s own tests only ever gate on the four upstream
rows.

## The six fixtures

| Root | Fixture | `linear` declaration | Measured outcome |
|---|---|---|---|
| upstream (`machine/conformance`) | `languages/fusional-realizational-morphology` | explicit, on the `MprGroups` stratum only (the `Main` stratum is already `unordered`) | **Dependent** — 4/63 words gain a spurious parse: `ygofz`, `yxpedz`, `gofwz`, `gofhw` |
| upstream (`machine/conformance`) | `languages/suffixing-extension-slot-ordering` | explicit, on its one `Main` stratum | **Dependent** — 4/53 words gain a spurious parse: `yxkibz`, `sekwz`, `sekhw`, `dalehiz` |
| upstream (`machine/conformance`) | `edge-cases/mpr-group-overwrite-without-realizational` | explicit, on its one stratum | **Dependent** — 1/5 words gains a spurious parse: `wudofq` |
| upstream (`machine/conformance`) | `edge-cases/feature-gating-breadth` | explicit, on its one stratum | **Dependent** — 3/15 words gain a spurious parse: `kalidka`, `kalnoka`, `kalidmu` |
| PanGloss (`conformance-staging/filter-passes`) | `exact-span` | explicit, on its one stratum | **Dependent** — 2/12 words duplicate an already-correct signature rather than gaining or losing one: `matinlu` (`MA+TIN+LU|matinlu` produced twice instead of once), `makesalu` (`MA+KESA+LU|makesalu` produced twice) |
| PanGloss (`conformance-staging/filter-passes`) | `structural-transition` | explicit, on its one stratum | **Dependent** — 1/10 words duplicates an already-correct signature: `takolurmu` (`TA+KOLUR+MU|takolurmu` produced twice instead of once) |

All six declare `linear` explicitly; none rely on the DTD default (`HermitCrabInput.dtd:244`) to get
there. This ledger happens not to exercise the default-vs-explicit distinction `PROTOCOL.md` section 9
warns about — that a fixture merely declaring `linear` (explicit or defaulted) says nothing on its own
about dependence — but the column is recorded per fixture regardless, since a future addition to this
table could easily be a defaulted one.

**Why the two PanGloss fixtures read differently from the four upstream ones.** All four upstream
dependent fixtures gain a wholly new, previously-absent signature under the flip (a genuine spurious
parse `expected [] got [...]`), because `unordered` search reaches a rule-order-dependent
zero-or-more-parses construct (an MPR-group overwrite the DTD's ordering was hiding, or a feature-
gated rule now reachable in the opposite order). Both PanGloss fixtures instead already have exactly
one correct analysis for the pivotal word under `linear`; what `unordered` adds is a *second,
identical* derivation of the *same* surface form and signature (prefix-then-suffix and suffix-then-
prefix serialize to the same text), so the harness reports the same signature string with the wrong
multiplicity rather than a different signature set outright. Both shapes are "dependent" under the
definition above — the found-analysis set (as a multiset, per `PROTOCOL.md` section 3/4) genuinely
changes either way — but the PanGloss fixtures' own `words.yaml` header comments already state this
distinction in their own words ("the duplicate is faithful but unreadable"), which is why their
authors chose `linear` on readability grounds rather than because `unordered` would have been wrong.

## Reproduction

The four upstream rows were reproduced during this session: each fixture's `grammar.xml`/`words.yaml`
pair was copied into a scratch directory (never the checked-in copy), the flip applied, and
`hc-conformance.exe --fixtures <scratch> --propose --include-pathological` run against a build of
`SIL.Machine.Morphology.HermitCrab.Conformance` from this worktree
(`src/SIL.Machine.Morphology.HermitCrab.Conformance/bin/Release/net10.0/hc-conformance.exe`). The
word lists and counts above (4/63, 4/53, 1/5, 3/15) are the tool's own reported output, not
transcribed from elsewhere.

The two PanGloss rows were also reproduced the same way during this session, copying
`conformance-staging/filter-passes/exact-span` and `.../structural-transition` (read-only; nothing in
the PanGloss checkout was modified) into a separate scratch directory and running the identical
`hc-conformance.exe` build against them. This is stronger evidence than the fixtures' own header
comments alone claim: those comments do not say which oracle produced the "measured" duplicate-
signature finding, and this session's reproduction confirms it holds under the C# founding oracle
specifically, not only under `pg_parse`.

## What this does NOT mean for the other fixtures marked `false`

`edge-cases/mpr-group-overwrite-without-realizational`, `languages/fusional-realizational-morphology`,
and `languages/suffixing-extension-slot-ordering` are each already `fieldworks_producible: false` for
their own, independently-cited reasons that have nothing to do with rule order (a custom-named
`MorphologicalPhonologicalRuleFeatureGroup`; `LexicalEntry.family`/`RealizationalRule`/
`CompoundingRule.outputObligatoryFeatures`; the same trio plus an `AffixTemplate`-slot MPR-features
write, respectively — see each fixture's own `fieldworks_producible_notes`, none of which mentions
`morphologicalRuleOrder` at all). Their presence in the table above documents a *second*, unrelated
reason each is HC-engine-only; it does not mean their `false` marks rest on this measurement, and a
reader should not read their notes as incomplete for omitting it. `edge-cases/feature-gating-breadth`
is the only upstream fixture whose own `fieldworks_producible_notes` cites `morphologicalRuleOrder` as
its reason, because it is the only one of the four with no other disqualifying construct.
