# HermitCrab optimization ledger — tried, closed, do not retry

**Purpose: stop this work being redone.** One row per optimization attempted, what was expected,
what actually happened, and the number that settled it. If you are about to try something on this
list, read its row first — several of these look irresistible on paper and three of them have
already been independently rediscovered and re-retracted.

Deliberately in-repo and durable, not evicted into a PR accordion. A closed avenue is only closed
if the next person can find out cheaply that it is closed.

Sources: `hermitcrab-packed-forest-research.md` (theory + prior branches),
`hermitcrab-forest-memo-plan.md` (key narrowing), `hermitcrab-forest-memo-ceiling.md`
(predictions), `hermitcrab-synthesis-fold-probes.md` (measurements, sections 6–10).

---

## Shipped and working

| # | Optimization | Expected | Outcome | Number |
| --- | --- | --- | --- | --- |
| 1 | Memoize analysis morphological-rule cascade | Same state reached by many rule orders; cache it | Works, but the cascade was never the cost | 2,555 expansions vs a 2,546-state floor; 1.4 s of a 30.5 s word |
| 2 | Memoize affix-template battery (same key) | Battery was 93% of wall time | **The big win.** Reduced how *often* the battery runs | 38,840 runs -> 2,581; word 30.5 s -> **6.1 s** |
| 3 | Shape sharing at clone (`CloneShareFrozenShape`) | Eager deep-copy of Shape dominates allocation | Works, allocation only | **-4.5 to -7.6% bytes**, no wall change |

## Closed — measured, does not pay

