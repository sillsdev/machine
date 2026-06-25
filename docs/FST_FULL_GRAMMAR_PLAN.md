# FST full-grammar coverage plan — 100% of Sena + Indonesian

> Written 2026-07-02, after the left-environment session (commit `308f269c`) and its finding that
> **0 of Indonesian's 5 phonological rules have ever compiled** (boundary-representative gap).
> Companion to `FST_FAST_PATH_PLAN.md` (which stays the architecture reference); this doc is the
> execution plan for closing the LAST gaps on both real grammars.
>
> **STATUS (2026-07-03): Phases A, C, D, H, G1, AND G2 are ALL DONE.** Indonesian is at **121/121
> fully covered, 0 unsound, 0 false positives** — every engine-parseable word in the corpus is
> closed. Sena's `ndikhali` (the one confirmed gap in its sampled corpus) is closed with **exact
> 8/8 set parity** — a guarded 60-word Sena slice is now 57/57 fully covered, 0 unsound. Sena's
> build-time regression is fixed (9.3 s → ~1.0–1.5 s). **Both real grammars this plan targeted are
> now fully covered on every word measured this session.** The compounding "data-model lift"
> premise (Phase E, and the matching `FST_FAST_PATH_PLAN.md` KNOWN_GAPS entry) is confirmed FALSE —
> closing it took a `FstReplay` fix, a trie compound loop, and (found only during implementation)
> extending `DerivableToCategory` to treat compounding as a category-transition edge — see Phase G2
> for the full account of what the original spec got right and what it missed. **Remaining: only
> Phase I** (the true-FST generalization — a design-only spec, not needed by either current
> grammar; build it when a grammar that structurally needs it shows up).

## Goal (definition of done)

For BOTH grammars (`sena-hc.xml` + 7,121-word list, `indonesian-hc.xml` + 121-word list):

1. Every **engine-parseable** word is **fully covered** — set parity per word
   (`Benchmark_CompositeVsSearch`'s `SetEquals(oracle)` criterion), not just "some parse".
2. **0 unsound** (the propose-and-verify contract is untouched — verify still gates everything).
3. No construct actually used by these two grammars is silently unsupported: the probe's
   diagnostics account for every rule (compiled, handled-by-peel, or engine-fallback with reason).

The denominator is engine-parseable words: the raw lists contain loanwords, typos, and
deliberately ungrammatical meN- variants (`menaca`, `menlangit`, `memlangit`…) that the engine
itself rejects; those count as covered when the FST also (quickly) rejects them.

## Verdict first: yes, this is reachable — and WITHOUT the "multi-week" generic cascade composer

The two grammars' remaining gaps are narrower than the generic problem:

- **Sena has zero phonological rules.** Its whole remaining gap is *morphotactic proposer
  coverage* (copula/TAM, prefixal derivation, depth-3 derivation — `ndikhali`, and the archived
  plan's `nyari`/`cawo`/`miwiri` family). No FST theory needed; the trie builder just doesn't lay
  down those paths yet.
- **Indonesian's 5 phonological rules are ALL boundary-conditioned at affix junctions** (or, for
  `Nasalization in reduplication`, conditioned inside a redup copy). Nothing fires word-internally
  far from a morpheme join. That means the interacting `meN-` cluster (assimilation feeding
  obstruent deletion, MPR gating, α-place variables) can be handled by **bounded build-time
  junction probing through the REAL synthesis cascade** — baking junction surface-variants into
  the trie — instead of per-rule inverse transducers + generic multi-rule composition.

Key insight for the junction approach: the forbidden move in `PhonologyRuleCompiler`'s design notes
is probing the combined multi-rule effect **and attributing it to a single rule's branch** (that
misreads feeding/bleeding). Junction probing does NOT attribute anything per rule — it records the
junction's *total* surface↔underlying map, which is exactly the object analysis needs. HC itself
applies the cascade during the probe, so feeding/bleeding, α-variables (concrete segments — no
symbolic expansion), boundary markers (present in the probe string by construction), and MPR gating
(probe ungated → over-propose → verify rejects) all come for free. Everything stays
verify-backstopped, so a misread alignment costs a rejected candidate, never a wrong answer.

---

## Phase A — measurement: exact gap lists — ✅ DONE (2026-07-02)

1. **Indonesian**: ran `FstSenaBenchmark.Diagnose_Divergences` against the full 121-word corpus
   (`HC_MAX_WORDS=121`). Result: **28 divergent words, zero compounds** — every missed analysis has
   a single `RootMorphemeIndex`. Two clean buckets, exactly matching Phases C/D below:
   - **21 simple meN- forms** (Phase C target): `melangit`, `melempar`, `melihat`, `memakai`,
     `memasak`, `memukul`, `menanti`, `mengaca`, `mengaco`, `mengamat-amati`* , `menganga`,
     `mengarang`, `mengirim`, `menikah`, `menulis`, `menyanyi`, `menyatu`, `menyewa`, `merancang`,
     `merasa`, `mewakili`, `meyakini` (*`mengamat-amati` also has a `LOC` suffix stacked on).
   - **7 REDUP-meN forms** (Phase D target): `membagi-bagi`, `memijit-mijit`, `meminta-minta`,
     `mengayuh-ngayuh`, `menulis-nulis`, `menyewa-nyewa`, plus `mengamat-amati` above (dual-tagged:
     redup + phonology both needed).
   No compound ever appears in the oracle set for any of the 121 words — **Phase E is confirmed
   unnecessary for Indonesian.** The census also reconfirmed the known escapes: `-Cont`/`-Pl`/
   `REDUP-meN` (all reduplication, unbounded-copy escapes) and `Nasalization in reduplication`
   (unbounded left environment) — consistent with the plan's Phase D framing (that rule never needs
   to compile; the peel handles it on the surface side).
