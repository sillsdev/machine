# Forest memoization: implementation plan

Branch: `feature/forest-memo`, off `feature/memoization` (`af809180`).
Companions: `hermitcrab-packed-forest-research.md` (why), `hermitcrab-forest-memo-ceiling.md`
(what the number can be). Section 8 of this file is the PR description, ready to lift.

Baseline on this branch at creation: **82/82 HermitCrab tests green**, Release, net10.0.

---

## 1. The change in one paragraph

`AnalysisStateKey` currently carries a per-rule unapplication-count multiset covering *every*
morphological rule. Because `MaxApplicationCount` defaults to 1, that component degenerates into
"which subset of the grammar's rules has been unapplied so far" — 2^k values in the worst case,
all sharing one shape and one pair of feature structures. Most of those distinctions are not
load-bearing: a rule whose unapplication *shrinks* the word cannot drive an infinite regress,
because the shape length is already a decreasing measure. This plan classifies rules by their
length effect under unapplication, keeps only the non-shrinking ones in the key, moves the
per-rule count limit from the search to a post-analysis filter, and records `<source state,
rule>` back-edges so derivations remain recoverable. Termination is preserved by a lexicographic
measure: shape length strictly decreases along a shrinking edge, and the retained-rule count
strictly increases along a non-shrinking one, so no cycle can close.

---

## 2. Safety invariants

These are not negotiable and every stage is checked against them.

1. **Search completeness is never reduced.** HermitCrab is the permanent fallback engine behind
   the FST work. A faster parser that loses parses is not a faster parser.
2. **Conservative default in the classifier.** *Keeping* a rule in the key is always sound — it
   is the status quo. *Dropping* one is the risky direction. Any rule the classifier cannot
   prove strictly shrinking is retained. `Unknown` is not a failure mode, it is the safe answer.
3. **The acceptance gate is analysis-set equality, not byte equality.** Compare canonical
   morpheme-signature sets (`join("+", morphemeIds)` plus root index, sorted, semicolon-joined),
   never object or byte identity. A replayed `Word` is legitimately not field-for-field
   identical to a freshly computed one.
4. **Never couple a pruning gate to memo presence.** The prototype bundled Phase 5's
   `HasReachableRoot` into memo-on and the Rust port had to remove it. Whatever the key does,
   memo-on and memo-off must return the same set.
5. **Freeze on read in the key constructor.** `AnalysisAffixTemplateRule.Apply` reassigns
   `SyntacticFeatureStruct` to a fresh unfrozen clone after the owning `Word` is frozen, and
   that setter has no `CheckFrozen()` guard. The existing defensive `Freeze()` calls in
   `AnalysisStateKey`'s constructor stay.

---

## 3. Stage 0 — measure before building (no product code)

**This stage can kill the whole plan, and that is its job.** Nothing in
`src/SIL.Machine.Morphology.HermitCrab` changes.

Add a diagnostic in the test assembly, modelled on the existing
`tests/.../MemoCorpusVerification.cs` (explicit-category, corpus-gated, skipped when the
grammar files are absent). It reports, per grammar and per heavy word:

| metric | why |
| --- | --- |
| rule classification census (shrinking / non-shrinking / unknown, per stratum) | is there anything to drop? |
| distinct full keys vs distinct narrowed keys = **R** | the entire Bound-1 ceiling |
| nogood hits under each key | free upside, counted separately |
| wall-time split: mrule cascade / template battery / lexical lookup / synthesis | confirms or destroys the ceiling doc's derived 3.3 s / 2.8 s split |
| synthesis input count (`ExpandAlternatives` outputs reaching `_synthesisRule.Apply`) | the number that must not inflate in Stage 2 |

Word sets: Sena heavy words (`atawirambo`, `cinacemerwa`, `kukucitirani`, `manyeredzero`,
`pidafikawo`, `cinagumanika`, `kamatamisa`) plus the first 300; Indonesian all 121; Amharic a
bounded head of the corpus — the full Amharic run took about 4.3 hours last time and word 29
(`ሌባዎቹ`) alone is pathological, so use `--start`-style resumption or a subset, and say which.

Instrumentation trick that already works here: an instrumented clone swapped in via reflection
plus the tests-assembly `InternalsVisibleTo`, as `HcDissect` did.

