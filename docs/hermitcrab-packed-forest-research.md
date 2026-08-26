# Packed parse forests and state memoization in HermitCrab: a research record

Status: research record, long-term storage. Not a plan — the plan is
`hermitcrab-forest-memo-plan.md`; the ambition analysis is
`hermitcrab-forest-memo-ceiling.md`.

Written 2026-08-26 on `feature/forest-memo` (off `feature/memoization`). This file exists so
the next person to pick this up does not have to re-derive the theory, re-read the paper, or
re-run the probes. Every number below is traceable to a named branch, commit, or paper page.

---

## 1. What was proposed

An external correspondent proposed making HermitCrab's analysis phase polynomial for
template-only grammars by changing what goes into `AnalysisStateKey`:

1. Before a memo lookup, **drop from the key any rule whose unapplication strictly shrinks the
   word.** Keep rules whose unapplication leaves the word the same length or longer.
2. Always record a `<source word, rule>` back-edge on each key, so the key set plus the edges
   form a **packed parse forest** rather than a set of independent memo entries.
3. Recover full derivations at readout by walking the back-edges, and **move the per-rule
   unapplication-count limit from the search to the readout**, filtering there.

The termination argument offered: along a shrinking edge the shape strictly shortens; along a
non-shrinking edge the retained rule count strictly increases. A cycle needs one or the other,
so no cycle can close. The `>=` boundary (rather than `>`) is what keeps a zero-morpheme
N -> V -> N loop out of the forest.

The claimed bound: for a template-only grammar the analysis states are substrings of the input
(O(n^2) of them), each derivable O(n) ways, giving O(n^3) — O(n^2) with bounded affix length
and no compounding — times a grammar constant "something like the number of distinct required
features in the rules."

### 1.1 What is right about it

**The termination argument is valid.** It is a well-founded ordering on the pair
(shape length, retained-rule count), lexicographic with the second component increasing. It is
also *sharper* than what the current key does: it identifies precisely which counts are
load-bearing for termination and licenses discarding the rest.

**It names a real exponential.** `AffixProcessRule.MaxApplicationCount` and
`CompoundingRule.MaxApplicationCount` both default to `1`
(`MorphologicalRules/AffixProcessRule.cs:28`, `MorphologicalRules/CompoundingRule.cs:19`).
With a limit of 1, the `_ruleCounts` component of `AnalysisStateKey` degenerates to *which
subset of the grammar's rules has been unapplied so far* — 2^k distinct values for k rules, in
the worst case, all sharing one shape and one pair of feature structures. That is a genuine
worst-case exponential sitting in the key, and removing the shrinking rules from it is the
right way to attack it.

**The static classification is feasible and half-built.** `GrammarAnalyzer` on
`parse-optimization-archive` already reasons about rule length effects
(`ComputeMaxAnalysisLength` bounds net insertion per affix rule and net restoration per
phonological deletion subrule) and already knows the direction trap — un-applying a deletion
rule *inserts*. `IsEdgeStripperQualified` already walks `AffixProcessAllomorph.Rhs` action by
action classifying `CopyFromInput` / `ModifyFromInput` / `InsertSegments` /
`InsertSimpleContext`. A length-effect classifier is a small addition to code that exists.

### 1.2 What it gets wrong about HermitCrab

**"The words in the AnalysisStateKeys are substrings of the input" is false in general and
shaky even for templates.** `AnalysisStateKey` is not a string. It is
`(Shape, Stratum, SyntacticFeatureStruct, RealizationalFeatureStruct, NonHeadCount,
ruleCounts)`. Unapplying an affix template rule writes required head features back into the
word's syntactic feature structure; realizational rules accumulate into
`RealizationalFeatureStruct`. So the state space is (substrings) x (a feature-structure
lattice), and that lattice is exponential in the number of features, not linear in the number
of categories. The proposal's "grammar constant" is doing very heavy lifting. This is not a new
observation — it is the standard result for constraint-based formalisms (Barton, Berwick and
Ristad 1987; see section 2).

**"Read out only happens when a stem matches the lexicon, and the time to perform read out
should be linear in the number of analyses produced" mis-models the engine.** HermitCrab's
readout is not a readout. `Morpher.Synthesize` (`Morpher.cs:299` / `:310`) takes every analysis
candidate, does `LexicalLookup`, expands `ExpandAlternatives`, re-runs the whole synthesis rule
cascade forward, and only then checks `IsMatch(word, validWord)` against the original surface
string. Candidates that survive analysis and lexical lookup and still fail are the normal case,
not the exceptional one. Measured: `cinacemerwa` (Sena) produces **218,847 synthesis inputs and
returns 0 parses**. There is nothing to enumerate; the entire cost is proving that.

