# Coverage strategy: mechanical majority, hand-crafted minority

This is the governing statement for what the conformance suite covers and how that claim is
produced. Where another document disagrees with this one, this one is right and the other is stale.

## The rule

**Every denominator is derived from the grammar's own schema and from HC's abilities and
limitations — never from the corpus.** A number that grows when you add a fixture is a statistic
about the fixtures, not a bound on the engine. The corpus supplies witnesses; it never supplies the
list of things that need witnessing.

Two sources, and they answer different questions:

- **The DTD** (`conformance/HermitCrabInput.dtd`) says *which* constructs exist and *which* may
  reference which. Structure. Mechanically extractable.
- **The engine** (`src/SIL.Machine.Morphology.HermitCrab/`) says *what happens* — order, timing,
  what overwrites what, what is silently ignored. Semantics. Extractable only by reading or running
  it.

Neither alone is enough. The DTD declares `MorphologicalPhonologicalRuleFeatureGroup/@outputType`
as `overwrite|append`; only the engine says that `overwrite` **drops** the accumulated feature set
before a later rule's gate reads it.

## The four layers

| Layer | Question | Denominator | Derived from |
|---|---|---|---|
| Unit | does this knob do anything? | 113 surfaces | DTD inventory + counterfactual neutralization |
| Integration / edge | does data cross this handoff? | 60 interfaces | DTD `IDREF`/`IDREFS` + corpus resolution |
| Integration / chain | does the payload survive the trip? | ~15 chains | the edge graph, at write/read junctions |
| Hand-crafted | the weird cases | 5-20 fixtures | experience and analysis; **not derivable** |

### Unit — 113 surfaces

Of 1,059 DTD surfaces, 264 are grammar-observable and 194 resolve. Only **113** carry real
parse-time evidence (`Evidenced` 106 + `EvidencedJointly` 7): neutralize the surface, and some parse
changes. The rest are `RequiredToLoad` — mostly a re-derivation of the DTD's own content model,
which proves the loader reads the attribute, not that anything semantic happened.

They group into eleven families: co-occurrence constraints (20), phonology (20), affix processes
(13), phoneme inventory (12), lexicon (11), inflectional features (11), templates and slots (8),
MPR features (8), compounding (5), strata (3), realizational rules (2).

### Integration / edge — 60 interfaces

The DTD declares 60 `IDREF`/`IDREFS` attributes across 28 elements. Each is a typed handoff: one
construct pointing at another. IDREFs are untyped in a DTD, so target types come from resolving
every reference against the real grammars — 51 distinct (source, attribute, target) triples.

**42 exercised, 18 not.** Seven of the 18 belong to the dead subcategorization feature (see the
exception list below) and are correctly uncovered. The rest are real gaps, one fixture each.

### Integration / chain — the junctions

An edge test is still a unit test, of a bigger unit. Real hazards live on **paths**, where each edge
passes individually and the composition fails.

Exactly two payload types in the whole grammar are both written by one construct and read by
another — `MorphologicalPhonologicalRuleFeature` and `PartOfSpeech`. Everything else is a one-way
reference. That is what makes this layer tractable: chains are writers x readers per junction,
roughly fifteen, not a combinatorial explosion.

The canonical failure, and the reason this layer exists:

```
MorphologicalOutput.MPRFeatures           [write]   a rule emits mprA
MPRFeatureGroup.outputType = "overwrite"  [mutate]  the accumulated set is dropped
PhonologicalSubrule.requiredMPRFeatures   [read]    the gate looks for mprA -- gone
```

Each edge in isolation is correct. The defect needs three constructs, one attribute *value*, and an
ordering that puts the mutation between the write and the read. No amount of edge coverage sees it.

### Hand-crafted — deliberately capped at 5-20

For what cannot be derived: mutation semantics like the case above, crash pins, host configuration,
and the accumulated "this grammar shape is weird" knowledge that only comes from experience with
real field grammars.