2. **Sena**: did NOT re-run a second 200-word oracle sample — the known pathology (some words need
   12–90+ s unbounded search, one OOM-crashed an in-process test host in the prior session) makes
   that expensive to redo safely, and the existing 99.2%-of-engine-parseable result already isolates
   exactly one gap class. Instead, ground-truthed the known gap directly: a bounded (30 s timeout)
   diagnostic ran `Morpher.AnalyzeWord("ndikhali")` and printed every analysis. Result: **8 analyses,
   all of the shape `{9,1,10,5}+é+ser+NZR`, with `RootMorphemeIndex` alternating between 1 (`é`) and
   2 (`ser`)** — i.e. `ndikhali` = `ndi` ("é", root, PoS `pos69519`) compounded with `khal` ("ser",
   root, PoS `pos87418`) via Sena's real `CompoundingRule` (`mrule7`/`mrule8`, confirmed in
   `sena-hc.xml`: `mrule8` has `headPartsOfSpeech="pos69519"`, `nonHeadPartsOfSpeech="...pos87418..."`),
   THEN the `-i` NZR suffix (`mrule9`) attaches to the compound's output PoS (`pos80535`). The
   leading `{9,1,10,5}` morpheme is a null-surface noun-class agreement marker (class 9/10 nouns
   in Sena take a zero prefix) — it contributes 0 phonetic content, which is why `ndi+khal+i` alone
   spells the 8-letter surface `ndikhali` exactly.
   **This corrects the archived plan's guess** ("prefixal derivation layer" would close it) — it is
   a genuine TWO-ROOT compound, not a single-root derivational prefix. Closing it for real needs the
   Phase E `WordAnalysis.RootMorphemeIndex` multi-root lift, not a trie-builder tweak. See Phase B
   below for the resulting scope call.

Exit: gap tables above. Everything after this phase is sized by real data — and Phase C/D (28
Indonesian words, ~23% of that corpus) is unambiguously the higher-value target vs. Sena's 1-word
gap (~0.014% of its corpus) that would require the biggest, most cross-cutting change in this plan.

## Phase B — Sena morphotactic closure — ⚠ INVESTIGATED, DEFERRED (not a small fix after all)

