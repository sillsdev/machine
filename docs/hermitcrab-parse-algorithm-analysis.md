# Where the 15 million steps go: an algorithmic dissection of HermitCrab parsing

This document dissects, empirically and against the literature, why a single legitimate Sena
word costs the HermitCrab engine millions of rule applications, and identifies the specific
redundancies that could be removed **without constraining the grammar and without losing valid
parses**. It is the analysis companion to `complexity-cap.md` (which bounds the damage) and
`docs/hermitcrab-grammar-performance.md` (which helps grammar authors avoid the damage). This
document is about making the engine itself stop doing provably repeated work.

All numbers below are from the real Sena grammar (`samples/data/sena-hc.xml`, ~33k lines, two
`morphologicalRuleOrder="unordered"` strata, 25 morphological rules + ~19 multi-slot affix
templates in the main stratum), measured with an instrumented harness that replicates
`Morpher.ParseWordCore` exactly and swaps in a behavior-identical, counting clone of the
analysis cascade. A "step" is one rule-application attempt (`ParseContext.Step`), the same unit
`MaxParseSteps` budgets.

## 1. The headline data

Two worst-case words, dissected end to end:

| | `atawirambo` (parses, 2 results) | `cinacemerwa` (fails, 0 results) |
|---|---|---|
| Total steps | 14,905,517 | 37,543,196 |
| Analysis phase steps | 14,202,364 (95.3%) | 29,494,226 (78.6%) |
| Analysis candidates produced | 41 | 41 |
| — of which reach any lexical root | 4 | 5 |
| — of which yield a parse | 2 | 0 |
| Synthesis phase steps | 703,153 | 8,048,970 |
| Synthesis inputs after `ExpandAlternatives` | 17,699 | 218,847 |
| Cascade node expansions (main stratum) | 158,227 | 523,773 |
| Unique states — (shape, rule-multiset) | 1,626 | 12,168 |
| Unique states — + syntacticFS in key | 2,546 | (not measured) |
| **Redundant re-expansions** | **98.4% of tree** | **97.7% of tree** |

Three facts jump out:

1. **The cost is analysis, not synthesis.** 79–95% of all steps are spent unapplying
   morphological rules to hypothesize underlying forms — producing just 41 candidates, of which
   only 4–5 ever match a lexical root.

2. **The analysis tree is ~98% transpositions.** The cascade re-expands states it has already
   fully explored. One state (`shape='t'`, a 12-affix multiset) was re-expanded **7,200 times**
   for `atawirambo`. These are order-variants: unapplying prefix `a-` then suffix `-mbo` vs.
   `-mbo` then `a-` reaches the same (shape, remaining-rules) state, and the engine explores the
   entire subtree below it again each time.

3. **Nothing prunes hopeless work.** The most expensive word in the corpus (`cinacemerwa`,
   37.5M steps) returns *zero* parses: 218,847 fully-synthesized candidate words, every one
   failing at the end-of-pipeline checks (surface match / `IsWordValid`). The engine has no
   notion of "this branch can no longer succeed."

## 2. The combinatorial structure, precisely

### 2.1 Analysis: all *orderings*, deduped only at the end

For `unordered` strata (both Sena strata), analysis morphology runs through
`CombinationRuleCascade` with `multiApp: true`
(`AnalysisStratumRule.cs:50-71` → `CombinationRuleCascade.cs:32-54`). In that mode the recursion
restarts at rule index 0 on **every** level: the search enumerates all ordered sequences (with
repetition, bounded per-rule by `MaxApplicationCount`, default 1) of rule unapplications. For a
word where k independent affixes can strip, that is O(k!) paths to the same end state, not
O(2^k) states.

Each node expansion attempts the **entire rule battery** — visible in the per-rule diagnostics
as bands of rules with *identical* attempt counts (14 prefix rules × 319,267 attempts, 30 rules
× 158,227 attempts = one attempt per rule per node). Every attempt costs one step plus, if the
rule's syntactic gates pass, a full-shape anchored FST match per allomorph
(`AnalysisAffixProcessRule.cs:61-64`, `MatchingMethod.Unification`, `AllSubmatches: true`), and
every successful unapplication deep-clones the `Word` including its `Shape`
(`AnalysisAffixProcessAllomorphRuleSpec.ApplyRhs`).

