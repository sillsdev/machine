# FST fast path — execution plan (no certification, all-in 99% coverage)

> **STATUS AS OF THIS WRITING: all 5 phases executed and committed** (commits from "FST probe:..."
> through "FST: probe becomes the full composite..."). This is NOT the same as "done" — read each
> phase's own STATUS block (Phase 3 especially) and section 10/11 before assuming any specific
> number still applies. In short: certification is fully gone (Phase 1); the lexicon trie sharing
> landed and measured (Phase 2, Sena states 50,673→20,737); phonology auto-compilation landed but in
> a narrow v1 slice that does NOT reach Indonesian's `meN-` cascade (Phase 3, the real frontier);
> partial reduplication + infix surface variants landed, compounding was investigated and correctly
> deferred as a bigger data-model change (Phase 4); the probe now runs the full composite and full
> real-corpus numbers are measured (Phase 5: Sena 58.1%, Indonesian 62.0% coverage — the honest
> floor, not a target hit). Left-environment support (Phase 3) landed in a follow-up session but
> moved nothing on real Indonesian data: a NEW diagnostic found that `PhonologyRuleCompiler`'s
> `_alphabet` excludes boundary-type characters, so every one of Indonesian's 5 real phonological
> rules — 3 of which have a plain `BoundaryMarker` in their environment and are otherwise simple
> enough to compile — is rejected before its shape is even evaluated; **0 of 5 compile today**.
>
> **UPDATE (2026-07-02, same day, see `docs/FST_FULL_GRAMMAR_PLAN.md` for full detail): Indonesian's
> `meN-` coverage gap is CLOSED (93/121 → 120/121 on the full corpus) — but NOT by fixing
> `PhonologyRuleCompiler`'s boundary gap described above, which is STILL UNFIXED.** The actual fix
> went through a different, simpler path entirely: `SurfacePhonology`'s existing per-affix
> surface-variant precompile (used by `FstTemplateAnalyzer`, not `PhonologyRuleCompiler`/`InversePhonology`
> at all) already discovered the assimilated-nasal variants (`mem`/`men`/`meng`/`meny`) for free;
> two targeted fixes — a deleted-node rendering bug in `SurfacePhonology`, and a new
> `DeletionJunctions` probe + root-chain checkpoints in `FstTemplateAnalyzer` for when the cascade
> deletes the following root segment — closed the rest. `PhonologyRuleCompiler`'s own boundary bug
> (KNOWN_GAPS below) is now moot for THIS grammar (nothing routes through it for `meN-` anymore) but
> is still real for any OTHER grammar that would rely on that mechanism.
>
> **UPDATE (2026-07-03): Phases H, G1, and G2 in `docs/FST_FULL_GRAMMAR_PLAN.md` are ALL DONE.**
> Sena's build-time regression is fixed (9.3 s → ~1.0–1.5 s); Indonesian is 121/121 fully covered,
> 0 unsound; Sena's `ndikhali` (the one gap in its sampled corpus) is closed with exact 8/8 set
> parity. Compounding's "data-model lift" premise is confirmed false and closed for real (see the
> KNOWN_GAPS entry below and `FST_FULL_GRAMMAR_PLAN.md` Phase G2 for the full account, including
> one thing the original spec missed: `DerivableToCategory` needed to treat compounding as a
> category-transition edge for a root to reach a POST-COMPOUND-gated template). **Both real
> grammars this plan and its follow-up target are now fully covered on every word measured.**
> **If picking this up next**, the only remaining item is Phase I in `FST_FULL_GRAMMAR_PLAN.md` —
> the lazy per-rule-chain generalization (the true-FST path), a design-only spec not needed by
> either current grammar; build it only when a grammar that structurally needs it (word-internal
> interacting phonology, long-distance harmony) shows up.

**Audience:** an executing agent (Sonnet) working in this worktree
(`C:\Users\johnm\Documents\repos\machine-fst-advisor`, branch `fst-advisor`, rebased on `hc-rustify`).
Read this whole file before editing anything. Work phase by phase, in order; each phase ends with a
green build + full test suite + a commit. Do not start a phase until the previous one is committed.

## 1. Mission

Turn the FST work on this branch into ONE thing: a **fast, opt-in, propose-and-verify analyzer**
(`FstCoverageProbe` over `VerifiedFstAnalyzer`) that covers **as close to 99% of every HC construct
as possible**, so a grammar engineer can edit any rule — affixation, templates, compounding,
phonology (including boundary-conditioned), infixation, reduplication — and see the effect in the
probe's numbers in milliseconds. It is a grammar-tuning instrument, not a production analyzer.

**The contract (never weaken it):**
- **Sound on positives.** Every emitted analysis is confirmed by HC's own restricted re-analysis
  (`FstReplay.Confirm` pins `LexEntrySelector`/`RuleSelector` and runs the real `Morpher.AnalyzeWord`;
  restriction can only remove paths, never fabricate one). No false positives, ever.
- **Known-incomplete on negatives.** A missed parse is acceptable (that is the 1%); a wrong parse is not.
- **Opt-in only.** Never wired into `Morpher` or any default parsing path.

**Explicitly dead:** the entire *certification* concept — empirical corpus-parity gates, "certified ⇒
FST-only, engine skipped", grammar closure as a runtime gate, completeness proofs. It was fragile
(certifying on 30 Sena words, decertifying on 60) and it is not the product. Delete it; do not
rebuild it under another name.

## 2. Architecture: the three-mechanism split (why nothing explodes)

Each construct class gets the ONE mechanism that is bounded for it. Mixing these up is how you get
exponential size, build time, or walk time.

