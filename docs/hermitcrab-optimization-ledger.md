# HermitCrab optimization ledger â€” tried, closed, do not retry

**Purpose: stop this work being redone.** One row per optimization attempted, what was expected,
what actually happened, and the number that settled it. If you are about to try something on this
list, read its row first â€” several of these look irresistible on paper and three of them have
already been independently rediscovered and re-retracted.

Deliberately in-repo and durable, not evicted into a PR accordion. A closed avenue is only closed
if the next person can find out cheaply that it is closed.

**This is the only report.** The working docs it was distilled from
(`hermitcrab-packed-forest-research.md`, `hermitcrab-forest-memo-plan.md`,
`hermitcrab-forest-memo-ceiling.md`, `hermitcrab-synthesis-fold-probes.md`) are chronological
process records containing retracted intermediate claims, and are deliberately **not** merged.
They are preserved only as this record. **The evidence branches were deleted after the round
closed**, along with all instrumentation: research scaffolding is not worth maintaining in two
product assemblies, since the probes gate on `volatile bool` reads inside `Matcher.cs`, a hot inner
loop in `SIL.Machine` used by every consumer.

**`docs/hermitcrab-probe-design.md` is the replacement.** It carries the probe architecture and its
two-assembly gate trap, the eight exclusive wall-time buckets and why `synForward` must be recorded
net of cascade and battery, the per-node decomposition, the census designs, all three key designs
with field-level audits, the classifier's direction trap, and the full A/B methodology with the
three measurement failures that forced each part of it — enough to rebuild any probe in a day. The
expensive part was never the code; it was learning what to measure and which measurements lie.

**No instrumentation is merged, by design.** The probes gate on `volatile bool` reads inside
`Matcher.cs` â€” a hot inner loop in `SIL.Machine` used by every consumer â€” which is an acceptable
cost for a measurement run and not one to carry in product code indefinitely. Rebuilding a probe
from a branch is cheap; maintaining research scaffolding in two product assemblies is not.

## Final disposition of the September follow-up

The follow-up branch does deliver product improvements. Its authoritative summary is
`hermitcrab-perf-2026-09-counts.md`, and each retained optimization has one record under
`docs/optimizations/`. The retained set is: deferred template cloning, feature-structure traversal-state
reuse, frozen-flat-tree equality, frozen-flat-tree cloning, single-pass `Shape.CopyTo` mapping,
copy-on-write shape sharing, flat FST register arrays, the analysis edge-segment prefilter, and
copy-on-write syntactic feature structures.

Four attractive experiments were deliberately excluded from the final product diff and are recorded under
`docs/rejected-optimizations/`: the negative two-pass nondeterministic traversal, the inert final-template
prune, the zero-value alternatives memo/hoist, and the low-ceiling compounding split prefilter. This final
disposition supersedes intermediate “ships” language for those experiments later in this historical ledger.

The minimized branch passed all 603 runnable tests in the conformance-inclusive suite (one expected
Windows symlink-privilege skip). A fixed three-word sample had exact output parity on Sena, Amharic,
Indonesian, Mbugwe, and Aweti; only Amharic (1.76x) and Mbugwe (1.83x) cleared the recorded spread and timing
reliability rules. The other three ratios are retained as directional evidence only.

## Earlier ledger round: net speed delivered none

No optimization in the "closed" section shipped, and none of them made anything faster. The
fastest thing tried was 4% **slower**. The pre-existing wins in this codebase (rows 1â€“3) predate
this work. What this effort produced is the measurement, the closed family, the located target,
and this ledger.

---

## Earlier shipped results

| # | Optimization | Expected | Outcome | Number |
| --- | --- | --- | --- | --- |
| 1 | Memoize analysis morphological-rule cascade | Same state reached by many rule orders; cache it | Works, but the cascade was never the cost | 2,555 expansions vs a 2,546-state floor; 1.4 s of a 30.5 s word |
| 2 | Memoize affix-template battery (same key) | Battery was 93% of wall time | **The big win.** Reduced how *often* the battery runs | 38,840 runs -> 2,581; word 30.5 s -> **6.1 s** |

### Retracted historical prototype

