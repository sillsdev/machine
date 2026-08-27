# HermitCrab optimization ledger — tried, closed, do not retry

**Purpose: stop this work being redone.** One row per optimization attempted, what was expected,
what actually happened, and the number that settled it. If you are about to try something on this
list, read its row first — several of these look irresistible on paper and three of them have
already been independently rediscovered and re-retracted.

Deliberately in-repo and durable, not evicted into a PR accordion. A closed avenue is only closed
if the next person can find out cheaply that it is closed.

**This is the only report.** The working docs it was distilled from
(`hermitcrab-packed-forest-research.md`, `hermitcrab-forest-memo-plan.md`,
`hermitcrab-forest-memo-ceiling.md`, `hermitcrab-synthesis-fold-probes.md`) are chronological
process records containing retracted intermediate claims, and are deliberately **not** merged.
They stay on the branches below, together with all instrumentation, as reproducible evidence:

| branch | holds |
| --- | --- |
| `feature/forest-memo` | key-narrowing measurement, `RuleLengthClassifier` + 12 tests |
| `feature/synthesis-fold-probes` | the eight-bucket wall-time split, die-point histogram, sharing census, harness |
| `feature/synthesis-fold-sharing` | `SynthesisStateKey` + fold memo, parity-clean, `hits = 0` |
| `feature/per-node-cost` | `NodeCostProbe` (SIL.Machine layer) + the per-node decomposition |

**No instrumentation is merged, by design.** The probes gate on `volatile bool` reads inside
`Matcher.cs` — a hot inner loop in `SIL.Machine` used by every consumer — which is an acceptable
cost for a measurement run and not one to carry in product code indefinitely. Rebuilding a probe
from a branch is cheap; maintaining research scaffolding in two product assemblies is not.

## Net speed delivered: none