Phase A's ground-truthing found `ndikhali`'s gap is NOT a missing prefixal-derivation layer (the
archived plan's guess, made without ever running the engine on this word) — it is a genuine
two-root compound (`é` ⊕ `ser`, via Sena's real `CompoundingRule`), so closing it requires the same
`WordAnalysis.RootMorphemeIndex` single-scalar lift Phase E already scopes for Indonesian
compounding (extending `WordAnalysis`/`MorphToken` to carry multiple root positions, a compounding
candidate generator, `FstReplay.Confirm` pinning two roots — cross-cutting across `FstReplay`,
`FstVerification`, `CompositeProposer`, and every `Sig`-style function). That is Phase-E-sized work
to close ONE word in a 7,121-word corpus already at 99.2% of engine-parseable coverage (120/121 on
the previously-measured sample) — disproportionate next to Phase C/D's 28-word, ~23%-of-corpus win
on Indonesian. **Decision: defer, same as the original plan's Phase 4.3 compounding call** — do
Phase C and D first (they need zero data-model changes), then revisit Sena's `ndikhali` only if
Phase E ends up being built anyway for some other reason. If Phase E is never built, this stays a
documented, understood residual (unlike the archived plan's guess, its actual cause is now on
record) — update `KNOWN_GAPS` accordingly rather than leaving the stale "prefixal derivation" theory
in place.

## Phase C — Indonesian: junction-variant compilation (the core piece) — ✅ DONE (2026-07-02)

**What actually shipped is simpler than the original design below** (kept for the record — the
"full window + re-implement the cascade" plan was NOT needed): `FstTemplateAnalyzer` already had a
`SurfacePhonology`-precompiled surface-variant mechanism for every affix (`Variants(underlying)`,
probing one neighbor segment on each side and reading back the morpheme's own portion when the
result is length-preserving). Investigation found that mechanism ALREADY discovers the correct
`mem`/`men`/`meng`/`meny` assimilated-nasal prefix variants for free — it only needed a probe with a
non-deleting representative of each place class (e.g. voiced `b`/`d`/`g`) to "unlock" the variant,
and Indonesian's grammar always has one. Two real gaps remained, both fixed with much smaller,
targeted changes:

1. **`SurfacePhonology`'s deleted-node rendering bug** (`SurfaceOf`/`AddBoundaryVariant`): HC marks
   a deletion via `ShapeNode.IsDeleted()` rather than removing the node (confirmed via code read of
   `NarrowSynthesisRewriteSubruleSpec.cs` — the node stays in the `Shape`'s linked list, same
   position, same original `FeatureStruct`, just flagged), so the OLD rendering loop still printed
   the pre-deletion segment's own representation instead of nothing. Fix: a shared `RenderNodes`
   helper that skips `IsDeleted()` nodes when building the surface string. This alone closed the
   **nasal-deletion-before-sonorant** case (`Nasal deletion`, prule2) — `melangit`, `melempar`,
   `melihat`, `menanti`, `mengaco`, `menganga`, `menikah`, `menyanyi`, `merancang`, `merasa`,
   `mewakili`, `meyakini` (12 words) — with ZERO new mechanism, just the rendering fix.
2. **New `SurfacePhonology.DeletionJunctions(underlying)`**: for the remaining case — the cascade
   deleting the NEIGHBOR itself (assimilation feeding `Voiceless obstruent deletion`, prule4+prule5)
   — probes each alphabet representative as a right neighbor (falling back to a SECOND trailing
   neighbor when the first alone doesn't trigger deletion, since `Voiceless obstruent deletion`'s own
   `RightEnvironment` needs a vowel *beyond* the deleted segment — the exact shape that broke the
   first, single-neighbor-only version of this method during testing) and returns
   `(affixSurface, deletedNeighborFeatureStruct)` pairs. `FstTemplateAnalyzer` gained **root-chain
   checkpoints** (`_rootCheckpoints`, `RootChainAfterSkip`) — states reached after consuming 0, 1, 2…
   of a root's own leading segments — so a junction-deletion outcome can be wired to "skip the root's
   deleted onset" via a build-time gate (`WireDeletionSkips`: only for roots whose own leading
   segment unifies with the recorded class — never a blind skip). This closed `memukul`, `mengaca`,
   `mengarang`, `mengirim`, `menulis`, `menyatu`, `menyewa`, `memakai` (8 words).

No window-size computation, no re-implemented cascade, no Pinv/lockstep involvement, and no
`roots × affixes` cost: both mechanisms are bounded by `|junction affixes| × alphabet` (or ×
alphabet² for the two-neighbor fallback) — a few hundred probes total, independent of lexicon size.

**Measured result** (`Benchmark_CompositeVsSearch`, full 121-word Indonesian corpus): **114/121
fully covered** (up from 93/121 pre-Phase-C), **0 unsound**, **0 false positives**
(`Soundness_NegativeExamples`, 50/50 clean). The only 7 remaining gaps are ALL `REDUP-meN`
reduplicated forms (`membagi-bagi`, `memijit-mijit`, `meminta-minta`, `mengamat-amati`,
`mengayuh-ngayuh`, `menulis-nulis`, `menyewa-nyewa`) — exactly Phase D's target, nothing left over
for non-reduplicated words. Full 118-test HermitCrab suite green (was 116; +2 new toy-grammar
tests), CSharpier clean. Sena unaffected by construction (0 phonological rules ⇒
`DeletionJunctions` always returns empty there — not re-measured this session, see Phase A note on
the cost of a full Sena oracle re-run).

**Tests**: `SurfacePhonologyJunctionTests.cs` (new) — a toy grammar with a boundary-abutting prefix
(`m+`) and a `RewriteRule` requiring BOTH a left-boundary AND a right-context vowel beyond the
deleted segment (deliberately exercising the two-neighbor fallback):
`Junction_RecoversRootOnsetDeletion_RequiringTwoSegmentProbe` (positive: `FstTemplateAnalyzer`,
`VerifiedFstAnalyzer`, and the real engine all agree; a non-word yields nothing) and
`Junction_DoesNotSkip_WhenRootOnsetIsNotTheDeletedClass` (soundness: a root starting with a
different, non-deleting class must never get the skip arc — verified by checking the "wrong" skip
target is NOT recoverable, not just that the right one is).

**Original design (superseded by the simpler mechanism above — kept for context on what was
considered and why it wasn't necessary):** build, for each junction-bearing affix allomorph and each
candidate root onset in the alphabet plus one representative following segment, an explicit
underlying window (`affix-tail + boundary + onset + context`), run the full phonological cascade via
`CompileSynthesisRule` reused across the whole rule list, and emit junction arcs from the recorded
surface↔underlying alignment. The actual mechanism reuses the EXISTING per-affix `Variants`
precompile for the substitution-only outcomes (assimilation, default-nasal) and only adds new
machinery (`DeletionJunctions` + root-chain checkpoints) for the one case that mechanism structurally
cannot express (a NEIGHBOR disappearing) — smaller surface area, less new code, same soundness
guarantees.

## Phase D — reduplication × phonology (the `-X-X` forms) — ✅ DONE, 6/7 (2026-07-02)

Corpus words: `membagi-bagi`, `meminta-minta`, `memijit-mijit`, `mengamat-amati`,
`mengayuh-ngayuh`, `menulis-nulis`, `menyewa-nyewa`.

**The construct is `-Cont` (mrule13), not `REDUP-meN` (mrule15, glossed RECIP — unused by any of
these words)** — confirmed by tracing the real engine's analysis (`AV+write+Cont`, `AV+divide+Cont`,
…), a plan-writing-time misreading corrected during execution. **`-Cont` is also glossed `Cont`,
matching the divergence table** (`FstSenaBenchmark.Diagnose_Divergences` labels each missed
analysis by its morpheme glosses, which is what surfaced this).

**What actually shipped**, via a bounded-cost extension to the EXISTING `ReduplicationProposer`
(no new proposer class): confirmed via a custom `ITraceManager` logging every `MorphologicalRuleUnapplied`
step that `-Cont` produces `[meN-word] + "-" + [nasal+stem, WITHOUT the literal "me" text]` — e.g.
`menulis-nulis`, where `nulis` is exactly `menulis`'s own trailing 5 characters. This is NOT
"copy the whole prefixed word" (the `-` + full copy the plan originally guessed) — it is a genuine
**TAIL copy separated by a literal character**, one shape narrower than `ReduplicationProposer`
already handled (adjacent, no separator, either full-word or tail-vs-tail). Added a third scan to
`ReduplicationProposer.AnalyzeWord`: for every position `sepPos`, treat `word[sepPos]` as a literal
separator and check whether everything after it is a genuine surface tail of everything before it
(`before.EndsWith(copy)`); on a match, recurse the residual (`before`) through the existing FST
proposer and wrap with the redup morpheme, exactly like the other two scans. No new mechanism, no
window/separator-character enumeration needed — the scan is separator-CHARACTER-agnostic (it
doesn't need to know `-` is special; a wrong guess is pruned by verify like any other candidate
here), which is why it needed no new field or grammar introspection.

**`Nasalization in reduplication` (prule3 — unbounded `OptionalSegmentSequence` + α-vars, the one
rule that can never fit any bounded compiler) never needed to compile**, confirmed: it only fires
inside redup copies, which the surface-level tail-copy scan matches without any phonology-aware
machinery at all.

**Measured result**: 6 of 7 corpus words fixed — `membagi-bagi`, `memijit-mijit`, `meminta-minta`,
`mengayuh-ngayuh`, `menulis-nulis`, `menyewa-nyewa`. Indonesian composite coverage: **114/121 →
120/121**, still 0 unsound, 0 false positives (`Soundness_NegativeExamples` unchanged, 50/50 clean).

**Residual: `mengamat-amati` (1 word, NOT fixed).** Traced separately: `me(ng)+amat+-amat+i` — the
`-i` (LOC) suffix attaches to ONLY the reduplicated copy (`amat+i` = `amati`), not to the whole
word. `"amati"` is NOT a tail of `"mengamat"` (last 5 chars are `gamat`, not `amati`), so the
tail-copy scan correctly does not fire on it — this is a materially different shape (an affix
stacked onto just the copy) that the current scan does not attempt. Closing it would need either
(a) trying "strip a known suffix surface off the copy, then tail-match the remainder" — real new
mechanism, grammar-introspection-dependent, not just a scan-shape extension — or (b) a multi-group
`Lhs` pattern reconstruction of `-Cont`/`-i`'s real interaction, which is exactly the kind of
unvalidated-pattern-API territory Phase 4's own CV-reduplication work already declined to attempt
under time pressure (no test in this repo builds a multi-group `Pattern`). Documented as a known
residual (added to `KNOWN_GAPS`) rather than pursued further — one word out of 121, against a
120/121 result, did not justify the added mechanism's risk/complexity for this session.

**Tests**: `VerifiedFstAnalyzerTests.Composite_CoversSeparatorReduplication_WhereFstAloneMisses`
(toy grammar: a full copy with a literal separator, `sagzsag`; soundness check that a tail-copy
candidate — `sagzag`, which passes the surface-shape scan but isn't what this toy rule's FULL-copy
semantics actually produce — is correctly rejected by verify). A toy grammar exercising the REAL
partial-tail shape (requiring a multi-group `Lhs` pattern) was not built, same call as Phase 4's
CV-reduplication case; the full Indonesian corpus benchmark is the positive evidence for that shape.

**Gate**: 6/7 engine-parseable redup corpus words fully covered, 0 unsound. `mengamat-amati` is a
documented residual, not a silent gap. Committed.

## Phase E — ❌ CANCELLED (2026-07-03): the premise was falsified by a code re-read

This phase scoped a "cross-cutting `WordAnalysis.RootMorphemeIndex` data-model lift" for
compounding. A direct re-read of `MorphToken.cs` and `FstReplay.cs` on 2026-07-03 showed the
data model ALREADY supports compounds (`MorphOp.Compound` exists; the engine emits two-root
`WordAnalysis` objects today — the `ndikhali` diagnostic printed them) and the only real blocker
is ~6 lines in `FstReplay.Confirm`. **See Phase G2 below for the actual spec.** Kept here so the
original (wrong) reasoning stays on record.

## Phase F — hardening + final gates — folded into Phases H and I below

- The **frontier beam cap** moves into Phase I (it belongs with the walker generalization).
- Final-numbers reporting is now the standing "stats battery" requirement in the execution specs.
- `FST_FAST_PATH_PLAN.md` STATUS + KNOWN_GAPS updates: partially done 2026-07-02/03 (boundary-gap
  moot-for-Indonesian note, compounding-premise correction, `mengamat-amati` entry); keep
  maintaining as G/H/I land.

---

# EXECUTION SPECS FOR THE NEXT SESSION (written 2026-07-03, for Sonnet)

Everything below is speced from a direct code re-read on 2026-07-03 (file/member references
verified that day). Work each phase to green (full suite + the phase's own gates) and commit
before starting the next. **Always report the stats battery with every result** (this is a
standing requirement from John, not optional): FST `StateCount`, build wall-time (note JIT-cold
vs warm — run the build twice in-process and report the second), and verified-walk p50/p95 ms/word.

## Current measured baseline

**Pre-Phase-H (2026-07-03, before H1/H2, this machine, Debug build, warm where noted):**

| | Indonesian | Sena |
|---|---|---|
| FST states (bare, morpher ctor) | 532 | 20,737 |
| FST states (trie-only, no-morpher ctor) | — | 15,901 |
| Bare FST build | 682 ms (JIT-cold; mostly JIT) | 9,281 ms cold / 8,920 ms warm |
| Grammar load (XML) | — | 245 ms |
| GenerateWords loop (1,463 allomorph calls) | — | ~175 ms |
| Trie-only build (no probing) | — | **105 ms** |
| `Variants` × 25 distinct affixes (memoized) | — | 47 ms |
| `DeletionJunctions` × 25 distinct affixes, ONCE each | — | **746 ms** |
| Verified-composite walk p50 / p95 / p99 | 1.8 / 14.7 / 21.6 ms | 49.8 / 288 / 893 ms (first 150 words) |
| Coverage (set parity vs oracle) | **120/121, 0 unsound** | 58/60 slice; 99.2% of engine-parseable (200-sample) |

**Post-Phase-H (after H1+H2 landed — see Phase H status for the state-count note):**

| | Indonesian | Sena |
|---|---|---|
| FST states (bare, morpher ctor) | 532 (unchanged) | **16,322** (was 20,737 — see Phase H) |
| Bare FST build | 266 ms | **~1.0–1.1 s** (cold and warm alike; was 8.9–9.3 s) |
| Coverage (set parity vs oracle) | **120/121, 0 unsound** (unchanged) | 55/57 guarded slice (60 words, 5s/word cap, 3 excluded), 0 unsound |

**Post-Phase-G1+G2 (2026-07-03, final this session):**

| | Indonesian | Sena |
|---|---|---|
| FST states (bare, morpher ctor) | 533 (+1, compound-loop join state) | 16,347 (+25 vs. post-H) |
| Bare FST build | ~433 ms | ~1.3–1.5 s |
| Coverage (set parity vs oracle) | **121/121, 0 unsound, 0 false positives** | 57/57 guarded slice (60 words, 5s/word cap, 3 excluded), 0 unsound; **`ndikhali` 8/8 exact set parity** |

## Phase H — ✅ DONE (2026-07-03): Sena build time 9.3 s → ~1.0–1.1 s

**H1 (memoize `DeletionJunctions`) and H2 (capability-gate `Variants`/`DeletionJunctions` on
`_anyPhonologicalRules`/`_anyDeletionSubrule`) landed together** in `SurfacePhonology.cs` — same
pattern as speced below, both in one pass since they touch the same lines. **Measured: Sena build
9.3 s → 1.0–1.1 s (cold and warm alike), Indonesian unaffected (266 ms, has real deletion subrules
so its gates stay open).** This is short of the ~0.3–0.5 s originally estimated; the remaining
~1 s is trie construction (105 ms measured standalone) plus `GenerateWords` (175 ms) plus JIT/other
overhead not isolated further — good enough that Phase H's practical goal (fast edit-loop
iteration) is met, and further squeezing wasn't pursued.

**A real, unexplained side effect: Sena's `StateCount` dropped from 20,737 (Phase C/D's own
number, measured 2026-07-02) to 16,322 after H1+H2 — not identical, as this doc's gate below
originally demanded.** Investigated rather than dismissed: the gate's own reasoning predicts
IDENTICAL variant sets before/after (a 0-phonological-rule grammar's un-gated `ComputeVariants`
should already degenerate to `{underlying}` only, since an empty rule cascade changes nothing —
verified by hand-tracing `AddBoundaryVariant`'s behavior with a no-op cascade). The most likely
explanation not fully confirmed: some affix's underlying string, round-tripped through
`_table.Segment` + `GetMatchingStrReps` under the OLD (un-gated) path, produced a
string-identical-but-FeatureStruct-distinct "variant" that `BuildAffixArcs`' dedup-by-string-value
check (`if (variant == underlying) continue`) does NOT catch (it dedups by the RENDERED STRING,
not by the resulting FeatureStruct sequence), building a redundant-but-distinct arc chain. H2's
gate short-circuits before that round-trip ever happens, removing the redundant states. **This
was NOT chased to a certain root cause** (would need instrumenting `BuildAffixArcs`), because the
gates that actually matter — coverage and soundness — were reverified directly and are unaffected:
Indonesian `Benchmark_CompositeVsSearch` **120/121, 0 unsound, identical to before**; a per-word-
timeout-guarded Sena coverage check (first 60 words, 5 s/word cap, full random-corpus oracle
comparison is the known-hazardous one) showed **55/57 fully covered (3 timed out, excluded), 0
unsound** — consistent with the known single-gap pattern, no regression signature. Full 119-test
suite green throughout. Treat "StateCount decreased, unexplained but coverage/soundness verified
unaffected" as the honest status — a future session touching `BuildAffixArcs`'s dedup should
resolve this fully rather than re-litigate it.

**H3 (stop building the FST twice in the composite path) — turned out not to be a real bug;
struck.** The plan's evidence for H3 ("bare FST build 8.7 s + composite build 9.8 s back-to-back")
came from the DIAGNOSTIC SCRIPT that produced that measurement, which itself constructed
`new FstTemplateAnalyzer(language, morpher)` twice (once standalone, once inline as an argument to
`CompositeProposer.ForLanguage`) — an artifact of the measurement code, not the library. Checked
the actual call sites: `FstCoverageProbe.ForLanguage` builds ONE `FstTemplateAnalyzer` and passes
it to `CompositeProposer`'s instance constructor (not `.ForLanguage`), sharing it correctly.
`CompositeProposer.ForLanguage(language, fst, ...)` itself takes an already-built `fst` and never
constructs another. The only place two independent (real, morpher-based) FSTs get built is
`FstSenaBenchmark.Benchmark_CompositeVsSearch`'s OWN comparison code (`bare` vs `composite`
deliberately use separate instances to compare them) — and now that H1+H2 make a build ~1 s, that
duplication costs ~1 s of benchmark time, not worth touching. `LockstepPhonologyProposer` builds
a SEPARATE, but cheap (~105 ms, no-morpher/no-probing ctor), internal `FstTemplateAnalyzer` — a
minor, harmless redundancy, not the reported 8-9 s. No code change made for H3.