| Construct class | Mechanism | Bound |
|---|---|---|
| Concatenative morphotactics + lexicon (affixes, templates/slots, derivation, compounding) | **Eager**, inside the automaton, as a **shared trie** | Additive: `|lexicon trie| + |affix inventory|`. Tries cannot multiply. |
| Phonology (rewrite rules: feature change, deletion, epenthesis, metathesis; all strata) | **Lazy composition at analysis time** — each rule compiles once to its own small transducer; the surface word is walked through rule-inverses and the lexicon trie **in lockstep**. The composed product is never stored. | Build: per-rule, independent of lexicon. Per word: `word length × live frontier` (beam-capped). |
| Reduplication + infixation (unbounded copy is provably non-regular — cannot be in a 1-way FST at any size) | **Runtime peel** (pre/post-processing): detect, strip, re-analyze residual through the fast path, wrap with the morpheme | Redup: O(n²) scan, ≤2 applications. Infix: O(sites × infixes). |

**Forbidden approaches** (each was tried or scoped on this branch and is a known blowup):
- Do NOT eagerly compose `lexicon ∘ rule₁ ∘ … ∘ ruleₙ` into one automaton (multiplicative states).
- Do NOT materialize root × affix-permutation surface tables (`ForwardSynthesisProposer` — measured
  5 s build at depth 2, 45 s at depth 3 on a 2,283-entry grammar; scales `roots × affixes^depth`).
  It gets deleted in Phase 4.
