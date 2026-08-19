# Severance mechanics: what the tool can and cannot show you

Read this before attempting a coverage obligation. Every fact here was discovered the expensive way,
by an author spending most of a budget rediscovering it. None of it is guessable from the ledgers.

## What severance is

`InterfaceWitnessGate.Sever` removes **one attribute's value from every element of that name in the
fixture**, then reparses. That is the whole primitive. Three consequences that decide most
obligations:

**It is fixture-wide, not per-element.** Severing `LexicalEntry.partOfSpeech` strips it from all
fifteen entries at once. So severance can *unblock* a word as easily as block one: remove the payload
a gate reads and the gate stops applying, so a word that failed may now parse. Reasoning about
severance as if it removed the payload from one entry makes a same-word fail-to-pass flip look
impossible when it is routine. This is the single most common wrong conclusion.

**Removing a gate's input makes the gate vacuous, not false.** Every MPR and stem-name check in the
engine is guarded on a non-empty set — `Subrules[i].RequiredMprFeatures.Count > 0 && !IsMatch(...)`.
Sever the attribute and the condition is skipped entirely. That is why a witness is a *flip*, not a
polarity change.

**It cannot add a value, and it cannot touch element content.** Anything reached only through child
elements — `RequiredEnvironments`, `ExcludedEnvironments`, `PhoneticSequence`, allomorph ordering — has
no severance primitive at all. `Environments`, `Pattern` and `DisjunctiveAllomorph` are unwitnessable
today for this reason, and no word fixes that.

## Attributes that can never be severed

Removing a DTD `#REQUIRED` attribute makes the grammar invalid, so the mutant throws and the row is
recorded `RequiredByDtd` — never `Evidenced`, whatever word you try. There are 46 such declarations;
the ones that block obligations are:

| attribute | element |
|---|---|
| `primaryMorpheme`, `otherMorphemes` | `MorphemeCoOccurrenceRule` |
| `primaryAllomorph`, `otherAllomorphs` | `AllomorphCoOccurrenceRule` |
| `partsOfSpeech` | (see the DTD for the declaring element) |

Both co-occurrence gates are therefore **unwitnessable by authoring**. Their every attribute is either
`#REQUIRED` or already equal to its own DTD default. Covering them needs a different severance lever
— whole-element removal — not a cleverer word.

## Control arms need a rule id, and most elements do not have one

A `Control` arm must name the rule that fired successfully, and `GrammarRuleIndex` maps a fired rule
back to a `grammar.xml` id for exactly these element names:

`MorphologicalRule`, `RealizationalRule`, `CompoundingRule`, `PhonologicalRule`, `MetathesisRule`,
plus pseudo-ids for `MorphemeCoOccurrenceRule` / `AllomorphCoOccurrenceRule`.

It does **not** resolve `Allomorph`, `MorphologicalInput`, `AffixTemplate`, or `PhonologicalSubrule`.
An obligation whose gated construct sits on one of those cannot have its Control arm attributed, even
when a perfectly good control word already exists in the corpus. That is a tooling gap, not a coverage
gap, and authoring cannot close it. Each of those four is a child of an element the index *does*
resolve, so the fix is ancestor-walking rather than new machinery.

## Three engine behaviours that look like something else

**`outputPartOfSpeech` overrides; it does not set.** Severing it does not blank the derived word's
category — it stops the override, so the stem reverts to its pre-derivation category. An
`AbsentGatedForm` through that writer therefore needs a reader that rejects the *pre-derivation*
category, which is a different grammar shape from the one you would first reach for.

**Stem names compare by object identity** (`SynthesisAffixProcessRule.cs:106`,
`RequiredStemName != input.RootAllomorph.StemName`). A fixture declaring one `StemName` can never
produce a same-word flip on both sides, because there is no second identity to contrast with. Two
`StemName` elements are a precondition, not a nicety.

**Compounding gates come in two independent tiers, both reading `input.MprFeatures`.** Rule-level
`HeadProdRestrictionsMprFeatures` (`SynthesisCompoundingRule.cs:116`) is checked once before any
subrule; subrule-level `RequiredMprFeatures`/`ExcludedMprFeatures` (`:137`, `:154`) are checked per
subrule inside a loop whose output is the union of every subrule that applies. If one attribute feeds
both tiers, severing it collapses both and neither can be observed alone. If two subrules partition a
single binary feature exhaustively, one of them always applies and the inner gate can never be the
sole cause of a failure.

## A Timeout is a statement about the machine

A `Timeout` verdict means the mutant did not finish inside the budget on the machine that ran it. It
is not a property of the grammar. Four rows once recorded `Timeout`, were argued to be genuinely slow
rather than load-dependent, and later resolved on a quieter machine — two of them to `Evidenced`,
having suppressed real coverage for as long as they stood. Treat a reappearance as a signal to
re-sweep, never as a verdict to record or reason from.

## Before you start

Run `conformance/tools/check-obligation-feasibility.ps1 -Obligation <id>`. It answers, mechanically,
whether the attribute is severable, whether writer and reader co-occur in a fixture, and whether the
Control arm can be attributed at all. An obligation that fails those checks is not an authoring task,
and no amount of word-writing changes that.