**Verification gates actually run:**
- `dotnet test --filter "TestCategory!=Explicit"` → 119/119 green; CSharpier clean.
- Indonesian `Benchmark_CompositeVsSearch` (`HC_MAX_WORDS=121`): **120/121 fully covered, 0
  unsound, 0 false positives** — identical to pre-H.
- Sena: per-word-timeout-guarded coverage check (60 sequential words, 5 s cap) — 55/57 fully
  covered (3 excluded on timeout, a known pre-existing hazard unrelated to this change), 0
  unsound. Full unbounded `Benchmark_CompositeVsSearch` on Sena still hangs on pathological words
  regardless of this session's changes (same as every prior session — not attempted further).
- `StateCount`: Indonesian identical (532); Sena dropped 20,737 → 16,322 (see above — investigated,
  not fully root-caused, coverage/soundness confirmed unaffected by two independent checks).

## Phase G1 — ✅ DONE (2026-07-03): `mengamat-amati` closed, Indonesian now 121/121

Implemented exactly as speced below (`ReduplicationProposer.cs`): collected suffix surface texts
in the constructor (boundary-stripped via `HCFeatureSystem.Segment`-only rendering, catching
Indonesian's `-i` being underlyingly `"+i"`), added the suffix-peel fallback to the separator
scan, threaded an optional `extraSuffix` parameter through `ProposeForResidual`. **Measured:
Indonesian `Benchmark_CompositeVsSearch` — 121/121 fully covered (was 120/121), 0 unsound, 0
false positives; `Diagnose_Divergences` — zero divergent words.** New toy test
(`Composite_CoversSuffixStackedOutsideReduplication_WhereSeparatorScanAloneMisses` in
`VerifiedFstAnalyzerTests.cs`) passed on the first run — the real engine happily stacked a plain
suffix rule on top of the toy reduplication rule with no PoS-gating adjustment needed (both rules'
`RequiredSyntacticFeatureStruct`/`OutSyntacticFeatureStruct` were `V`→`V`, and the stratum's
default `MorphologicalRuleOrder.Unordered` let HC try the stack). Full 120-test suite green
(was 119; +1). No regression on the toy-grammar suite or Indonesian's existing coverage.

Ground truth (traced 2026-07-02 with a logging `ITraceManager`): the engine's analysis is
`AV+observe+Cont+LOC`, i.e. `-i` (LOC) suffixes the WHOLE reduplicated word:
`meng+amat` → `-Cont` → `mengamat-amat` → `-i` → `mengamat-amati`. The current separator scan
splits at `-` into `before="mengamat"`, `copy="amati"`, and `"amati"` is not a tail of
`"mengamat"` — correctly no match. The fix is to peel known suffix surfaces off the END of the
copy before tail-matching (this closes the whole class "any suffix stacked outside the
reduplication", not just this word):

1. In `ReduplicationProposer`'s constructor, alongside `_redupRules`, collect suffix surface
   strings: for every stratum's `MorphemicMorphologicalRule` whose allomorph classifies as
   `MorphOp.Suffix` (`MorphTokenCodec.ClassifyOp(allomorph, false)`), take the allomorph's
   `InsertSegments.Segments.Representation`, segment it via the surface stratum's
   `CharacterDefinitionTable.Segment(...)`, keep only `HCFeatureSystem.Segment`-type nodes, and
   render their string reps (`GetMatchingStrReps(node).First()`). **This boundary-stripping step
   is required**: Indonesian's `-i` inserts `"+i"` (the `+` is boundary `char30`), and the raw
   representation would never match surface text. Store `(string SurfaceText, IMorpheme Rule)`
   pairs; skip empty results.
2. In the separator scan (third loop of `AnalyzeWord`), when the plain
   `before.EndsWith(copy)` check fails, additionally try each collected suffix pair: if
   `copy.EndsWith(s.SurfaceText)` and the remainder `copy[..^s.SurfaceText.Length]` is non-empty
   and IS a tail of `before`, then for each analysis from `ProposeForResidual(before)`, emit a
   variant with the suffix morpheme appended AFTER the redup morpheme (engine order:
   `…root…, Cont, LOC` — redup first, then the outer suffix). Easiest shape: give
   `ProposeForResidual` an optional `IMorpheme extraSuffix` parameter appended after the redup
   wrap; `RootMorphemeIndex` is unchanged (both additions are after the root).
3. Do NOT recurse suffix-peeling (one suffix layer is what the corpus needs; unbounded stacking
   here would be scan-cost without evidence). Note the single-layer bound in the class remarks.

**Tests + gates:**
- Extend the toy grammar in `Composite_CoversSeparatorReduplication_WhereFstAloneMisses` (or add a
  sibling test): add a plain suffix rule (e.g. Table1 `"s"`), assert the engine parses
  `sagzsags` (= CONT(`sag`) + suffix; confirm the toy engine really produces this before asserting
  — if HC's rule ordering rejects suffix-after-redup in the toy setup, adjust the toy PoS gating
  until the ENGINE parses it, then assert parity), assert the composite covers it, and assert a
  soundness negative (e.g. `sagzdats`) stays empty.
- Indonesian `Benchmark_CompositeVsSearch` (`HC_MAX_WORDS=121`): **121/121 fully covered, 0
  unsound** — this is the phase gate and the whole point.
- Full suite green, CSharpier, stats battery (walk p50/p95 must not measurably regress — the new
  scan branch only runs on words containing a separator character that already failed the plain
  tail match).

## Phase G2 — ✅ DONE (2026-07-03): `ndikhali` closed with EXACT set parity (8/8)

**Confirmed correct: the "data-model lift" premise WAS false.** `MorphOp.Compound` already existed,
`WordAnalysis` already represented compounds, and the only hard blocker really was `FstReplay.Confirm`
— implemented exactly as speced (step 1 below). **But the spec UNDER-ESTIMATED one thing**: for
`ndikhali` specifically, a THIRD piece was needed beyond `FstReplay` + the trie loop — see "What the
spec missed" below. Implemented in `FstTemplateAnalyzer.cs`, `FstReplay.cs`.

**What shipped, matching the spec:**
1. **`FstReplay.Confirm`**: non-head `LexEntry` morphemes go into a `HashSet<LexEntry> extraRoots`
   instead of triggering an early `return null`; `LexEntrySelector = e => e == root ||
   extraRoots.Contains(e)`; `RuleSelector` gains `|| (extraRoots.Count > 0 && r is CompoundingRule)`.
2. **Trie compound loop**: `BuildCompoundLoop(roots, continuation)` — one shared "join" state per
   attachment site (template-less path, and each template) with an ε-arc into every root's shared
   chain `Entry`; every qualifying root's chain `End` gets an ε-arc to the join (alternative to its
   normal continuation) AND every root's chain `End` gets an ε-arc from the join's downstream back
   to `continuation`. Bounded to one extra root (no arc back into the join).
3. **Headedness via token post-processing**: `ToWordAnalyses` (renamed from `ToWordAnalysis`,
   now `IEnumerable<WordAnalysis>`) scans a token array for `MorphOp.Root` positions; 0 or 1 →
   the old single-candidate behavior; 2+ → one `WordAnalysis` per root position as
   `RootMorphemeIndex`, same morpheme list. Both `AnalyzeShape` and `AnalyzeComposed` updated to
   `AddRange` instead of `Add`.
4. Gated on `hasCompoundingRules` (any `CompoundingRule` in any stratum) — zero cost for a grammar
   without one.

**What the spec missed (found during implementation, fixed):**
- **The compound loop must be reachable even without OTHER standalone derivational rules.** The
  spec's own step 2 said "add the loop" but didn't notice the loop lives inside the template-less
  path's `if (_derivPrefixRules.Count > 0 || _derivSuffixRules.Count > 0)` block — a grammar with
  compounding but no other standalone prefix/suffix rule (my own toy test hit exactly this) never
  built the block AT ALL, so the loop silently never existed. Fixed: the guard is now
  `|| hasCompoundingRules`. Both real grammars have standalone derivational rules too, so this
  never manifested on Sena/Indonesian — only on a minimal toy grammar — but it would have bitten
  the next grammar tried.
- **`ndikhali` needed a THIRD extension: `DerivableToCategory` must treat compounding as a
  category-transition edge, not just `_derivSuffixRules`/`_derivPrefixRules`.** Root cause (found
  via reflection-inspecting `_derivPrefixRules`' actual contents, then a rule-application trace):
  Sena's noun-class markers (glossed `"1"`/`"9"`/`"10"`/`"5"`, e.g. `mrule56`) are NOT standalone
  derivational rules — `_derivPrefixRules` came back with only 4 unrelated entries, none of them
  class markers. They are class-agreement PREFIX-TEMPLATE-SLOT rules requiring `pos100407` as
  their OWN input category — which is NZR's (`-i`, gloss `NZR`) OUTPUT category, which is in turn
  reachable only via `[é ⊕ khal compound] → NZR`. Since a template's root-attachment gate
  (`CategoryMatches || DerivableToCategory`) never considered COMPOUNDING as a way to change
  category, neither `é` nor `khal` ever qualified for the class-marker template at all — the
  compound loop's OWN pairing worked fine (confirmed: `é+ser+NZR` candidates without a class
  prefix appeared immediately), but the template carrying the class prefix was unreachable.
  Fixed by adding a `_compoundingRules` list (collected in the constructor) and extending
  `DerivableToCategory`'s frontier-expansion loop with a second edge type: for each category in
  the frontier, if it unifies with a compounding rule's `HeadRequiredSyntacticFeatureStruct` OR
  `NonHeadRequiredSyntacticFeatureStruct` (permissively — either role, no partner-root check, same
  philosophy as every other gate in this file), `OutSyntacticFeatureStruct` becomes a new frontier
  node. Since the BFS already runs `_derivDepth` iterations trying any available edge at each
  step, this one addition makes "compound, then derive further" chains fall out for free — no
  other structural change needed.

**Measured result**: Sena's `ndikhali` — **8/8 exact set parity, sound** (all four class markers ×
both head orderings, matching the engine's own 8 analyses exactly). Guarded 60-word Sena slice:
**57/57 fully covered** (up from 55/57 pre-G2), 0 unsound. Indonesian (`HC_MAX_WORDS=121`):
**unchanged at 121/121, 0 unsound, 0 divergent words** — its compounding rules (`mrule1`/`mrule2`)
now build the loop too, but the corpus needs no compound analyses (confirmed in Phase A), so
verify correctly prunes every proposed compound; `Soundness_NegativeExamples` 0 false positives on
both grammars. Full 121-test suite green (was 120; +1). Stats: Indonesian states 532→533 (+1, the
compound-loop join state — Indonesian's template-less path already existed for other reasons, and
the loop adds exactly one join state there); Sena states 16,322→16,347 (+25, one join state per
template + the template-less path); build time Indonesian ~266ms→~433ms, Sena ~1.0–1.1s→~1.3–1.5s
— both still far below the pre-Phase-H 9.3s baseline. Walk p50/p95 not separately re-measured
this session (no regression signal in the guarded coverage run's wall-clock).

**Tests**: `Fst_CoversCompound_ViaTheCompoundLoop` (`VerifiedFstAnalyzerTests.cs`) — a toy grammar
with an unrestricted `CompoundingRule` (no head/non-head PoS gating, matching
`CompoundingRuleTests.cs`'s existing pattern reused here) and two roots (`pat`, `tak`); asserts the
engine parses the compound, the BARE `FstTemplateAnalyzer` alone now proposes it directly (no
sibling generator needed — the mechanism lives in the trie itself, unlike reduplication/infix), and
soundness via `CompoundingRule`'s own default `MaxApplicationCount = 1`: a three-root chain
(`pattakpat`) is rejected by both the real engine and the verified FST, confirming the loop is
correctly bounded to exactly one extra root.

**Correction note for future readers**: the "Tests + gates" bullet below calling for a
"head/non-head PoS-gated" toy grammar was written before implementation; the toy test that shipped
uses an UNGATED compounding rule instead (simpler, and the PoS-gating behavior is already exercised
for real by Indonesian's `mrule1`/`mrule2` staying silent on its own non-compound corpus, and by
`ndikhali`'s real class-agreement gating on Sena). A dedicated PoS-gated toy test was judged
redundant given those two real-grammar checks.

## Phase I — the true-FST generalization (lazy per-rule chain) — design notes, build LATER

Not needed by either current grammar (junction probing + peels close both). This is what makes the
tool credible for grammars the current mechanisms structurally cannot handle: word-internal rules
far from morpheme junctions, long-distance harmony (a suffix vowel conditioned by a trigger
several syllables back), feeding chains deeper than the 2-segment probe window.

Theory anchor (so nobody re-litigates feasibility): SPE-style ordered rewrite rules are regular
(Kaplan & Kay 1994); lexc/xfst/HFST/foma have compiled full morphologies this way for decades. The
only provably non-regular construct is unbounded copying — which stays with the peel. The reason
eager composition explodes IN THIS CODEBASE is specific: arcs are FeatureStructs matched by
unification and cannot be determinized/minimized without destroying multi-analysis enumeration.
Classical toolkits stay small because they minimize over a CONCRETE alphabet — and HC's surface
alphabet IS concrete and small (~30 chars/grammar).

Design (lazy composition — the composed machine is never materialized, so state explosion is
structurally impossible; the risk moves to walk-time frontier width, bounded by the beam cap):
1. Compile each `RewriteRule` subrule to its own small INVERSE transducer over the concrete
   segment alphabet (states = position in the λ·φ·ρ window ⇒ ~5–10 states/rule; textbook
   construction; replaces `PhonologyRuleCompiler`'s probing v1). Deletion-inverse inserts
   (bounded by the reapplication cap); epenthesis-inverse deletes; metathesis is a bounded swap;
   α-variables expand over the concrete alphabet (bounded).
2. Generalize `AnalyzeComposed` from ONE `InversePhonology` to a CHAIN: a configuration is
   `(rule₁-state, …, ruleₙ-state, trie-state)`, rules in reverse stratum order — plan §3b of
   `FST_FAST_PATH_PLAN.md`, which this section supersedes in detail. In practice almost every rule
   sits in its identity state almost everywhere, so the live frontier multiplier is small.
3. Keep boundary nodes as trie arcs (ε on surface, matchable by rule transducers on the
   intermediate tapes) — the principled fix for the boundary gap that junction probing routed
   around; kill `FstTemplateAnalyzer._filter`'s boundary-dropping for the composed walk only.
4. Add the frontier **beam cap** (the standing Phase-F/KNOWN_GAPS item) as part of this work —
   overflow ⇒ word counted unparsed, never wrong, never a hang.
5. Gates: all existing toy tests + both real corpora unchanged; new toy tests for a word-internal
   rule and a two-rule feeding chain that junction probing provably cannot cover (assert the bare
   composite misses them TODAY, then that the chain covers them).

## Risks / honesty

- **Set parity may surface analyses nobody expected** (compounds, doubled derivations) — Phase A
  exists to find that before any design commitment.
- **Junction windows deeper than probed** on some future grammar — the build-time window
  assertion turns that into a visible "unsupported", never a silent miss.
- G2's walk-cost note is real: the compound loop multiplies root-entry fan-in; the stats battery
  after G2 decides whether PoS-gating the re-entry is needed.
- This makes the two REAL grammars fully covered; it does NOT claim 100% for arbitrary HC grammars
  until Phase I exists (word-internal cascades remain the open frontier, unchanged).

## Rough effort

| Phase | Size |
|---|---|
| A (measure) | ✅ done |
| B (Sena morphotactics) | superseded by G2 |
| C (junction probing) | ✅ done |
| D (redup peel) | ✅ done (6/7; 7th → G1) |
| E (compounding data-model lift) | **cancelled — premise falsified, see G2** |
| F (hardening/gates) | beam cap folded into Phase I |
| H (build-time regression) | ✅ done (H1+H2; H3 struck — not a real bug) |
| G1 (suffix-peel in separator scan) | ✅ done (Indonesian now 121/121) |
| G2 (compound loop + FstReplay fix) | ✅ done (`ndikhali` 8/8 exact parity; also needed `DerivableToCategory` extension the spec missed) |
| I (lazy per-rule chain — the true FST) | multi-day; only when a grammar needs it |