**Gates (from the ceiling doc's falsification table):**

- R ≥ 1.3 on Sena, else **stop and report the negative result**.
- key-proportional share ≥ 30% of wall time, else recompute the ceiling before building.
- Record R for all three grammars regardless — Amharic is the one most likely to surprise, since
  infixation, truncation-reinsertion and a `ModifyFromInput` may leave it with almost no
  provably-shrinking rules.

Deliverable: a committed results table in this file, replacing the ceiling doc's derived
figures with measured ones.

### 3.1 Results — rule classification census

`ForestMemoCensus.RuleClassificationCensus`, run 2026-08-26 against the local grammars.

| grammar | rules | Shrinking | NonShrinking | Unknown | retained in key | worst-case key subsets |
| --- | --- | --- | --- | --- | --- | --- |
| Sena | 27 | 19 | 0 | 8 | **8** | 2^27 -> 2^8 |
| Indonesian | 15 | 10 | 0 | 5 | **5** | 2^15 -> 2^5 |
| Amharic | 36 | 31 | 4 | 1 | **5** | 2^36 -> 2^5 |

The classifier proves the majority of rules shrinking on all three grammars, so the worst-case
key space collapses hard everywhere. Amharic — predicted in the ceiling doc as the grammar most
likely to have almost no provably-shrinking rules — is in fact the best case, 31 of 36. Its 4
`NonShrinking` verdicts are the only genuine zero-or-truncating rules in any of the three; Sena's
and Indonesian's retained rules are all `Unknown` (compounding and reduplication), which is the
classifier being conservative rather than the grammar being awkward.

That is the worst case. What matters is the *realised* collapse, below.

### 3.2 Results — realised key collapse (R)

| grammar | words | state-weighted R | words at or above the 1.3 gate |
| --- | --- | --- | --- |
| Indonesian | 120 | **1.17** | 3/120 |
| Sena | 7 heavy words | **1.12** | 1/7 |
| Amharic | 24 words (corpus head) | 1.36 | 1/24 |

**The Amharic 1.36 does not clear anything, and must not be quoted as if it did.** It is a
state-weighted pooled average over words whose state counts are trivial — the largest is 212
distinct keys, against Sena's 18,686 — and 40% of the numerator comes from one word (`ለካ`, 198 ->
73 keys, R = 2.71). Every other word sits between 1.00 and 1.25. The subset also deliberately
stops before word 29 (`ሌባዎቹ`), the one genuinely pathological Amharic word, so the grammar's
actual worst case is unmeasured. Pooling a ratio over words of wildly different size is exactly
the error the T1 tandem probe made and had to retract; it is flagged here so it is not made a
second time.

What Amharic does show, more usefully: a *consistent* small collapse (1.03–1.25 nearly
everywhere) rather than Sena's flat 1.00. That fits the census — Amharic is the only one of the
three with genuine `NonShrinking` rules, so more rules get dropped and states merge steadily but
slightly.

And one striking corroboration of the central finding: `ሁለተኛ` takes 15,994 ms with 124 states,
and `ሄዶ` takes 30,335 ms with 212 states and 186 synthesis inputs. **State count and cost are
decoupled by two orders of magnitude on this grammar.** Roughly 160 ms per synthesis run is where
Amharic's time actually goes. Nothing done to the analysis state key can touch that.

Sena, per word:

| word | ms | parses | key builds | full keys | narrowed keys | R | memo hits | nogood hits | template hits | synthesis inputs |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| atawirambo | 16,709 | 2 | 159,557 | 2,556 | 2,556 | **1.00** | 29,736 | 88,426 | 37,512 | 17,699 |
| kamatamisa | 39,199 | 4 | 421,889 | 13,915 | 13,914 | **1.00** | 20,505 | 340,294 | 43,013 | 44,377 |
| manyeredzero | 14,933 | 0 | 96,023 | 11,762 | 11,762 | **1.00** | 2,794 | 73,460 | 5,812 | 1,074 |
| pidafikawo | 14,755 | 0 | 117,022 | 3,700 | 3,262 | 1.13 | 14,113 | 75,580 | 21,882 | 5,016 |
| kukucitirani | 72,788 | 5 | 541,712 | 18,130 | 18,130 | **1.00** | 48,486 | 415,718 | 53,407 | 158,480 |
| cinagumanika | 38,431 | 0 | 305,986 | 11,896 | 10,289 | 1.16 | 22,920 | 231,677 | 35,416 | 47,517 |
| cinacemerwa | 74,075 | 0 | 528,057 | 18,686 | 13,588 | **1.38** | 25,102 | 434,628 | 45,359 | 218,847 |