Deduplication exists but fires **after the work is done**:

- Each cascade's terminal `HashSet<Word>` collapses equal results — but
  `Word.ValueEquals` (`Word.cs:583-600`) includes the `_mruleApps` **sequence**, so two
  orderings of the same affix set are *not* equal and are both kept, and in any case the
  HashSet dedups storage, not the recomputation that produced the duplicate.
- `MergeEquivalentAnalyses` (`AnalysisStratumRule.cs:140-178`) merges by **shape only**, at the
  stratum output boundary — after the tree has been fully walked. The merged variants are
  stashed in `Word.Alternatives`… and then `ExpandAlternatives` (`Word.cs:452-494`)
  re-materializes every one of them as a separate synthesis input. Merging defers the
  explosion; it does not remove it (16,330 alternatives for one candidate of `atawirambo`;
  98,197 for one candidate of `cinacemerwa`).

On top of the cascade, templates and morphological rules mutually recurse
(`AnalysisStratumRule.cs:188-230`): every cascade output gets the full template battery applied,
and every template output re-enters the full cascade — again with no memoization, which is why
total analysis steps (14.2M) are ~3.6× the cascade-internal rule attempts (3.96M).

### 2.2 Synthesis: a directed replay that still scans the whole battery

Synthesis is *not* a search — each analysis trail dictates the exact rule sequence, gated by
`IsMorphologicalRuleApplicable` (`Word.cs:269-276`: the next pending rule must equal the rule
being tried). But the `CombinationRuleCascade` used for unordered synthesis
(`SynthesisStratumRule.cs:35`) still **attempts all ~40 rules at every node** and lets the gate
reject 39 of them, one step each: every rule shows exactly 17,877 synthesis attempts for
`atawirambo`'s 17,699 synthesis inputs. The engine already knows the one rule that can apply
(`_mruleApps[_mruleAppIndex]`); it looks for it by exhaustive scan.

And the expensive correctness checks run dead last: allomorph environments, allomorph/morpheme
co-occurrence, disjunctive allomorph selection, and the surface-form match are all evaluated
only after the entire synthesis cascade has produced a finished word
(`Allomorph.IsWordValid`, `Morpher.IsWordValid`, `Morpher.IsMatch` — `Morpher.cs:711-753`).
`cinacemerwa` synthesized 218,847 complete words and threw away every single one at that final
stage.

## 3. What the literature says

