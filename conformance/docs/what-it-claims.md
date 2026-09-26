# What this suite claims

Every number here was read from a checked-in ledger in this directory. None is estimated, and each
names the file it came from so you can recompute it.

## The claim

**An engine that produces the expected parse for every case in this suite has correctly implemented
every construct the suite demonstrably covers — where "demonstrably covers" means: removing the
construct changes a parse.**

That last clause is the whole claim, and it is narrower than it sounds. Read the next section before
relying on it.

## The primary funnel: gate-keyed obligations (23 engine gates, 46 obligations)

**This is the headline number now.** `gate-obligations.tsv` is keyed to
`SIL.Machine.Morphology.HermitCrab.FailureReason` — HermitCrab's own 23-member enumeration
(excluding `None`) of the decisions it makes when declining to apply or unapply something — rather
than to a DTD attribute pair. Every gate contributes exactly two obligations, the two arms MC/DC
demands: **Blocked** (a word fails, the engine's own trace names this exact gate as why, and severing
the construct that feeds the gate flips that same word to a successful parse) and **Control** (the
same grammar rule instance fires in a different, successful parse, proving the rule can apply at all
and that the Blocked arm's failure is attributable to the gate, not to a rule that never runs).

| | count |
|---|---|
| gates | 23 |
| obligations (gates × 2 arms) | 46 |
| worth covering (`xml_reachable`=Yes and `flex_producible`=Yes) | 42 |
| arms evidenced | **14** (9 Blocked, 5 Control) |
| gates with *both* arms evidenced | **5** (`ExcludedMprFeatures`, `HeadProdRestrictMprFeatures`, `HeadRequiredSyntacticFeatureStruct`, `RequiredMprFeatures`, `RequiredSyntacticFeatureStruct`) |

**Why this ledger, not `dataflow-obligations.tsv`, is now the primary claim.** The older ledger
enumerates four MC/DC arms per writer/reader chain but its generator can certify at most one of them
per chain (see "342 gaps, but only 14 an author can pick up" below) — of 346 enumerated cells, 28 are
even certifiable, and only 9 are satisfied. Worse, it has no row at all for six of the engine's 23
gates, because its cells are DTD-attribute write/read pairs and those six gates are not that shape
(`PartialParse`, `BoundRoot`, `MaxApplicationCount`, `DisjunctiveAllomorph`, `Environments`,
`SurfaceFormMismatch` — see `how-it-is-computed.md`'s engine-gate-inventory section). Keying the
denominator to the engine's own 23 gates instead fixes both problems at once: every gate gets a row
regardless of whether the schema names it with an attribute, and the Blocked arm's evidence is
stronger than the old chain pairing because the engine's own trace *names* the failure reason,
rather than the old generator inferring an arm from an attribute's spelling.

**Both ledgers are published; they measure different things.** `dataflow-obligations.tsv` still
answers its own question honestly — whether a specific WRITER/READER PAYLOAD PAIR survives full MC/DC
treatment for the 40 chains `interaction-chains.tsv` derives from the DTD — and neither its file nor
its tests are going away. `gate-obligations.tsv` answers a different, now-primary question: whether
each of the engine's 23 own decision points is independently shown to matter. Read
`how-it-is-computed.md`'s gate-obligations section for the full mechanics, including exactly which
9 obligations are evidenced today and why the rest are not (a corpus gap the current fixtures never
trigger, a construct FieldWorks' HCLoader can never produce, an element-content gate with no
severance primitive built yet, or a rule element `GrammarRuleIndex` cannot resolve to a fired-rule
id for the Control arm) — every one of those reasons is named in the row's own evidence text, never
collapsed to a bare "Unknown".

## The DTD-attribute-pair layers (kept, no longer the headline)

The four layers below were this suite's original, and until now only, coverage measurement. They are
still computed, still tested, and still meaningful for the narrower question they ask (does a
specific *schema-legal construct* do anything) — but see the section above for why the gate-keyed
ledger, not this one, is the number to cite as the suite's primary claim.

## Coverage is measured in four layers

| layer | denominator | demonstrated | source |
|---|---|---|---|
| Unit surfaces | 264 grammar-observable | **110** | `semantic-coverage-counterfactuals.tsv` |
| Interface edges | 60 declared, 44 present | **19** | `interface-inventory.tsv`, `interface-witness.tsv` |
| Interaction chains | 40 declared, 26 exercised | **9** paired | `interaction-chains.tsv` |
| Obligation cells | 346 enumerated, 18 worth covering | **9** | `dataflow-obligations.tsv`, `fieldworks-producibility.tsv` |
| Gate arms (MC/DC) | 46, 42 worth covering | **14** | `gate-obligations.tsv` |

Each layer is stricter than the one above, so the numbers fall as the demand rises. The unit layer
asks "does this knob do anything." The cell layer asks "is there a named word whose parse flips when
this exact payload is severed, with a control proving the rule was capable of firing." Nine cells meet
that bar today.

**The suite does not claim 346/346.** It claims 9, and it publishes every other cell as a named gap that
cannot be quietly absorbed.

One row changed what it measures, not just its number. The interaction-chain row used to report the
chains whose writer and reader are each evidenced *somewhere* -- a weak reading, since two separate
words can satisfy it. That figure is 23 today. The row now reports chains with a same-word PAIRED
witness, which is 9 and is the bar the obligation layer actually uses. The weaker number is still
derivable from `interface-witness.tsv` if anyone wants it; it is simply not what this table claims.

### 337 gaps, but only 9 an author can pick up

That 342 is honest as a count and misleading as a work list.

`DataflowObligationLedger` emits four MC/DC arms per chain, and `FindPairedWitness` can certify
exactly one of them. It reads the arm's direction off the reader attribute's *name*: a `required*`
reader blocks on absence, so the arm it can certify is `AbsentGatedForm`; an `excluded*` reader
blocks on presence, so it is `PresentGatedForm`. Any other reader name returns nothing at all.

| cells | count | can the generator ever mark it Satisfied? |
|---|---|---|
| the one certifiable arm on each chain with a `required*`/`excluded*` reader | 28 | yes |
| the other three arms on those same chains | 84 | no |
| all four arms on the 12 chains reading `head*`/`nonHead*` | 48 | no -- polarity is unreadable from the name |
| Mutator cells | 182 | no -- the detectors prove a structural precondition, never a word's outcome |
| ConditionExtension cells | 4 | no |

So 28 of the 346 are certifiable at all. Then the third constraint layer applies:
`fieldworks-producibility.tsv` marks **10 of those 28 as constructs HCLoader cannot emit**, so no
FieldWorks project could ever exercise them and a witness would prove nothing about real use --
six through `CompoundingRule.outputProdRestrictionsMprFeatures` as writer, six through the
`HeadMorphologicalInput` MPR readers.

**That leaves 18 cells worth covering, of which 9 are satisfied and 9 are outstanding -- but one of the
nine should not be counted.** An adversarial review found that
`MorphologicalOutput.MPRFeatures -> MorphologicalInput.excludedMPRFeatures` has a genuine behavioural
witness on a chain FieldWorks cannot produce: HCLoader populates that reader only from its own
irregular-form blocking mechanism, never from an affix output. So the defensible figure is **8 of 18**,
and the ninth is retained in the corpus as an engine test with its coverage claim withdrawn. See
`severance-mechanics.md`, "Producibility does not compose along a chain" -- the per-attribute
producibility ledger cannot express this, and no automated gate here can catch it. The funnel
is the honest form of this number:

| | cells |
|---|---|
| enumerated by the generator | 346 |
| certifiable by it at all | 28 |
| also producible by FieldWorks | 18 |
| satisfied today | **9** |

Do not read the other 318 as phantom. A control arm is a real obligation -- this suite's own rule is
that a gated form proves nothing without one -- and the twelve `head*`/`nonHead*` chains gate real
behaviour. What those 318 lack is a **certifier**, not a reason to exist. The distinction this ledger
does not yet draw, and should, is between "unsatisfied" and "nothing can currently establish this",
because only the first is work someone can pick up.

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
