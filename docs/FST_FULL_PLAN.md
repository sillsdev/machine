# FST_FULL_PLAN — closing the coverage gap (phonology, infixation, reduplication)

Implementation plan for expanding the propose-and-verify FST accelerator to cover **all attested
phonology**, **all infixation**, and **bounded reduplication**. Companion to
`FST_FULL_COVERAGE_PLAN.md` (the construct audit) and `HERMITCRAB_FST_PLAN.md` (the spine design).

## The principle that makes this safe

The propose-and-verify split puts **all correctness in verify + certification, none in the proposer**.
The proposer's only job is to emit a *sound superset* of candidates fast; `VerifiedFstAnalyzer` re-runs
HC (real analysis + synthesis, real phonology) on each candidate and discards any HC does not confirm;
the empirical parity gate (`FstVerification.Compare`) certifies a grammar only when FST≡engine on the
corpus.

Consequence: **expansion can never produce a wrong answer.** A new candidate generator that
under-generates simply accelerates fewer words (parity gate → engine fallback); one that over-generates
has its junk pruned by verify. Correctness is invariant; only the *acceleration ratio* moves. So we can
add coverage aggressively.

This reframes "can an FST represent X?" into **"can we cheaply enumerate a superset of candidates for X
that verify then prunes?"** — which decouples coverage from FST-representability and lets non-regular
constructs (full reduplication) be handled *beside* the FST by bounded generators feeding the same gate.

## Architecture: a composite of candidate generators

```
                    ┌─────────────────────────────────────────┐
   surface word ───▶│ CompositeProposer (union + dedup)         │
                    │   ├─ FstTemplateAnalyzer  (regular bulk)  │
                    │   ├─ ReduplicationProposer (strip + recurse)
                    │   └─ InfixProposer        (remove + recurse)
                    └───────────────┬───────────────────────────┘
                                    │  candidate (root+rules) sets
                                    ▼
                    ┌─────────────────────────────────────────┐
                    │ VerifiedFstAnalyzer  (FstReplay verify)   │  ── discards anything HC won't confirm
                    └───────────────┬───────────────────────────┘
                                    ▼  genuine HC analyses
```

`VerifiedFstAnalyzer` already wraps an `IMorphologicalAnalyzer` proposer, so the only new plumbing is a
`CompositeProposer : IMorphologicalAnalyzer` that unions + dedups candidates from several generators.

**Three invariants every generator must respect** (learned before building, not after):

1. **Recurse the residual through the FST proposer — never propose a flat root.** A reduplicated or
   infixed surface can have an *inflected/affixed* base: `"wakaswakas"` is REDUP of inflected `"wakas"`,
   not bare `"waka"`. So a generator strips/removes its own material, then calls the FST proposer on the
   remainder, and wraps each returned analysis with its morpheme. Terminates: the residual is strictly
   shorter, reduplication bounded to 1–2 copies, infixation to 1 site per pass.
2. **Dedup before verify.** Two generators (or a generator and the FST) can propose the same morpheme
   set → verify would confirm it twice → duplicate analyses. `CompositeProposer` dedups by candidate
   signature before the gate.
3. **The coverage signal must reflect the composite.** `FstTemplateAnalyzer.CoversAllConstructs` trips
   `false` on a redup/infix slot. Once a sibling generator covers that construct, certification must see
   the *composite's* coverage, not just the FST's — else the grammar won't certify and the now-covered
   words stay on the engine. The parity gate keeps results correct regardless; this only governs whether
   acceleration kicks in.

---

## Point 2 — Infixation (regular; in-scope)

Infixation splits the root and inserts the affix inside it (Tagalog `-um-`: sulat → s‹um›ulat). It is a
regular operation; the proposer already *recognizes* infix slots (`MorphTokenCodec.ClassifyOp → Infix`)
but skips them (`_hasUnbuiltConstructs = true`).

**Generator (`InfixProposer`).** For each infix rule and each candidate insertion site in the surface:
remove the infix's surface segments at that site, recurse the remainder through the FST proposer, wrap
each analysis with the infix morpheme. Sound-superset shortcut: try every segment boundary the rule's
partition pattern allows (or over-approximate to all boundaries) — verify prunes the wrong splits.
`O(surface-length × infixes)` candidates — bounded. Composed with surface-precompile it also handles
infixes that trigger phonology.

**Soundness.** Verify re-synthesizes `base + infix` and surface-matches; a wrong split won't confirm.
**Test.** A grammar with one infix rule; show the FST alone misses the infixed surface, `InfixProposer`
covers it, verify rejects a non-word.

---

## Point 3 — Reduplication (non-regular; handled beside the FST)

Full reduplication (copy the whole base, `ww`) is the one provably non-regular construct — an FST cannot
represent it. It doesn't need to: a bounded **string-repetition scanner** contributes candidates to the
same verify gate.