The most striking finding is internal: **HermitCrab's founding paper already solved this
problem, by packing rather than forking.** Maxwell (1994) — the original Hermit Crab design
(Michael Maxwell's, not David Weber's; Weber's tools are AMPLE/STAMP) — avoids exponential
analysis explicitly *"by encoding into the form being parsed the ambiguities which arise
during parsing"*: rule unapplication uninstantiates features and marks undone
deletions/epentheses `[+optional]`, producing **one underspecified shape that denotes the whole
candidate set**, with lexical lookup as unification against it. The .NET implementation keeps
this for phonology (`AnalysisRewriteRule` mutates one shape in place, which is why phonological
rules are invisible in the step counters) but forks a concrete `Word` per choice at the
morphological level — losing the design's central invariant exactly where Bantu grammars
multiply. Maxwell quotes Anderson (1988): with realistic rule depth, "simply undoing the
effects of the rules… [is] quite impractical" if candidates multiply. The measured
98%-transposition tree is that prediction come true. (Historically, Hermit Crab benchmarked
within ~3× of PC-KIMMO when ambiguity stayed *in the form* rather than in the agenda.)

The rest of the (verified) literature converges on the same handful of completeness-safe
mechanisms:

1. **The complexity is real but local.** Two-level morphological recognition is NP-complete in
   general, PSPACE-complete with unrestricted deletion (Barton 1986; Barton, Berwick & Ristad
   1987) — so no restructuring gives a polynomial worst case, and the budget/soft-stop outer
   net stays. But the hardness is driven by *"local rather than global ambiguity"*, and
   Koskenniemi & Church (1988) locate the exponent precisely: parse cost is linear in word
   length and exponential in the number of **unresolved choice points that coexist before the
   first lexical anchor** — regressive-harmony prefixes in their data; subject/tense/object
   prefix slots before the verb root in Sena.

2. **"Overanalysis" and its two published cures.** Unapply-everything-then-look-up is what
   Karttunen & Beesley call the overanalysis problem. Cure (a): **interleave lexical lookup
   with analysis** (Koskenniemi's tandem lookup "does not pursue analyses that have no matching
   lexical path"); sound whenever the lexical filter over-approximates the lexicon. Cure (b):
   compose lexicon and rules at compile time — the FST endgame, out of scope here. Notably,
   rule *composition alone does not help*: "the ambiguity remains" (Karttunen & Beesley) —
   only lexicon information and state merging shrink the candidate set.

3. **Memoization is compatible with exact all-parses output.** Memoizing a backtracking parser
   keyed on state yields chart-parser complexity (Norvig 1991); Earley deduction / tabling
   (Johnson 1995; Shieber et al. 1995) gives the answer-complete discipline for it (memo entry
   = subscribers + answers; converging searches subscribe instead of recomputing). The exact
   model-counting literature (Sang et al. 2004; Bacchus et al. 2009) proves caching coexists
   with exhaustive (not just best-first) semantics. The game-search literature contributes the
   key-design discipline (Kishimoto & Müller 2004: keys must contain exactly what the remaining
   computation reads — a full-path key blew up searches 1000×).

4. **Dead-end pruning is unambiguously sound.** Nogood recording / UNSAT-component caching
   (Dechter & Mateescu 2007; Sang et al. 2004): discarding states proven to yield zero
   completions can never lose a parse. The boolean residue of A* heuristics — precomputed
   necessary conditions for *any* completion to exist — is the admissible-pruning transfer
   (Klein & Manning 2003); best-first *ordering* itself buys nothing when running to
   exhaustion.

5. **Packed representations are guaranteed to exist.** Rewrite-rule cascades denote regular
   relations (Johnson 1972; Kaplan & Kay 1994), so for a fixed surface word the candidate set
   is a regular language — representable as a lattice/DAG where each rule applies once to the
   whole structure (polynomial in lattice size), instead of once per enumerated path. Shared
   forests with tail sharing (Billot & Lang 1989; Tomita-style local-ambiguity packing) are the
   grammar-level version; AND/OR search with context-based merging (Dechter & Mateescu 2007)
   the search-level one. HFST optimized-lookup demonstrates the endpoint: cost bounded by
   distinct (position, state) pairs, not derivation paths.

6. **What does NOT transfer:** classical dominance pruning and symmetry breaking keep one
   representative per equivalence orbit — sound only for optimization/best-parse, unsound for
   literally-all-parses unless the merged items are provably output-identical (which is just
   deduplication); Viterbi-style weighted DP is best-parse machinery.

7. **Field precedent.** The FLEx mailing list documents this exact pain (Awetí words at ~9-20
   minutes), fixed until now only by hand-editing grammars (Andy Black's audit took a word from
   ~9 min to ~100 s). Maxwell (1998) shows IA (listed-allomorph) and IP (rule) descriptions are
   mechanically interconvertible — precedent for precompiling cheap rules into listed
   allomorphs. No published engine-side fix exists; this analysis + `GrammarAnalyzer` would be
   the first citable treatment.

## 4. Concrete opportunities, ranked

Ranking merges the empirical measurements (§1–§2) with the literature's soundness analysis
(§3). The first three are engine changes with no formalism impact and no lost parses; the
later ones are progressively larger architectural moves.

### 4.1 Transposition table over analysis states (~50–100× on the dominant phase)

Key: `(shape, per-rule unapplication counts, SyntacticFeatureStruct, stratum)` — measured
98.4% hit rate on `atawirambo`, 97.7% on `cinacemerwa`. Two designs:

- **Conservative (output-identical):** memo value = the set of (result `Word`, trail-suffix)
  continuations discovered below the state; on a revisit, replay the continuations onto the
  new prefix trail (cheap list operations — no FST matching, no shape cloning). Produces
  byte-identical output including all order-variant trails and traces.
- **Aggressive (canonical trails):** for `unordered` strata, record the trail as a canonical
  multiset and stop generating order-variants entirely; synthesis gates on multiset membership
  instead of sequence position. Semantically defensible — "unordered" means order is not
  linguistically meaningful — and collapses `ExpandAlternatives` too, but changes trace output
  and needs corpus-level verification that parse *results* are unchanged.

The conservative design alone converts the 158,227-expansion tree into a 2,546-expansion DAG.

Key-design discipline from the literature (the "GHI problem" in game search): the memo key must
contain **exactly** what the remaining computation can read — here that means the shape, the
per-rule unapplication counts (they gate `MaxApplicationCount`), the syntactic FS (it gates
`OutSyntacticFeatureStruct.IsUnifiable`), and for compounding the non-head state; but *not* the
trail order. Keying on too much (e.g. the full trail) silently degrades the hit rate back to
zero. The measured 1,626 → 2,546 state growth when adding the FS to the key shows the FS
splits few states in practice — cheap to include, and required for soundness.

The **cheapest first slice** of this is a *nogood cache* only: record states whose subtree
yielded zero results, skip them on revisit. No continuation replay, no trail bookkeeping,
trivially sound (discarding a zero-completion branch can never lose a parse). Since failure is
the overwhelmingly common case (only 4/41 candidates ever reach the lexicon), most of the
98.4% redundancy is *failed* subtrees re-searched — a nogood-only table captures most of the
win for a fraction of the implementation risk. The tabling literature's discipline applies on
upgrade to a full memo: a memo entry holds subscribers + answers, and a search converging on an
in-flight entry subscribes rather than recomputing.

### 4.2 Early lexical intersection — "tandem lookup" (the literature's decisive fix)

37 of 41 `atawirambo` candidates never matched any lexical root, and the tree that produced
them is the entire cost. This is Karttunen & Beesley's "overanalysis" problem, and the
published cure that doesn't require FST compilation is Koskenniemi's tandem lookup: consult the
lexicon *during* analysis and refuse to pursue hypotheses no lexical path can complete.
Soundness condition: the filter must **over-approximate** the lexicon (only kill hypotheses
that could never survive lookup), which tolerates underspecified segments conservatively.

Concretely: if every remaining unappliable rule only strips edge material (true for ordinary
affix rules — verifiable statically per grammar by `GrammarAnalyzer`), then a candidate can
only ever reach roots already present inside its current shape. Precompute a substring index
over root allomorphs (Aho-Corasick / suffix automaton, matching at the natural-class level so
underspecified nodes over-approximate); prune any branch whose shape contains no possible
root. This attacks the exponent the literature identifies — unresolved prefix choice points
stacking up *before the search ever touches the root* — and AMPLE's dictionary-first
architecture is the existence proof that the same grammar content can be searched
lexicon-anchored.

### 4.3 Direct rule indexing in synthesis (~40× on synthesis steps)

Unordered synthesis knows the single rule that can apply next; replace the scan-all-rules
cascade with a `Dictionary<IMorphologicalRule, IRule<Word,int>>` lookup (compounding-rule
`null` entries fall back to the scan). Behavior-identical by construction: the 39 skipped
attempts are exactly the ones `IsMorphologicalRuleApplicable` rejects today. Turns
`cinacemerwa`'s 8.0M synthesis steps into ~200K.

### 4.4 Early constraint checking in synthesis

Allomorph environment and co-occurrence constraints that are already decidable mid-derivation
(the environment's context is fully inside an already-built portion of the word, morphemes
already placed) could fail candidates before the rest of the cascade runs, instead of at
`IsWordValid`. Requires care with material later phonological rules could still change; the
statically-safe subset is identifiable per grammar (`GrammarAnalyzer` again).

### 4.5 Rule-battery prefiltering in analysis (constant factor)

At every analysis node all 25+ rules are attempted; most fail their anchored FST match
immediately. An index from edge-segment natural classes to the affix rules whose patterns could
possibly match (AMPLE-style position/anchor indexing) skips guaranteed-miss attempts without
changing semantics.

### 4.6 Cross-word memoization (corpus-scale extension)

The transposition state contains no reference to the original surface word — states like
`('t', {12 affixes})` recur across *words*. A bounded (LRU) cross-word memo could make
"Parse All Words" batch runs dramatically sublinear in practice. Interaction with per-parse
`ParseContext` budgets needs design; flagged as an extension, not a first step.

### 4.7 Packed candidate representation (the endgame short of full FST)

Restore Maxwell's original invariant at the morphological level: represent the analysis
candidate set as a shared lattice/DAG (guaranteed to exist — the candidate set of a
rewrite-rule cascade over a fixed surface form is a regular language, Kaplan & Kay 1994),
where each rule stage applies once to the whole structure and equal states merge (the foma/HFST
habit of determinize-minimize between stages, transplanted). `Word.Alternatives` +
`ExpandAlternatives` is a half-built version of this — it packs (by shape, at stratum
boundaries) but then fully unpacks before synthesis. Making synthesis verify *lattice nodes*
instead of expanded candidates is the biggest win and the biggest change; it converges with
the separate FST effort and should be weighed against it rather than built independently.

### Priorities

1. **4.1 nogood slice** — cheapest, trivially sound, captures most of the measured 98%.
2. **4.1 full memo + 4.3 synthesis rule indexing** — mechanical, output-identical.
3. **4.2 tandem lexical intersection** — the decisive fix per the literature; needs the
   `GrammarAnalyzer` edge-stripper check.
4. **4.4 / 4.5 invariants and prefilters** — constant factors, fit the existing lint.
5. **4.6 / 4.7** — corpus-scale and architectural endgames, coordinate with the FST effort.

The complexity cap (`complexity-cap.md`) stays regardless: the worst case is NP-complete
(PSPACE-complete with unrestricted deletion), so a budget outer net is formally motivated, and
Barton's "bounded nulls" + Maxwell's own "unapply a deletion rule only N times" sanction the
existing `DeletionReapplications`/`MaxAnalysisShapeGrowth` knobs as part of the formalism, not
an apology.

## 5. Sources

Primary sources verified against fetched text by the research pass (adversarial spot-checks
6/6 confirmed):

- M. Maxwell (1994), *Parsing Using Linearly Ordered Phonological Rules* — the original Hermit
  Crab: packing ambiguity into underspecified forms. https://arxiv.org/abs/cmp-lg/9411015
- M. Maxwell (1991), *Phonological Analysis and Opaque Rule Orders*, IWPT-2.
  https://aclanthology.org/1991.iwpt-1.13/ (overgeneration bound; full text not yet retrieved)
- M. Maxwell (1998), *Two Theories of Morphology, One Implementation*, SILEWP 1998-001.
  https://www.sil.org/resources/publications/entry/7814
- G.E. Barton (1986), *Computational Complexity in Two-Level Morphology*, ACL.
  https://aclanthology.org/P86-1009.pdf; and *Constraint Propagation in KIMMO Systems*, ACL.
  https://aclanthology.org/P86-1008.pdf; Barton, Berwick & Ristad (1987), *Computational
  Complexity and Natural Language*, MIT Press.
- K. Koskenniemi & K. Church (1988), *Complexity, Two-Level Morphology and Finnish*, COLING.
  https://aclanthology.org/C88-1069.pdf
- L. Karttunen & K. Beesley (2005), *Twenty-Five Years of Finite-State Morphology*.
  https://web.stanford.edu/group/cslipublications/cslipublications/koskenniemi-festschrift/8-karttunen-beesley.pdf
- L. Karttunen, R. Kaplan & A. Zaenen (1992), *Two-Level Morphology with Composition*, COLING.
  https://aclanthology.org/C92-1025.pdf
- R. Kaplan & M. Kay (1994), *Regular Models of Phonological Rule Systems*, CL 20(3).
  https://aclanthology.org/J94-3001.pdf
- P. Norvig (1991), *Techniques for Automatic Memoization with Applications to Context-Free
  Parsing*, CL 17(1). https://aclanthology.org/J91-1004/
- M. Johnson (1995), *Memoization in Top-Down Parsing*, CL 21(3).
  https://aclanthology.org/J95-3005.pdf
- S. Shieber, Y. Schabes & F. Pereira (1995), *Principles and Implementation of Deductive
  Parsing*. https://arxiv.org/abs/cmp-lg/9404008
- S. Billot & B. Lang (1989), *The Structure of Shared Forests in Ambiguous Parsing*, ACL.
  https://aclanthology.org/P89-1018.pdf
- D. Klein & C. Manning (2003), *A* Parsing: Fast Exact Viterbi Parse Selection*, HLT-NAACL.
  https://nlp.stanford.edu/pubs/klein2003astar.pdf; (2001) *Parsing and Hypergraphs*, IWPT.
- T. Sang, F. Bacchus, P. Beame, H. Kautz & T. Pitassi (2004), *Combining Component Caching and
  Clause Learning for Effective Model Counting*, SAT.
  http://www.cs.toronto.edu/~fbacchus/Papers/SangetalSAT2004.pdf
- R. Dechter & R. Mateescu (2007), *AND/OR Search Spaces for Graphical Models*, AIJ.
  https://ics.uci.edu/~dechter/publications/r147.pdf
- A. Kishimoto & M. Müller (2004), *A General Solution to the Graph History Interaction
  Problem*, AAAI. https://cdn.aaai.org/AAAI/2004/AAAI04-102.pdf
- M. Mohri & R. Sproat (1996), *An Efficient Compiler for Weighted Rewrite Rules*, ACL.
  https://aclanthology.org/P96-1031.pdf; L. Karttunen (1995), *The Replace Operator*, ACL.
  https://arxiv.org/pdf/cmp-lg/9504032; W. Skut et al. (2004), bimachines.
  https://arxiv.org/pdf/cs/0407046
- M. Mohri, F. Pereira & M. Riley (2002), *Weighted Finite-State Transducers in Speech
  Recognition*, CS&L. https://cs.nyu.edu/~mohri/pub/csl01.pdf; OpenFst.
  https://cs.nyu.edu/~mohri/pub/fst.pdf
- M. Hulden (2009), *Foma: a Finite-State Compiler and Library*, EACL.
  https://aclanthology.org/E09-2008.pdf; HFST optimized-lookup.
  https://github.com/hfst/hfst/wiki/OptimizedLookupFormat
- M. Silfverberg & K. Lindén (2009), HFST runtime lookup (67k–308k words/s).
- E. Antworth, PC-KIMMO v2 morphological parsing (chart over morphemes).
  https://software.sil.org/pc-kimmo/morphological-parsing/
- D. Weber, H.A. Black & S. McConnel (1988), *AMPLE: A Tool for Exploring Morphology*, SIL
  OPAC 12. https://www.sil.org/resources/archives/5761
- FLEx field evidence: flex-list "parsing broke down" thread (Awetí, ~9 min → ~100 s by manual
  grammar audit). https://groups.google.com/g/flex-list/c/pkxCwIxIktg
- Negative results consulted: Ibaraki (1977) dominance pruning (optimality-only guarantee);
  Crawford et al. (1996) symmetry breaking (one-representative-per-orbit, unsound for
  all-parses); Anders et al. (2024). https://arxiv.org/abs/2407.04419

## 6. Corpus context

*(top-N step counts per corpus — completed when the full-corpus scan lands)*