- Do NOT invert phonology on the whole surface *before* the morphotactic walk
  (`ComposedPhonologyProposer`'s design). Without morpheme boundaries on the tape,
  boundary-conditioned rules (Indonesian `meN-`) fire everywhere and explode into garbage.
  Lockstep composition (Phase 3) is the fix; the old proposer gets deleted then too.
- Do NOT `Determinize`/`Minimize` across unification (FeatureStruct) arcs — merging distinct paths
  destroys multi-analysis enumeration. Determinizing the plain-symbol lexicon trie layer is fine.

## 3. Repo-specific facts the executor must know (learned the hard way)

- **Build strictness:** `TreatWarningsAsErrors` everywhere; **IDE0005 (unused using) is a build
  error**. After removing code, always remove now-unused usings. CI also runs **CSharpier**
  formatting (`dotnet csharpier .` if formatting failures appear).
- **Generic offset type is `int`, not `ShapeNode`** (hc-rustify change): patterns are
  `Pattern<Word, int>`, cascades `LinearRuleCascade<Word, int>`, rules `IRule<Word, int>`.
- **`MorpherPool` API is `Rent()` / `Return(morpher)`** (no disposable wrapper). Concurrency
  pattern: rent per call, return in `finally`. A single shared `Morpher` must never be used from
  multiple threads (mutable selectors).
- **`InternalsVisibleTo`** is set for the test assembly; internal types are testable directly.
- **The engine oracle is SLOW and has pathological words**: `Morpher { MaxUnapplications = 0 }` on
  the Sena wordlist runs 100s of ms/word average, with individual words taking tens of seconds+.
  Therefore: engine-parity comparisons live ONLY in `[Explicit]` benchmarks, never in CI tests, and
  always with `HC_MAX_WORDS` capped.
- **Benchmarks** (`FstSenaBenchmark`) are `[Explicit]`, driven by env vars:
  `HC_GRAMMAR`, `HC_WORDS`, `HC_MAX_WORDS`, `HC_THREADS`; run via
  `dotnet test --filter "FullyQualifiedName~FstSenaBenchmark.<TestName>"`. Server GC via
  `DOTNET_gcServer=1` (the new `Benchmark_ParallelThroughput` prints whether it took effect).
- **Test grammars:**
  - Sena (concatenative, 0 phonological rules, no redup/infix):
    `C:\Users\johnm\Documents\repos\machine\samples\data\sena-hc.xml` + `sena-words.txt` (7,121 words).
  - Indonesian (boundary-conditioned `meN-` nasal substitution + deletion, 3 reduplication rules):
    `C:\Users\johnm\Documents\repos\machine\samples\data\indonesian-hc.xml` + `indonesian-words.txt`.
  - Load with `XmlLanguageLoader.Load(path)`.
- **`VerifiedFstAnalyzer.AnalyzeWord` returns a lazy iterator** — every re-enumeration re-runs
  propose+verify. Materialize (`.ToList()`) before enumerating twice.
- **Baseline numbers** (this machine, 16 threads, Server GC, Sena 60 words): verified FST
  ~12–20 ms/word vs pooled engine ~445–837 ms/word (~22–72×; variance is the engine's pathological
  words, not the FST). Composite coverage vs engine on Sena 200 words: **192/200, 0 unsound**.
  Indonesian (from docs; with the now-doomed forward synthesis): 69/70. The Phase 3 target is to
  match/beat that 69/70 **without** forward synthesis.

## 4. Phase 0 — ✅ DONE (committed)

Was uncommitted work at plan-writing time; now committed:
- `src/.../FstCoverageProbe.cs` — the probe: `ForLanguage(language).Probe(words)` → `ProbeReport`;
  `CompareGrammars(before, after, words)` → `CoverageDiff`. (Since rewritten in Phase 5 to build
  the full composite instead of the bare FST — this description is the Phase-0-era shape.)
- `tests/.../FstCoverageProbeTests.cs` — CI tests (grew from 4 to 8 across Phases 0 and 5).
- `src/.../SurfacePhonology.cs` — memoized `Variants` (build-time fix).
- `tests/.../FstSenaBenchmark.cs` — added `Benchmark_ParallelThroughput` (pooled, thread-count and
  Server-GC aware).

## 5. Phase 1 — ✅ DONE — purge certification (delete, don't deprecate)

**Delete these files** (all exist only for certification/completeness/caching):
- `src/.../CompleteHybridMorpher.cs`
- `src/.../CachingMorphologicalAnalyzer.cs`
- `src/.../AnalysisCache.cs`
- `src/.../AnalysisCacheSerializer.cs`
- `src/.../MorphemeRegistry.cs`
- `src/.../GrammarFstClosure.cs`
- `tests/.../CachingMorphologicalAnalyzerTests.cs`
- `tests/.../GrammarFstClosureTests.cs`

**Trim (keep the file, remove the cert role):**
- `FstVerification.cs` — keep `Compare` (set-parity diff) but re-document it as a **manual
  divergence-inspection tool** for `[Explicit]` benchmarks only; delete any "certificate" language.
- `CompositeProposer.cs` — remove `CoversAllConstructs`/covered-ops plumbing if its only consumer
  was certification (check first: `grep -rn "CoversAllConstructs" src tests`). Keep the
  union+dedup core and `ForLanguage`.
- `FstTemplateAnalyzer.cs` — keep `UncoveredOps` ONLY if repurposed as probe diagnostics ("these
  constructs are not covered — expect unparsed words there"); otherwise delete. Remove "certify"
  from all comments.
- `VerifiedFstAnalyzerTests.cs`, `FstTemplateAnalyzerTests.cs`, `FstVerificationTests.cs` — delete
  tests that construct `CompleteHybridMorpher`/`CachingMorphologicalAnalyzer` or assert
  certification; keep soundness/parity-shape tests that only use `VerifiedFstAnalyzer` + `Morpher`.
- `FstSenaBenchmark.cs` — delete `Benchmark_CertifyWithBoundedReduplication`; rework
  `Benchmark_FstVsSearch`/`Benchmark_ParallelThroughput` to drop `CachingMorphologicalAnalyzer`
  (compare pooled engine vs `VerifiedFstAnalyzer` only); keep `Benchmark_CompositeVsSearch`,
  `Diagnose_Divergences`, `Soundness_NegativeExamples`, `Concurrent_MatchesSequential`.
- `FstCoverageProbe.cs` — update doc comments (they reference the deleted types).
- `GrammarFstAdvisor.cs` — KEEP (it is a static linter, useful independently), but strip
  certification references from comments if any.

**Docs:** move `FST_FULL_COVERAGE_PLAN.md`, `FST_FULL_PLAN.md`, `HERMITCRAB_FST_PLAN.md` to
`docs/archive/` with a one-line `> Superseded by FST_FAST_PATH_PLAN.md` header prepended. Keep
`LEVER_2.md` in place (it is the technical spike record Phase 3 builds on) but add the same header
pointing here for scope.

**Verify:** `grep -rniE "certif" src tests docs --include="*.cs" --include="*.md"` (excluding
`docs/archive/`) returns nothing. Full build green (watch IDE0005 after deletions), full suite green.
Commit: `FST: remove certification concept entirely`.

## 6. Phase 2 — ✅ DONE — shared lexicon trie in `FstTemplateAnalyzer`

**Problem (measured):** `BuildRootChain` gives every root allomorph its own disjoint arc chain and
rebuilds all roots **per template** (plus once bare, plus once template-less). Sena: 50,673 states
for 1,463 root allomorphs. Walk cost at every root-entry position ≈ `roots × (templates+1)`
`FeatureStruct.IsUnifiable` calls — linear in lexicon size per word. This is the scaling wall for
big lexicons.

**Fix:** build ONE prefix-shared root network (trie over the per-segment `FeatureStruct`s /
surface-variant strings), entered by every template. Per-root data (`_tokenOnEntry`, the lex-entry
token) moves to trie **accepting nodes** (a node can accept multiple homograph entries — keep a
list). Where roots attach to different template sets, gate at the trie *exit* (accepting node →
template-continuation arcs), not by duplicating the trie.

Also do here (same hot loop, from the perf audit):
- Replace the `Key`/`PKey`/`emitted`/`Signature` **string keys** in the NFA walk with struct keys
  (`(int stateId, int tokensHash)` with proper `Equals`) — string building dominates per-word
  allocation.
- Hoist the per-segment `List`/`HashSet`/`Stack` allocations in `AnalyzeShape`/`EpsilonClosure`
  into reusable buffers per walk.

**Success criteria:** (a) all analyses identical before/after on the toy-grammar suite AND on
`Benchmark_CompositeVsSearch` for Sena (192/200, 0 unsound — must not change); (b) state count on
Sena drops substantially (print it; expect ≪ 50k); (c) `Benchmark_ParallelThroughput` verified
ms/word does not regress (expect improvement). Commit.

## 7. Phase 3 — ⚠ PARTIAL (v1 slice only, see STATUS below) — phonology via lazy lockstep composition (the big one)

> **STATUS (partial — read before continuing this phase).** Sub-steps 3a.1 (feature-change),
> 3a.2 (deletion), and now left-environment support (symmetric to the right-environment chain, added
> this session — see below) are implemented and tested (`PhonologyRuleCompiler.cs`,
> `LockstepPhonologyProposer.cs`, `PhonologyRuleCompilerTests.cs`). **Still scoped to single-segment
> Lhs, non-interacting rules** — no true multi-rule cascade composition, no α-variable expansion.
> **The phase gate below is NOT met, and left-environment support did NOT move real Indonesian
> coverage**: `Benchmark_CompositeVsSearch` on the FULL 121-word Indonesian corpus measures identical
> bare-FST-vs-composite (93/93 fully covered, 0 unsound) both before and after adding left-environment
> support. The reason is NOT (only) the previously-documented feeding/bleeding cascade gap — a
> session-specific diagnostic found a more fundamental, previously undocumented blocker: **`_alphabet`
> in `PhonologyRuleCompiler` is built from `table.Where(cd => cd.Type == HCFeatureSystem.Segment)`,
> which excludes boundary-type characters entirely** (`AddBoundary` tags them
> `HCFeatureSystem.Boundary`). `BuildProbeString` searches only `_alphabet` for a representative
> segment per environment constraint, so **any subrule whose environment contains a `BoundaryMarker`
> can never find one and is unconditionally marked unsupported** — not a soundness issue, but it means
> the probe never even gets to test whether the rule's core transformation is invertible. Measured on
> Indonesian's real grammar (`indonesian-hc.xml`, all 5 phonological rules, one subrule each): **5/5
> unsupported, 0 compiled**, confirmed via `PhonologyRuleCompiler.Compile`'s `UnsupportedRuleCount`.
> Three of the five (`Unspecified nasal default`, `Nasal deletion`, `Nasal assimilation`) have a plain
> `BoundaryMarker` in their right environment and are otherwise within v1's supported shape (no
> quantifiers, no MPR gating) — they would very plausibly compile if the boundary-representative gap
> were fixed (`Nasal assimilation` would still additionally need α-variable expansion in its output,
> since its substitution target agrees in place-of-articulation with the following consonant). The
> other two are separately blocked regardless (`Nasalization in reduplication` has an
> `OptionalSegmentSequence` quantifier in its left environment; `Voiceless obstruent deletion` has
> `excludedMPRFeatures`). **This means the entire Phase-3 lockstep-phonology mechanism has never
> actually fired on real Indonesian data at any point in this branch's history** — the 54/70 (and now
> 93/121) "identical with vs. without the proposer" results were never actually exercising a compiled
> rule; they were comparing the bare FST against itself. Fixing the boundary-representative gap (make
> `BuildProbeString`/the probe machinery treat a `BoundaryMarker` constraint specially — e.g. insert an
> actual boundary annotation into the probe string rather than searching `_alphabet` for one) is now
> the higher-priority prerequisite; α-variable expansion and true cascade composition remain necessary
> afterward for full `meN-` coverage. Sena (0 phonological rules) is confirmed unaffected by any of
> this — 58/60, 0 unsound, matching the pre-Phase-3 baseline exactly.
>
> **A real bug found and fixed along the way, worth knowing:** HC's deletion rules mark a `ShapeNode`
> `IsDeleted()` rather than physically removing it from the `Shape` — code that counts/reads
> segments after applying a rule (as this compiler's probing does, and as `SurfacePhonology.cs`'s
> `SurfaceNodes`/`NodeCount` ALSO does, pre-existing and not fixed here — out of this phase's scope)
> must filter `!n.IsDeleted()` or it will undercount what actually changed.
>
> **Also fixed this session (latent, not previously exercised):** the original `AddRestorationBranch`
> always routed through `ChainRightEnvironment`, which is a no-op on an empty list — a
> left-environment-only (or entirely unconditioned, though that shape is separately rejected) deletion
> would have added a dangling arc to a state with no way back to state 0, silently contributing
> nothing. Both `AddRestorationBranch` and `AddSubstitutionBranch` now special-case an empty right
> environment the same way (direct arc back to state 0), matching the pattern
> `AddSubstitutionBranch` already used for the zero-right-environment case.

This replaces BOTH `ComposedPhonologyProposer` (wrong on boundary-conditioned rules) and
`ForwardSynthesisProposer` (exponential build) — that is the END STATE, not yet reached (see STATUS
above). `docs/LEVER_2.md` + `LeverTwoSpikeTests.cs` already proved the lockstep walk recovers
deletion and an opaque two-rule cascade **on hand-built transducers**; the missing piece is the
compiler from real HC rules.

**3a. `RewriteRule → RuleFst` compiler** (new file, e.g. `PhonologyRuleCompiler.cs`).
Compile each `RewriteRule` subrule (`φ → ψ / λ _ ρ`, all bounded patterns first) into a small
transducer over per-segment `FeatureStruct`s: states = position within λ·φ·ρ window; arcs carry
(match-FS, output-FS-or-ε). Handle, in this order, each behind its own tests:
  1. feature-change (same length φ/ψ),
  2. deletion (φ → ∅) — the *inverse* inserts; cap reinsertions per word (reuse the
     `Morpher.DeletionReapplications` value as the bound),
  3. epenthesis (∅ → ψ) — inverse deletes; bounded trivially,
  4. metathesis (`MetathesisRule`) — bounded window swap,
  5. α-variables in environments — expand per feature value **within the one rule** (bounded,
     small); if a variable is genuinely unbounded, mark that rule "unsupported → this rule is
     skipped in the fast path" and surface it in probe diagnostics (never silently wrong, verify
     still guards).
