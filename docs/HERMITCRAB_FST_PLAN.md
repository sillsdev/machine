# HermitCrab FST acceleration — plan

> **Shipped MVP (read this first).** The MVP that landed is a **sound, fast, optionally-complete**
> analyzer that reuses HC's own engine:
> - **`FstTemplateAnalyzer`** proposes candidate analyses by walking a precompiled template/derivation
>   FST (fast, immutable → thread-safe to share).
> - **`VerifiedFstAnalyzer`** confirms each candidate by **restricted re-analysis** (`FstReplay`): HC's
>   own `AnalyzeWord` pinned to that candidate's root+rules via a pooled `Morpher`. A confirmed,
>   genuine HC analysis is emitted; anything HC won't confirm is discarded. **Sound** (no wrong
>   analyses), **~13×** on Sena, **multithread-safe** (each verify rents a Morpher from `MorpherPool`).
> - **`CompleteHybridMorpher`** adds completeness: a grammar that passes **empirical set-parity**
>   (`FstVerification`) runs FST-only; otherwise the search engine is used (the known slow path).
>   Per-word control via `AnalyzeWord(word, useFst)` / `UseFstFor`.
> - **`GrammarFstAdvisor` + `GrammarFstClosure`** — the grammar census/linter (PR #441's original core).
>
> **Out of scope / explored-then-abandoned:** the *per-stem completeness proof* (proving the fast path
> complete for every word without the engine). Sections §11.5+ and §12.3+ below document that
> exploration and why it was dropped (rule/symbol coverage ≠ path coverage; the segmentation-superset
> proposer was slower and still incomplete). The shipped completeness model is the empirical
> certificate + engine backstop, not a static per-stem predicate. Deferred to later PRs: the
> generator (reverse direction) and a 2-way-FST/compounding treatment of the residual ~3%.

Goal: replace HC's combinatorial un-application *search* (measured ~10,000 `Word` clones/word,
397 MB/word, the cause of the ~3× parallel ceiling) with a precompiled **transducer walk** for
the finite-state fraction of a grammar — while **degrading gracefully** to the existing engine
for the parts that aren't finite-state. A grammar census of the real Sena grammar found it
**~100% FST-able** (0 rewrite rules, 0 variables, 0 productive reduplication, all-concatenative
affixation) — and the `GrammarFstAdvisor` in this PR confirms it (Tier 1, 0 escapes) — so for
Sena-like grammars an automaton walk could be 10–100× and near-zero-allocation (which also lifts
the thread ceiling).

## 1. Tech stack — build on SIL.Machine's own `Fst` (not OpenFst/Foma/HFST)

The decisive fact: **`SIL.Machine.FiniteState.Fst` already provides the full algebra we need** —
`Compose`, `Determinize`, `Minimize`, `Intersect`, `EpsilonRemoval`, transducer outputs
(`IFstOperations`: Insert/Replace/Remove), and crucially **`UseUnification`** (arcs carry
*feature structures* matched by unification, not just plain symbols). The `RootAllomorphTrie`
is already a lexicon FST built on it.