Row 3 in the original ledger claimed that shape sharing through `CloneShareFrozenShape` had shipped. No
reachable production commit carried that implementation. Its historical prototype measured 4.5–7.6% fewer
allocated bytes and no wall-time change. The September follow-up independently built a different, retained
copy-on-write design through `CloneForEngine`; see `docs/optimizations/shape-sharing-on-clone.md`.

## Closed â€” measured, does not pay

| # | Optimization | Expected | Why it failed | Number |
| --- | --- | --- | --- | --- |
| 4 | **Narrow `AnalysisStateKey`** (drop strictly-shrinking rules' un-application counts) | `MaxApplicationCount` defaults to 1, so the count multiset is "which subset of rules was used" â€” 2^27 states. Dropping 19 of 27 rules gives 2^8 | Shape + syntactic FS + realizational FS *already* discriminate. The count component was implied by its neighbours. A worst-case bound collapsing is not the realised state count collapsing | Realised state collapse **Sena 1.12x** (state-weighted) vs a 1.3 gate; **4 of 7** heavy probes exactly 1.00, including the 2,556-state probe. The three that moved were 1.13x, 1.16x, and **1.38x**, and all returned zero parses: R tracks failure, not size. **Reopening condition:** a grammar with genuinely high R; the census is cheap and is the right first question to ask of any new grammar |
| 5 | **Fold-step sharing memo** (share deterministic synthesis fold steps by computed value) | Probe measured 3.22x and 8.10x shareable steps; ~50% wall ceiling on two reliable fixtures | A sound key must carry the **ordered remaining trail**. Two candidates then have to agree on their entire future to share a step, which almost never happens | **hits = 0** on the best fixture; **0.96x** (4.5% *slower* â€” pure key-construction cost); 1.06x on the second, inside 21.9% noise |
| 6 | **Order-insensitive synthesis-input dedupe** | 9,774x duplicate synthesis inputs | HermitCrab's morphological rules are **not order-invariant**: same rule multiset, different order, different output. Merging loses parses | 2 violations on Sena, 12 independently on Indonesian. Sound version: **15â€“40%** |
| 7 | **Trail-position indexing of synthesis rules** | 11,445,538 of 11,445,538 rejections are "this rule is not the pending trail rule" â€” 100% of the histogram | Each rejection is one array index plus one reference compare. Count is not cost | 11.4M x 29 ns = 0.33 s of 143 s = **0.2%** |
| 8 | Synthesis-side length bound (Gate A) | Reject candidates that cannot reach the surface length | At the comparison point the candidate is still the bare root; its affix trail applies later inside `_synthesisRule.Apply`. Rejected valid parses | Unit suite 64 -> 34 passing. Reverted |
| 9 | Analysis length ceiling (Gate B) | Same idea, analysis side | Sound and correct. No available corpus contains the pathology it prunes | Byte-identical output, **no measurable speedup** (~4â€“7% slower, within noise) |
| 10 | Lexical reachability gate (Phase 5) | Prune candidates from which no lexicon root is reachable | Both reference corpora have real compounding in their deepest stratum, which disqualifies the gate everywhere | Proven **no-op** on both corpora |
| 11 | Tandem lexical intersection (T2) | Kill doomed branches early via the lexicon | The oracle only sees lexical unreachability. The expensive words fail on checks that run *after* lexical lookup succeeds | Pooled **23.5%** dead steps vs a 30% gate; one failure word at exactly **0.0%** dead |
| 21 | **Memo-key caching on the frozen `Word`** | Keys are built ~28x per distinct state, which looked like a cheap win. | High count did not imply high cost; caching would add state and invalidation complexity around negligible work. | **CLOSED:** all `AnalysisStateKey` construction and hashing on Amharic measured **3 ms of 81,707 ms = 0.0%**. |
| 12 | Pool small short-lived collections | Fewer allocations, faster | `HashSet/Dictionary.Clear()` is O(capacity); Gen0 already beats pooling at this size | -15â€“17% bytes but **+8.6% wall**, +12% on the parallel battery. Reverted |

## Closed before building â€” motivation refuted by measurement

| # | Optimization | Expected | Why it died |
| --- | --- | --- | --- |
| 13 | Surface-length pruning inside the synthesis fold (P2) | "The only lever on Amharic's ~160 ms per synthesis run" | Amharic's forward synthesis is **14 ms**. The 160 ms was arithmetic on an unmeasured denominator â€” dividing 30 s by 186 synthesis inputs and assuming the time was there |
| 14 | Nogood lattice subsumption (P4) | Nogood hits dominate positive replays **434,628 to 25,102** â€” the engine mostly proves subtrees *empty*, and generalising "empty" over the feature lattice would turn nogoods from points into regions | **CLOSED on direct evidence** (it was previously closed on adjacent evidence, which is why it was reopened). Two heavy Sena probes produced 16,176 expansions -> 16,176 distinct keys -> **16,176 singleton buckets, 0 subsumable (0.0%)**, and 12,595 -> 12,595 -> **12,595 singletons, 0 subsumable**. Not one pair of nogood entries even shares a `(shape, stratum, counts, non-head)` bucket, so there is nothing for a subsumption ordering to act on. Breadth across all 33 conformance fixtures agrees: 7,493 empty expansions, 7,398 distinct keys, **0 subsumable (0.0%)** pooled, every non-empty bucket a singleton. Direction checked explicitly as `earlier.Subsumes(later)` (E subsumes Q, general over specific); the reverse would be unsound. Independently reconfirms row 4 from a different angle: the keys are maximally discriminating |
| 15 | Move constraints into the analysis key (Maxwell & Kaplan's category-splitting) | Their biggest measured win: make the chart prune what the constraint solver otherwise would | Everything synthesis rejects on is root- or realization-dependent, and the root is unknown until `LexicalLookup`. Also a Sena analysis state costs ~0.73 ms while a synthesis input costs at most ~0.12 ms â€” **states are dearer than synthesis inputs** |

## Reopened this round, and their outcomes

| # | Optimization | Expected / observation | Outcome |
| --- | --- | --- | --- |
| 22 | **Analysis-side rule prefilter** | 94.4% of Sena allomorph pattern matches failed, so a cheap necessary-condition check had a large direct-work ceiling. | **RETAINED after the earlier self-time cap was refuted by direct work counts.** The original ~10% ceiling counted matcher self-time but omitted traversal allocation, feature unification, and GC booked elsewhere. The implemented edge-segment prefilter removed **87% of FST traversals**, 54% of arc checks, and 85% of traversal instances on 45 Sena words, with exact parity on those words and all 464 conformance-fixture words. See `docs/optimizations/edge-segment-prefilter.md`. |
| 16 | **`ExpandAlternatives` dedupe** | 20.7% of Sena wall, the largest bucket nobody had instrumented. Apparent census: 395,026 alternatives -> **61** distinct (0.0%), 85% of duplicates same-analysis-word | **CLOSED on the sound census.** With the pending-trail content added to the key, 395,026 -> **351,414 distinct (89.0%)** -- only **11%** are duplicates at all, and all 43,612 of them are same-analysis-word. Sound ceiling = 20.7% x 0.110 = **2.28% of Sena wall**, against a gate of >=10% to build and <5% to close. **The fourth apparent redundancy to evaporate under a complete key** |
| 17 | **Per-node cost in the analysis cascade** | 84% of the Amharic cascade is not linguistic computation; clone is 46.7% and 37.4% was unattributed | **Partly settled, remainder still open.** (a) The remainder split **MISSES its <15% gate by ~2.5x** â€” still ~38% on Amharic `anCascade`. Memo, Trail and **Equality each measured <0.2%**, so `Word.ValueEquals` behind the cascade's `HashSet<Word>` is *not* the missing mass, contrary to the standing hypothesis. **Two caveats the first write-up missed.** (i) The ~38% figure comes from a *contended* run — three timing probes were live — and descheduled wall time lands preferentially in the remainder while deflating every self-time share, so **CPU starvation is itself an unbracketed candidate biasing in exactly the direction observed**. (ii) The claim that GC pause time is unmeasurable is **wrong at the measurement point**: only the library assemblies are netstandard2.0; the probe harness lives in `tests/SIL.Machine.Morphology.HermitCrab.Tests`, which targets **net10.0**, so `GC.GetTotalPauseDuration()` and `GC.GetGCMemoryInfo().PauseTimePercentage` are directly callable. Next probe, gated: re-run serial — remainder <20% means contention was the missing mass and 17(a) recloses on the cheaper-clone target; still >=30% then bracket with `GetTotalPauseDuration()` deltas — ΔPause >=50% of the remainder's absolute ms confirms GC (build target converges with clone/freeze at 46.7%), <=15% refutes it and the next split is enumerator/LINQ orchestration (the 2026-07 alloc audit already flagged `ExecuteCommands` enumerator boxing); a collection census over 24 Amharic words gives gen0 8,217 / gen1 2,729 / gen2 301, an unusually high gen1+gen2 ratio consistent with â€” not proof of â€” GC as the remainder. (b) **Clone-discard fraction = 66.2%** (created 2,505, discarded 1,659), a count and therefore contention-immune, against a >=70% gate: **misses by ~4 points, so the build is "make a clone cheaper", not "defer cloning"**. Scoped to `AnalysisAffixTemplateRule`'s eager per-template clone, the one clone-before-verification site; the other four analysis-side sites clone only after a successful match and were excluded rather than blended in |
| 18 | Corpus-scope memoization | `AnalysisScope` dies per word, but `AnalysisStateKey` is word-independent by construction, so entries are reusable across a corpus. Nogood hits dominate positive replays 10-17x, making cross-word nogood reuse the cheapest available form of reuse | **CLOSED, and worse than a no-op.** Indonesian, 121 words, **serial on a clean machine: 1.015x, 1.4% wall-clock reduction** against a >=20% build gate and <5% close gate. Costs **68 MB** added peak managed memory. Cross-word reuse is real but small: 39 memo, 73 nogood, 237 template-nogood hits. Token-vs-type: **121 tokens, 121 distinct**, so no type-level dedupe is available either. **A first, contended run of the same harness reported 2.385x / 58.1%** with a 210.9% spread on the shared arm against 13.7% on the baseline; two testhosts were live. The counts were byte-identical across both runs â€” only the timing was contaminated, and min-of-N on a high-spread arm picks the luckiest sample. Analysis-set equality held throughout, so the key genuinely is word-independent. **Sena, first 300 words, is the decisive case: the shared scope is 28-32% *slower* than per-word scopes** (two clean serial runs, no contention: 282.7s vs 372.2s and 308.9s vs 396.7s baseline-vs-shared; deterministic counters byte-identical across both runs), and costs **~29.3 GB** added peak managed memory against a 2 GB budget. Root cause: `AnalysisScope.MaxMemoEntries` (100,000) is a *per-scope*, not per-word, budget. Two heavy early words (#74, #76 of 300) alone fill the shared `Memo` table to the cap and `TemplateMemo` to 55,071; every word after that loses the free within-word memoization a fresh per-word scope would have given it for nothing, because `Store` silently no-ops past the cap (see its comment) with no eviction. Cross-word hits (shared minus baseline) are **negative** â€” memo -77,648, nogood -747,607 â€” i.e. sharing produced *fewer* total hits than 300 independent fresh-table runs would have on their own; the small `+820` template-hit / `+9` template-nogood gain does not come close to covering it. Amharic, capped at 28 words (word 29 hangs, excluded per standing guidance): **0.9% wall-clock reduction**, table nowhere near the cap (Memo 1,251, TemplateMemo 652 of 100,000), consistent with row 17's finding that Amharic's cost is dominated by clone/allocation per node rather than distinct-state count, so avoiding a handful of cross-word re-derivations barely moves wall time. Token-vs-type on all three corpora, at the runs' own sizes and on each corpus's full word list: **zero duplicate tokens** â€” whole-word caching is not available as a cheaper alternative anywhere here. No analysis-set divergence on any corpus, at any scope regime: the stop condition that would have put rows 1-3 in question did not fire. **A shared-but-capped memo table is not "the same optimization, smaller ceiling" â€” past the cap it actively takes away the free per-word memoization the product already has, and retains every live entry for the run's full duration instead of per-word, which is where the 29 GB comes from.** Any future attempt needs real eviction (LRU, generational, or per-word sub-budgets) before this is worth re-measuring, and even then the >=20% gate looks unreachable given Indonesian's ceiling under an uncapped-in-practice regime was already only 1.4% |
| 19 | Contexted constraints / abstract feature-only replay | Pack without merging states by replaying only shape-independent constraints. | **CLOSED.** Cost-weighted P1b die-point histograms put shape-free work at **6.0%** and **0.0%** on two heavy Sena probes and **0.4%** on 28 Amharic words; all fire the <30% close gate. All 33 conformance fixtures were measured for breadth: 24 close, 3 nominally fire the build gate, 3 are undecided, 2 have zero rejections, and 1 has zero words. The three build-gate fixtures are all sub-50 ms and cannot carry timing claims. One reliable fixture confirms that count and cost can diverge, but die-point rejection cost itself is only ~0.1–0.2% of wall on the real corpora. Abstract replay would optimize a phase that costs approximately nothing on the corpora treated as ground truth. Not built. |
| 20 | **Row 5's family on `GenerateWords`** (generation, ~100% synthesis share vs parsing's ~5%/~0.3%) | Row 20 was unmeasured; generation's share is ~100% so row 5's fold ratios would apply at face value if they transferred. `UseSynthesisFoldMemo` had never been wired into `GenerateWords` (only into the parse-then-synthesize path) â€” wired in under the same untraced/`MaxDegreeOfParallelism==1` restriction to make this measurable at all | Hits are no longer uniformly zero: 3 of 23 lookup-reaching fixtures clear the 5% gate (diacritic-segments 33.3%, suffixing-vowel-harmony 28.6%, suffixing-evidential-adjacency-chain 37.7%; a 4th sits just under it at 4.8%), reopening row 5 narrowly for generation. But the reopened case recloses immediately: the A/B on the best fixture shows the wall-clock "win" sitting inside its own noise floor, and the pooled hit rate is dominated by `deep-optional-affix-nesting` (925 of 1,613 lookups) at hits=0, reproducing row 5's exact 4.5%-slower key-construction cost on the identical fixture. Parity: 0 divergences, 1,236 generations, 32/33 fixtures (33rd throws identically both sides). Best case: 130 hits / 5 reps (37.7% hit rate), **7.8%** wall-clock reduction inside a **22.2%** off-arm spread â€” not a result. 19/23 lookup-reaching fixtures at exactly **0%**; pooled hit rate **2.67%** (43/1,613), skewed by the one large zero-hit fixture. |

---

## The lesson that generalises

Three independent measurements, three boundaries, same collapse:

| measurement | key used | apparent | sound |
| --- | --- | --- | --- |
| synthesis-input dedupe (#6) | order-insensitive | 9,774x | 15â€“40% |
| fold-entry census | trail position only | 6,476x | not established |
| fold-step sharing (#5) | trail position only | 3.22x / 8.10x | **hits = 0** |

**The redundancy in HermitCrab's synthesis is apparent, not real. The trail is what makes each
step distinct, and every measurement showing large shareable work is measuring a key that omits
it.** The thing that makes sharing visible is the thing that makes sharing wrong.

That closes a *family*: packed parse forests **of the merge-by-key kind**, fold-step sharing and
synthesis-input dedupe all require distinct derivations to converge on a genuinely identical
state. In this engine they do not converge -- the same fact as the rules being non-order-invariant,
seen from the other side. This argument did not itself close row 19 because a contexted-constraint forest
packs without merging states; row 19 was later measured independently and closed by its die-point costs.

## Where the time actually is

Measured with eight exclusive buckets summing to wall. Amharic is a single run (`unaccounted`
0.1%). **Sena is spliced from two runs -- see the note in its row.**

| grammar | breakdown |
| --- | --- |
| **Sena** heavy words | battery **51.4%**, cascade 18.4%, all synthesis ~5% -- *pre-`ExpandAlternatives` run (143,303 ms, unaccounted 20.1%)*; `ExpandAlternatives` **20.3%**, unaccounted 1.5% -- *later run (112,713 ms)*. **Two runs: not co-summable.** One re-run would put all Sena shares on one denominator |
| **Amharic** | analysis cascade **~95%** of analysis, which is 99.5% of wall; **all synthesis 0.3%** |

**Every optimization in this ledger that worked reduced how many nodes are visited. None touched
what a node costs.** Amharic spends **~170 ms per analysis state**. (The *measured* state-count floor -- 2,555
expansions against 2,546 states -- is a Sena heavy probe. For Amharic, near-floor behaviour is
**inferred** from R ~ 1, which compares the full key against the narrowed key, not against a
semantic floor.)

### Inside one node (Amharic, 28 words, both probe layers live)

| bucket | `anCascade` 81,707 ms | `anBattery` 7,368 ms |
| --- | --- | --- |
| clone / freeze / allocation | **46.7%** | 41.9% |
| **remainder (unattributed)** | **37.4%** | 15.4% |
| matcher traversal | 12.5% | **39.3%** |
| FeatureStruct algebra | 3.4% | 3.5% |
| memo-key construction | 0.0% | 0.0% |

Censuses: 27,385 rule attempts â€” guards reject 19.5% (application-count 6.3%, unifiability 13.2%),
**80.5% reach matching**. Of 25,178 allomorph pattern matches, **1,938 succeed (7.7%)**, 23,240
fail (**92.3%**).

**The headline: only ~16% of the Amharic analysis cascade is *identified* as linguistic
computation** -- matching 12.5% plus feature unification 3.4%. Clone/freeze is **46.7%** and
**37.4% is unattributed**, which is unknown rather than known-non-linguistic. The defensible
statement is that at most a sixth of the cascade has been shown to decide linguistic questions;
of the rest, half is measured (clone) and half is still open.

**Gate outcomes, written before the run.** "Any bucket >=50% becomes the sole build target" â€” clone
is 46.7%, does not fire. "Pattern matching dominates *and* >=80% of attempts fail -> build a
prefilter" â€” 92.3% do fail, but matching is 12.5%, so it does not dominate; does not fire.
**"Remainder >=30% means the decomposition is incomplete and that is the result" â€” 37.4%, FIRES.**

So there is no declared build target yet. The next probe splits the 37.4%: `RuleCascade` /
enumerator orchestration, trail dictionary operations, LINQ allocation, memo lookups, GC.

**A caution on the 46.7% before anyone acts on it.** `Word.Clone` being half the cascade does not
by itself mean cloning is waste â€” the cascade forks a `Word` per rule application precisely so it
can explore alternatives without mutating shared state. The deciding question is what fraction of
clones are *discarded unused*: cloned, rejected by a guard or a failed match, thrown away. Given
92.3% of pattern matches fail, that fraction has now been **measured at 66.2%** (row 17b), which misses the >=70% gate and points at the cheaper-clone build rather than the defer-clone one. If most
clones are discarded, the lever is **not cloning until a rule commits**. If most survive, the lever
is **making a clone cheaper** (copy-on-write shape, which the RUSTIFY branch already explored).
Different builds; measure first. **Gate: probe the clone-discard fraction; >=70% discarded means
the defer-clone build, below that the cheaper-clone build.**

## Two durable code constraints

Neither is expressible in code, and both bite silently.

1. **`Word.ReplayOnto` does not splice `_mrulesUnapplied`.** It splices `_mruleApps` and
   `_nonHeadApps` only. This is safe *solely* because `AnalysisStateKey` includes the per-rule
   count multiset, which guarantees a memo hit has identical arrival and stored-arrival counts.
   **Anyone narrowing that key breaks correctness with no test failure.** Fix, if the key ever
   changes: store arrival counts on `MemoEntry` and compute `stored - storedArrival + query` â€” a
   no-op under today's key, which is also how to test it. This is the one code change from this
   work worth making on its own merits, as defensive hardening.
2. **HermitCrab's morphological rules are not order-invariant.** Same pending-rule multiset,
   different application order, different synthesis output â€” 2 cases on Sena, 12 independently on
   Indonesian. This bounds every packed-readout scheme. It is the assumption
   Maxwell and Kaplan (CL 19(4):571â€“590) rely on and this engine does not satisfy.

## Calibration record â€” predictions vs outcomes

Written before the measurements, scored after. Kept because the misses are informative.

| prediction | outcome |
| --- | --- |
| Sena key collapse 2â€“4x | **1.12x** â€” wrong, and it was the number that decided the project |
| Indonesian key collapse 1.0â€“1.3x | 1.17x â€” right |
| Amharic "most likely to disappoint" | best static classification of the three (31 of 36 rules) â€” wrong |
| Fold sharing ~50% wall ceiling | **0.96x realised** â€” wrong; the ceiling was measured with a key that would lose parses |
| Amharic synthesis-bound at ~160 ms/run | forward synthesis is **14 ms** â€” wrong; arithmetic on an unmeasured denominator |

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
6. **Min-of-N is only valid when the arm's spread is small relative to the effect.** Row 18's
   first run read 2.385x with a 210.9% spread on one arm and 13.7% on the other; serial and clean
   it is **1.015x**. Min-of-N picks each arm's luckiest sample independently, so a high-spread arm
   wins spuriously. Print the spread beside every speedup and refuse a number whose spread exceeds
   it.
7. **Never dispatch timing-sensitive probes concurrently.** Three at once put four testhosts on the
   machine and invalidated the bucket percentages in rows 17 and 22 as well as row 18's headline.
   Counts survive contention; shares and speedups do not.
8. **A shared bounded cache without eviction is not a smaller version of the optimization** — it
   competes with itself for a fixed budget and can land strictly worse than no sharing. Row 18's
   shared scope produced *negative* cross-word hits.
9. **Never aggregate the test logger's console output.** It prints every line twice, the second
   copy space-prefixed; a `^\s*` grep doubles every total. This produced a 2x error in row 20's
   first write-up.
10. **Search completeness is never traded for speed.** HermitCrab is the permanent fallback engine
   behind the FST work.
7. **A bucket reporting exactly `0.00` is broken until proven otherwise.** `NodeCostProbe.Enabled`
   was declared and read in `Matcher.cs` but set by nothing â€” two gates in two assemblies, one
   wired. Both lower-layer buckets read `0.00ms`, which looks like "measured and negligible"
   rather than "never ran", and their time silently inflated the remainder. Wire cross-assembly
   probe gates from a single place.
8. **Always carry an explicit remainder column.** It caught the wall-split misattribution and the
   dead probe gate. Four of the five retractions in this work would have been caught at the point
   of measurement by a remainder that did not sum.

---

## Addendum, September 2026: the round that followed, measured by counts

Branch `perf/hc-defer-template-clone` (on upstream master, after the merged memoization PR 456) re-ran
this ledger's open questions with deterministic step counters instead of wall time. Its records live
with the code: `docs/hermitcrab-perf-2026-09-counts.md` and one file per change under
`docs/optimizations/`. Corrections to this ledger that fell out of it:

- **Row 3 ("shape sharing shipped") was not true on master.** No reachable commit carried it; master's
  `Word.Clone` deep-copied the Shape and every node feature structure. The follow-up independently implements
  a different design: internal `CloneForEngine` copy-on-write with un-share at mutation sites, while public
  `Word.Clone` remains a deep mutable clone. A shared shape is frozen, so a missed mutation site throws.
- **Row 22's ~10% cap for an analysis-rule prefilter was wrong by an order of magnitude.** It was
  computed from matcher *self-time*. Counted directly, an edge-segment prefilter (patterns are anchored
  at both ends and matched by unification, so the first/last literal segment must unify with the
  shape's first/last segment) removes **87% of FST traversals** and 54% of arc checks on 45 Sena words,
  with identical analyses on every Sena word and all 464 conformance-fixture words.
- **"memo-key construction 0.0%" (row 21) was wrong at the profile level.** Hashing each new word's
  syntactic feature structure for `AnalysisStateKey` is ~15% of CPU on the heaviest Sena word; the key
  itself is cheap but freezing a fresh clone of a nested feature structure is not.
- **Row 16's complete-key result still stands.** An experimental `ExpandAlternatives` memo/hoist did not
  reduce clones and was removed. The final branch has no alternatives-expansion change relative to the
  pinned production baseline; the intermediate 75% counter reduction is not a shipped result.
- **The ledger never cited John Maxwell's work**: PR 491 (final-template prune in analysis) and the
  2025 `investigate_linear_traverse` branch. Both were tested. The prune was inert on every local corpus
  because each has a partial morpheme, and the traversal measured negative by counts (instances used 2.3x)
  because the current traverser already pools instances. Both implementations and their toggles were removed;
  their rejected-lead records remain.
- **Wall time on the measurement machine moved 2x between windows with identical binaries** (a session
  transition in between). Every verdict in this ledger that rests on a wall-time delta under ~20% should
  be read with that in mind; the round's method note is to count steps on the sequential path.