No optimization in the "closed" section shipped, and none of them made anything faster. The
fastest thing tried was 4% **slower**. The pre-existing wins in this codebase (rows 1–3) predate
this work. What this effort produced is the measurement, the closed family, the located target,
and this ledger.

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
| 4 | **Narrow `AnalysisStateKey`** (drop strictly-shrinking rules' un-application counts) | `MaxApplicationCount` defaults to 1, so the count multiset is "which subset of rules was used" — 2^27 states. Dropping 19 of 27 rules gives 2^8 | Shape + syntactic FS + realizational FS *already* discriminate. The count component was implied by its neighbours. A worst-case bound collapsing is not the realised state count collapsing | Realised state collapse **Sena 1.12x** (state-weighted) vs a 1.3 gate; **4 of 7** heavy words exactly 1.00, `atawirambo` 2,556 -> 2,556. The three that moved -- `pidafikawo` 1.13, `cinagumanika` 1.16, **`cinacemerwa` 1.38** -- all return **zero parses**: R tracks failure, not size, and the corpus's single most expensive word *did* clear the gate. **Reopening condition:** a grammar with genuinely high R; the census is cheap and is the right first question to ask of any new grammar |
| 5 | **Fold-step sharing memo** (share deterministic synthesis fold steps by computed value) | Probe measured 3.22x and 8.10x shareable steps; ~50% wall ceiling on two reliable fixtures | A sound key must carry the **ordered remaining trail**. Two candidates then have to agree on their entire future to share a step, which almost never happens | **hits = 0** on the best fixture; **0.96x** (4.5% *slower* — pure key-construction cost); 1.06x on the second, inside 21.9% noise |
| 6 | **Order-insensitive synthesis-input dedupe** | 9,774x duplicate synthesis inputs | HermitCrab's morphological rules are **not order-invariant**: same rule multiset, different order, different output. Merging loses parses | 2 violations Sena, 12 independently Indonesian (e.g. `{meN, -Cont}`). Sound version: **15–40%** |
| 7 | **Trail-position indexing of synthesis rules** | 11,445,538 of 11,445,538 rejections are "this rule is not the pending trail rule" — 100% of the histogram | Each rejection is one array index plus one reference compare. Count is not cost | 11.4M x 29 ns = 0.33 s of 143 s = **0.2%** |
| 8 | Synthesis-side length bound (Gate A) | Reject candidates that cannot reach the surface length | At the comparison point the candidate is still the bare root; its affix trail applies later inside `_synthesisRule.Apply`. Rejected valid parses | Unit suite 64 -> 34 passing. Reverted |
| 9 | Analysis length ceiling (Gate B) | Same idea, analysis side | Sound and correct. No available corpus contains the pathology it prunes | Byte-identical output, **no measurable speedup** (~4–7% slower, within noise) |
| 10 | Lexical reachability gate (Phase 5) | Prune candidates from which no lexicon root is reachable | Both reference corpora have real compounding in their deepest stratum, which disqualifies the gate everywhere | Proven **no-op** on both corpora |
| 11 | Tandem lexical intersection (T2) | Kill doomed branches early via the lexicon | The oracle only sees lexical unreachability. The expensive words fail on checks that run *after* lexical lookup succeeds | Pooled **23.5%** dead steps vs a 30% gate; one failure word at exactly **0.0%** dead |
| 21 | **Memo-key caching on the frozen `Word`** | **CLOSED.** Keys are built ~28x per distinct state, which looked like a cheap win. Measured cost of all `AnalysisStateKey` construction and hashing on Amharic: **3 ms of 81,707 ms = 0.0%.** Same error shape as #7: high count, no cost |
| 12 | Pool small short-lived collections | Fewer allocations, faster | `HashSet/Dictionary.Clear()` is O(capacity); Gen0 already beats pooling at this size | -15–17% bytes but **+8.6% wall**, +12% on the parallel battery. Reverted |

## Closed before building — motivation refuted by measurement

| # | Optimization | Expected | Why it died |
| --- | --- | --- | --- |
| 13 | Surface-length pruning inside the synthesis fold (P2) | "The only lever on Amharic's ~160 ms per synthesis run" | Amharic's forward synthesis is **14 ms**. The 160 ms was arithmetic on an unmeasured denominator — dividing 30 s by 186 synthesis inputs and assuming the time was there |
| 14 | Nogood lattice subsumption (P4) | Nogood hits dominate positive replays **434,628 to 25,102** -- the engine mostly proves subtrees *empty*, and this is the only pointer anywhere at the nogood path | Closed on **adjacent** evidence: #4 measured key equality after projection, not a subsumption ordering over nogood feature structures. The direct probe -- a subsumption census over recorded nogood keys, gate >=30% subsumable -- was never run. Conclusion probably holds; the citation overclaims |
| 15 | Move constraints into the analysis key (Maxwell & Kaplan's category-splitting) | Their biggest measured win: make the chart prune what the constraint solver otherwise would | Everything synthesis rejects on is root- or realization-dependent, and the root is unknown until `LexicalLookup`. Also a Sena analysis state costs ~0.73 ms while a synthesis input costs at most ~0.12 ms — **states are dearer than synthesis inputs** |

## Open

| # | Optimization | Status |
| --- | --- | --- |
| 22 | **Analysis-side rule prefilter** (index rules by segment/feature signature so a rule that cannot match is never handed to the matcher) | **Bounded, not built.** 92.3% of allomorph pattern matches fail (23,240 of 25,178) and 80.5% of rule attempts reach matching — so the prefilter target is real. But matcher traversal is only **12.5%** of the Amharic cascade, capping this at ~11.5% there. Worth more on the template battery, where matching is **39.3%** -- but that is the *Amharic*
battery, which is only ~8% of its wall. **Sena's battery is 51.4% of its wall and has never been
decomposed per-node.** If its match share is comparable, the Sena cap is ~0.514 x 0.39 = **~20% of
wall**, outranking every other open row. Probe: `NodeCostProbe` over the 3 Sena heavy words; gate --
matcher >=30% of Sena `anBattery` makes this the top open lead |
| 16 | **`ExpandAlternatives` dedupe** | Live, and probably the strongest open lead. 20.3% of Sena wall, never instrumented before this work. 926 alternatives -> 3 distinct on one fixture; **85% of duplicates pooled (100% on two words) trace to the same analysis word**, so they share a trail, so the existing key's omission is harmless *for those*. The 3-8% figure applies the cross-word sound rate (15-40%) to all of them and therefore likely **understates it -- the same-word ceiling is nearer 17%**. Run the sound same-word census before quoting 3-8% |
| 17 | **Per-node cost in the analysis cascade** | Live and unexplored — see below |
| 18 | Corpus-scope memoization (P3) | Not run. `AnalysisScope` dies per word but the key is word-independent by construction. Amharic's corpus run takes 4.3 hours |
| 19 | Contexted constraints / abstract feature-only replay | Not built. Gated on a die-point histogram that says candidates die on shape-free checks |
| 20 | Generation (`GenerateWords`) | **Unmeasured.** Pure synthesis, so the *share* is ~100% rather than ~5%. That changes the share, **not** the soundness argument: rows 5 and 6 fail because a trail-incomplete key is unsound, which is workload-independent. Do **not** read this row as "the ratios apply at face value" -- that is the artifact rows 5/6 closed. Every verdict above is scoped to **parsing**. Probe: run the existing off-by-default fold memo over the 33 fixtures in generation; gate -- hit rate >=5% of steps reopens row 5 for generation, 0 closes the family there too |

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

That closes a *family*: packed parse forests **of the merge-by-key kind**, fold-step sharing and
synthesis-input dedupe all require distinct derivations to converge on a genuinely identical
state. In this engine they do not converge -- the same fact as the rules being non-order-invariant,
seen from the other side. **It does not close row 19**: a contexted-constraint forest packs
*without* merging states, so this argument does not reach it. Row 19 stays open and unmeasured.

## Where the time actually is

Measured with eight exclusive buckets summing to wall. Amharic is a single run (`unaccounted`
0.1%). **Sena is spliced from two runs -- see the note in its row.**

| grammar | breakdown |
| --- | --- |
| **Sena** heavy words | battery **51.4%**, cascade 18.4%, all synthesis ~5% -- *pre-`ExpandAlternatives` run (143,303 ms, unaccounted 20.1%)*; `ExpandAlternatives` **20.3%**, unaccounted 1.5% -- *later run (112,713 ms)*. **Two runs: not co-summable.** One re-run would put all Sena shares on one denominator |
| **Amharic** | analysis cascade **~95%** of analysis, which is 99.5% of wall; **all synthesis 0.3%** |

**Every optimization in this ledger that worked reduced how many nodes are visited. None touched
what a node costs.** Amharic spends **~170 ms per analysis state**. (The *measured* state-count floor -- 2,555
expansions against 2,546 states -- is Sena `atawirambo`. For Amharic, near-floor behaviour is
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

Censuses: 27,385 rule attempts — guards reject 19.5% (application-count 6.3%, unifiability 13.2%),
**80.5% reach matching**. Of 25,178 allomorph pattern matches, **1,938 succeed (7.7%)**, 23,240
fail (**92.3%**).

**The headline: only ~16% of the Amharic analysis cascade is *identified* as linguistic
computation** -- matching 12.5% plus feature unification 3.4%. Clone/freeze is **46.7%** and
**37.4% is unattributed**, which is unknown rather than known-non-linguistic. The defensible
statement is that at most a sixth of the cascade has been shown to decide linguistic questions;
of the rest, half is measured (clone) and half is still open.

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
Different builds; measure first. **Gate: probe the clone-discard fraction; >=70% discarded means
the defer-clone build, below that the cheaper-clone build.**

## Two durable code constraints

Neither is expressible in code, and both bite silently.

1. **`Word.ReplayOnto` does not splice `_mrulesUnapplied`.** It splices `_mruleApps` and
   `_nonHeadApps` only. This is safe *solely* because `AnalysisStateKey` includes the per-rule
   count multiset, which guarantees a memo hit has identical arrival and stored-arrival counts.
   **Anyone narrowing that key breaks correctness with no test failure.** Fix, if the key ever
   changes: store arrival counts on `MemoEntry` and compute `stored - storedArrival + query` — a
   no-op under today's key, which is also how to test it. This is the one code change from this
   work worth making on its own merits, as defensive hardening.
2. **HermitCrab's morphological rules are not order-invariant.** Same pending-rule multiset,
   different application order, different synthesis output — 2 cases on Sena, 12 independently on
   Indonesian (e.g. `{meN, -Cont}`). This bounds every packed-readout scheme. It is the assumption
   Maxwell and Kaplan (CL 19(4):571–590) rely on and this engine does not satisfy.

## Calibration record — predictions vs outcomes

Written before the measurements, scored after. Kept because the misses are informative.

| prediction | outcome |
| --- | --- |
| Sena key collapse 2–4x | **1.12x** — wrong, and it was the number that decided the project |
| Indonesian key collapse 1.0–1.3x | 1.17x — right |
| Amharic "most likely to disappoint" | best static classification of the three (31 of 36 rules) — wrong |
| Fold sharing ~50% wall ceiling | **0.96x realised** — wrong; the ceiling was measured with a key that would lose parses |
| Amharic synthesis-bound at ~160 ms/run | forward synthesis is **14 ms** — wrong; arithmetic on an unmeasured denominator |

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
7. **A bucket reporting exactly `0.00` is broken until proven otherwise.** `NodeCostProbe.Enabled`
   was declared and read in `Matcher.cs` but set by nothing — two gates in two assemblies, one
   wired. Both lower-layer buckets read `0.00ms`, which looks like "measured and negligible"
   rather than "never ran", and their time silently inflated the remainder. Wire cross-assembly
   probe gates from a single place.
8. **Always carry an explicit remainder column.** It caught the wall-split misattribution and the
   dead probe gate. Four of the five retractions in this work would have been caught at the point
   of measurement by a remainder that did not sum.