| Option | Verdict |
|---|---|
| **SIL.Machine `Fst`** (in-repo) | ✅ **Recommended.** Managed, cross-platform, no interop; *natively models HC's feature-bearing segments with unification*; composition algebra already present; lexicon-FST precedent. |
| OpenFst / Foma / HFST (C/C++) | ❌ for now. Mature + fast, but: plain-symbol alphabets (must flatten feature structures → blowup; even though Sena has no variables, this loses HC's native model), heavy P/Invoke + native build/cross-platform burden, and reconciling results back to HC's `Word`/`Properties`. Reserve only if SIL.Machine's FST can't scale. |

So the stack is: **C#/.NET on SIL.Machine's `Fst`**, reusing the existing `ShapeNode`/`FeatureStruct`
model. The work is a *compiler* (Language → composed transducer) plus a *runtime* (`IMorphologicalAnalyzer`
that walks it), not a new FST engine. Risk to retire early: validate that `Fst.Compose` +
`Minimize` behave correctly for **unification** arcs at grammar scale (they're proven for the
matcher's pattern FSTs; composition of large lexicon∘affix transducers is the unknown).

## 2. The compile pipeline (Language → one analyzer transducer)

1. **Classify** every construct (the census, made a reusable pass): concatenative affix /
   template / environment-allomorphy / bounded compounding = **FS-able**; rewrite rule with
   unbounded environment, α-variable, productive reduplication, infixation = **non-FS island**.
2. **Build component transducers** for the FS-able fraction:
   - lexicon → root transducer (extend `RootAllomorphTrie`),
   - each concatenative affix subrule → an insert/concat transducer,
   - affix templates → position-class concatenation,
   - environment-conditioned allomorphy → context-restricted arcs,
   - bounded compounding (`MaxStemCount`) → bounded recursion unrolled.
3. **Compose** them (`lexicon ∘ affixes ∘ templates`) into one transducer, then
   **Determinize + Minimize**. Composition bakes in rule ordering/opacity; minimization gives
   the optimal shared state set (the Myhill–Nerode classes).
4. **Invert/orient** for analysis (surface → underlying+gloss): the analyzer walks the input
   word through the transducer, reading off morpheme IDs / `Properties` on accepting paths —
   the same IDs HC's `Word` carries, so the consumer mapping (FieldWorks → LCM) is unchanged.

## 3. Graceful degradation — the tiered hybrid (the key design)

The architecture must never regress: the FST is a **sound optimization layered over the proven
search engine**. Three tiers, chosen automatically by the compile-time census:

- **Tier 1 — fully FS-able grammar (e.g. Sena).** The whole grammar compiles; the transducer is
  **complete**. Analysis = automaton walk only; the search engine is never invoked. Maximum win.
- **Tier 2 — FS-able with isolated non-FS rules.** Compile the FS fraction into the transducer;
  mark each non-FS operation with an **escape** (flag-diacritic-style arc). At runtime:
  - cheaply detect whether any non-FS rule *could* apply to this word (e.g. a reduplication
    signature, or a segment a rewrite rule targets);
  - if **not** → the transducer is complete for that word → fast path;
  - if **yes** → fall back to the existing `Morpher` search for that word (or delegate just the
    escaped sub-operation, then resume the walk).
  Most words avoid the islands → mostly fast, with the slow path only where needed.
- **Tier 3 — pervasively non-FS (heavy rewrite rules, α-variables, productive reduplication).**
  The FST covers too little; **disable it** and use today's search engine. No regression.

**Soundness contract (non-negotiable):** the FST must (a) never emit a wrong analysis, and
(b) for any word it claims complete, never miss one. Guaranteed by: only compiling
*provably*-FS-able rules; in Tier 2, falling back whenever completeness is uncertain (conservative);
and a **verification mode** during rollout that runs FST + search and asserts identical analyses
across a corpus (we already have the Sena rig + signature comparison for exactly this).

The degradation is *monotone in grammar complexity*: more FS-able ⇒ more handled by the fast
walk; less FS-able ⇒ more fall-back, down to pure search. Nothing ever gets slower than today.

## 4. Where it bolts onto the code

- New `FstMorpherCompiler`: `Language → ComposedAnalyzerFst` (+ the per-grammar tier decision).
- New `FstMorpher : IMorphologicalAnalyzer` (and `IMorphologicalGenerator` for the reverse): walks
  the transducer, emits `WordAnalysis` / the morph `Properties`; on a Tier-2 escape, delegates to
  an inner `Morpher`.
- Reuse: `RootAllomorphTrie` (lexicon FST), the `Fst` algebra, the `ShapeNode`/`FeatureStruct`
  model, the census classifier, and the benchmark + signature comparison for verification.
- Consumers are unaffected: same `IMorphologicalAnalyzer` interface; FieldWorks keeps mapping
  morpheme IDs → LCM.

## 5. Risks & mitigations

| Risk | Mitigation |
|---|---|
| `Fst.Compose`/`Minimize` unproven on large **unification** transducers | Spike on Sena first; validate output == HC output on the corpus before scaling; fall back to plain-symbol flattening (Sena has no variables) if needed |
| State/alphabet blowup | The **eager/lazy partition knob** (§10): a state/memory budget that auto-demotes expensive-cold layers from precompiled (A) to on-the-fly (B); completeness is invariant under the knob (composition associativity), so bounding size never drops analyses. Minimize-after-compose only on safe (non-unification) layers |
| Tier-2 "is the FST complete for this word?" detector unsound → missed analyses | Make it conservative (fall back when unsure); verification mode catches misses in rollout |
| **Closure**: a normal (FST) step *feeds* an escape, so the automaton's "no path" is a false "done" | Confirm FST closure (§9): static feeding-closure pass (`range(F) ∩ T_E = ∅` via `Fst.Intersect`) + stratal containment; corpus closure verification (set parity) gates replacing the search engine. Undecidable feeding (non-regular escapes in a loop) ⇒ conservatively keep those words on the search backstop |
| Generator (synthesis) direction | Same transducer inverts; or keep HC synthesis initially and only FST-accelerate analysis |
| Grammar-specificity | The census decides the tier per grammar; production grammars must be censused before enabling Tier 1/2 |

## 6. Phased plan

1. **Spike (decisive):** compile Sena's lexicon ∘ concatenative-affixes into one transducer via
   `Fst.Compose`/`Minimize`; build a minimal `FstMorpher.AnalyzeWord`; **verify** its analyses
   equal `Morpher.AnalyzeWord` on the Sena corpus (signature comparison); **measure** clones (→~0),
   allocation, and wall-time vs. the search engine. This proves or kills the SIL.Machine-FST stack.
2. **Complete Tier 1:** add templates, environment-allomorphy, bounded compounding; full Sena
   parity + the parallel-scaling re-measurement (expect the 8-thread/3× ceiling to lift, since the
   walk barely allocates). Build the compiler as a **pipeline of composable layers behind an
   eager/lazy interface** (§10) from the start — the partition knob and state budget are Phase 1–2
   architecture, not a later bolt-on.
3. **Tier 2 hybrid:** census-driven escape arcs + per-word fallback detection + verification mode,
   gated on **confirming FST closure** (§9) — the static feeding-closure pass + corpus closure
   verification that certify the transducer's "no analysis" is a proof, not a guess.
4. **Generator + productionize:** reverse direction, the `IMorphologicalAnalyzer` wiring, and a
   FieldWorks adapter; run the census on real production grammars to set each project's tier.

## 7. Decision gate

Step 1 (the spike) is the gate: it answers, with numbers, whether SIL.Machine's FST can compose
a real grammar correctly and how big the speedup is. If yes → proceed; if `Fst.Compose` can't
handle it → reassess (flatten to symbols, or external lib). Everything past Step 1 is contingent
on that result.

## 8. Transducer output schema — the packed morpheme-token array

What the analyzer transducer emits on an accepting path must be the *structured derivation*
(ordered morphemes + root), not just accept/reject — otherwise it is a **recognizer, not an
analyzer**. HC carries this today as per-segment morph annotations + an ordered allomorph list
(`Word.MorphemesInApplicationOrder` → `WordAnalysis.Morphemes`/`RootMorphemeIndex`); the FST must
emit the same structure as transducer output.

**Encoding — one 32-bit token per morpheme, in application order:**

```
 31            24 23                              0
+----------------+--------------------------------+
|  8-bit MorphOp |     24-bit morpheme index      |
+----------------+--------------------------------+
```

- **op (high 8 bits)** = the morpheme's *role/operation*: Root, Prefix, Suffix, Infix,
  Reduplication, CircumfixPrefix/Suffix, Compound, Clitic, Process (simulfix/ModifyFromInput),
  Null (zero morph). This is the "ordered operations connected to the letters" — it lets a
  consumer rebuild the gloss/bracketing without re-running any rule.
- **morpheme index (low 24 bits)** = an index into the grammar's compiled morpheme table
  (→ `IMorpheme.Id`/gloss via a side table — don't pack strings).
- An accepting path's output is the **`uint[]` of these tokens — that array *is* the analysis**,
  and it is **self-describing**: `Morphemes` = the indices in array order; `RootMorphemeIndex` =
  the position of the `Root` token (no separate field).

**Why this shape (verdict: sound):**

- **Compact / cache-friendly / hashable:** 4 bytes per morph (a 5-morph word = 20 bytes); analyses
  compare and dedupe as plain integer arrays.
- **24-bit ceiling = 16,777,215 morphemes** — ample (largest FLEx projects are ~10⁵–10⁶ entries);
  the compiler asserts `morphemeCount ≤ MaxMorphemeId`.
- **8 bits for the op** is byte-aligned headroom (only ~5 bits used); keep it for growth.

**What it deliberately does NOT carry — keep these as separate optional channels, do not widen the
token:**

- **Surface segmentation** (which input letters belong to which morph): if interlinear morph-breaks
  are needed, the same walk emits a parallel `int[]` of morph start-offsets. The 32-bit token stays
  the pure (op, morpheme) derivation.
- **Specific allomorph** (vs morpheme): an optional second channel; consumers (FieldWorks → LCM)
  key on the morpheme.

Realized now as `MorphToken` / `MorphOp` (the codec + bounds check + root recovery); the FST
compiler (the spike, §6.1) emits these tokens as arc outputs, so the analyzer is structured from
day one rather than a bare recognizer retrofitted later.

## 9. Confirming FST closure — the completeness certificate

An FST analyzer is only trustworthy if its **silence is a proof**: "no accepting path" must mean
"no analysis exists", and "these K paths" must mean "exactly these K analyses" (all homographs,
nothing spurious). That is **completeness**, and it does not come for free — it must be *certified*
per grammar. Completeness has two parts, and the second is the hard one:

1. **No escape applies to the current form.** A local trigger check. Easy.
2. **No FST-able ("normal") step reachable from the input can *create* a form where an escape
   then applies.** This is **feeding** (Kiparsky): rule A feeds rule B if A builds B's
   environment. If a normal step can feed an escape, the compiled automaton — which excluded
   escapes — is **not closed**: a valid derivation exists that it has no path for, so its silence
   is a false "done". Everything rests on ruling this out.

### 9.1 Can closure be guaranteed? Decidably yes for the regular fragment; not universally

The universal question ("can this grammar *ever* reach an escape configuration?") is **undecidable**
in the limit — general rewriting with non-regular escapes in a feeding loop is Turing-complete. So
"guaranteed for any grammar" is impossible. **But for a given grammar it is usually decidable**, and
when the answer is yes the automaton's silence becomes a theorem. Two mechanisms:

- **(a) Decidable feeding-closure (the computable certificate).** Each escape `E` has a *trigger
  set* `T_E` — the configurations where it fires. For a *regular* escape `T_E` is a regular
  language. Each FST-able rule `F` is a regular relation. The question "can `F` ever produce a form
  in `T_E`?" is exactly the **regular-language emptiness test**

  ```
  range(F  restricted to FST-reachable forms)  ∩  T_E   =   ∅ ?
  ```

  which **SIL.Machine's `Fst.Intersect` + a reachable-accepting-state check computes directly**. Run
  it over every (FST-rule `F`, escape `E`) pair:
  - **all intersections empty** ⇒ no normal step can ever feed an escape ⇒ the FST fragment is
    **closed** ⇒ "no escape now, and no path in the automaton" is a *complete certificate* — the
    sufficient "done";
  - **some intersection non-empty** ⇒ feeding is possible: if the fed escape is *regular*, fold it
    into the automaton (Kaplan–Kay, §7-era reasoning) and re-check; if it is *non-regular/opaque*,
    closure cannot be certified and those words must fall to the search backstop.

- **(b) Stratal containment (the practical guarantee).** HC is stratal, and strata *bound* feeding.
  If every escape is confined to a stratum the FST fragment never feeds *into* — e.g.
  reduplication/templatic processes apply innermost, *before* FST-able affixation/phonology — then
  by construction no later normal step can reach them. Verify by checking escape-rule strata against
  FST-rule strata and the (downward) feeding direction. For most real grammars the "funny"
  processes are exactly the innermost ones, so this holds.

### 9.2 The per-grammar verdict

| Situation | Is "no FST form ⇒ done" sufficient? |
|---|---|
| No FST-rule feeds any escape (∩ = ∅), **or** escapes stratally contained upstream | **Yes — provably.** The walk enumerates all paths; absence is a theorem; all homographs surface. |
| FST-rule feeds a **regular** escape | Fold the escape in → row above. |
| FST-rule feeds a **non-regular/opaque** escape | **No.** A valid derivation can hide from the surface; those words go to the bounded search. |

### 9.3 Homographs (positive completeness)

"Found one, are there others?" is the *easy* direction **once closure holds**: the walk returns
**all** accepting paths, never the first only (the spike already shows this — `dat` returns both
lexical entries). A homograph is missed only by (i) **unsafely determinizing/minimizing** and
merging paths — which is exactly why the analyzer stays nondeterministic and never `Minimize`s
unification arcs — or (ii) the compiler not encoding one decomposition (a closure failure), caught
by §9.5.

### 9.4 The search backstop's own "done"

For words that fall out of the FST (uncertifiable feeding to a non-regular escape), completeness
comes from the existing **bounded** search: "done" = all branches within the depth bound explored.
That is sound iff the bound is a *true* upper bound on derivation length — finite exactly when the
rule-interaction graph has **no unbounded self-feeding cycle**. A grammar with such a cycle has no
finite completeness guarantee from anyone (FST or search) and should be flagged.

### 9.5 How we make it sufficient (the work)

- **Static feeding-closure pass** (extends `GrammarFstAdvisor`): build the feeding graph — for each
  FST-able rule and each escape, the `range(F) ∩ T_E` emptiness test via `Fst.Intersect` — and emit
  a per-grammar verdict: **"closed — FST silence is a proof"** vs **"rule X feeds escape Y → those
  words need the search backstop"**, plus the stratal-containment check as a fast pre-filter.
- **Corpus closure verification** (empirical backstop to the static proof): run the FST and the
  sound+complete search engine over a corpus and assert the analysis **sets are identical** (same
  cardinality and members) for every word, including ambiguous ones. Any divergence is a missing or
  spurious path — a closure bug — localized to the offending rule. This converts "closed" from a
  claim into a measured guarantee, and is the gate before an FST analyzer may *replace* (not just
  shadow) the search engine for a grammar.

### 9.6 Phase placement

Closure confirmation is **Phase 3 (Tier-2 hybrid)** in §6: the static feeding-closure pass decides,
per grammar, which words the transducer is complete for and which escape to the search; the corpus
closure verification is the rollout gate. Until it passes for a grammar, the FST runs in
**shadow/verification mode** (alongside the search, asserting set parity), never as the sole
analyzer.

## 10. Completeness under load — the eager/lazy partition knob (designed in from day one)

Eagerly composing the whole grammar into one transducer is fastest to *walk* but the state count is
roughly **multiplicative across composed layers**, so a single high-branching layer (a position
class with hundreds of allomorphs, productive bounded compounding, a large affix inventory) can blow
the automaton up. We must be able to **bound the compiled size without ever sacrificing
completeness**. That requires a tunable partition — and because it changes correctness-adjacent
machinery, it has to be in the architecture from the start, not retrofitted.

### 10.1 Three buckets

Every construct lands in exactly one bucket, and the boundary between the first two is a **knob**:

- **A — Precompiled (eager).** Composed into the static transducer ahead of time. Fastest walk;
  costs states.
- **B — On-the-fly (lazy).** Kept as a separate composable layer and **applied at analysis time by
  on-demand composition** against the partial result. Bounded memory; slower per word. Still
  finite-state, still complete.
- **C — Search / probe fallback.** The non-FS escapes (and any construct whose closure can't be
  certified, §9). The sound backstop.

**What bucket C actually is (sharpened — see §11.3).** C is *not* a wide, murky middle that
"spans" A and B. Formally (Kaplan & Kay) everything concatenative — affixation, derivation,
inflection, ordered phonological rewrite rules — is a **regular relation**, hence A-or-B. The only
genuinely non-regular operations are a short list: **unbounded copying (reduplication)** and
**unbounded recursion** (productive compounding/incorporation with no depth bound), plus the rarer
**bracketing paradox**. So a C construct is a **thin, local, non-regular core wrapped in B on both
sides — `B ∘ C ∘ B`** — not a fog. That thinness is what makes the §11.3 release valves work: a
local core is **detectable and peelable**. Critically, *a construct missing from the FST is not
automatically C* — it is usually just **unbuilt B** (regular, simply not yet enumerated), which is
exactly what the Sena derivation gap turned out to be (§11.2).

The **A↔B boundary is the knob**; **C is fixed by the §9 closure analysis, not the knob**. There is
always a safe floor setting — *everything in B* (nothing precompiled) — which is bounded in memory
and still complete; the knob only interpolates between "fast and big" (more A) and "small and slow"
(more B). The automaton can therefore never be forced to explode: when eager composition would
exceed a **state/memory budget**, the compiler demotes layers A→B until under budget.

### 10.2 Why completeness is *independent of the knob* (the load-bearing guarantee)

This is the property the knob must never break, and it holds for three composing reasons:

1. **Composition is associative.** `(A ∘ B) ∘ rest  ≡  A ∘ (B ∘ rest)`. Precompiling a layer versus
   applying it lazily denotes the **same transduction** — the split point changes *when* the work
   happens, never *which* relation is recognized. So moving a rule from A to B cannot add or drop a
   single analysis.
2. **The walk enumerates all paths in either bucket.** A lazy layer expands *all* its applicable
   arcs on demand (not the first), exactly as a baked-in layer would, so homograph/positive
   completeness (§9.3) is preserved across the split.
3. **Closure (§9) is computed on the full relation `A ∘ B`, not on the precompiled subset.** The
   feeding-closure certificate and the corpus set-parity gate validate the *whole* partition, so
   "no path ⇒ done" stays a proof wherever the knob sits.

Net: the knob is a pure **space/time dial**; the **analysis set is invariant** under it. That is why
it is safe to expose it (even to auto-tune it) without re-proving correctness each time.

### 10.3 The knob's policy — and why it is per-language (yes, it would differ)

The optimal A/B cut is grammar- and corpus-specific. Rank each candidate layer by two measurable
quantities:

- **state-multiplier** — how much it grows the composed automaton (measure by composing it and
  diffing the minimized state count);
- **hotness** — how often a corpus sample actually exercises it.

Precompile (A) the **cheap-and-hot** layers; keep lazy (B) the **expensive-and-cold** ones; demote
A→B in descending cost/benefit until under the state budget. These quantities vary by language: a
language with one rarely-used 200-allomorph class should keep it lazy (precompiling multiplies the
whole automaton ×200 for little corpus payoff), while a language whose hot morphology is a handful of
low-branching affixes should precompile nearly everything. **So the same construct can be A in one
project and B in another** — the partition is a *pluggable policy* (with an optional auto-tuner that
reads the state-multiplier/hotness numbers), not a hard-coded rule.

### 10.4 What "designed in from the beginning" demands

- The compiler is a **pipeline of self-contained composable layers**, each carrying metadata
  (state-multiplier, hotness, closure status), **not** a monolithic "compose everything." 
- Each layer can be realized **either** as composed-in arcs (A) **or** as a lazy applicator (B)
  behind one interface, so moving the knob is a config change, not a rewrite.
- The analyzer walks the **eager core and lazily expands B-layers on demand**, accumulating the same
  `MorphToken` outputs (§8) regardless of bucket.
- A **state/memory budget** is a first-class compile input; exceeding it triggers automatic A→B
  demotion (never a silent truncation — log what was demoted).
- The **corpus set-parity gate (§9.5) runs against the chosen partition**, so any A/B setting that is
  shipped is verified complete before it can replace the search engine.

### 10.5 Phase placement

The layered, lazy-capable compiler and the budget/policy interface are **Phase 1–2 architecture**
(the spike's `FstMorpher` is already structured as discrete composable pieces — lexicon chains +
affix chains — rather than a monolith, which is the seed of this). The auto-tuner and per-project
policy tuning are **Phase 4 (productionize)**. The completeness invariant (§10.2) is an **invariant
checked at every phase**, not a phase of its own.

## 11. Findings from the Sena drive (the corrected picture)

This section records what the actual Sena implementation taught us, *correcting* earlier divergence
analysis that was measured against a broken baseline. Read it before §9/§10 are taken as final.

### 11.1 The measurement bug that invalidated earlier divergence numbers

The benchmark forced `Morpher.MaxUnapplications = 3` on the **search engine used as ground truth**.
But in HC `MaxUnapplications = 0` means **unlimited** (the cap engages only when `> 0`,
`AnalysisStratumRule.cs:144`). Setting it to `3` throttled the reference search down to **0–few
analyses per word**, so every "divergence" the FST showed against it was the FST disagreeing with a
*crippled* oracle — artifacts, not morphology bugs. **Lesson: always run the reference `Morpher`
with `MaxUnapplications = 0` (unlimited) when measuring FST parity.** A `=3` ground truth is
meaningless.

With the corrected (unlimited) oracle:

| corpus | FST template analyzer vs search | speed |
|---|---|---|
| curated 15 words | **IDENTICAL** (sound + complete) | 2.4 vs 177.8 ms/word (**~74×**) |
| broader 60 words | 12 real divergences (below) | 2.9 vs 245.5 ms/word (~85×) |

So the FST approach is **already sound + complete on the regular fraction** it builds; the residual
is coverage and a verification subtlety, not a flaw in the "walk the forest" design.

### 11.2 The two real residuals (neither is bucket C)

The 12 genuine divergences split cleanly, and **both kinds are bucket B, not C**:

- **Over-generation** (FST proposes readings search rejects — e.g. `kulemba` as `INF+[escrever]+IND`,
  `mbalira`, `ndiende`, invalid agreement combos in `akudza`/`aikwata`). These are killed cleanly by
  **verify-discard** (`VerifiedFstAnalyzer` / `FstReplay`): re-synthesize each candidate through the
  proven engine and drop any that does not regenerate the surface. Re-synthesis enforces *every* HC
  constraint at once (category, MPR, co-occurrence, obligatoriness) — so this is the "install all the
  gates" mechanism, and it removed every over-generation in the corpus with no FST-encoded gate.

- **Under-generation** (search has readings the FST never proposes — `aikhane`, `angwera`, `kunduli`,
  `paoneke`, `khalani`, `cidzo`, `ikoyiwe`). **Every one is a derivational suffix the FST build
  omits:** `REC` (reciprocal), `APPLIC` (applicative), `REV` (reversive), `NZR` (nominalizer), `NEU`
  (neuter/stative), `PAS` (passive), `acção`. The build covers the *inflectional* layer (subject/
  object agreement + TAM) but not the *derivational* layer between root and inflection (e.g.
  `[vencer]+REV+NZR`, `[cair]+APPLIC+IND`, `[ser]+REC+NZR`). This is **unbuilt B** — concatenative,
  regular — not a non-regular gap.

  *Build-order wrinkle:* derivation reintroduces the surface-vs-derivation order problem. In
  `kunduli = 10+[vencer]+REV+NZR`, the class-10 *prefix* is licensed only because `NZR` (a later
  suffix) nominalized the stem — a left-to-right surface walk cannot gate that. **Resolution: build
  permissively (propose the derivation paths) and let verify-discard kill the bad combos.** Do not
  attempt to gate derivation order in the walk.

- **Verify false-rejections** (`kubvuna`, `akhaona`, `nyabasa`, `ndalama`): verify-discard dropped
  *valid* analyses it could not re-synthesize. This is **token under-determination** — the
  `(op, morpheme)` token (§8) omits an allomorph or feature needed to regenerate the surface, so the
  replay fails on a legitimate analysis. This — not reduplication — is the real "last nut" for a
  *lossless fast path*, because it makes verify-discard lose true analyses. (The `SoundHybridMorpher`
  fallback variant stays complete by routing any unconfirmable word to full search, at the cost of a
  high fallback rate — 88% here — so it is correct but not yet fast.)

### 11.3 Bucket C in the wild, and the release valves (does it even occur here?)

**In *this* Sena grammar: there is no bucket C.** The grammar file has **0 reduplication rules**
(`grep reduplicat` = 0; all rules are `CopyFromInput` + `InsertSegments`, i.e. ordinary affixation),
the census reports **Tier 1 / FST-CLOSED / 0 escapes**, and compounding is bounded (8 rules). So the
slow path may never need to fire for Sena; the `HybridMorpher` total-reduplication route is a
never-triggered safety net here.

**In general, genuine C does occur** — Bantu verb reduplication (`-famba-famba` "walk around"),
Indonesian/Malay full reduplication (`buku-buku` "books"), Tagalog aspect reduplication, and
bracketing paradoxes (English `un-happi-er`). For those, three resolution paths — and the key
insight that **for copy, detection and parsing are the same local problem** (a reduplicant is an
adjacent repeated substring; detecting it *is* finding the split):

1. **Bounded fold into B (length-cap the copy).** Precompile reduplication for stems up to length
   `N` — finite, therefore regular, therefore pure B. Cost is **linear** in `N×|stems|`, not
   exponential. Stems longer than `N` (vanishingly rare) fall to the backstop. Best when copy shapes
   are few.
2. **Detect-and-peel (compile-replace).** At parse time run a cheap repeat-scan that *proposes*
   candidate reduplicant splits; strip the copy and hand the base to the B-FST; accept any split
   whose base parses and whose copy relation holds. No precompile blow-up, handles unbounded copy,
   and the live work is just the scan + peel — the heavy lifting stays in B. This is the "look for it
   live as well" valve, and the standard finite-state-morphology answer (Beesley & Karttunen's
   `compile-replace`). **Preferred.**
3. **2-way FST.** Replace the 1-way transducer with a two-way one for the reduplicative fragment — it
   re-reads its input, so it *computes* the copy a 1-way FST cannot, while staying finite-state and
   linear-time (Dolatian & Heinz, computing reduplication with 2-way FSTs). Cleanest in theory;
   biggest lift (SIL.Machine's `Fst` is 1-way).

**The A/B/C balance is computable, not guessed.** For each candidate C-feature, build the FST with
and without it folded and measure `Δ|states|`/`Δ|arcs|` (the precompile blow-up), and measure the
corpus frequency of words needing it; fold iff `Δmemory` fits the budget *and* `freq × slow_latency`
saved is worth it. This is the §10.3 knob made quantitative — a knapsack over the state-multiplier
and hotness numbers the layered compiler already exposes.

**Theory load-bearing here** (attributed by idea; verify exact citations before quoting): rewrite
rules compose to regular relations (Kaplan & Kay 1994); reduplication is *the* canonical non-regular
morphological process; 2-way FSTs can compute it (Dolatian & Heinz); subregular locality (Chandlee)
explains why everything else is cheaply finite-state.

### 11.5 Why re-synthesis verification failed — and why it is fixable (the confirmed root cause)

The verify-discard mechanism (§11.2) leaned on `Morpher.GenerateWords` to confirm a candidate by
re-synthesis. A round-trip self-test exposed that **HC's own search analyses do not round-trip
through `GenerateWords`** for derivational/inflected *verb* forms (`aikhane`/`angwera`/`kunduli`/
`ikoyiwe` → all NO), while *noun*/simple forms do (`kulemba`/`mbalira` → OK). A deep probe of
`aikhane` settled the cause — and it is **not** fundamental loss:

- All its morphemes are plain `AffixProcessRule`; the analysis's `RealizationalFeatureStruct` is
  empty (`ANY`). So it is not a realizational-FS reconstruction problem.
- Re-synthesis reproduced the surface under **none** of: all-morphemes-as-rules, non-realizational
  only, empty FS, or the ground-truth FS.
- The grammar has **0 phonological rules**, so it is not opacity.

The real cause is the **two synthesis doors** in `Morpher`:

| Door | Input | Behavior |
|---|---|---|
| `Synthesize` (internal, used by `ParseWord`) | the **rich analysis `Word`** (stripped shape + exact template/slot structure + features, via `LexicalLookup`) | **faithful** — reproduces every valid analysis |
| `GenerateWords` (public convenience) | a **flat bag** of morphemes, re-permuted and applied as **free** morphological rules | **lossy** — re-guesses order/context, bypasses templates |

Confirmed in the grammar: the inflectional affixes (`3P+2`, `SBJV`, …) are **template-slot rules**
(`mrule26+`, inside `<Slot>`), while only compounding/derivation (`mrule1–25`) are free stratum
rules. `GenerateWords` applies the slot rules as free rules — no slot order, no obligatoriness, no
template gating — so feature-dependent verb combinations never synthesize, even from the exact right
morphemes. A simple noun + class-prefix (one slot, no interdependency) happens to survive, which is
why nouns round-trip and verbs do not.

**The under-determination is therefore self-inflicted, not fundamental.** The FST *walk* knows
exactly which template and slots it traversed and in what order — it discarded that when it emitted
the lean `(op, morpheme)` token (§8). The fix is to **preserve the template/slot path the walk took
and verify through HC's faithful door** (`Synthesize`-style, template-aware directed synthesis),
rather than the flat `GenerateWords`. That makes verify both **sound and lossless**: a real
over-generation (e.g. an object marker on an intransitive stem) still fails HC's template-aware
synthesis and is dropped, while a valid verb form now confirms instead of being false-rejected. This
also collapses the 90% `SoundHybridMorpher` fallback (which was driven by false-rejection, not by
genuine over-generation).

### 11.6 The measured corpus picture (200 Sena words, unlimited oracle)

| analyzer | result | speed |
|---|---|---|
| search (oracle) | 480 analyses | 224 ms/word |
| raw FST template+derivation | 49/200 diverge (~24.5%): **~19% over-gen, ~7% under-gen** | 3.5 ms/word (**~64×**) |
| verify-discard (`GenerateWords`) | 48/200 — barely helps (the §11.5 lossy door) | 8.3 ms/word |
| sound fallback | 2/200 — near parity, but **90% fallback** (false-rejection driven) | — |

Reading: completeness is *nearly* there (the derivation layer cut under-gen but ~7% remains —
category-changing derivation, §11.4 Part 2, and prefixal derivation). Over-gen (~19%) is the larger
axis and is what the template-aware verify (§11.5) must remove. The headline speed (~64×) is real;
the open work is making the *verified* path lossless so the fallback rate falls from 90% toward the
true over-gen rate.

### 11.7 Status: correctness essentially done; speed is the one remaining lever

A check of the `SoundHybridMorpher` path on the full 200-word corpus settles where the project is:
**both residual divergences (`miwiri`, `mitemo`) are `extra=[]` — pure under-generation, zero
over-generation.** So:

- **Sound** — the hybrid never emits a wrong analysis (the fallback catches every over-gen). ✓
- **~99% complete** — 198/200 exact set-match; 2 residual under-gen. ✓
- **Not yet fast** — 90% fallback, so no net speedup *yet*. ⚠

Correctness is therefore effectively achieved. The single open axis is **speed**, and it has one
precise lever: the 90% fallback is driven by the **lossy `GenerateWords` verify false-rejecting valid
words** (§11.5), *not* by genuine errors. A lossless verify collapses the fallback toward zero and
unlocks the ~64× the raw FST already shows.

### 11.8 The precise remaining build — a faithful (lossless) verify

`GenerateWords` fails because it re-synthesizes from a **flat, permuted pool of rules**, losing the
**cross-stratum / template-slot ordering** that HC's internal `Synthesize` reads off the rich
analysis `Word`. Confirmed on `aikhane`: stem shape = root citation shape = `ikh` (so it is *not* a
stem-shape problem); its rules `a-5 -e -an` mix template-slot inflection (`a-5`, `-e`) with a
free derivational rule (`-an`, REC) that live in **different strata**, and the flat pool cannot
reconstruct the stratum order. The FST walk, by contrast, *knows* the stratum/template/slot/order it
traversed.

**Caveat (measured):** `GenerateWords(WordAnalysis)` *permutes* the rule order — so it already tries
the correct order — and still fails. So the missing ingredient is **not** merely rule ordering; it is
state the rich analysis `Word` carries (syntactic features established during un-application) that a
from-citation synthesis does not re-establish. That makes a *cheap* faithful verify harder than
"apply the rules in the right order," and points to **two viable routes** (pick by measured payoff):

- **Route A — faithful reconstruction verify.** Reconstruct enough of the rich analysis `Word`
  (root + ordered rules + stratum/template/slot context the walk knows) to drive HC's internal
  `Synthesize` rather than `GenerateWords`. Lossless if the reconstruction is faithful; the open risk
  is whether the analysis-derived syntactic features are reconstructable from the walk's knowledge.
- **Route B — build-time constraint gates (make the FST faithful, no verify).** The over-generation
  is concrete constraints — e.g. an object marker on an intransitive stem is a **subcategorization**
  fact known at build time, hence order-independent and gateable like the existing category gate.
  Encode the few over-gen-causing constraints on the FST arcs so it stops proposing them; then the
  FST is faithful and needs no per-word verify. Cross-slot *feeding* constraints that are genuinely
  not left-to-right gateable route to the search backstop (§9).

Either route ends the same place: `VerifiedFstAnalyzer`/raw FST becomes sound *and* complete with
**near-zero fallback**, at full FST speed.

**Decision: Route A** (chosen). Route B *duplicates* HC's constraint logic as a parallel set of FST
arc-gates that must be kept aligned with the real engine and debugged independently — a second
morphology engine, the anti-pattern this whole design avoids. Route A *reuses* HC: the constraints
stay where they already live and are already correct.

**Route A, sharpened — "directed un-application, then `Synthesize`":** HC parsing is *search
backward* (the slow combinatorial un-application, ~10k clones/word) → *synthesize forward* to confirm
(cheap, ~2.7 ms). The FST replaces only the slow backward search — it already knows the exact path
(root + ordered rules + stratum/template). So the verify should:
1. **Directed un-application** — apply the analysis rules for *only the FST's chosen path* (no search
   breadth) to the surface, producing HC's own rich analysis `Word` (with the syntactic features that
   `GenerateWords`-from-citation never establishes — the §11.8 caveat).
2. **`Synthesize`** that rich `Word` through HC's existing machinery and check it matches the surface.

Faithful by construction (HC's exact pipeline with the FST navigating instead of brute force), and
the cost is ~(rules in the path) × per-rule-apply rather than the full fan-out — the source of the
≥10×. The remaining engineering question is the cleanest way to drive HC's per-rule analysis
un-application from the FST token sequence (the rules are recoverable from the `(op, morpheme)`
tokens via the codec; the analysis-rule objects are `mrule.CompileAnalysisRule`).

**DONE — Route A is implemented and works (the cleanest possible form).** HC's `Morpher` exposes
settable `LexEntrySelector`/`RuleSelector` (default `=> true`), checked at every analysis *and*
synthesis step. So the verify never reconstructs anything: it simply runs HC's own `AnalyzeWord` with
those selectors **pinned to the candidate's root and rules**, which prunes the combinatorial fan-out
to the single path the FST found. A candidate is valid iff it appears in that restricted result
(restriction can only remove paths, never fabricate one — HC still runs full synthesis + surface
match). Implemented in `FstReplay.Reproduces`; `VerifiedFstAnalyzer` keeps confirmed candidates and
discards the rest. **Measured (200 Sena words, unlimited oracle): verify-discard went from 48 → 14
divergences (186/200 set-match) at 15.6 ms/word vs 234 ms/word oracle (~15×), with ALL
over-generation removed and zero false-rejection (lossless).** The 14 residual are pure
under-generation. This is the thin wrapper the design wanted — HC's real engine, navigated by the
FST, no reimplemented constraints.

**Feasibility confirmed (why this works where `GenerateWords` fails).** `AnalysisAffixTemplateRule`
unifies the template's `RequiredSyntacticFeatureStruct` and **writes it onto the word**
(`outWord.SyntacticFeatureStruct.Add(fs)`, plus each slot rule's analysis sets its features). That
populated `SyntacticFeatureStruct` is the precondition the inflectional rules check during synthesis
— and is precisely what a from-citation `GenerateWords` never establishes (root citation form carries
bare features), which is why even the correct rule order fails there. Directed un-application calls
those *same* `CompileAnalysisRule` objects along the FST's path, so it reconstructs the populated
`SyntacticFeatureStruct` for free, then `Synthesize` succeeds. Reuse, not reimplementation. The build
applies the FST path's analysis rules (template + free derivation) to the surface `Word` — bounded by
the path, not the full search — yielding rich analysis `Word`(s) to hand to the existing `Synthesize`.

### 11.4 The path to a full solution (what "done" means for Sena)

1. ✅ **Re-validated the gates** built against the broken oracle: the `mbale` obligatoriness gate is
   still load-bearing under the unlimited oracle (5→4 divergences); the category gate is faithful
   build-time logic.
2. ✅ **Built the derivation layer** into the FST (§11.2 under-gen largely closed — `aikhane`/
   `angwera`/`paoneke`/`ikoyiwe` now proposed).
3. ✅ **Faithful (lossless) verify** (§11.8) — done via restricted re-analysis; sound + lossless at
   ~15×, no fallback. `verify-discard` = 186/200 set-match (was 151 raw / 152 old verify).
4. ✅ **Category-changing derivation** — `DerivableToCategory` attaches a template over a derived
   stem of its output category (verb + `NZR` → noun + class prefix), closing `kunduli`/`cidzo`/
   `khalani`. Took `verify-discard` from 14 → **6** divergences (194/200 set-match).
5. ⬜ **The last 6 (diverse proposer gaps, diminishing returns)** — all pure under-gen, all in the
   *proposer*: **prefixal derivation** (`nyari` = `nominalizador`-prefix + `[ser]`; `cawo` associative),
   **depth-3 derivation** (`miwiri` = `[ter]+PAS+APPLIC+NZR`; depth 3 gains it but ~2× verify cost, so
   left to the backstop), and **copula/TAM** constructions (`ndico`/`ndimwe` = `é+[ele]`/`é+[vós]`;
   `kuumadi` = `INF+…+IND+EVID`). Each is a small proposer-coverage item; a prefixal derivation layer
   (mirror of the suffix layer) would close the first two.
6. **Target metric:** FST analyses == search analyses (set parity), at ≥10×. **Achieved: sound ✓,
   lossless verify ✓, ~13× ✓ (17.2 ms/word vs 237 ms oracle), no fallback ✓, 194/200 (97%)
   set-match.** The last 6 are diverse proposer coverage gaps, not a verify or soundness issue.

### 11.9 Metric correctness and two productionization caveats

**The parity signature was sharpened (important).** It was `join(morpheme.Id) + ":" + rootIndex`, but
affix `Morpheme.Id` is empty in this grammar, so it encoded only *(morpheme count, root position)* —
collapsing distinct affixes of the same shape (e.g. subject markers `3P+2` / `3S+1` / `6`) into one
key and hiding same-shape under-generation. Replaced with **per-morpheme object identity** (both
analyzers reference the same `Morpheme` instances from the `Language`, so it is a faithful shared
discriminator). Under the strict signature the raw-FST divergences rose 44 → 90 (shape-parity *had*
been hiding raw over-gen), but **`verify-discard` stayed at 6 (194/200), all pure under-gen** — i.e.
the verify result is robust to the metric and the soundness/lossless claim is real, not a shape
artifact. `FstReplay`'s candidate-match signature was sharpened the same way.

**Caveat 1 — the verify mutates shared `Morpher` selectors (thread-safety).** `FstReplay` sets
`LexEntrySelector`/`RuleSelector` on the morpher with try/finally restore — correct sequentially, but
two words verified concurrently on one morpher would race the selectors. Since a core motivation is
lifting the parallel ceiling, production must give the verify a **per-thread morpher or a morpher
pool** (the analysis FST walk itself is allocation-light and parallel-friendly; only the verify step
carries this constraint).

**Caveat 2 — the ~13× is vs the unlimited-unapplication oracle** (`MaxUnapplications=0`, 237 ms/word).
That is the sound+complete baseline (the only correct one — §11.1), and is what the FST must match.
If production HC runs a *bounded* cap for speed, it trades completeness for time, so the real-world
multiple against that configuration should be sanity-checked separately before quoting a single
headline number.

## 12. The completeness certificate — a grammar-level proof (not per-word)

Completeness is not a per-word heuristic; it is a **property of the grammar's rule structure**,
certified once. The contract is two exhaustive enumerators joined at a cut no derivation can cross:

- **Side B (precompute / FST) is complete** because the regular sub-relation is a *finite automaton*:
  by Myhill–Nerode it has finitely many states, and walking **all** accepting paths enumerates **all**
  analyses — "enumerated absolutely everything," mechanically. (Never `Minimize` underspecified-feature
  arcs: that merges distinct paths and destroys the guarantee — §9.3.)
- **Side A (live) is complete** iff (1) it tries *every applicable rule* at each node (HC's `RuleBatch`
  does), and (2) a **well-founded measure** strictly decreases each step (un-application shortens the
  surface; or a stratum/depth bound), so the finite search tree is fully visited. This is "I check
  these N things, then I'm done."

### 12.1 Why two complete halves can still miss — and the cut that fixes it

If a derivation **weaves** across the boundary (`A→B→A→B`), B enumerates only B-internal paths and A
only A-internal paths, so the interleaving is **silently missed** even though each half is complete.
The fix is a **clean directed cut**: every feeding edge crosses the boundary in *one* direction. Inner
morphology feeds outer (the inner stem is what an outer affix attaches to), never the reverse — so put
**A = inner, B = outer**. Then every derivation factors uniquely as `(A-core) ∘ (B-shell)`: analysis
peels the B-shell with the FST (all ways) and hands each residual stem to A (all ways); the composition
is provably the whole analysis set. No weaving ⇒ no gap.

### 12.2 The graph theory of a valid cut

Model the grammar as a **feeding graph** `G` (nodes = rule/construct classes; edge `r→s` iff `r` can
create the environment `s` needs — Kiparsky feeding).

1. Condense strongly-connected components (Tarjan) → a DAG of SCCs (an SCC = mutually-feeding rules,
   i.e. a potential cycle).
2. A **valid cut** is a downward-closed set in the DAG's topological order — a *topological separator*
   with all cross-edges pointing `A→B`. (HC's strata are a hand-built such stratification.)
3. Two further obligations: the **B-side relation must be regular** (Kaplan–Kay: concatenation +
   ordered rewrite = regular), and every **SCC kept in A must be well-founded** (no unbounded-growth
   cycle — bounded copy ok, unbounded copy not).

A grammar admitting such a cut with B regular and A well-founded has, by construction,
`A-complete ∧ B-complete ⇒ whole-complete`. **This is the certificate, computed on the grammar.**

### 12.3 The construct-coverage half (why "FST-closed" is necessary but not sufficient)

`GrammarFstClosure` / the census already certify the *regularity / no-escape* half (the B-side relation
is regular; for Sena, 0 escapes). That is necessary but **not** sufficient: the FST must also actually
**enumerate every construct on the B-side**. A regular construct the builder never emits is a
*hole inside B* — a silent under-generation, not a boundary problem. So the certificate has two
mechanical checks:

- **Closure** — the B-side is regular / no un-handled escape (existing `GrammarFstClosure`).
- **Coverage** — every grammar construct on the B-side (every affix rule in a template slot or as a
  standalone morphological rule, every compounding rule, every root) is represented on some FST arc.

`Closure ∧ Coverage` over the cut ⇒ the FST enumerates the entire B-relation ⇒ **complete for every
word** with no per-word check. If coverage fails, the certificate **names the uncovered constructs**
and the build is *flagged* (those derivations route to the proven engine) — never a silent miss.

### 12.4 Sena under the certificate

Census: 0 escapes, 0 reduplication, 0 phonological rules → the entire feeding graph is regular, with no
non-regular SCC. So the unique maximal valid cut is **A = ∅, B = everything**: Sena is provably
completable *entirely* in the FST, with **no live side needed**. The residual divergences are therefore
not a cut/soundness issue — they are **coverage holes** (constructs the builder omits: prefixal
derivation, depth-3 derivation chains, copula/compounding). The certificate's job is to (a) confirm
`A = ∅` and (b) list exactly those holes, turning "97% empirically" into "complete once coverage = 100%,
and known-incomplete-where-flagged until then."

### 12.5 Why this does not balloon (size rationale)

B is an **automaton with shared structure**, not a stored list of words: size ≈ `|lexicon trie| +
|affix inventory × template structure|` — **additive**, not the multiplicative `|roots| × |affix
combinations|` of a materialized word list. Measured on Sena: **50,673 states from 1,463 root
allomorphs + 24 templates**, sub-second build, a few MB. "Enumerate everything" means *walk all paths
at parse time*, not *materialize the cross-product at build time*. The genuine blow-up risks — eager
composition+determinization across layers, high-branching position classes, productive deep
compounding/reduplication — are bounded by the **§10 eager/lazy partition knob + state budget**, which
auto-demotes expensive layers from precompiled (A-eager) to on-the-fly (B-lazy). **Completeness is
invariant under the knob** (composition associativity: precompiling vs applying lazily denote the same
relation), so the size dial never drops an analysis; worst case "everything lazy" is bounded memory,
slower per word, still provably complete.

### 12.6 Implementation and proof (built + stress-tested)

Implemented:
- `FstCompletenessCertificate.Certify(language, codec)` → `FstCompletenessReport`: the closure half
  (`GrammarFstClosure`) + the coverage half (every affix rule emitted by the FST, read from the codec's
  covered-morpheme set), plus the compounding-rule count. `IsCertified` = closed ∧ all affixes covered
  ∧ no compounding. It **names the uncovered constructs** when it fails.
- `FstTemplateAnalyzer.CoversAnalysis(WordAnalysis)`: the sound structural predicate of what the FST
  provably enumerates — single root (no compounding), every morpheme covered, ≤ `DerivDepth`
  derivational affixes per side, **and the canonical morph order** `[infl-prefix][deriv-prefix][root]
  [deriv-suffix][infl-suffix]`. (The stress test forced each of these: depth, compounding, and order
  were all discovered as required constraints by analyses that broke a weaker predicate.)
- `CompleteHybridMorpher`: the provably-complete analyzer. Certified grammar → the fast verified FST
  (complete by §12.3); else → the search engine (complete; the known slow path). Completeness is by
  construction, decided by the grammar-level certificate — **no per-word heuristic.**

**Certification is the EMPIRICAL set-parity gate, not the static coverage check.** A first attempt
made `IsCertified` = closed ∧ all-affixes-covered ∧ no-compounding. A stress test exposed this as
**unsound**: `cawo = coisa + d'eles` has every morpheme covered yet the FST cannot build it (a prefix
on a pronoun root that takes no template), so a grammar could pass the static check and still silently
drop `cawo`-type words — precisely the forbidden failure. Rule/symbol coverage is **necessary, not
sufficient**; completeness is about *paths (attachments)*, not symbols present. So the static check is
demoted to a fast **pre-filter / gap-namer** (`PreFilterPasses`), and the real gate is
`FstCompletenessCertificate.CertifyEmpirically` — **FST analyses == search analyses (morpheme-identity
set parity) over a representative corpus** (§9.5). It is path-level, so it catches `cawo`.

**Proof (stress test `Prove_CertificateCompleteness`, 200 hard Sena words, unlimited oracle):**
- *FST path tested directly* (non-vacuous): the FST itself produces **467/480** search analyses; 13 it
  misses route to the engine.
- *Static check shown unsound*: **1** analysis (`cawo`) is "in-class" by the static predicate yet
  missed by the FST — the concrete witness that coverage ⇏ completeness.
- *Empirical gate*: Sena is **NOT certified** (5 divergent words), so `CompleteHybridMorpher` routes to
  the engine; **complete-system misses = 0** — every true analysis is returned.

**What the stress test taught (the key result).** A *predictive per-analysis* coverage predicate is
whack-a-mole (it broke on derivation depth, then morph order, then the template-less prefix `cawo`),
and even grammar-level *symbol* coverage is unsound. **Soundness rests on the empirical set-parity gate
+ engine backstop**, not any static predicate: certified (set parity holds) ⇒ FST-only is evidence-
backed complete; uncertified ⇒ the engine guarantees completeness, and the gate names exactly which
words still diverge. The system is **100% complete today** (0 misses, via the engine for the 5
divergent words), and the path to FST-only speed is to drive those divergences to 0 (build the 3
remaining prefixes, compounding, deeper derivation, template-less prefixation) until the grammar
certifies — never at the cost of a silent miss.

## 13. The two-path caching analyzer (fast + slow, the shipped front end)

The FST fast path is **sound but not guaranteed complete** — it answers *"does this have at least one
FST-findable valid analysis?"* (a trustworthy *yes*-detector for "is this a word", never the complete
analysis set, and able to false-negative on words whose only readings use un-built constructs, e.g. a
pure compound). On its own that is not safe for a consumer that needs all readings. The shipped design
pairs it with the proven engine behind a cache:

- **Slow path = truth, cached.** HC's search engine is complete; its result per word is stored in
  `AnalysisCache`. For a fixed corpus the cache is **warmed** (in the background, in parallel) until
  every word has its complete analysis — after which queries are fast *and* complete.
- **Fast path = immediate, provisional.** The verified FST answers instantly on a cache miss; its
  result is flagged provisional (`FastAnalysisResult.IsComplete == false`).
- **Default is guaranteed (backwards-compatible).** `CachingMorphologicalAnalyzer.AnalyzeWord` returns
  the cached complete analyses, or computes them with the engine on a miss and caches them. Existing
  callers get the same analyses as before — faster once warm, never wrong.
- **Fast is opt-in.** `AnalyzeWordFast` returns the cached complete set if warm, else the provisional
  FST result, and never runs the slow engine. Applications (FieldWorks) can show the fast result now
  and the authoritative result once cached, querying both.
- **Persistence (fixed corpora across sessions).** `AnalysisCacheSerializer` writes/reads the cache as
  text, keying morphemes by `MorphemeRegistry` (a deterministic morpheme↔key map rebuilt from the
  grammar) and guarding with a **grammar-version** string — a cache built against a different grammar
  is rejected, forcing a re-warm (the one way this design could otherwise serve stale, unsound
  analyses). Confirmed non-words (empty analysis) are cached too, so they are not recomputed.

Net: correctness equals the engine (the cache never invents or hides an analysis), the FST removes the
cold-start latency, and a warmed fixed corpus resolves every word fast and complete. The FST's
incompleteness — including the "is this a word" false-negative — is corrected the moment a word's
complete analysis lands in the cache.