**Generator (`ReduplicationProposer`).** Scan the surface for an adjacent repeated substring matching a
reduplication template (full-copy `XX`; partial CV-copy as a later refinement). For each detected
repetition: strip one copy, recurse the remainder through the FST proposer, wrap each analysis with the
reduplication morpheme. **Bound to 1–2 applications** (the "once or twice") — finite, tiny candidate set.
`O(n²)` scan per word, trivial.

**Soundness.** A coincidental repeat (`"murmur"` that is not actually reduplicated) is proposed but
pruned because HC synthesis of `base + REDUP` won't reproduce it. **"Well enough for 99.9%":** the 1–2
bound covers essentially all attested reduplication; triple/unboundedly-interacting reduplication
doesn't certify and rides the engine (still correct).
**Test.** A grammar with a full-reduplication rule; show the FST alone misses `"wakawaka"`, the composite
covers it (including an inflected reduplicant via the recursion), verify rejects a non-reduplicated word.

---

## Point 1 — All phonology: affix surface-precompile + C-boundary (in-scope, incremental)

The shipped C-internal tier handles **bare-root** alternation via `GenerateWords`. Two extensions:

**1a. Affix surface-precompile.** Build affix arcs from each affix allomorph's *surface* segments, not
only the underlying `InsertSegments`. Forward-application helper: compile the stratum's
`PhonologicalRules` via `prule.CompileSynthesisRule(morpher)` into a `LinearRuleCascade` (exactly what
`SynthesisStratumRule._prulesRule` does), wrap the affix segments in a `Word`, `Apply`, read the surface
shape(s). An affix's surface depends on stem context, so this is fiddlier than the bare-root case —
**validate on one minimal affix-triggered alternation first**, then generalize.

**1b. C-boundary context.** Over-approximate the neighbor: apply rules with each natural-class boundary
segment on each side, so boundary-conditioned variants (assimilation across a seam) are included. Bound
the variant count per morpheme (cap + drop-to-underlying fallback) so a long-distance harmony grammar
degrades rather than explodes.

**Soundness.** Underlying arcs are kept (union), so the 0-phonology path is unchanged; the token is
always the underlying morpheme; verify confirms with real phonology; a missed variant shows up as
FST≠engine → no certify → engine (never wrong).
**Test.** A rewrite rule altering an affix's surface; show the underlying-only proposer misses it, the
surface-precompile proposer covers it, verify stays sound.

### Result (shipped — 1a affix surface-precompile, C-internal tier)

`SurfacePhonology.Variants(underlying)` compiles each stratum's synthesis phonological rules (reusing
HC's `IPhonologicalRule.CompileSynthesisRule`, exactly what `SynthesisStratumRule` runs) and applies
them to a segment string in isolation, returning the distinct surface forms (always including the
underlying). `FstTemplateAnalyzer.BuildAffixArcs` builds the affix's segment arcs from the underlying
form AND each altered surface variant (shared by both affix-arc sites: derivational layers and template
slots); the default ctor passes an identity variant function so the 0-phonology path is byte-identical.

Verified by `Proposer_CoversPhonologicallyAlteredAffix` (a suffix inserts "t"; an unconditional t→d
rule makes it surface only as "d", so sag+SUF = "sagt" → "sagd"; the underlying-only proposer builds a
"t" arc and misses "sagd", the surface-precompile proposer builds the "d" arc and verify confirms it)
and `SurfacePhonology_AppliesRulesForwardToASegmentString`. Full suite green (101).

### Result (shipped — 1b C-boundary)

`SurfacePhonology.Variants` now also probes each surface-alphabet segment as a left/right neighbor: it
forward-applies phonology to `neighbor·morpheme` / `morpheme·neighbor` and, when the rule is
length-preserving (output node count = morpheme + 1), reads back the morpheme's own surface nodes.
Bounded by alphabet size × 2; a length-changing context is skipped (no reliable portion) so it stays a
sound superset. This catches an affix whose *own* surface is conditioned by a neighbor across the seam
(e.g. a suffix that voices after the root-final segment). Verified by
`SurfacePhonology_BoundaryTier_RecoversAffixSurfaceFromNeighborContext` (a "t" suffix that voices to "d"
only after "g": isolation keeps "t", the boundary tier recovers "d"). Full suite green (104).

What the precompile still cannot see — a *neighbor's* surface changing (e.g. a root devoicing before an
affix) or any longer-distance interaction — is covered completely by Point 4 below.

---

## Point 4 — C-exact: full phonology via composition with HC's phonology inverse (shipped)

**Goal.** Cover *all* bounded phonology — including the cross-boundary, opaque, stem-conditioned
interactions the per-morpheme precompile (Point 1) cannot see.