**The cap is a design constraint, not a budget.** If this set grows past about twenty, it means a
mechanical layer is missing and hand-authoring is being used to paper over it. Treat growth here as
a signal to look for the missing derivation.

## Coverage comes from realistic grammars; confidence comes from the mapping

The suite holds 8 language-family grammars and 25 fabricated edge cases. Measured against the
interface layer, the realistic grammars are carrying nearly all of it: of the 42 exercised
interfaces, **40 are reached by a language-family grammar**, and only two are reached solely by a
fabricated fixture (`MorphologicalInput.excludedMPRFeatures` and `SymbolicFeature.defaultSymbol`).

The 25 fabricated fixtures are therefore not what makes the claim true. They are pins for specific
hazards, which is a real job, but it is not coverage.

So the standing preference, when an obligation is unwitnessed:

1. **Add the feature and the words to an existing language-family grammar.** A realistic grammar that
   grows a realistic feature buys the obligation *and* keeps it in a context where it interacts with
   everything else the grammar already does.
2. Only when that is genuinely impossible — the construct cannot occur in a plausible grammar of that
   shape — author a fixture for it.

A new narrow grammar per feature is the failure mode this is written to avoid. It inflates the
fixture count, witnesses one thing each, and produces confidence that is really just arithmetic.

**Confidence comes from the mapping, not from the count.** Every obligation must name the grammar
and the word that witness it, and that attribution has to be derived rather than assumed. The
recorded hazard here is inheritance: a coarse construct vocabulary lets a fixture appear to cover
something because a sibling does. Attribution is per-obligation or it is worthless.

Note against the cap above: 25 hand-crafted fixtures already exceeds the stated 5-20, which by this
document's own rule is the signal that a mechanical layer was missing. It was — the edge and chain
layers are that layer. Expect the fabricated set to shrink as obligations fold into real grammars,
not to grow.

## Mine real grammars for feature shapes, never for scale

Real field grammars are the best source of *which* constructs genuinely co-occur — they exercise
combinations nobody thinks to fabricate. They are a terrible source of *how much* grammar a fixture
should contain.

Measured against one real Bantu grammar converted to HermitCrabInput XML:

| | all 33 fixtures | that one grammar |
|---|---|---|
| MorphologicalRule | 137 | 224 |
| MorphologicalSubrule | 152 | 415 |
| Slot | 46 | 93 |
| size | 11KB median, 52KB max | 1024KB |

One grammar outweighs the whole corpus, and buys two interfaces. Importing it would dominate every
gate that walks the fixture set, several of which regenerate a ledger per fixture, and would trade a
suite that runs in minutes for one that does not.

So the procedure when a real grammar reveals an uncovered construct is to take the **shape** and
leave the **scale**: identify the minimal configuration that witnesses the obligation, and build
that. Where the real grammar had 12 MPR features, 14 tagged entries and 4 gated phonological rules,
the fixture needs one feature, one gated rule, and **two** entries.

## Every claim needs a control

The count follows from what is being attributed, and it is not always two.

**One, when the machinery supplies the contrast.** For a unit surface the counterfactual generates
the control by mutating the grammar and re-parsing. Nothing extra needs authoring; intact-versus-
neutralized is the pair.

**Four, for a gate** -- a 2x2, because the mutation alone is confounded. `mpr-gated-exception` is
the worked example already in the corpus: entries `SANIT` (no MPR feature) and `VOKAD` (carrying
`ruleFeatures="mprException"`), each with a bare-root control and a suffixed form, against a rule
whose `excludedMPRFeatures` names that feature. `sanitan` applies, `vokadan` is `expect_fail`.

The ungated arm is what carries the argument. With only the gated stem, severing the gate changes
the outcome -- but so would a rule that never fires at all, and the fixture cannot tell those apart.
`sanitan` proves the rule *can* apply, which is what makes `vokadan`'s failure attributable to the
gate. The fixture's own notes call the bare roots "a plain control"; that vocabulary is right and
should spread.