| # | Optimization | Expected | Why it failed | Number |
| --- | --- | --- | --- | --- |
| 4 | **Narrow `AnalysisStateKey`** (drop strictly-shrinking rules' un-application counts) | `MaxApplicationCount` defaults to 1, so the count multiset is "which subset of rules was used" — 2^27 states. Dropping 19 of 27 rules gives 2^8 | Shape + syntactic FS + realizational FS *already* discriminate. The count component was implied by its neighbours. A worst-case bound collapsing is not the realised state count collapsing | Realised state collapse **Sena 1.12x** vs a 1.3 gate; 6 of 7 heavy words exactly **1.00**; `atawirambo` 2,556 -> 2,556 |
| 5 | **Fold-step sharing memo** (share deterministic synthesis fold steps by computed value) | Probe measured 3.22x and 8.10x shareable steps; ~50% wall ceiling on two reliable fixtures | A sound key must carry the **ordered remaining trail**. Two candidates then have to agree on their entire future to share a step, which almost never happens | **hits = 0** on the best fixture; **0.96x** (4.5% *slower* — pure key-construction cost); 1.06x on the second, inside 21.9% noise |
| 6 | **Order-insensitive synthesis-input dedupe** | 9,774x duplicate synthesis inputs | HermitCrab's morphological rules are **not order-invariant**: same rule multiset, different order, different output. Merging loses parses | 2 violations Sena, 12 independently Indonesian (e.g. `{meN, -Cont}`). Sound version: **15–40%** |
| 7 | **Trail-position indexing of synthesis rules** | 11,445,538 of 11,445,538 rejections are "this rule is not the pending trail rule" — 100% of the histogram | Each rejection is one array index plus one reference compare. Count is not cost | 11.4M x 29 ns = 0.33 s of 143 s = **0.2%** |
| 8 | Synthesis-side length bound (Gate A) | Reject candidates that cannot reach the surface length | At the comparison point the candidate is still the bare root; its affix trail applies later inside `_synthesisRule.Apply`. Rejected valid parses | Unit suite 64 -> 34 passing. Reverted |
| 9 | Analysis length ceiling (Gate B) | Same idea, analysis side | Sound and correct. No available corpus contains the pathology it prunes | Byte-identical output, **no measurable speedup** (~4–7% slower, within noise) |
| 10 | Lexical reachability gate (Phase 5) | Prune candidates from which no lexicon root is reachable | Both reference corpora have real compounding in their deepest stratum, which disqualifies the gate everywhere | Proven **no-op** on both corpora |
| 11 | Tandem lexical intersection (T2) | Kill doomed branches early via the lexicon | The oracle only sees lexical unreachability. The expensive words fail on checks that run *after* lexical lookup succeeds | Pooled **23.5%** dead steps vs a 30% gate; one failure word at exactly **0.0%** dead |
| 12 | Pool small short-lived collections | Fewer allocations, faster | `HashSet/Dictionary.Clear()` is O(capacity); Gen0 already beats pooling at this size | -15–17% bytes but **+8.6% wall**, +12% on the parallel battery. Reverted |

## Closed before building — motivation refuted by measurement

| # | Optimization | Expected | Why it died |
| --- | --- | --- | --- |
| 13 | Surface-length pruning inside the synthesis fold (P2) | "The only lever on Amharic's ~160 ms per synthesis run" | Amharic's forward synthesis is **14 ms**. The 160 ms was arithmetic on an unmeasured denominator — dividing 30 s by 186 synthesis inputs and assuming the time was there |
| 14 | Nogood lattice subsumption (P4) | Nogood hits dominate positive replays **434,628 to 25,102** — the engine mostly proves subtrees *empty*, and generalising "empty" over the feature lattice would turn nogoods from points into regions | **CLOSED on direct evidence** (it was previously closed on adjacent evidence, which is why it was reopened). Census over every empty-expansion key: `cinacemerwa` 16,176 expansions -> 16,176 distinct keys -> **16,176 singleton buckets, 0 subsumable (0.0%)**; `kukucitirani` 12,595 -> 12,595 -> **12,595 singletons, 0 subsumable**. Not one pair of nogood entries even shares a `(shape, stratum, counts, non-head)` bucket, so there is nothing for a subsumption ordering to act on. Breadth check across all 33 conformance fixtures agrees exactly: 7,493 empty expansions, 7,398 distinct keys, **0 subsumable (0.0%)** pooled, every non-empty bucket a singleton. Direction checked explicitly as `earlier.Subsumes(later)` (E subsumes Q, general over specific); the reverse would be unsound. Independently reconfirms row 4 from a different angle: the keys are maximally discriminating |
| 15 | Move constraints into the analysis key (Maxwell & Kaplan's category-splitting) | Their biggest measured win: make the chart prune what the constraint solver otherwise would | Everything synthesis rejects on is root- or realization-dependent, and the root is unknown until `LexicalLookup`. Also a Sena analysis state costs ~0.73 ms while a synthesis input costs at most ~0.12 ms — **states are dearer than synthesis inputs** |

## Open

| # | Optimization | Status |
| --- | --- | --- |
| 21 | **Memo-key caching on the frozen `Word`** | **CLOSED.** Keys are built ~28x per distinct state, which looked like a cheap win. Measured cost of all `AnalysisStateKey` construction and hashing on Amharic: **3 ms of 81,707 ms = 0.0%.** Same error shape as #7: high count, no cost |
| 22 | **Analysis-side rule prefilter** | 94.4% of Sena allomorph pattern matches fail (6,609,627 of 7,005,273) and 86.7% of 4,327,182 rule attempts reach matching, so the target is real | **CLOSED, cap ~10%.** The reopening hypothesis was that Sena's battery would badly understate the Amharic-derived ~11.5% bound. **It does not.** Sena `anBattery` matcher share is **21.2%**, against a >=30% gate — does not fire — giving a cap of 47.6% x 21.2% = **~10.1%**, essentially the same bound. Contended run; the gap to the gate is wide enough that contention will not close it |
| 16 | **`ExpandAlternatives` dedupe** | 20.7% of Sena wall, the largest bucket nobody had instrumented. Apparent census: 395,026 alternatives -> **61** distinct (0.0%), 85% of duplicates same-analysis-word | **CLOSED on the sound census.** With the pending-trail content added to the key, 395,026 -> **351,414 distinct (89.0%)** -- only **11%** are duplicates at all, and all 43,612 of them are same-analysis-word. Sound ceiling = 20.7% x 0.110 = **2.28% of Sena wall**, against a gate of >=10% to build and <5% to close. **The fourth apparent redundancy to evaporate under a complete key** |
| 17 | **Per-node cost in the analysis cascade** | 84% of the Amharic cascade is not linguistic computation; clone is 46.7% and 37.4% was unattributed | **Partly settled, remainder still open.** (a) The remainder split **MISSES its <15% gate by ~2.5x** — still ~38% on Amharic `anCascade`. Memo, Trail and **Equality each measured <0.2%**, so `Word.ValueEquals` behind the cascade's `HashSet<Word>` is *not* the missing mass, contrary to the standing hypothesis. Leading unbracketed candidate is **GC pause time**, which netstandard2.0 cannot measure directly (`GC.GetTotalPauseDuration` is .NET 7+); a collection census over 24 Amharic words gives gen0 8,217 / gen1 2,729 / gen2 301, an unusually high gen1+gen2 ratio consistent with — not proof of — GC as the remainder. (b) **Clone-discard fraction = 66.2%** (created 2,505, discarded 1,659), a count and therefore contention-immune, against a >=70% gate: **misses by ~4 points, so the build is "make a clone cheaper", not "defer cloning"**. Scoped to `AnalysisAffixTemplateRule`'s eager per-template clone, the one clone-before-verification site; the other four analysis-side sites clone only after a successful match and were excluded rather than blended in |
| 18 | Corpus-scope memoization | `AnalysisScope` dies per word, but `AnalysisStateKey` is word-independent by construction, so entries are reusable across a corpus. Nogood hits dominate positive replays 10-17x, making cross-word nogood reuse the cheapest available form of reuse | **CLOSED, and worse than a no-op.** Indonesian, 121 words, **serial on a clean machine: 1.015x, 1.4% wall-clock reduction** against a >=20% build gate and <5% close gate. Costs **68 MB** added peak managed memory. Cross-word reuse is real but small: 39 memo, 73 nogood, 237 template-nogood hits. Token-vs-type: **121 tokens, 121 distinct**, so no type-level dedupe is available either. **A first, contended run of the same harness reported 2.385x / 58.1%** with a 210.9% spread on the shared arm against 13.7% on the baseline; two testhosts were live. The counts were byte-identical across both runs — only the timing was contaminated, and min-of-N on a high-spread arm picks the luckiest sample. Analysis-set equality held throughout, so the key genuinely is word-independent. **Sena, first 300 words, is the decisive case: the shared scope is 28-32% *slower* than per-word scopes** (two clean serial runs, no contention: 282.7s vs 372.2s and 308.9s vs 396.7s baseline-vs-shared; deterministic counters byte-identical across both runs), and costs **~29.3 GB** added peak managed memory against a 2 GB budget. Root cause: `AnalysisScope.MaxMemoEntries` (100,000) is a *per-scope*, not per-word, budget. Two heavy early words (#74, #76 of 300) alone fill the shared `Memo` table to the cap and `TemplateMemo` to 55,071; every word after that loses the free within-word memoization a fresh per-word scope would have given it for nothing, because `Store` silently no-ops past the cap (see its comment) with no eviction. Cross-word hits (shared minus baseline) are **negative** — memo -77,648, nogood -747,607 — i.e. sharing produced *fewer* total hits than 300 independent fresh-table runs would have on their own; the small `+820` template-hit / `+9` template-nogood gain does not come close to covering it. Amharic, capped at 28 words (word 29 hangs, excluded per standing guidance): **0.9% wall-clock reduction**, table nowhere near the cap (Memo 1,251, TemplateMemo 652 of 100,000), consistent with row 17's finding that Amharic's cost is dominated by clone/allocation per node rather than distinct-state count, so avoiding a handful of cross-word re-derivations barely moves wall time. Token-vs-type on all three corpora, at the runs' own sizes and on each corpus's full word list: **zero duplicate tokens** — whole-word caching is not available as a cheaper alternative anywhere here. No analysis-set divergence on any corpus, at any scope regime: the stop condition that would have put rows 1-3 in question did not fire. **A shared-but-capped memo table is not "the same optimization, smaller ceiling" — past the cap it actively takes away the free per-word memoization the product already has, and retains every live entry for the run's full duration instead of per-word, which is where the 29 GB comes from.** Any future attempt needs real eviction (LRU, generational, or per-word sub-budgets) before this is worth re-measuring, and even then the >=20% gate looks unreachable given Indonesian's ceiling under an uncapped-in-practice regime was already only 1.4% |
| 19 | Contexted constraints / abstract feature-only replay | **CLOSED.** Cost-weighted the P1b die-point histogram (count → wall-time per die point, gated before the run: shape-free cost ≥60% builds, <30% closes). Sena `atawirambo` **6.0%**, `cinacemerwa` **0.0%**, Amharic 28 words **0.4%** — all fire the close gate, none come close to the middle band. All 33 conformance fixtures measured too (breadth, not part of the gate): 24 close, 3 fire the build gate (`morphotactic-attribute-breadth` 67.1%, `mpr-group-overwrite-without-realizational` 91.5%, `prefixal-discontinuous-slot-dependency` 71.8%), 3 land in the undecided 30–60% band, 2 have zero rejections, 1 has zero words. **All three build-gate fixtures are sub-50 ms** (38.15 ms / 1.49 ms / 4.28 ms wall) — the method-rules-earned "cannot carry a timing claim" flag, so they do not override the real-corpus verdict. On the three real grammars, count and cost agree in direction (`RuleNotApplicableOrPatternMismatch`, shape-dependent, dominates both). Among the fixtures, one reliable one (wall ≥50 ms) shows the count/cost divergence the gate exists to catch: `suffixing-extension-slot-ordering` (53 words, 63.98 ms) shape-free count 12.9% vs. cost 53.3% — cost roughly 4x count, same direction as the ApplicationCount/MprFeatures/FeatureUnification checks costing more per event than pattern-mismatch's near-O(1) trail check. (`feature-gating-breadth`, 28.6% vs. 56.8%, shows the same direction but is sub-50 ms and unreliable, same flag as the three build-gate fixtures.) Orthogonal to classification: die-point *rejection* cost itself is negligible against wall time on all three real-corpus runs regardless of class (atawirambo ~19.8 ms of 11,547 ms wall = 0.17%; cinacemerwa ~112.9 ms of 57,275 ms = 0.20%; Amharic ~88.8 ms of 91,456 ms = 0.10%) — reconfirming row 7/6.2's finding that rejections are cheap. Abstract replay would be optimizing a phase that costs approximately nothing on the two grammars this project treats as ground truth. Not built |
| 20 | **Row 5's family on `GenerateWords`** (generation, ~100% synthesis share vs parsing's ~5%/~0.3%) | Row 20 was unmeasured; generation's share is ~100% so row 5's fold ratios would apply at face value if they transferred. `UseSynthesisFoldMemo` had never been wired into `GenerateWords` (only into the parse-then-synthesize path) — wired in under the same untraced/`MaxDegreeOfParallelism==1` restriction to make this measurable at all | Hits are no longer uniformly zero: 3 of 23 lookup-reaching fixtures clear the 5% gate (diacritic-segments 33.3%, suffixing-vowel-harmony 28.6%, suffixing-evidential-adjacency-chain 37.7%; a 4th sits just under it at 4.8%), reopening row 5 narrowly for generation. But the reopened case recloses immediately: the A/B on the best fixture shows the wall-clock "win" sitting inside its own noise floor, and the pooled hit rate is dominated by `deep-optional-affix-nesting` (925 of 1,613 lookups) at hits=0, reproducing row 5's exact 4.5%-slower key-construction cost on the identical fixture | Parity: 0 divergences, 1,236 generations, 32/33 fixtures (33rd throws identically both sides). Best case: 130 hits / 5 reps (37.7% hit rate), **7.8%** wall-clock reduction inside a **22.2%** off-arm spread — not a result. 19/23 lookup-reaching fixtures at exactly **0%**; pooled hit rate **2.67%** (43/1,613), skewed by the one large zero-hit fixture |

---

## The lesson that generalises

Three independent measurements, three boundaries, same collapse:

| measurement | key used | apparent | sound |
| --- | --- | --- | --- |
| synthesis-input dedupe (#6) | order-insensitive | 9,774x | 15–40% |
| fold-entry census | trail position only | 6,476x | not established |
| fold-step sharing (#5) | trail position only | 3.22x / 8.10x | **hits = 0** |

**The redundancy in HermitCrab's synthesis is apparent, not real. The trail is what makes each
step distinct, and every measurement showing large shareable work is measuring a key that omits
it.** The thing that makes sharing visible is the thing that makes sharing wrong.

That closes a *family*: packed parse forests, fold-step sharing, and synthesis-input dedupe all
require distinct derivations to converge on a genuinely identical state. In this engine they do
not converge. It is the same fact as the rules being non-order-invariant, seen from the other side.

## Where the time actually is

Measured with eight exclusive buckets summing to wall (`unaccounted` 0.1% on Amharic, 1.5% on Sena):

| grammar | breakdown |
| --- | --- |
| **Sena** heavy words | analysis template battery **51.4%** (still, after the 5x memo), `ExpandAlternatives` **20.3%**, analysis cascade 18.4%, **all synthesis ~5%** |
| **Amharic** | analysis cascade **~95%** of analysis, which is 99.5% of wall; **all synthesis 0.3%** |

**Every optimization in this ledger that worked reduced how many nodes are visited. None touched
what a node costs.** Amharic spends **~170 ms per analysis state** in a cascade already at its
state-count floor.

### Inside one node (Amharic, 28 words, both probe layers live)

| bucket | `anCascade` 81,707 ms | `anBattery` 7,368 ms |
| --- | --- | --- |
| clone / freeze / allocation | **46.7%** | 41.9% |
| **remainder (unattributed)** | **37.4%** | 15.4% |
| matcher traversal | 12.5% | **39.3%** |
| FeatureStruct algebra | 3.4% | 3.5% |
| memo-key construction | 0.0% | 0.0% |

Censuses: 27,385 rule attempts — guards reject 19.5% (application-count 6.3%, unifiability 13.2%),
**80.5% reach matching**. Of 25,178 allomorph pattern matches, **1,938 succeed (7.7%)**, 23,240
fail (**92.3%**).

**The headline: only ~16% of the Amharic analysis cascade is linguistic computation.** Pattern
matching plus feature unification is 15.9%. Clone plus unattributed machinery is **84%**. The
engine spends its time constructing and discarding objects, not deciding linguistic questions.

**Gate outcomes, written before the run.** "Any bucket >=50% becomes the sole build target" — clone
is 46.7%, does not fire. "Pattern matching dominates *and* >=80% of attempts fail -> build a
prefilter" — 92.3% do fail, but matching is 12.5%, so it does not dominate; does not fire.
**"Remainder >=30% means the decomposition is incomplete and that is the result" — 37.4%, FIRES.**

So there is no declared build target yet. The next probe splits the 37.4%: `RuleCascade` /
enumerator orchestration, trail dictionary operations, LINQ allocation, memo lookups, GC.

**A caution on the 46.7% before anyone acts on it.** `Word.Clone` being half the cascade does not
by itself mean cloning is waste — the cascade forks a `Word` per rule application precisely so it
can explore alternatives without mutating shared state. The deciding question is what fraction of
clones are *discarded unused*: cloned, rejected by a guard or a failed match, thrown away. Given
92.3% of pattern matches fail, that fraction is plausibly very high, but it is unmeasured. If most
clones are discarded, the lever is **not cloning until a rule commits**. If most survive, the lever
is **making a clone cheaper** (copy-on-write shape, which the RUSTIFY branch already explored).
Different builds; measure first.

## Method rules earned here

1. **Counting is not timing.** Three published findings in this work were retracted, all arithmetic
   on an unmeasured denominator: "synthesis is the bottleneck" (candidate counts), "Amharic is
   synthesis-bound" (30 s / 186 inputs), "fixtures only look synthesis-heavy because they are small"
   (divided by `synForward`, which excludes the buckets the work runs in).
2. **A ratio measured with an incomplete key is not a ratio.** See the table above, three times.
3. **Warm-up dominates small fixtures.** A first A/B here showed 1.53x purely from JIT; the off-arm
   alone varied 42.7% between its own first and second sample. Discard warm-up, take min of N,
   interleave arms, and print the off-arm spread as a noise floor beside every speedup.
4. **Sub-50 ms fixtures cannot carry a timing claim.** One 10 ms fixture's share moved 1.3% -> 14.5%
   between two runs of identical code.
5. **Three grammars minimum, reported unpooled.** Maxwell & Kaplan measured a 100x swing between two
   variants of one grammar. A pooled average across fixtures of different size has had to be
   retracted twice in this project.
6. **Search completeness is never traded for speed.** HermitCrab is the permanent fallback engine
   behind the FST work.