**Moving the count filter to readout removes pruning from the cheap phase and adds work to the
expensive one.** As a set-equality claim it is sound: an analysis is valid iff every rule's
count is within its limit along that path, so filtering during search and filtering at
enumeration yield the same set. But the search-time filter currently stops those paths before
they reach synthesis, and synthesis is where the money is. This has to be measured, not assumed
in either direction — see the ceiling doc.

---

## 2. The theory: Maxwell & Kaplan 1993

The correspondent's link, and the right frame for the whole discussion:

> John T. Maxwell III and Ronald M. Kaplan. "The Interface between Phrasal and Functional
> Constraints." *Computational Linguistics* 19(4):571–590, 1993.
> https://aclanthology.org/J93-4001/

The paper is about hybrid systems that split into a **context-free phrasal component**
(polynomial, packable into a chart or forest) and a **functional constraint component**
(unification/equality, exponential in the size of the constraint system). Its subject is the
*interface* between them. The mapping to HermitCrab is close enough to be useful and different
enough to matter:

| Maxwell & Kaplan | HermitCrab |
| --- | --- |
| context-free phrasal constraints | analysis cascade — unapplying morphological rules |
| chart edge; equivalence of edges | `AnalysisStateKey` |
| parse forest (nested free-choice form) | the proposed `<source word, rule>` back-edge forest |
| functional constraints (unification) | feature unification, allomorph co-occurrence, `IsWordValid` |
| checking a solution | forward synthesis + `IsMatch` against the surface string |

### 2.1 The findings that transfer

**(a) The exponential lives at the interface, not in either component.** p.572:

> "even though a context-free parser can very quickly determine that those trees exist, if the
> grammar is exponentially ambiguous then the net effect is to produce an exponential number of
> potentially exponential functional constraint problems. ... This exponential does not come
> from either of the components independently; rather, it lies in the interface between them."

That is exactly the shape of our measurements. The analysis cascade is cheap and already at its
state floor (section 3.1). The cost is the fan-out from analysis states into synthesis problems.

**(b) Edge equivalence must account for everything downstream reads.** p.574:

> "the notion of equivalence must also be augmented to take account of the constraints: two
> edges are equivalent now if, in addition to satisfying the conditions specified above, they
> have the same constraints (or perhaps only logically equivalent ones)."

and the consequence:

> "there can be a different set of constraints for every way in which a particular category can
> be realized over a given substring. ... the algorithm becomes exponential in the worst case."

This is the key-completeness problem, stated in 1993. `AnalysisStateKey`'s doc comment already
carries a hand-audited key-completeness argument against every `Analysis*.cs` rule; the F1 probe
(section 3.2) found the same failure mode empirically at a *different* boundary. Any key
narrowing must re-run that audit.

**(c) Packing and pruning are in tension, and pruning is not always right.** Section 2.4,
"Still Exponential", p.576:

> "Although pruning can eliminate an exponential number of trees, this strategy is still
> exponential in sentence length in the worst case when the grammar is exponentially ambiguous
> with few constituents that are actually pruned."

and from the abstract:

> "A surprising outcome is that under certain circumstances an algorithm that does no pruning in
> the interface may perform significantly better than one that does."

Their measurements (Tables 2 and 3, pp.586–587; scaled so interleaved pruning on the base
grammar = 100) bear this out and are worth internalising:

| Grammar | Strategy | Benchmark unifier | Contexted unifier |
| --- | --- | --- | --- |
| Base | interleaved pruning | 100 | 42 |
| Base | factored extraction (no interface pruning) | >1000 | >1000 |
| Modified | interleaved pruning | 38 | 26 |
| Modified | factored extraction (no interface pruning) | 21 | **7** |

The same no-pruning strategy is the worst option on one grammar and the best on the other — a
100x-plus swing between two variants of the *same* grammar. That is a standing warning about our
own probe methodology.

**(d) The way to make the forest pay is to move discriminating features into the phrasal
component.** The "modified" grammar above is the base grammar with categories split so that
features which would otherwise be checked functionally are checked by the chart instead (V into
V_AUX / V_OBL / V_TRANS / V_OTHER, N into N_OBL+ / N_OBL-, and so on; p.585). Every strategy
improves on the modified grammar, and factored extraction improves by 50x. Their citation for
this is Nagata 1992's finding that a medium-grain phrase structure grammar beats both a
coarse-grain and a fine-grain one.

**For HermitCrab this is the most actionable idea in the paper, and it points the opposite way
from the proposal.** Putting `SyntacticFeatureStruct` into `AnalysisStateKey` already *is* the
medium-grain move. The right instinct is not "shrink the key"; it is "keep in the key exactly
what discriminates, drop exactly what does not." The *order* of shrinking-rule unapplications
does not discriminate. Whether the *set* of shrinking rules unapplied discriminates is an
empirical question — see the plan's Stage 0.