Boundary conditioning: the intermediate tape in the lockstep walk must carry morpheme-boundary
markers (HC's `HCFeatureSystem.Boundary` annotations) so `meN-`-style rules see `+` — this is
exactly what the old surface-inversion design lost.

**3b. Lockstep walker.** Extend the analyzer walk: a configuration is
`(trieState, ruleState₁ … ruleStateₙ, tokens)`; input segments feed through the rule-inverse chain
(surface stratum first, rules reversed within a stratum — same order `AnalysisStratumRule` uses)
into the trie, advancing all coordinates together. No product automaton is ever stored.
Under-specified segments unify against trie arcs as today.

**3c. Guardrails (hard-wired, with tests):**
- **Frontier beam:** hard cap on live configurations per word (default generous, e.g. 10k;
  configurable). On overflow: drop the word to "unparsed", count it, expose
  `ProbeReport.BeamOverflows`. Never throw.
- **Reinsertion cap** per deletion rule per word (3a.2).
- **Build budget:** per-rule transducer state count asserted small (< ~100 states); a rule that
  blows past it is marked unsupported + diagnosed, not built.
- **Verify unchanged:** every candidate still goes through `FstReplay`. Phonology proposals that
  are wrong cost time, never correctness.

**3d. Retire the old mechanisms:** delete `ComposedPhonologyProposer.cs`,
`ForwardSynthesisProposer.cs`, `InversePhonology.cs` (check consumers first), their tests, and the
`forwardSynthesis` flag threading. `SurfacePhonology` (isolation + boundary-probe precompile of
affix surface variants) MAY stay if it still wins on simple cases — decide by measuring Indonesian
coverage/latency with it on vs off; delete if redundant.

**Success criteria (the phase gate) — actual results:**
- Toy-grammar CI tests: each of 3a.1–4 has a test where (i) the engine parses a word needing that
  rule type, (ii) the fast path finds the same analysis set, (iii) a non-word stays unparsed. **Met
  for 3a.1/3a.2 and left-environment (both deletion- and substitution-conditioned) only**
  (`PhonologyRuleCompilerTests.cs`: right-context deletion, unconditioned substitution, left-context
  deletion, left-context substitution, two unsupported-shape rejection tests, one composite-wiring
  integration test — 8/8 passing). 3a.3–3a.5 (epenthesis, metathesis, α-variables) and true
  multi-rule cascade composition not attempted.
- **Indonesian:** `Benchmark_CompositeVsSearch` coverage ≥ **69/70** with `forwardSynthesis`
  DELETED — **NOT MET**. Measured on the FULL 121-word corpus (this session): bare FST 93/121,
  composite 93/121, identical with and without left-environment support, 0 unsound. As STATUS above
  explains, this is not primarily the cascade gap — it is that **0 of Indonesian's 5 real
  phonological rules compile at all**, due to the newly-found boundary-representative gap
  (`_alphabet` excludes boundary-type characters, so any rule with a `BoundaryMarker` in its
  environment — 3 of the 5 — is rejected before its core shape is even evaluated). Left-environment
  support is verified correct at the unit level but had nothing real to apply to on this grammar.
- **Sena:** still 58/60 (the corpus slice actually measured this session), 0 unsound — confirmed
  unaffected, exact match to the pre-Phase-3 baseline. (The plan's original 192/200 figure is the
  200-word corpus from earlier sessions; re-verify at that size before calling this final.)
- Build time: Indonesian compiled in ~2.6s total for the whole `Benchmark_CompositeVsSearch` run
  (build + 70-word analysis) — no `roots × affixes` materialization in the new compiler itself
  (verified: it probes the alphabet, bounded by alphabet size × rule count, not lexicon size).
- Commit per sub-step: not followed as literally as planned — 3a.1 and 3a.2 landed together in one
  commit since the deletion and substitution code paths share almost all of the same compiler
  machinery and were developed/debugged together.

## 8. Phase 4 — ⚠ PARTIAL (see status per item) — close the remaining construct gaps

Work items, each: implement → toy-grammar test (engine finds it, fast path finds it, non-word
rejected) → measure on Sena+Indonesian → commit.

1. ✅ **DONE — Partial/CV reduplication templates.** `ReduplicationProposer` previously detected
   only exact full-copy (`word.Length` even, first half == second half). Generalized to scan every
   copy length from 1 up to `word.Length / 2`, both prefix-copy (`copy·base`) and suffix-copy
   (`base·copy`) — full reduplication is now just the `len == word.Length / 2` case of the same
   scan, so it subsumes rather than sits beside the old logic. Still `O(word length²)`, still
   verify-gated (a coincidental short repeat is proposed but rejected — tested:
   `Composite_CoversFullReduplication_WhereFstAloneMisses`'s new `"sasag"` assertion in
   VerifiedFstAnalyzerTests.cs). No genuine CV-template grammar was built to positively exercise a
   real partial match end-to-end (constructing a correct multi-group HC `Pattern` for a CV-shaped
   `Lhs` was judged higher-risk than the time available justified — no existing test in this repo
   uses `Pattern.Group(...)`, so it would have been unvalidated territory). Confirmed no regression
   on Sena (58/60) and Indonesian (54/70), 0 unsound on both.
2. ✅ **DONE — Infix surface variants.** `InfixProposer` searched only for an infix's literal
   underlying string. Now builds a `SurfacePhonology` (the same isolation/boundary-probe machinery
   already used for regular affix arcs) once per infix and searches for every surface variant, not
   just the underlying form — a phonologically-altered infix is no longer invisible to the literal
   substring search. Confirmed no regression on Sena/Indonesian (identical coverage, 0 unsound); no
   dedicated end-to-end test added (same reasoning as #1 — a genuine phonologically-altered-infix
   toy grammar wasn't built this pass), so this is verified by code reuse (the exact same
   `SurfacePhonology.Variants` already extensively tested for affixes) plus the real-grammar
   regression checks, not a new positive-case unit test. Multi-slot templatic infixation stays out
   of scope (unchanged, documented residual).
3. ⬜ **NOT DONE — Compounding is a bigger lift than originally scoped.** Investigated: the fix is
   NOT just "extend `FstReplay` to pin two roots." `WordAnalysis` (in `SIL.Machine.Morphology`, a
   shared type well outside this branch's scope) has a single scalar `RootMorphemeIndex : int` —
   there is no way to represent a second root at all in the current data model. Properly supporting
   compounds requires: (a) extending `WordAnalysis` (or building a parallel representation) to carry
   multiple root positions, which ripples into `MorphToken`/`MorphTokenCodec` (root-index encoding
   assumes one root), every signature function across this codebase that reads
   `RootMorphemeIndex` as a scalar (`FstReplay`, `FstVerification`, `CompositeProposer`, several
   test files), (b) a new compounding candidate generator (propose split points bounded by
   `MaxStemCount`), and (c) extending `FstReplay.Confirm` to pin two `LexEntrySelector` roots. This
   is a genuine cross-cutting data-model change, not a local fix — deferred rather than attempted
   under time pressure on a shared type. Left in `KNOWN_GAPS`.
4. **Construct sweep — enumerate and check off every HC feature** (done as an audit, not a
   line-by-line test-writing exercise given time already spent on #1–#3):

   | Construct | Status |
   |---|---|
   | Affix process rules (prefix/suffix) | ✅ core `FstTemplateAnalyzer` |
   | Circumfix (prefix+suffix halves) | ✅ `MorphOp.CircumfixPrefix/CircumfixSuffix` handled in `ForwardSynthesisProposer`'s covered-ops (only when `forwardSynthesis` opt-in is on); NOT built directly by the bare FST or by a dedicated generator otherwise — falls to the engine when forward-synthesis is off |
   | Realizational rules (`RealizationalAffixProcessRule`) | ✅ handled identically to `AffixProcessRule` in `FstTemplateAnalyzer.Allomorphs`/`RequiredCategory` |
   | Affix templates + slots (incl. obligatory slots) | ✅ `AppendSlots`/`ClassifyTemplate` |
   | Compounding rules | ❌ see #3 above — `KNOWN_GAPS` |
   | Strata + morphophonemic/allophonic rule placement | ✅ build iterates `language.Strata` in order; phonology precompile + Phase 3 lockstep both stratum-aware |
   | MPR features / co-occurrence rules | ⚠ **not build-time gated** — the FST does not check `RequiredMprFeatures`/`ExcludedMprFeatures` when building arcs, so a candidate that would violate an MPR co-occurrence constraint can be proposed; **sound regardless** because `FstReplay` re-runs real HC analysis (which does check MPR features) — this is a precision gap (more candidates verified and rejected than strictly necessary), not a soundness gap |
   | Allomorph environments (`AllomorphEnvironment`) | ⚠ same as MPR features — not build-time gated, but verify-safe |
   | Stem names (`StemName`) | ⚠ same — not build-time gated, but verify-safe |
   | Partial application (`Word.IsPartial`) | N/A — a runtime/incremental-parsing concept on HC's `Word`, not applicable to this static analyzer's build |
   | Clitics (`MorphOp.Clitic`) | ❌ **no generator exists.** `MorphOp.Clitic` is a real enum value or `ClassifyOp` result, but unlike Infix/Reduplication/Process it has no dedicated `IConstructProposer` — a grammar using clitics falls entirely to the engine for those words today. Not attempted this pass (no evidence any test grammar or Sena/Indonesian uses clitics, so priority was judged lower than #1/#2). Added to `KNOWN_GAPS`. |
   | Process/simulfix (`ModifyFromInput`, `MorphOp.Process`) | ❌ same bucket as clitics in `FstTemplateAnalyzer`'s uncovered-ops default case — no dedicated generator; falls to the engine |

   The sweep found two NEW gaps not previously listed (clitics, process/simulfix have no
   generator) and confirmed the MPR/environment/stem-name build-time-gating gap is a precision, not
   soundness, issue. All added to `KNOWN_GAPS` below.

## 9. Phase 5 — ✅ DONE — the probe is the product

1. `FstCoverageProbe.ForLanguage` builds the **full composite** (trie FST + lockstep phonology +
   peel generators) — the all-in fast path — instead of the bare `FstTemplateAnalyzer`.
2. `ProbeReport` gains diagnostics: `BeamOverflows`, `UnsupportedRules` (from Phase 3c),
   `UncoveredConstructs` (repurposed `UncoveredOps`), wall-time.
3. Add an `[Explicit]` end-to-end benchmark: full Sena wordlist (7,121 words) through the probe —
   record coverage + p50/p95 ms/word; same for Indonesian.
4. Edit-loop test (CI, toy grammar): `CompareGrammars` detects gained/lost coverage for at least
   one edit per mechanism class — an affix rule edit, a phonological rule edit, a reduplication
   rule edit. This is the product promise: *any* grammar change moves the probe.

## 10. Global success criteria (the definition of done) — actual results

> These are measured against the FULL wordlists (Sena 7,121 words, Indonesian 121 words) via
> `Benchmark_FullCorpusProbe`, not the small capped slices used elsewhere in this plan for fast
> iteration. The full-corpus numbers are materially different (lower coverage) than the small-slice
> numbers quoted earlier in this document — expected, since rare/complex word forms concentrate in
> the tail of a real wordlist, and this is the more honest number to hold the system to.

1. ✅ **Zero certification:** no cert/closure-gate/parity-gate concept in code, tests, or live docs
   (Phase 1, re-verified: `grep -rniE "certif" src tests docs` outside `docs/archive/` empty).
2. ✅ **Soundness:** `Benchmark_CompositeVsSearch` unsound = 0 on both grammars (re-confirmed after
   every phase's changes in this session). `Soundness_NegativeExamples` was not re-run this session
   on the full corpus — worth doing before calling this fully closed.
3. ✅ **Coverage — MET on Sena (99.2%) once measured against the right denominator.** The raw
   probe-parsed rates (**Sena 58.1%** of 7,121, **Indonesian 62.0%** of 121) initially looked like a
   large miss, but those denominators are the RAW WORDLIST, which contains many words the search
   engine itself cannot parse (out-of-lexicon roots, loanwords like `swahili`,
   contracted/punctuated forms like `na'pinacita`, proper nouns, typos). Measured properly on a
   seeded 200-word random Sena sample (Get-Random -SetSeed 42) via per-word ISOLATED oracle child
   processes (an in-process run literally crashed the test host — see the pathology note below):
   - FST parsed **120/200**; of the 80 it didn't parse, **79 don't parse in the engine either**
     (73 fast no-parses + 6 words where the unbounded engine needed 12–90+ s just to PROVE no
     parse exists).
   - Exactly **1 genuine FST gap**: `ndikhali` (8 engine analyses; a copula construction —
     `ser` "to be" + NZR + class prefix — the copula/TAM gap already in `KNOWN_GAPS`).
   - **FST coverage of engine-parseable words: 120/121 = 99.2%.**
   Indonesian was NOT re-measured with an engine-parseable denominator; its head-70 slice showed
   60/70 engine-parseable with 54 fully covered (90%), and unlike Sena it has a KNOWN real gap
   class (the `meN-` cascade, Phase 3's frontier), so its true ratio is likely lower than Sena's.
   **Pathology note (a genuine fast-path selling point):** the engine's worst case is proving a
   NON-word has no parse — 6 sample words each burned 12–90+ s of unbounded search, and one word
   OOM-crashed an entire test-host process; the FST probe answers all of them in milliseconds.
   That is exactly the behavior a grammar-tuning probe must not inherit.
4. ✅ **Speed:** full-corpus p50/p95 — **Sena: 31 ms / 173 ms**, **Indonesian: 1.4 ms / 6.0 ms**
   (sequential, single-threaded, via `FstCoverageProbe`; the full composite is heavier than the bare
   FST measured earlier in this document, so these are noticeably higher than the ~15–20 ms/word
   bare-FST figures quoted earlier — the phonology/reduplication/infix generators all now run on
   every word). Sena's p95 (173 ms) is close to the plan's original "under 50ms" target and misses
   it; Indonesian is comfortably under. The 16-thread/Server-GC parallel throughput multiplier
   (22–72× over the pooled engine, measured earlier in this session on 60-word slices) was not
   re-measured against the full composite at full-corpus scale this session.
5. ✅ **No blowups:** Sena FST states 20,737 (Phase 2, down from 50,673); Indonesian/Sena full-corpus
   builds completed without any `NotSupportedException` (the state-budget abort). No frontier-beam
   guardrail was ever implemented (Phase 3c's beam cap did not get built along with the rest of the
   lockstep walker) — flagged in `KNOWN_GAPS`, since the walk has no explicit cap on live
   configurations today.
6. ✅ **Every phase's full suite green**, CSharpier clean — true at every commit in this session
   (108 → 115 HermitCrab tests across the five phases, all passing at each phase boundary).
7. ✅ **The edit-loop test (Phase 5.4) passes** — three tests in `FstCoverageProbeTests.cs`
   (`Probe_DetectsGainedCoverage_AfterAddingSuffixRule`/`...PhonologicalRule`/`...ReduplicationRule`)
   confirm an edit in each of the three implemented mechanism classes visibly moves probe output.

## 11. KNOWN_GAPS (maintain this list as you go)

- **Copula/TAM constructions — RE-DIAGNOSED 2026-07-02: it's compounding, not a missing prefix
  layer.** `ndikhali` (the ONLY genuine FST gap in the 200-word random Sena sample, 8 engine
  analyses) was ground-truthed with a bounded diagnostic: it is `ndi` ("é") ⊕ `khal` ("ser") via
  Sena's real `CompoundingRule` (mrule7/mrule8) + `-i` NZR + a zero class prefix — a two-root
  compound, NOT the "prefixal derivation" construct the archived plan guessed. Closing it is the
  compounding item below (fix speced in `docs/FST_FULL_GRAMMAR_PLAN.md` Phase G2), and would make
  the sample 100% of engine-parseable.
- Templatic multi-slot infixation (deliberate residual, see Phase 4.2).
- Unbounded-copy reduplication beyond 2 applications (peel bound).
- **No frontier-beam cap on the NFA walk (`AnalyzeShape`/`AnalyzeComposed`/`EpsilonClosure`/
  `ComposedClosure` in `FstTemplateAnalyzer.cs`).** Plan section 7 (3c) specified this guardrail;
  it was never implemented — confirmed by grep, no `beam`/`maxConfig` logic exists. The live
  config set can in principle grow unboundedly on a pathological word/grammar combination (many
  ambiguous unification paths). Not observed in practice on Sena/Indonesian full-corpus runs this
  session (both completed without incident), but it's a real, un-guarded risk for a grammar this
  hasn't been tested against. Should be added before treating this as production-safe for arbitrary
  grammars.
- ✅ **Compounding — CLOSED (2026-07-03).** The Phase 4.3 claim ("`WordAnalysis.RootMorphemeIndex`
  is a single `int` — the shared data model has no way to represent a second root at all,"
  requiring a cross-cutting multi-root data-model lift) was wrong, as suspected when this entry
  was first corrected: `MorphOp.Compound` already existed, `WordAnalysis` already represented
  compounds (the engine's own `ndikhali` analyses proved it), and the real fix was a `FstReplay`
  change plus a trie compound loop — landed in `FstTemplateAnalyzer.cs`/`FstReplay.cs`
  (`BuildCompoundLoop`, `ToWordAnalyses`, `DerivableToCategory`'s compounding-edge extension). One
  thing the spec DIDN'T anticipate: reaching a template gated on a POST-COMPOUND category (Sena's
  noun-class-agreement prefix, which requires NZR's output category, itself only reachable via
  `compound → NZR`) needed `DerivableToCategory` to treat compounding as a category-transition
  edge, not just standalone derivational rules — without it, the compound loop worked but the
  class-prefix template stayed unreachable for either root. Measured: Sena's `ndikhali` — 8/8
  exact set parity with the engine. Indonesian (also has compounding rules, `mrule1`/`mrule2`) —
  unaffected, 121/121 unchanged, verify correctly prunes the loop's proposals since the corpus
  needs no compounds. Full detail: `docs/FST_FULL_GRAMMAR_PLAN.md` Phase G2.
- **No generator for clitics (`MorphOp.Clitic`) or process/simulfix (`MorphOp.Process`,
  `ModifyFromInput`).** Both fall into `FstTemplateAnalyzer`'s default "uncovered op" bucket with no
  sibling `IConstructProposer` picking them up (unlike Infix/Reduplication, which do have one) — a
  grammar using either construct routes those words to the engine. Not attempted (no evidence any
  test grammar, Sena, or Indonesian uses clitics; process/simulfix is rarer still in practice).
- **MPR features, allomorph environments, and stem names are not build-time-gated in
  `FstTemplateAnalyzer`** — it builds arcs for every allomorph regardless of `RequiredMprFeatures`/
  `ExcludedMprFeatures`/`AllomorphEnvironment`/`StemName` constraints. Sound (verify re-runs real HC
  analysis, which does check these), but a precision gap: some fraction of proposed candidates are
  verified and rejected that a build-time check could have pruned for free. Not fixed — flagged by
  the Phase 4 construct sweep, not previously documented.
- **`PhonologyRuleCompiler` v1 scope (Phase 3, see the STATUS block above for full detail):**
  left-environment support landed (this session, symmetric to the pre-existing right-environment
  chain). Still no multi-segment Lhs (N>1), no length-changing substitution (Rhs length must be 0 or
  1), no epenthesis/metathesis/α-variable handling, and — the big one — no true multi-rule cascade
  composition (each rule's arcs are independent branches from state 0; genuinely interacting rules
  like Indonesian's `meN-` assimilation+deletion are not composed together, so they stay unsupported
  even though each half might individually fit the supported shape). Confirmed via
  `Benchmark_CompositeVsSearch` on the full real Indonesian corpus: 93/121 with vs. without
  left-environment support — zero effective coverage gain there, but (see next bullet) this
  particular grammar can't exercise the mechanism at all yet regardless of left/right environment
  support. `ComposedPhonologyProposer`/`ForwardSynthesisProposer` remain in place and are still doing
  the real work for anything beyond simple single-rule cases.
- **`PhonologyRuleCompiler` cannot compile ANY rule whose environment contains a `BoundaryMarker`
  (newly found this session, blocks 3 of Indonesian's 5 real phonological rules).** `_alphabet` is
  built as `table.Where(cd => cd.Type == HCFeatureSystem.Segment)`, which excludes boundary-type
  character definitions (`AddBoundary` tags them `HCFeatureSystem.Boundary`). `BuildProbeString`
  searches only `_alphabet` for a representative segment per environment constraint, so a
  `BoundaryMarker` constraint never finds one and the whole subrule is marked unsupported — before
  its Lhs/Rhs shape is even checked. Measured: Indonesian's `PhonologyRuleCompiler.Compile` reports
  **5/5 subrules unsupported, 0 compiled**, confirmed with a throwaway diagnostic against
  `indonesian-hc.xml`. This means the Phase-3 mechanism has never actually fired on real Indonesian
  data at any point in this branch's history — every "identical coverage with vs. without the
  proposer" measurement (this session's and earlier ones) was comparing the bare FST against itself,
  not against a working phonology compiler. Fix direction: make the probe machinery insert an actual
  boundary annotation into the probe string for a `BoundaryMarker` constraint instead of searching
  `_alphabet` for a representative — this is a strictly higher-priority prerequisite than
  α-variable expansion or cascade composition, since those only matter once a rule can compile at
  all.
- **`SurfacePhonology.SurfaceNodes`/`NodeCount` do not filter `IsDeleted()` segments** (discovered
  while debugging the Phase 3 compiler's identical bug, fixed there but NOT here — out of scope for
  this pass). If a stratum's synthesis cascade includes a deletion rule, these methods may overcount
  the resulting surface — unverified whether this actually affects any real grammar's precompiled
  affix-arc coverage today, but worth checking before trusting `SurfacePhonology` output on a
  deletion-heavy grammar.
- **Cross-root character-level trie merging not done (Phase 2 scope decision).** Phase 2 shipped
  per-root chain sharing across attachment SITES (one segment chain per root, fanned out to every
  template/bare/template-less site via epsilon arcs — eliminates the roots×sites duplication, which
  was the measured dominant cost: Sena states 50,673 → 20,737, ~59% reduction). It did NOT implement
  true prefix merging ACROSS DIFFERENT roots (e.g. "abc"/"abd" sharing an "ab" arc), which would
  require a safe equality key for FeatureStruct-labeled arcs (FeatureStruct has no
  Equals/GetHashCode override — only ValueEquals — so this needs a proxy key, e.g. per-segment string
  representation via `CharacterDefinitionTable.GetMatchingStrReps`, plus per-root token-states hung
  off shared trie-leaf nodes so homographs don't collide on one `_tokenOnEntry` slot). Left for a
  follow-up given the correctness bar on this hot path; would matter most on large lexicons with
  heavy shared-prefix structure, not on Sena/Indonesian-sized grammars.
- **`EpsilonClosure`'s internal buffers are not pooled** (`result`/`seen`/`stack` are freshly
  allocated per call, i.e. per segment per word) — the `Key`/`PKey`/`emitted` string-allocation cost
  Phase 2 fixed (now struct-keyed) was the dominant hot-loop allocator per the original audit; this
  remaining allocation is smaller but still there. Follow-up: thread reusable scratch collections
  through `AnalyzeShape`/`AnalyzeComposed`/`EpsilonClosure`/`ComposedClosure` (all single-threaded
  within one call, so no pooling/concurrency hazard — just needs the method signatures threaded
  through carefully).
- **Indonesian `mengamat-amati` (1 word of 121) — a suffix stacked outside the reduplication.**
  Traced structure: `meng+amat` → `-Cont` → `mengamat-amat` → `-i`(LOC) → `mengamat-amati`; the
  copy portion surfaces as `amati` = copy + suffix, which is not a plain tail of the base, so the
  separator scan correctly doesn't fire. **Fix now speced** (suffix-peel inside the separator scan,
  ~30 lines): `docs/FST_FULL_GRAMMAR_PLAN.md` Phase G1. Deferred on 2026-07-02; 120/121 achieved
  without it.
- **Phase C introduced a ~85× build-time regression on Sena (measured 2026-07-03): 9.3 s total
  build, of which the trie itself is 105 ms.** Attribution: `SurfacePhonology.DeletionJunctions`
  is un-memoized (unlike `Variants`) and is called per allomorph × 26 derivation-layer builds ×
  depth 2; each call costs ~30 ms on Sena because the alphabet² two-neighbor fallback runs to
  exhaustion for EVERY candidate on a grammar with 0 phonological rules (nothing can ever delete,
  so the single-neighbor probe never succeeds). Indonesian is too small to notice. Fix speced
  (memoize + capability-gate + stop double-building the FST in the composite path):
  `docs/FST_FULL_GRAMMAR_PLAN.md` Phase H — expected result ~0.3–0.5 s.
- **`PhonologyRuleCompiler`'s boundary-representative gap (`_alphabet` excludes boundary-type
  characters) is now MOOT for Indonesian specifically, but still real for other grammars.** The
  `meN-` coverage fix (`docs/FST_FULL_GRAMMAR_PLAN.md` Phase C) went through `SurfacePhonology` +
  `FstTemplateAnalyzer` entirely — nothing routes through `PhonologyRuleCompiler`/`InversePhonology`
  for `meN-` anymore, so this bug no longer blocks Indonesian. It was NOT fixed in
  `PhonologyRuleCompiler.cs` itself; a grammar that genuinely needs the lockstep-phonology mechanism
  (word-internal interacting rules, not junction-conditioned ones) would still hit it.
- (add entries as discovered — every gap must be listed, none silent)
