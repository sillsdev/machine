# HermitCrab FST acceleration — plan

Goal: replace HC's combinatorial un-application *search* (measured ~10,000 `Word` clones/word,
397 MB/word, the cause of the ~3× parallel ceiling) with a precompiled **transducer walk** for
the finite-state fraction of a grammar — while **degrading gracefully** to the existing engine
for the parts that aren't finite-state. The grammar census (see `sound-memorization-hc.md` §8)
showed the real Sena grammar is **~100% FST-able** (0 rewrite rules, 0 variables, 0 productive
reduplication, all-concatenative affixation), so for Sena-like grammars an automaton walk could
be 10–100× and near-zero-allocation (which also lifts the thread ceiling).

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
| State/alphabet blowup | Minimize after compose; measure state count; cap + Tier-3 fallback if it explodes |
| Tier-2 "is the FST complete for this word?" detector unsound → missed analyses | Make it conservative (fall back when unsure); verification mode catches misses in rollout |
| Generator (synthesis) direction | Same transducer inverts; or keep HC synthesis initially and only FST-accelerate analysis |
| Grammar-specificity | The census decides the tier per grammar; production grammars must be censused before enabling Tier 1/2 |

## 6. Phased plan

1. **Spike (decisive):** compile Sena's lexicon ∘ concatenative-affixes into one transducer via
   `Fst.Compose`/`Minimize`; build a minimal `FstMorpher.AnalyzeWord`; **verify** its analyses
   equal `Morpher.AnalyzeWord` on the Sena corpus (signature comparison); **measure** clones (→~0),
   allocation, and wall-time vs. the search engine. This proves or kills the SIL.Machine-FST stack.
2. **Complete Tier 1:** add templates, environment-allomorphy, bounded compounding; full Sena
   parity + the parallel-scaling re-measurement (expect the 8-thread/3× ceiling to lift, since the
   walk barely allocates).
3. **Tier 2 hybrid:** census-driven escape arcs + per-word fallback detection + verification mode.
4. **Generator + productionize:** reverse direction, the `IMorphologicalAnalyzer` wiring, and a
   FieldWorks adapter; run the census on real production grammars to set each project's tier.

## 7. Decision gate

Step 1 (the spike) is the gate: it answers, with numbers, whether SIL.Machine's FST can compose
a real grammar correctly and how big the speedup is. If yes → proceed; if `Fst.Compose` can't
handle it → reassess (flatten to symbols, or external lib). Everything past Step 1 is contingent
on that result.