### 2.2 The finding that does not transfer — and this is the important one

Section 3.4, "Order Invariance", p.578:

> "Phrasal constraint systems and functional constraint systems commonly used for linguistic
> description have the property that they can be processed in any order without changing the
> final result."

**HermitCrab's morphological rules do not have this property, and we have measured it on two
unrelated grammars.** The F1 forest probe on `parse-forest-tandem` found candidate pairs with
*the same pending-rule multiset, a different application order, and different synthesis output*:
2 such pairs on Sena, and independently 12 on Indonesian (e.g. `{meN, -Cont}`). Two grammars
with no shared ancestry, same failure mode — a real property of the formalism, not a probe
artifact.

Order invariance is the assumption under which a packed forest can be *read out* packed. Without
it, every distinct order is a distinct solution, and the forest can be built compactly but must
be enumerated in full. This single fact separates the 28x figure from the 15–40% figure in
section 3.2, and it is why the honest description of a packed forest in HermitCrab today is "a
representation and memory optimization" rather than "a polynomial parser."

Note carefully what this does *not* invalidate. The shipped `AnalysisStateKey` uses an
order-independent multiset and is sound, because no *analysis-side* rule reads trail order. The
non-commutativity shows up on the *synthesis* side. Analysis-side order-independence and
synthesis-side order-dependence coexist, and conflating them is the easiest mistake to make in
this area.

### 2.3 Other literature, already surveyed

`docs/hermitcrab-parse-algorithm-analysis.md` (complexity-cap branch) carries the verified
survey: Sheil 1976 on the polynomial bound depending on edge equivalence being independent of
daughter substructure; Barton, Berwick & Ristad 1987 on feature systems as the source of
intractability; Karttunen/Beesley "overanalysis" and Koskenniemi tandem lookup for the
lexical-intersection idea. HermitCrab itself is Michael Maxwell's design — a different Maxwell
from the author of J93-4001, worth stating once so nobody assumes a lineage that is not there.

---

## 3. What we have already measured

All numbers below are from this repository. Sources are named so they can be re-run.

### 3.1 The analysis cascade is already at its state floor

From the Phase 3b instrumentation on `parse-optimization`, measured on Sena `atawirambo`:

- fair sequential unmemoized baseline: **30.5 s**
- morphological-rule cascade after the Phase 2/3 memo: **2,555 node expansions against a
  2,546-state floor** — 0.4% off optimal — and **1.4 s of the 30.5 s**
- affix-template battery: **93% of wall time**, run **38,840x** against **~2,581 distinct keys**
- after memoizing the template battery too: 30.5 s -> **6.1 s** (`cinacemerwa` 102.7 -> 26.9 s)

The subsystem the proposal makes polynomial is the 1.4 s one, and it is already within 0.4% of
its own state-count floor. Even making it free saves about 5% of the unmemoized word.

**But there is a second-order effect the proposal does not mention, and it is the strongest
argument in its favour.** `AnalysisScope.TemplateMemo` is keyed by the *same*
`AnalysisStateKey`. The template battery runs once per distinct key. Narrowing the key therefore
reduces template-battery runs **one for one** — and that is the subsystem that was 93% of the
cost. If key narrowing halves the state count, it halves the battery runs. This is the mechanism
by which a change to a 1.4 s component can move a 6.1 s word.

### 3.2 The forest's dedup ceiling, and why it is not reachable today

From the F1/F2 probes on `parse-forest-tandem` (commits 59d1e730, 5215fc01, f09714ba..7409cf40):

- dedup of synthesis inputs on an **order-insensitive** key: **28.72x** aggregate on the Sena
  heavy words (5 of 7 individually clear 3x; `manyeredzero` 2.89x and `pidafikawo` 3.81x are
  marginal). Indonesian: **1.41x** — the win is Sena-shaped, not universal.
- that key is **unsound**: 2 residual violations on Sena, 12 on Indonesian, all genuine rule
  non-commutativity (section 2.2).
- dedup on the **fully order-sound** key (F2 as shipped): **~15–40%** call reduction on Sena
  heavies.

An earlier probe iteration reported 9,774x. That number was inflated by a key that conflated
genuinely different rule sets. It is recorded here only so nobody rediscovers and believes it.

### 3.3 The lexical-reachability oracle does not rescue readout

The T1 probe (same branch, finalized commit aaeb2b35) asked how many cascade steps are provably
dead because no lexicon root can be reached from that node. Failure words — the expensive ones —
came in at `cinagumanika` 32.6%, `cinacemerwa` 24.4%, `manyeredzero` 18.5%, `pidafikawo` 0.0%;
pooled step-weighted **23.5%**, under the 30% build gate. `pidafikawo` at exactly 0.0% is the
diagnostic case: a root substring exists at every node visited and the word still fails, because
it fails on checks that happen *after* lexical lookup succeeds — environments, co-occurrence,
disjunctive allomorphs, syntactic features, surface match. A forest that only prunes
lexically-dead states does not prune those.

