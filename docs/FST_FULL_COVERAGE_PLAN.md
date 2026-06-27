# FST full-coverage plan — auditing how much of HermitCrab an FST can cover

Audited by four parallel reviews against (a) the formal-language status of each construct, (b) HC's
implementation, and (c) our FST implementation. "Regular?" classifies the *linguistic operation*
(Kaplan & Kay 1994: a finite composition of concatenation + bounded-context rewrite over a finite
lexicon is a **regular relation**, hence 1-way-FST-able). "Coverage" is what the **proposer**
(`FstTemplateAnalyzer`) actually builds — `VerifiedFstAnalyzer`/`FstReplay` only *confirm or discard*
proposer candidates, so they can never add coverage: **every under-generation must be closed in the
proposer.**

## 0. The headline

Almost all of HC is formally **regular** and therefore coverable by a 1-way FST. The genuinely
non-regular core is tiny: **unbounded full-stem reduplication** (`{ww}`) and an **unbounded
self-feeding rewrite cycle** (HC already caps it at 256). Everything else — affixation, templates,
derivation, **all phonology**, bounded compounding, partial/fixed reduplication, strata — is regular.

But "regular ⇒ coverable" is about the *ceiling*, not what we built. Two findings matter most:

1. **The proposer is only correct for 0-phonology grammars.** Its arcs are built from *underlying*
   segments; it walks the *surface*. Any feature-change/epenthesis/deletion/metathesis desyncs the
   walk, so for a grammar **with** phonology the FST **silently under-generates** (it fails *safe* —
   verify rejects anything spurious, so no wrong analyses — but it misses valid ones). Sena has 0
   phonological rules, which is the only reason it certifies. **This is the single biggest limit on
   real-grammar coverage.** The certification parity-gate catches it (such a grammar won't certify), so
   it is not a *soundness* hole — it is a *coverage* ceiling.
2. **The proposer throws (`NotSupportedException`) on infix / circumfix / reduplication / process
   slots**, aborting the *entire* build rather than degrading. So a grammar with **any** such slot
   can't build the FST at all today. This is a robustness bug, not a math limit.

## 1. Coverage scorecard

**COVERED (proposer builds it):** prefix, suffix, realizational affixes, multiple template slots,
optional slots, slot ordering, root lexicon, category + stratum gating, category-changing derivation
(bounded), bounded derivation (depth ≤ `derivDepth`, default 2, tunable).

**PARTIAL:** derivation depth (capped — deeper stacks silently dropped, caught only by the parity
gate); zero affix (the `[CopyFromInput, InsertSegments(non-empty)]` form is covered; a **true
zero-segment** affix `[CopyFromInput]`-only is dropped/throws — a silent gap); Linear-vs-Unordered rule
order (modeled as a bounded any-order superset — sound via verify, not faithful to the flag).

**COVERABLE (regular; not built — listed with the work + blow-up):**
- **All phonology** — RewriteRule (feature-change / epenthesis / deletion), metathesis, iterative &
  simultaneous application, α-variables, allomorph environments. Regular by Kaplan–Kay. Needs the
  proposer to be built by **composition** (lexicon ∘ affixes ∘ phonology) instead of the underlying-
  segment walk, or phonology folded into a richer verify. Largest single win.
- **Bounded compounding** — regular (capped by `MaxStemCount`, default 2). Needs shared per-category
  stem automata spliced N−1 times (additive in states) **and** an extension to `FstReplay` (which today
  requires a single `LexEntry` root, so it can't even *confirm* a compound).
- **Infixation** — regular (positioned insertion). Needs `BuildRootChain` to split a root mid-stem
  (`pre · infix · post`); ≈2×|root| arcs for infixing roots, bounded.
- **Circumfix** — regular. Needs one morpheme emitted at two surface positions (the `MorphOp` enum
  already has `CircumfixPrefix`/`CircumfixSuffix`, but the codec only ever emits `CircumfixPrefix` —
  `CircumfixSuffix` is dead code).
- **Simulfix / process (`ModifyFromInput`)** — regular (length-preserving feature rewrite). Needs
  feature-mutation arcs; entangled with phonology (the mutated segment must be in the arc condition).
- **Partial / fixed-size reduplication** — regular (bounded copy). Unroll the fixed template into arcs
  (Beesley–Karttunen compile-replace).
- **Strata / cyclicity** — regular (finite composition of per-stratum regular relations); already
  partly modeled via stratum-index gating.
- **MPR features, morpheme/allomorph co-occurrence, allomorph environments, stem names, disjunctive
  allomorphs, obligatory features, bound roots** — all regular, currently **VERIFY-ONLY** and *sound*
  there (HC's real synthesis enforces them). Coverable on arcs but **not worth it**: verify already
  guarantees soundness, so baking them in buys only speed, at a multiplicative state cost. Leave them
  in verify.

**NOT COVERABLE by a 1-way FST (genuinely non-regular):**
- **Unbounded full-stem reduplication** — `{ww : w∈Σ*}` is not regular (not even context-free); a
  1-way FST has no memory for an arbitrary-length copy (Dolatian & Heinz 2020). HC expresses it when a
  `CopyFromInput` part is an unbounded quantifier over the stem.
- **Unbounded self-feeding rewrite cycle** — not finitely bounded; HC tames it with a 256-length cap
  (which *is* a regular fold — see Appendix A).
- (Unbounded recursive compounding/incorporation is non-regular in theory, but HC can't express it —
  `MaxStemCount` is always finite — so it is moot here.)

## 2. Per-feature table (synthesis of the four audits)

| Feature | Regular? | Where handled now | Status | What's needed to cover |
|---|---|---|---|---|
| Prefix / suffix | yes | FST proposer | COVERED | — |
| Template slots / optional / order | yes | FST proposer | COVERED | — |
| Realizational affixes | yes | FST (as slots) | COVERED | feature-blocking deferred to verify (sound) |
| Category + stratum gating | yes | FST build-time gate | COVERED | faithful when stem ⊑ template category |
| Category-changing derivation | yes (bounded) | FST (≤ depth) | COVERED | deeper chains → raise `derivDepth` |
| Derivation depth | n/a | FST cap (2) | PARTIAL | knob; deeper → engine (parity-gated) |
| Zero affix (with segments) | yes | FST | COVERED | — |
| **True zero-segment affix** | yes | throws/dropped | **PARTIAL (bug)** | emit token with no arcs |
| Linear vs Unordered order | yes | FST (any-order superset) | PARTIAL | sound via verify; not flag-faithful |
| **Phonology (all kinds)** | **yes (Kaplan–Kay)** | **engine/verify only** | **COVERABLE (big)** | compile by composition into the proposer |
| **Bounded compounding** | yes | engine/cache | COVERABLE | shared stem automata + extend `FstReplay` |
| Infixation | yes | throws | COVERABLE | mid-stem root split |
| Circumfix | yes | throws (half dead) | COVERABLE | one morpheme, two positions |
| Simulfix / process | yes | throws | COVERABLE | feature-mutation arcs (needs phonology) |
| Partial/fixed reduplication | yes | throws | COVERABLE | unroll bounded copy |
| Strata / cyclicity | yes | partial (gating) | COVERABLE | compose per-stratum transducers |
| MPR / co-occurrence / env / stemname / disjunctive / obligatory / bound | yes | **verify** | VERIFY-ONLY (sound) | leave in verify (speed-only to move) |
| **Unbounded full-stem reduplication** | **no** | engine (escape) | **NOT COVERABLE (1-way)** | length-cap / detect-peel / 2-way FST |
| Unbounded self-feeding cycle | no (capped) | engine (256-cap) | NOT COVERABLE (unbounded) | length-cap fold |

## 3. Architecture changes / optimizations / reconfigurations

**A. Graceful degradation instead of `throw` (do now — robustness).** The proposer must never abort a
build on an unbuildable construct. On an infix/circumfix/reduplication/process slot (and any construct
it can't model), it should **skip that path and ensure the grammar is not certified** (so those words
route to the engine), exactly as it already does for non-regular escapes. Today a single such slot
throws `NotSupportedException` and kills the whole FST — so the analyzer is unusable on most real
grammars. This one change makes the FST **safe on any grammar** (full coverage where it can, engine
backstop where it can't), which is the right "as much as we can get" posture.

**B. Fix the true zero-segment affix (do now — small).** Emit the morpheme token at a token-bearing
state with no segment arcs (the mechanism already exists for empty-insert affixes). Today it is a
silent under-generation or a throw.

**C. Phonology by composition (follow-on — the big coverage win).** Replace/augment the hand-rolled
underlying-segment walk with the textbook construction: compile `Lexicon ∘ Affixes ∘ Phonology`
(each `RewriteRule` already carries everything needed to emit its transducer) and analyze the surface
through the composed, **minimized** machine. This is what lifts the FST from "0-phonology grammars
only" to the majority of real grammars. Risks: multiplicative state blow-up before minimization (use
lazy/per-stratum composition + the existing `Determinize().Minimize()` for variable-free layers), and
α-variable expansion (arc multiplication by feature cardinality). Verify-only cannot substitute —
`FstReplay` can reject but not *generate*, so phonology must enter the proposer.

**D. Bounded compounding (follow-on — highest discrete coverage gain).** Build per-category shared stem
automata, splice up to `MaxStemCount`, emit `Compound`/`Root` tokens — **and extend `FstReplay`** to
confirm multi-root candidates (today it hard-requires a single `LexEntry` root, so a compound can't be
verified even if proposed). Additive in states (Σ category automata × depth), not multiplicative.

**E. Keep soundness constraints in verify (decision, not work).** MPR, co-occurrence, environments,
stem names, disjunctive allomorphs, obligatory/bound — all sound in verify because verify *is* HC's
synthesis. Baking them into arcs buys only speed at a state cost; the over-generation they cause is a
few cheap rejected candidates per word. Leave them.

**F. The certification interlock is the safety contract (preserve + strengthen).** `certified =
FST-closed ∧ set-parity`. The parity check is what catches proposer gaps (phonology, compounding,
depth) even when closure says "regular" — so **a phonology-bearing or reduplicating grammar must never
certify**, or `AnalyzeWord` (which skips the engine when certified) would silently under-generate.
`GrammarFstClosure.IsEscape` flags reduplication/infix; ensure the proposer's *coverage* limits
(phonology, compounding, depth-truncation) are likewise reflected so certification can't outrun what
the proposer actually builds. The empirical parity gate already enforces this; make it explicit.

## 4. Roadmap — close this PR vs. follow-on

**This PR (mathematically sound, tractable, robustness):**
- **A. Graceful degradation** (no throw → skip + don't certify). Makes the FST usable on any grammar.
- **B. Zero-segment affix** fix (close the silent gap).
- **F. Certification guard** — verify (it already holds via parity) and document that only
  fully-covered, FST-closed grammars certify; everything else uses the engine/cache backstop.
- Tunable `derivDepth` (already shipped) + document depth-truncation as parity-gated.

**Follow-on PR(s) (the bigger builds, in value order):**
1. **Phonology by composition** (C) — unlocks the majority of real grammars.
2. **Bounded compounding** (D) — biggest discrete construct gain; needs the `FstReplay` extension.
3. **Infix / circumfix / partial-reduplication / simulfix** — the remaining concatenative/bounded
   constructs (each COVERABLE; medium effort).
4. **The non-regular core** — Appendix A.

---

## Appendix A — closing the gap on the non-FST-able constructs

Two HC constructs are genuinely non-regular for a 1-way FST: **unbounded full-stem reduplication**
(`{ww}`) and the **unbounded self-feeding rewrite cycle**.

### A1. Unbounded reduplication
- **Length-cap fold.** Unroll `{ww : |w| ≤ L}` into explicit arcs for a chosen max reduplicant length
  L (e.g. the longest lexical stem). Sound + complete up to L; FST grows with L×|Σ|; longer stems fall
  to the engine. Precedented — HC itself caps the self-feeding cycle at 256.
- **Detect-and-peel (Beesley–Karttunen compile-replace).** Detect an adjacent repeated span, peel one
  copy, analyze the remainder with the regular grammar. For copy, **detection == parsing** (a
  reduplicant *is* an adjacent repeat), so the live work is a cheap repeat-scan + peel; ambiguous peels
  resolved by verify. The standard finite-state-morphology tool.
- **2-way FST (Dolatian & Heinz 2020).** A two-way transducer re-reads its input and computes `{ww}`
  exactly, staying linear-time. The *correct* device, but the current 1-way NFA walk would need a
  two-way execution engine — the largest change.
- **Sound detector + engine backstop (current posture, recommended default).** Keep the proposer
  reduplication-blind; `GrammarFstClosure.IsEscape` flags it → grammar not certified → those words go
  to the engine via the cache. Zero blow-up, always correct, slower only on reduplicating words.
  Combine with the length-cap fold as an opportunistic fast path for short stems.

### A2. Self-feeding cycle
Already closed by a length-cap (shape ≤ 256). To FST-ize, bake the same cap as a maximum-length
acceptance bound; identical tradeoff to the reduplication length-cap.

## Appendix B — do current architecture decisions help or hinder the non-FST-able work?

**HELP — verify-by-re-analysis + engine backstop.** The proposer is *allowed* to be
sound-but-under-generating: every kept analysis is a genuine HC analysis, and the FST need not model
reduplication/compounding/phonology at all — those words are quarantined to the complete engine.
Adding any Appendix-A mechanism later only *widens* the fast path; it cannot break soundness, because
verification re-runs HC end to end.

**HELP — the escape-aware codec + closure.** `MorphTokenCodec.ClassifyOp` already distinguishes
`Reduplication`/`Infix`/`Compound`/`Process` from concatenative ops, and `GrammarFstClosure` consumes
those tags. A future reduplication/compounding builder has a ready, principled signal for which rules
to special-case.

**HAZARD to preserve — certified-skip.** A certified grammar skips the engine entirely. A grammar with
a non-regular construct must therefore *never* certify. The interlock (`closed ∧ parity`, with closure
flagging escapes and parity catching proposer gaps) is what guarantees this — it is the explicit safety
contract tying "construct ∉ regular" to "never skip the engine for it." Keep it inviolable as coverage
grows.

**NEUTRAL — the 1-way template walk.** Bounded folds (length-cap, detect-peel) and all the COVERABLE
concatenative constructs fit the existing 1-way walk as "more arcs." The only thing it blocks is the
exact **2-way FST** reduplication solution (A1), which needs a different execution model — a
reconfiguration to weigh only if unbounded reduplication becomes a priority grammar.

### Citations
Kaplan & Kay 1994 (regular relations; closure under composition → phonology, strata, bounded
compounding); Dolatian & Heinz 2020 (2-way FSTs compute reduplication; 1-way cannot); Chandlee 2017
(subregular morphology; partial reduplication is local/regular); Beesley & Karttunen 2003 (compile-
replace for bounded reduplication).
