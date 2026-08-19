# What this suite claims

Every number here was read from a checked-in ledger in this directory. None is estimated, and each
names the file it came from so you can recompute it.

## The claim

**An engine that produces the expected parse for every case in this suite has correctly implemented
every construct the suite demonstrably covers — where "demonstrably covers" means: removing the
construct changes a parse.**

That last clause is the whole claim, and it is narrower than it sounds. Read the next section before
relying on it.

## Coverage is measured in four layers

| layer | denominator | demonstrated | source |
|---|---|---|---|
| Unit surfaces | 264 grammar-observable | **110** | `semantic-coverage-counterfactuals.tsv` |
| Interface edges | 60 declared, 42 present | **15** | `interface-inventory.tsv`, `interface-witness.tsv` |
| Interaction chains | 40 | **11** | `interaction-chains.tsv` |
| Obligation cells | 346 | **2** | `dataflow-obligations.tsv` |

Each layer is stricter than the one above, so the numbers fall as the demand rises. The unit layer
asks "does this knob do anything." The cell layer asks "is there a named word whose parse flips when
this exact payload is severed, with a control proving the rule was capable of firing." Two cells meet
that bar today.

**The suite does not claim 346/346.** It claims 2, and it publishes the other 344 as named gaps that
cannot be quietly absorbed.

## Presence is not coverage

The single most important distinction here, and the one this suite got wrong five times before
getting it right.

A construct can be *present* in a grammar, referenced correctly, load cleanly — and still change no
parse when removed. Such a construct is invisible to any engine that compares parses, so it cannot
be part of a claim that transfers to another implementation. `interface-inventory.tsv` therefore
labels its 42 as **present**, structural only, and defers to `interface-witness.tsv` for what is
actually witnessed. The gap between 42 and 15 is exactly the size of the mistake.

Of the unit layer's 264: 110 carry a real parse-time witness (`Evidenced` plus `EvidencedJointly`),
66 are `RequiredByDtd` — the grammar will not load without them, which re-derives the DTD's own
content model and proves nothing about semantics — and 13 are `RequiredByLoader`, which is genuine
loader behaviour but still not a parse-time difference.

## What is expressible and never read by the engine

Eleven surfaces across three feature areas are legal in the DTD and consulted by no engine code
path. A grammar using them loads and behaves as though they were absent:

- **Cyclic strata and simultaneous phonological rule order** — `Stratum/cyclicity="cyclic"`,
  `Stratum/phonologicalRuleOrder="simultaneous"`.
- **Syntactic subcategorization** — six surfaces, the whole feature unimplemented end to end.
- **Cross-word phonological context** — `PreviousWord`, `NextWord`, `Null`.

This list is machine-derived from the proof ledgers and gated, so it cannot silently drift. See
`../README.md` for the full statement.

## What the criteria cannot express

Two hazard classes sit outside the data-flow and MC/DC criteria entirely, and are pinned only by
hand-crafted fixtures:

- **Order dependence under an unordered stratum.** The criteria are existentially quantified — one
  witnessed interleaving discharges a pair — so a second interleaving can still fail with the ledger
  green.
- **The analysis direction.** Chains are derived and judged in the synthesis direction only. Nothing
  in the chain layer speaks to unapplication.

## What is not claimed at all

- That HermitCrab is correct. Every gate here validates the **test suite**, never the engine. An
  unwitnessed obligation is a statement about this corpus.
- That the fixture set is minimal, or that its 33 grammars are the right 33.
- That a passing engine is correct on grammars unlike these. The fixtures are synthetic and small by
  design; scale is deliberately not tested here.
- That the hand-crafted fixtures are exhaustive for the hazards they pin. They were arrived at by
  analysis and experience, and there is no denominator behind them.

## Host configuration

Every expected result was generated under specific `Morpher` settings — `MaxStemCount`,
`DeletionReapplications`, `MaxAlternatives`, `MergeEquivalentAnalyses`, and the entry/rule selectors.
`DeletionReapplications` in particular changes *which parses are found*, not merely how fast. An
engine choosing different values may diverge on a fixture that is otherwise green. See `PROTOCOL.md`
section 8.