**The gate is not met. R = 1.12 on Sena against a 1.3 bar, and 6 of 7 words show no collapse at
all.**

The harness is measuring the right thing — two independent cross-checks say so. `atawirambo`
reports 2,556 distinct full keys against the 2,546-state floor measured on
`parse-optimization` by completely different instrumentation, and `cinacemerwa` reports
218,847 synthesis inputs, matching the historical figure exactly.

### 3.3 Why the worst case collapses and the realised state count does not

The census says the key space drops from 2^27 to 2^8 on Sena. The measurement says the number
of states actually visited drops by 0%. Both are true, and the gap is the whole finding.

For two paths to merge once the shrinking counts are dropped, they have to strip *different*
affix sets and still land on the same shape with the same syntactic and realizational feature
structures. The shape and the feature structures already discriminate almost perfectly: a
different affix set almost always means a different residue or a different set of required head
features. The 2^k blowup the proposal identifies is real as a bound and essentially never
realised on these grammars — the rule-count component of the key was, in practice, already
implied by the components next to it.

This is the same lesson as "counting redundant expansions is not counting cost", in a new
guise: **a worst-case bound collapsing is not the same as the realised state count collapsing.**

### 3.4 What the data says instead

Three observations worth keeping, none of which were what we set out to measure.

**R tracks failure, not size.** Every word with R = 1.00 has parses (`atawirambo` 2,
`kamatamisa` 4, `kukucitirani` 5) or is cheap (`manyeredzero`). Every word with R > 1 returns
zero parses. The collapse happens exactly where the search wanders into territory that leads
nowhere — which is at least the right place for it to happen. `cinacemerwa`, the single most
expensive word in the corpus, is the best case at 1.38. That is not enough to carry the change,
but it is the opposite of noise.

**The nogood cache is carrying the memo, by an order of magnitude.** On `cinacemerwa`, 434,628
nogood hits against 25,102 positive replays; on `kukucitirani`, 415,718 against 48,486. The
expensive part of analysis is proving subtrees empty, not reusing subtrees that produced
something. Any future work here should be aimed at the nogood path.

**Key construction is hit about 28x per distinct state** (`cinacemerwa`: 528,057 builds for
18,686 states). The memo is doing a great deal of work and the state count is a genuine floor.

### 3.5 Verdict

Per the ceiling doc's falsification table, written before the measurement: *"If Stage 0 shows
R < 1.3 on Sena, stop. Do not build Stage 1's consumers. Report the negative result."*

**Stopping.** Stages 2 through 4 are not built. The classifier and the census harness stay —
they are the evidence, they are correct, and they are reusable by anything that needs to know
which rules can grow a word. The Stage 2 wiring exists as a stash on this branch and should not
be merged on the strength of a 1.12.

What would change this verdict: a grammar whose R is genuinely high. The classification census
is cheap to run and is the right first question to ask of any new grammar — but on the three
grammars this project has, the answer is no.

---

## 4. Stage 1 — the length-effect classifier

New file `src/SIL.Machine.Morphology.HermitCrab/RuleLengthEffect.cs`, or an addition to a ported
`GrammarAnalyzer` (the archive version on `parse-optimization-archive` has the reusable
`AffixProcessAllomorph.Rhs` walk and the hard-won doc comments; port only what is used).

```
internal enum UnapplicationLengthEffect { Shrinking, NonShrinking, Unknown }
internal static UnapplicationLengthEffect Classify(IMorphologicalRule rule)
```

Classification rules:

- **`AffixProcessRule` / `RealizationalAffixProcessRule`** — unapplication removes what the
  allomorph's `Rhs` inserts and preserves what it copies.
  - every allomorph inserts at least one segment (`InsertSegments` / `InsertSimpleContext`),
    copies each part at most once, and has no part copied twice -> **Shrinking**
  - any allomorph inserts nothing (a zero morpheme) -> **NonShrinking**
  - reduplication (a part copied more than once), or anything the `Rhs` walk does not recognise
    -> **Unknown**
- **`CompoundingRule`** -> **Unknown**. Unapplication splits a word into head and non-head;
  total material is preserved even though the head shrinks. `NonHeadCount` is in the key
  independently, so there is nothing to gain by being clever here.
- **Anything else** -> **Unknown**.

`Unknown` and `NonShrinking` are treated identically by the key (both retained). They are kept
distinct in the enum so the Stage 0 census can tell "this grammar has zero morphemes" apart from
"this grammar has constructs we cannot analyse."

Tests (`RuleLengthEffectTests.cs`), all on hand-built grammars:

- ordinary suffix -> Shrinking
- zero morpheme -> NonShrinking
- reduplication -> Unknown
- infixation -> Shrinking if it still inserts material (it does) — assert the direction
  explicitly, because this is the case most likely to be got backwards
- compounding rule -> Unknown
- a rule with a mix of inserting and zero allomorphs -> NonShrinking (the weakest allomorph
  governs)

No behaviour change. Classification only, plus the Stage 0 census consuming it.

**Exit criterion:** classifier tests green, census numbers committed, 82/82 still green.

---

## 5. Stage 2 — narrow the key and move the count gate

These two sub-parts are unsound apart and must land in one commit.

**2a. Narrow the key.** `AnalysisStateKey` filters `word.UnappliedRuleCounts` to entries whose
rule classifies as `NonShrinking` or `Unknown`. For a template-only grammar the filtered
multiset is empty and the key collapses to `(Shape, Stratum, SyntacticFS, RealizationalFS,
NonHeadCount)` — which is exactly the correspondent's claim, and correct for that class.

Do the filtering once per `Morpher`, not once per key construction: build an
`IReadOnlyDictionary<IMorphologicalRule, bool> retainInKey` at grammar-load time and hang it off
`AnalysisScope`. Key construction is on the hottest path in the engine; a per-key LINQ filter
would eat the win it is trying to create.

**2b. Move the count limit to readout.** `AnalysisAffixProcessRule.cs:45` and
`AnalysisCompoundingRule.cs:46` stop enforcing `MaxApplicationCount` during analysis *for rules
classified Shrinking* — they must, because once a shrinking rule leaves the key, two words with
different counts for it share a memo entry and the gate would otherwise give them different
answers. A post-analysis filter in `Morpher.ParseWord`, sitting between `_analysisRule.Apply`
(`Morpher.cs:141`) and `Synthesize` (`Morpher.cs:155`), drops any candidate whose trail exceeds
any rule's `MaxApplicationCount`.

Note that `SynthesisAffixProcessRule.cs:46` already enforces the same limit on the way back, so
an over-count derivation cannot escape into the results even if the new filter is wrong — but it
would escape into *synthesis*, which is the expensive phase. The explicit filter is what keeps
Stage 2 from being a net loss, and its effectiveness is measured directly as the
synthesis-input count from Stage 0.

**Rollout:** `Morpher.NarrowAnalysisStateKey`, default `true`, meaningful only when the memo path
is active (`maxDegreeOfParallelism: 1`). The flag exists so the A/B harness can flip it inside
one process; it is not a user-facing feature.

Tests:

- **the correspondent's own cycle fixture**: a grammar with a zero morpheme turning N into V and
  another turning V into N. Assert the parse terminates and returns the same set as memo-off.
  This is the case the `>=` boundary exists for, and it belongs in the suite by name.
- key equality: two words differing only in shrinking-rule counts -> equal keys; differing in a
  zero-morpheme count -> unequal keys.
- count limit: a rule with `MaxApplicationCount = 2` and a word admitting three unapplications ->
  the three-unapplication analysis is absent from the result set, and the set equals memo-off.
- the `SelfOpaquing` two-iteration simultaneous-epenthesis fixture from the memoization plan —
  PanGloss flagged a real latent C# nogood-cache divergence in this exact code path (memo-on 0
  parses vs memo-off 1) whose trigger was never isolated. If it reproduces here, fix it and draft
  a JIRA issue per the standing C#-oracle-bug process.
- `DiagMemoHits` / `DiagNogoodHits` / `DiagTemplateMemoHits` asserted non-zero wherever a test
  covers the replay path, so a memo that silently stopped firing cannot look like a pass.

**Gates:** analysis-set identical against this branch's HEAD, memo-on, on Sena (first 300 +
the heavy set), Indonesian 121/121, Amharic (same subset Stage 0 used). Synthesis input count
must not rise more than 10%. 82/82 plus the new tests green in both flag states.

---

## 6. Stage 3 — back-edges, written but not read

The forest proper, added in a shape that cannot break anything because nothing depends on it yet.

Record on each memo entry the predecessor edges `(AnalysisStateKey source, IMorphologicalRule
rule)` for every successful rule application that produced it — the `<source word, rule>` pair
from the proposal. Store them in a new `AnalysisScope.Forest` table rather than widening
`MemoEntry`, so the existing replay path is untouched.

