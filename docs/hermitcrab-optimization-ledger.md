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
| 14 | Nogood lattice subsumption (P4) | Nogood hits dominate real hits 434,628 to 25,102; generalise "empty" over the feature lattice | Shape + features already discriminate almost perfectly (see #4), so subsumption buckets hold one member and nothing generalises |
| 15 | Move constraints into the analysis key (Maxwell & Kaplan's category-splitting) | Their biggest measured win: make the chart prune what the constraint solver otherwise would | Everything synthesis rejects on is root- or realization-dependent, and the root is unknown until `LexicalLookup`. Also a Sena analysis state costs ~0.73 ms while a synthesis input costs at most ~0.12 ms — **states are dearer than synthesis inputs** |

## Open

| # | Optimization | Status |
| --- | --- | --- |
| 16 | **`ExpandAlternatives` dedupe** | Live. 20.3% of Sena wall time, never instrumented before this work. 926 alternatives -> 3 distinct on one fixture, **100% same-analysis-word** so interceptable pre-expansion. Sound ceiling **3–8% Sena**. Needs a trail-complete key |
| 17 | **Per-node cost in the analysis cascade** | Live and unexplored — see below |
| 18 | Corpus-scope memoization (P3) | Not run. `AnalysisScope` dies per word but the key is word-independent by construction. Amharic's corpus run takes 4.3 hours |
| 19 | Contexted constraints / abstract feature-only replay | Not built. Gated on a die-point histogram that says candidates die on shape-free checks |
| 20 | Generation (`GenerateWords`) | **Unmeasured.** Pure synthesis, no analysis phase, so share ~100% — the fold-step ratios would apply at face value. Every verdict above is scoped to *parsing* |

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
state-count floor. That is the unexplored axis and the reason #17 is open.

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