**What shipped.** `ComposedPhonologyProposer` composes **HC's phonology inverse with the morphotactic
FST**: it un-applies the grammar's phonological rules to the surface — reusing each stratum's
`IPhonologicalRule.CompileAnalysisRule`, exactly the rules `AnalysisStratumRule` runs (strata
surface→inner, rules reversed within a stratum) — to recover the underlying form, then walks the
underlying-arc FST on it (`FstTemplateAnalyzer.AnalyzeShape`). That is literally phonology⁻¹ ∘
morphotactics. Because the inverse is applied to the *assembled* surface, it sees cross-boundary context
the per-morpheme tiers cannot. The un-applied shape carries under-specified nodes (analysis is
non-deterministic) which the unification walk matches against every compatible arc; verify prunes the
spurious ones, so it is a sound superset. Complete for bounded (non-cyclic) phonology; an unbounded
self-feeding cycle is not a regular relation and simply will not certify.

**Why this form, not FST∘FST composition.** The morphotactic proposer accumulates tokens in a side-table
(`_tokenOnEntry`), not transducer outputs, so a literal `Fst.Compose` would require re-architecting the
spine. Composing HC's *existing* phonology inverse instead reuses the engine's real, tested phonology
(no reimplementation) and reaches the same coverage. Wired into `CompositeProposer.ForLanguage` (inert
when the grammar has no phonological rules — it short-circuits). Verified by
`ComposedPhonology_CoversCrossBoundaryAlternation_WherePrecompileMisses` (a root-final "g"
devoices to "k" before a suffixal "t" — "sag"+SUF = "sagt" → "sakt"; the per-morpheme precompile misses
"sakt", composition recovers it, verify confirms, a non-word still yields nothing).

---

## Order of work & status

1. ☑ `CompositeProposer` plumbing (union + dedup + coverage-signal) — established with reduplication.
2. ☑ Point 3 Reduplication (full-copy generator; strip + recurse + verify).
3. ☑ Point 2 Infixation (remove + recurse + verify; single-contiguous-infix first cut).
4. ☑ Point 1 phonology precompile — bare-root C-internal, affix C-internal (1a) and C-boundary (1b).
5. ☑ Point 4 C-exact — `ComposedPhonologyProposer` (phonology⁻¹ ∘ morphotactics); covers all bounded
   phonology including cross-boundary.

All four wired into `CompositeProposer.ForLanguage`, which both production factories
(`CompleteHybridMorpher`, `CachingMorphologicalAnalyzer`) build and certify on. Commit + test after each
point; each construct test shows (a) the FST alone misses it, (b) the composite covers it, (c) verify
still rejects a non-word.

## Summary of what shipped

| Construct | Coverage | Mechanism | Residual |
|---|---|---|---|
| Bare-root phonology | C-internal | `BareRootSurfaces` (GenerateWords) + verify-allows-phonology | — |
| Affix phonology | C-internal + C-boundary | `SurfacePhonology` (1a isolation + 1b neighbor) + `BuildAffixArcs` | — |
| **All phonology** (incl. cross-boundary, opaque) | **complete (bounded)** | `ComposedPhonologyProposer` — phonology⁻¹ ∘ morphotactics | unbounded self-feeding cycle (not regular) |
| Infixation | single contiguous infix | `InfixProposer` (remove + recurse) | templatic multi-slot; phonologically-altered infix surface |
| Reduplication | full copy, one application | `ReduplicationProposer` (strip + recurse) | partial/CV copy; 2+ applications |

The phonology precompile tiers (1a/1b) are the cheap fast-path; `ComposedPhonologyProposer` is the
complete backstop, so phonology is fully covered. The remaining infix/reduplication residuals are
covered correctly today by the engine via the parity gate — the only thing not yet accelerated for those
narrow cases is *speed*, never correctness.

## Production wiring

Both factories — `CompleteHybridMorpher.FromLanguage` and `CachingMorphologicalAnalyzer.FromLanguage` —
build `CompositeProposer.ForLanguage(language, fst)` (the FST plus the reduplication and infix
generators) and certify on the *composite's* `CoversAllConstructs`. For a grammar with no
reduplication/infixation the generators hold no rules and yield nothing, so this is near-zero overhead
and byte-identical behavior; for a reduplicating/infixing grammar it is what lets the grammar certify
(the generator covers the construct the FST skips) instead of falling entirely to the engine. Covered by
`CompleteHybrid_WiresGenerators_ReduplicatingGrammarCertifiesAndMatchesEngine`.

**Certification caveat (extended).** A certified grammar skips the engine entirely, so correctness on
unseen words rests on the proposer being complete on the certification corpus. With the generators wired
this now extends to reduplication/infix completeness as well — same empirical-certification property as
before, just over a larger construct set. Choose a certification corpus that exercises the grammar's
reduplication/infix patterns.