Consequence for the proposal: routing readout only through lexicon-live states — the obvious way
to exploit the forest — recovers at most the dead fraction, roughly a quarter on the words that
hurt.

### 3.4 A hidden coupling in the shipped memo, found while designing the narrowing

`Word.ReplayOnto` splices two things when it grafts a stored subtree onto a new arrival: the
ordered rule trail `_mruleApps` and the non-head list `_nonHeadApps`. It does **not** splice
`_mrulesUnapplied`, the per-rule un-application count dictionary — the replayed word simply
inherits the stored result's copy.

That is correct today, but only by accident of the key. Because `AnalysisStateKey` includes the
full count multiset, a memo hit guarantees the arriving word and the stored entry's own arrival
word had *identical* counts, so the stored result's counts are already the right ones. The key's
count component is silently doing double duty: it is not only a state distinction, it is what
makes `ReplayOnto`'s omission safe.

**Any narrowing of the count component breaks that invariant** — arrival and stored-arrival can
then differ on exactly the dropped rules, and the replayed word inherits counts that were never
its own. Anything reading `Word.UnappliedRuleCounts` after a replay (such as a post-analysis
limit filter, which is precisely what the proposal calls for) would read wrong numbers.

The fix is small and worth doing regardless, because it removes the hidden coupling: store the
arrival's counts on `MemoEntry` and have `ReplayOnto` compute
`clone.counts = stored.counts − storedArrival.counts + query.counts`. With the full key the
delta is zero and the change is a no-op, which is also how it should be tested.

Recorded here because it is a property of the shipped memoization, not of any proposal, and the
next person to touch the key needs to know about it.

### 3.5 Things already closed, so they are not re-proposed

- **Tandem lexical intersection (T2): not built.** Gate not met; mechanism understood (3.3).
- **Gate A (synthesis-side length bound): reverted.** At the point of comparison the candidate
  is still the bare root; its affix trail applies later inside `_synthesisRule.Apply`. Any future
  length reasoning on the synthesis side hits this same wall.
- **Phase 5 lexical gating: a proven no-op** on both reference corpora, because both have real
  compounding in their deepest stratum.
- **Pooling of small short-lived collections: reverted, net loss on every axis.** `Clear()` is
  O(capacity); Gen0 beats pooling here.

---

## 4. Where the open questions actually are

Ranked by how much they gate the outcome.

1. **Static rule-pair commutativity.** If pairs of morphological rules can be shown
   order-independent — by static analysis of their allomorph `Rhs` actions and feature effects,
   or by a verify-once-per-equivalence-class dynamic check — then order variants can be merged at
   readout and the 28x becomes reachable. Without it, everything else is bounded by section
   3.2's 15–40%. This is the highest-value unbuilt work in this area.
2. **Does the shrinking-rule *set* discriminate?** Two paths that strip different affix sets and
   land on the same shape and feature structures exist only where the grammar has homophonous
   affixes. Sena has many. Directly measurable before any code is written — Stage 0 of the plan.
3. **Does readout-time count filtering inflate the synthesis input set?** Set-equivalent, but the
   work moves from the cheap phase to the expensive one. Measure synthesis input counts, not just
   wall time.
4. **Can any part of synthesis run on a packed representation?** Maxwell & Kaplan's
   contexted-constraint question, transposed. Feature unification and allomorph co-occurrence
   plausibly can. The phonological rewrite cascade almost certainly cannot — it is a sequential
   transduction, not a constraint system. Answering "which half of synthesis is a constraint
   system" would decide whether a genuinely polynomial end-to-end parser is available at all.

---

## 5. Standing methodological rules for this area

Earned the hard way on the branches cited above.

- **Counting redundant expansions is not counting cost.** 98% measured redundancy yielded ~32%
  wall clock, because guard clauses reject cheaply before FST matching. Always take the fair
  same-mode baseline and split wall time by subsystem before believing a redundancy ratio.
- **A dedup ratio measured against an unsound key is not a dedup ratio.** 9,774x -> 28.72x ->
  15–40% is one measurement getting progressively honest.
- **Three grammars, always.** Maxwell & Kaplan's own 100x-plus swing between two variants of the
  same grammar is the argument. Sena, Indonesian and Amharic behave differently enough that any
  one of them alone will mislead.
- **The acceptance gate is analysis-set equality, not byte equality.** A memo-replayed `Word` is
  not field-for-field identical to a freshly computed one. Compare canonical
  morpheme-signature sets.
- **Search completeness must never be reduced.** Standing owner constraint: HermitCrab is the
  permanent fallback engine behind the FST work, so a faster parser that loses parses is not a
  faster parser.