Then a test-only verifier enumerates derivations from the forest and asserts the enumerated set
equals the materialized result set, on real corpus words. This is the cheap way to prove the
representation correct before anything depends on it — and it is the stage that would catch a
mistaken termination argument, because a cycle in the forest shows up here as a hang or a
duplicate rather than as a wrong answer in production.

**Gate:** forest enumeration equals materialization on all three grammars; no wall-clock
regression beyond noise (the forest is write-only at this stage, so any regression is pure
bookkeeping cost and must be small enough to carry into Stage 4).

---

## 7. Stage 4 — deferred materialization (optional, gated)

Only if Stages 0 and 3 justify it. The two wins, from the ceiling doc:

- **per-state lexical lookup** instead of per-path — bounded by the T1 probe's pooled 23.5%
  lexically-dead fraction on failure words, and 0.0% on `pidafikawo`
- **less `ReplayOnto` cloning** — bounded by the allocation work's consistent finding that
  allocation reduction converts to wall clock at well under 1:1 on this codebase

Expected contribution: 10–20% of the residual. If Stage 0's measured split shows the residual is
smaller than the ceiling doc's derived 2.8 s, skip this stage and say so.

**Explicitly out of scope for this branch:** the static rule-pair commutativity analysis. It is
the highest-value unbuilt work in this area — it is what would let order variants merge at
readout and make the F1 probe's 28.72x synthesis-input dedup reachable — but it is a separate
piece of research with its own soundness burden, and "merge on multiset equality alone" is
already *proven* unsound on two grammars. It gets its own branch.

---

## 8. PR description (lift this)

> ### Narrow the analysis memo key to the rules that can actually loop
>
> `AnalysisStateKey` carries a per-rule unapplication-count multiset spanning every
> morphological rule in the grammar. Since `MaxApplicationCount` defaults to 1, that component
> is effectively *which subset of rules has been unapplied* — up to 2^k distinct keys for k
> rules, all with the same shape and the same feature structures. Most of those distinctions do
> no work: a rule whose unapplication shrinks the word cannot drive an infinite regress, because
> shape length is already a decreasing measure.
>
> This PR classifies each morphological rule by its length effect under unapplication, keeps only
> the non-shrinking rules in the memo key, and moves the per-rule unapplication limit from the
> search into a post-analysis filter. Termination is preserved by a lexicographic measure: shape
> length strictly decreases along a shrinking edge, and the retained-rule count strictly
> increases along a non-shrinking one, so no cycle can close — including the zero-morpheme
> N -> V -> N cycle, which the `>=` (not `>`) boundary keeps in the key on purpose and which is
> covered by a named fixture.
>
> The classifier is conservative by construction: anything not *provably* shrinking stays in the
> key, which is the status quo and always sound.
>
> **Why it matters beyond the cascade.** `AnalysisScope.TemplateMemo` is keyed by the same
> object, and the affix-template battery runs once per distinct key. Narrowing the key reduces
> battery runs one for one — and the battery was 93% of wall time before it was memoized. That
> is the mechanism by which a change to a 1.4 s component moves a 6.1 s word.
>
> **Measured:** _(Stage 0 / Stage 2 table goes here — R per grammar, wall-clock per heavy word,
> synthesis input counts)_
>
> **Verification.** Analysis-set equality (canonical morpheme-signature sets, not byte
> equality) against the pre-change memo path on Sena, Indonesian and Amharic, in both flag
> states, plus new unit coverage for the classifier, key equality, the count-limit filter, and
> the zero-morpheme cycle.
>
> **Scope.** This does not make HermitCrab polynomial end to end, and the PR does not claim to.
> Analysis becomes polynomial in input length for template-only grammars — the restricted class
> where the key collapses to (shape, stratum, feature structures) — but the grammar constant is
> a feature-structure lattice, and synthesis remains path-enumerated. The exponential that
> dominates our pathological words lives at the analysis/synthesis interface, not in the state
> graph; see `docs/hermitcrab-packed-forest-research.md` for the measurements and for why
> merging order variants at readout is unsound without a commutativity analysis we have not
> built.

---

## 9. Provenance

The proposal is an external correspondent's, in an email thread about
Maxwell & Kaplan 1993 (`https://aclanthology.org/J93-4001/`). The termination argument, the
`>=` boundary, the `<source word, rule>` pairs, and the static length classification are theirs.
The measurements, the ceiling analysis, and the scoping are ours. `docs/
hermitcrab-packed-forest-research.md` records both, including where the proposal's model of the
engine and ours differ.