**Three, for a mutation in transit** -- write, mutate, read. No pair exposes it, which is the whole
argument for the chain layer.

The general rule, of which "at least two" is a symptom rather than a statement: a fixture must
contain enough cases that exactly one explanation of the observed difference survives. Anything less
exercises the construct and witnesses nothing, which is the present-versus-witnessed distinction
this document draws elsewhere, arriving again at fixture-design scale.

## What this replaces

`conformance/rule-interaction-pairs.tsv` (1,305 rows) is **not** a coverage denominator and must not
be cited as one. It enumerates rule *instances* within whatever fixtures exist; it moved from 1,290
to 1,305 when four fixtures were added, and 1,217 of its rows are `Undetermined` by construction.
Its honest job is per-grammar pruning — given one grammar, which adjacent swaps need a counterfactual
run and which are provably inert. It is good at that. It is kept for that.

The ordering constraint it encodes removes 203 pairs of 1,480, or 13.7%. The morphology block is the
full n^2 in both directions, because morphological rules and templates recurse mutually. Ordering is
a real property of the engine; it was never a bound on the interaction space.

## The exception list

Eleven surfaces across three feature areas are expressible in the DTD and read by no engine code:
cyclic strata and simultaneous phonological rule order; syntactic subcategorization (six surfaces);
cross-word phonological context (`PreviousWord`, `NextWord`, `Null`). The aspiration remains coverage
of any expressible grammar; these are the named exceptions, machine-derived from the proof ledgers
rather than asserted, and gated so the list cannot drift.

## Who this apparatus is for

All of it -- census, interface inventory, witness sweep, chains, obligations -- exists so
**HermitCrab** can justify the claim that this fixture set is complete. None of it is a consuming
engine's problem. A consumer's entire obligation is to produce the same parses; if it does, it
inherits whatever the suite covers, without reproducing any of the machinery that established what
that is.

That separation is only sound because of one property: **an obligation counts as covered only when
severing it changes a parse.** A construct whose removal changes no parse is invisible to an engine
that compares parses, so it cannot participate in a claim that transfers to one. This is why
presence was never good enough, and it reframes the severance sweep -- it is not bookkeeping about
our own fixtures, it is the proof that the words themselves carry the claim.

It also sets the standard for every layer added later. A new obligation is worth declaring only if
failing it would change a parse. If it would not, no consuming engine could ever fail it, and
covering it buys nothing.

## The inclusion rule

One sentence decides whether anything belongs in a denominator:

> Test every mechanism that can change a parse. Do not test one that cannot — **unless** it sits in a
> chain that does, in which case test the chain.

Both halves do work. The first is why presence was never sufficient: a construct whose removal changes
no parse cannot be detected by an engine comparing parses, so covering it buys nothing and inflates
the number. The second is why the chain layer exists at all: `A` alone may be inert and `C` alone may
be inert, while `C -> D -> E` changes the parse, and testing the parts in isolation would have
concluded there was nothing to test.

This is the test to apply to every proposed obligation, and it is also the disposal rule. A layer
that cannot show its obligations change parses is measuring its own XML.

Worked consequence: `SymbolicFeature.defaultSymbol` is present in a fixture and severing it changes
neither of that fixture's words. Under the first half it is out of scope. It returns to scope only if
some chain routes through it to an observable difference -- which is a question about chains, not
about the attribute.

## Two standing cautions

**Mechanical is not the same as meaningful.** The first census was mechanically generated, honestly
gated, and almost useless — because it was complete over a denominator that was 90% XML scaffolding.
Deriving a number does not make it worth measuring. Every layer above is chosen so that each member
is semantically load-bearing.

**Uncovered must mean named.** A gap is a row someone can read, not a residual. If a layer cannot
name what it misses, the layer is wrong.
