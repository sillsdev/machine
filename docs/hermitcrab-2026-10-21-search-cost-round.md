# HermitCrab search cost, 2026-10-21 round

**Disposition: three large reductions in search work stand up. Two of them were bugs in the Rust
port, not algorithmic gains.** HC-Rust ([sillsdev/PanGloss](https://github.com/sillsdev/PanGloss))
is a port of `SIL.Machine.Morphology.HermitCrab` under test against `hc.dll`. On one private
grammar, HC-Rust did far more search than C# because two kinds of shape node had lost the segment
identity that C# gives them. Fixing both made HC-Rust match C# and removed most of that grammar's
cost. The one algorithmic change that survived, sharing trailing affix-template slot batches, was
built in HC-Rust and then ported to C# as [Machine PR #546](https://github.com/sillsdev/machine/pull/546).
The lead ranked first in the [search-asymptotics research](hermitcrab-search-asymptotics-research.md),
lexically-dead branch pruning, was measured first and rejected on its ceiling.

This record follows the [optimization ledger](hermitcrab-optimization-ledger.md): one entry per
attempt, the number that settled it, and what differs from earlier attempts. Ledger rows 24-30
summarise it.

## Data and units

**Grammars.** The public reference grammars are Sena, Amharic, Mbugwe, Aweti and Indonesian. The
round's motivating grammar was a **private featureless project (Austronesian, ~30k lexical entries,
3 strata)**. It was measured in three variants: *base*, *v1*, and a *deletion variant* that adds a
rule deleting two stop classes after nasals and has no compounding. Each variant was run on three
word lists: 200 nonwords, a 563-word dev list and a 7,206-word held list. Only aggregate numbers
from it appear here; no word forms, glosses or lexical entries.

**Units.** The owner rule for this round is that gains are stated as **parser work**: "N% less work
to parse the words".
- **Parser work** is HC-Rust's per-word `work_steps`, summed over a word list. A work unit is one
  reservation against HC-Rust's per-word work cap. Every analysis attempt reserves one, and so does
  every inner matcher step, segmentation step, root-lookup step and confirmation step. It is
  deterministic, so it survives machine contention, unlike wall time.
- **Steps** are analysis attempts (`steps <= work_steps`). They are a secondary figure.
- **Milliseconds** appear only as labelled secondary figures.
- **C# figures** are in C#'s own counters (slot batches, lookups), never in HC-Rust work units. They
  are not converted.

**Parity comparators.** HC-Rust used `rust/tools/parse_compare.py`, which compares complete
analyses per word and reports each row IDENTICAL or not. Entries 090 and 103 ran it with
`--multiset`; entry 095 records only `parse_compare.py`. PR #546 compared ordered morpheme-ID/surface signature lists,
ordinal-sorted, with multiplicity. Neither is the legacy morph-count `WordAnalysisSignature` that the
September disposition calls out.

**Runs.** Unless noted, HC-Rust runs were `pangloss batch --threads 1` with `HC_STEP_STATS=1`.
Change A's full-list parity runs used `--threads 2` with the default step cap and no wall timeout.

## What stands up

| # | Change | Kind | Where | Effect, in parser work | Parses |
|---|---|---|---|---|---|
| A | Analysis-side affix literals keep `StrRep` identity on wide featureless tables | HC-Rust port bug. C# already exact | PanGloss divergence entry 090 (analysis-side `Lhs` follow-up) | Private project, 7 spot words: base **98.5% less** (2,365,602 -> 35,913); worst variant **99.8% less** (50,778,583 -> 83,677). Steps on the full lists fell 46-55x | IDENTICAL on all three private lists and on Amharic, Sena, Indonesian |
| B | A re-inserted segment-class node keeps its members on a featureless grammar | HC-Rust port bug. C# already exact | PanGloss entry 103, [`414ac6b5`](https://github.com/sillsdev/PanGloss/commit/414ac6b5), [record](https://github.com/sillsdev/PanGloss/blob/main/docs/divergences/103-class-deletion-reinsertion-identity.md) | Deletion variant, held list: **94.7% less** (303,966,311 -> 16,220,190). Its 16 words that hit a 1 s limit: **99.9% less** | IDENTICAL on every list. Byte-identical work wherever it cannot fire |
| C | Shared trailing slot trie in the affix-template battery | Algorithmic, no memo | PanGloss entry 095, [record](https://github.com/sillsdev/PanGloss/blob/main/docs/divergences/095-shared-template-slot-batches.md); C# [PR #546](https://github.com/sillsdev/machine/pull/546) | Sena **11.5% less** (all 6,146 words); Mbugwe **18.1% less** (1,364-word partial run); Aweti 0.35% less; zero elsewhere | IDENTICAL in HC-Rust; zero signature differences in C# samples |

The census instrumentation that settled the rejected lead is
[`d1690c13`](https://github.com/sillsdev/PanGloss/commit/d1690c13). It is gated behind
`HC_BRANCH_CENSUS=1` and inert when unset: parser work and parses are identical to main on six
lists.

## The lesson: diff the port on the oracle's own export before optimizing the algorithm

The bugs were found by a comparison, not by a profile. HC-Rust normally loads a FieldWorks project
through its own snapshot reader. Here it was also run on the XML that C# itself exports for the same
project. On the private project's slowest word:

| Engine and input | Lexical lookups |
|---|---|
| C#, its own export | 49 |
| HC-Rust, C#'s export | 18 |
| HC-Rust, its own snapshot | 1,655 |

These counts were supplied by the round's brief; this write-up did not reproduce them. A ~90x gap
between two inputs to the same engine is a loading or compilation difference, not a search-strategy
difference. Following it led to both identity bugs:

- **A (entry 090 follow-up).** HC-Rust's analysis-side affix literals carried identity only through
  a single 64-bit id lane. That lane does not exist above 64 character definitions. Provisional
  letters pushed the private project to 70, and the grammar has no phonological features, so
  nothing else distinguished segments. An enclitic literal therefore matched *any* final segment.
  Every word long enough was unapplied against it, and synthesis rejected each bogus candidate. C#
  gives a featureless segment a `StrRep` `EqualTo` constraint (`CharacterDefinitionTable.cs:68` at
`a4b29742`, the same snapshot the research doc's anchors use), so
  its literal matches only itself. The fix applies HC-Rust's chunked `StrRep` matcher on this path,
  only for featureless grammars. Featured wide tables (Amharic) keep the feature-only
  over-approximation, which C# shares, because a featured node has no `StrRep`.
- **B (entry 103).** `NarrowAnalysisRewriteRuleSpec.Unapply` re-inserts a deletion rule's LHS
  constraint as an optional node. For a `SegmentNaturalClass` that node carries the union of its
  members' structures, which on a featureless grammar is "`StrRep` in members", and
  `RootAllomorphTrie` unifies against it. HC-Rust gave the node no identity, and its root trie also
  ignored a query node's member set. Analysis and root lookup could therefore take any segment at
  every re-insertion site. The fix sets the member set on featureless grammars only, and the trie
  refuses an edge only when C#'s unification would fail.

**Neither fix is a C# optimization.** Before the fixes HC-Rust did more work than C#; afterwards it
does what C# does. They are recorded here because they decided where this round's effort went, and
because they explain several figures in the research brief:
- **The deletion variant's 1 s timeouts.** The brief reported 200-1,600 inputs reaching a 1 s
  per-input timeout, from an earlier build that had neither fix; that count was not re-measured.
  What was measured is narrower: after fix A and before fix B, 16 held-list words still hit a 1 s
  limit, and fix B cut their parser work by 99.9%. Both port bugs are implicated, and the two counts
  are not comparable.
- **Where the time went.** In a two-word profile of the deletion variant before fix B, analysis was
  0.1-0.3% of parser work, root lookup about 10% and synthesis about 90%. Zero branches were
  lexically dead. No analysis-side pruning could have helped.

One residual is **not explained**. On the slowest word, HC-Rust on C#'s export made 18 lookups where
C# made 49. Parses were identical, but an efficiency nobody can argue for is not a result. It is
listed under open leads rather than claimed.

**Method rule.** Before measuring an algorithmic lead on a port, run the port on the oracle's own
export of the same grammar and compare a cheap count, such as lookups per word. A large gap between
the port's two inputs is a port bug, and its fix can dwarf any algorithmic change. This round, it
did so twice out of three.

## C1: prune analysis branches that cannot reach a root (research lead #1) — rejected on its ceiling

**Expected.** A sound summary of the remaining transformations would prune analysis nodes from
which no lexical lookup can find a root. This is the research doc's rank-1 candidate, residual
viability. It differs from ledger rows 10 and 11 only if compounding and the remaining morphology
are modelled.

**Measured.** A hindsight census charged each unapplication's steps and work to its input node.
Folded alternatives alias their canonical. A node is *live* when a lookup on it, on an alias or on
a descendant found a root. Dead cost is split three ways:
- **Dead leaves:** a failing rule battery plus one failing lookup.
- **Dead, may compound:** fewer non-heads than `MaxStemCount`, with a compounding rule in this or a
  deeper stratum.
- **Dead internal, plain:** the residual.

Only the plain bucket is what a sound, compound-aware summary could actually remove at a profit. A
dead leaf costs about as much to prove dead as to run. A may-compound node is row 10's
disqualifier.

| List (words) | Parser work | Analysis / lookup / synthesis | Lexically-dead ceiling | Dead leaves | Dead, may compound | **Dead internal, plain** | Repeat-lookup work |
|---|---|---|---|---|---|---|---|
| Private, base, held (7,206) | 50,345,810 | 52.7 / 11.8 / 35.2% | 22.6% | 6.1% | 14.6% | **1.9%** | 2.4% |
| Private, v1, held (7,206) | 105,768,202 | 70.5 / 6.7 / 22.6% | 38.7% | 19.9% | 6.6% | **12.2%** | 1.8% |
| Private, deletion variant, held (7,206), before fix B | 303,966,311 | 8.5 / 35.7 / 55.8% | 16.0% | 12.7% | 0.0% | **3.4%** | 10.4% |
| Indonesian (70) | 1,906,012 | 85.8 / 2.1 / 12.0% | 41.0% | 15.3% | 23.8% | **1.9%** | 0.7% |
| Amharic (669 completed) | 39,395,506 | 97.2 / 1.4 / 1.4% | 66.1% | 20.3% | 45.8% | **0.0%** | 0.6% |
| Sena (5,134 completed at 30 s) | 271,894,826 | 89.9 / 3.0 / 7.1% | 55.5% | 11.9% | 39.0% | **4.7%** | 2.2% |
| Mbugwe, first 400 (394 completed) | 610,569,774 | 73.6 / 3.3 / 23.0% | 45.5% | 9.7% | 35.8% | **0.0%** | 3.3% |
| Aweti (183 of 206 completed at 30 s) | 488,089,456 | 93.6 / 1.0 / 5.3% | 74.9% | 35.1% | 0.0% | **39.8%** | 1.0% |

The census builds include fix A but predate change C. Template-heavy shares (Sena, and possibly
Amharic and Mbugwe) are therefore against an older, larger denominator. For scale, Sena was 271.9M
work units on 5,134 completed words then, and is 240.7M on all 6,146 now.

**Verdict: not built.**
- **Plain bucket.** It is 0-5% of parser work on six of eight lists. The exceptions are v1 at 12.2%
  and Aweti at 39.8%, the latter on 183 completed words.
- **Most dead work is unprunable.** It sits in leaves, or in nodes that could still split off a
  compound non-head. Amharic, Sena and Mbugwe put 36-46% of parser work there.
- **Any sound summary has a large scope.** It must model reduplication, infixes, truncation,
  deletion re-insertion and compounding before it is allowed to prune anything.

**What is different from prior attempts.**
- **Row 11 ceiling.** Row 11 found 23.5% pooled dead steps. Dead *steps* here are 43-94% of steps,
  so dead branches are common. But they are cheap: the ceiling falls once it is weighted by work
  and the unprunable buckets are removed.
- **Row 11's other failure mode persists:** the root is reachable but synthesis fails. On the
  deletion variant before fix B, 86% of synthesis work went to canonicals whose lookups hit but
  never matched the surface. On Mbugwe the share is 98%. No lexical oracle can prune either.

**C# transfer.** C#'s `Morpher`, `AnalysisStratumRule` and `AnalysisRewriteRule` run the same
search, so the same buckets should apply. No Machine PR unless the shadow experiment under open
leads shows a sound summary deciding a meaningful share of the plain bucket.

### Per-parse distinct-shape lookup — rejected

The brief's "97,575 lookups over 56 shapes" came from trace mode, where analysis merging is off. The
research doc warned that a trace-mode count cannot establish an untraced duplicate rate, and the
untraced census confirms it:
- **Calls per key.** The private project makes 1.13-1.49 lookup calls per distinct
  (stratum, shape).
- **Ceiling.** Exact repeat-lookup work is 0.5-3.3% of parser work on every list, or 10.4% on the
  deletion variant before fix B.
- **Policy.** Avoiding it would be a per-parse memo, which this work excludes.

C#'s `LexicalLookup` repeats lookups the same way, so there is no Machine PR. Dead-candidate lookups
are 30-75% of lookup work, but lookup is only 1-12% of parser work (35.7% on the
deletion variant before fix B), so they were not pursued either.

## C2: share trailing slot batches inside one battery call (research lead #2) — kept, ported to C#

**Expected.** The battery walks each template's slots last-first. Templates whose trailing slots
use the same rules, in the same order, with the same optionality, run the same slot batch on the
same word. Factoring the templates into a trie runs that batch once. This is the research doc's
rank-2 candidate. It is control factoring, not a cache: nothing is keyed by a visited word, and
nothing outlives one battery call.

**Battery share of all parser work (HC-Rust, before C):**

| Grammar | Battery share |
|---|---|
| Mbugwe | 61% (86 step-capped words are about 2/3 of the total) |
| Private project, v1, held | 45% |
| Sena | 23% |
| Amharic | 21% |
| Private project, base | 8% |
| Private project, loaded from C#'s export | 9% |
| Private project, deletion variant | 6.5% |
| Indonesian | 0 (no templates) |

**Static census.** This counts rule applications on the battery's own input, before and after
sharing:
- **Shared tails:** Sena 111 -> 45, Mbugwe 397 -> 196, Aweti 143 -> 100.
- **No sharing possible:** Amharic 50 -> 50, and the private project 30 -> 30. Each has one template.

**Dynamic ceiling, Sena.** A trie-exact key (same trailing slots and same full word state) predicts
448,573 repeated slot batches, which is 30.8M work units or 11.3% of all Sena work. The built trie
avoided exactly 448,573.

### HC-Rust (PanGloss entry 095)

Run with `--threads 1 --step-cap 2000000` (Aweti 500000) and no wall timeout.

| Grammar | Words compared | Parses | Parser work | Steps | Fire count |
|---|---|---|---|---|---|
| Sena | 6,146 | IDENTICAL | **11.5% less** | 31.6% fewer | 171,319 shared, 448,573 avoided (= predicted ceiling) |
| Mbugwe | 1,364 (partial run) | IDENTICAL | **18.1% less** | 19.3% fewer | - |
| Aweti | 170 | IDENTICAL | 0.35% less | 0.16% fewer | 1,939 shared, 2,159 avoided |
| Private project (4 grammar variants: base, v1, deletion, C#'s export; x 3 lists) | all | IDENTICAL | 0 | 0 | 0 |
| Amharic, Indonesian | all | IDENTICAL | 0 | 0 | 0 |

**Tests.**
- **Differential test.** Compares the shared walk with each template's own walk for all words up to
  5 segments. It fails when optionality is dropped from the trie key.
- **Fire-count test.** Fails with sharing disabled.
- **Conformance.** `-Scope all` passed 2043/2043. Fixtures at the PR #480 head showed the same
  failures as the baseline.

### C# (Machine PR #546)

`AnalysisAffixTemplateRule` has the same shape, so the trie ported directly to sequential
`ApplySlots`. It is restricted to untraced, sequential walks with no alternative limit. Tracing,
`ParallelApplySlots`, finite alternative limits, custom compiled templates, changed slot
optionality, and grammars without shared tails all keep the original battery.
`AnalysisScope.TemplateMemo` (row 2) still wraps the battery unchanged: row 2 reduces how often a
battery runs, and #546 reduces repeats inside one run, so the two compose.

The figures below are C# slot batches on deterministic 200-row samples, single-threaded and warmed,
with mode order alternated per input:

| Input | Completed pairs | Slot batches before -> after | Fewer batches | Parse time, secondary (ms under concurrent load) |
|---|---|---|---|---|
| Synthetic shared-tail fixture (10 words x 100) | 1,000 | 2,000 -> 1,000 | 50.0% | 1.27 s -> 1.05 s |
| Sena (200 sampled rows) | 172 | 47,072 -> 32,287 | 31.4% | 20.07 s -> 16.67 s |
| Mbugwe (200 sampled rows; 61 pairs capped at 2 s in either mode, excluded) | 139 | 72,467 -> 58,486 | 19.3% | 58.43 s -> 51.33 s |

- **Parity in the samples.** Every completed pair had identical signature lists, with zero
  differences and zero errors.
- **Differential test.** `SharedWalkMatchesPerTemplateWalkInOrder` compares ordered `Word` equality
  and complete `AnalysisStateKey` sequences across four grammar arrangements and 2,904 input/state
  combinations.
- **Fire test.** `UntracedBatteryRunsSharedSlotBatchOnce` fails with sharing disabled (`Expected: 1;
  But was: 2`).
- **Unit tests.** HermitCrab passed 126/126.
- **Conformance.** The PR #480-head fixtures were run on the branch assembly and on the unchanged
  base `aab237b2`. With memoization on, both were 58 passed / 2 failed. With memoization off, the
  branch was 58/2 and the base 59/1. Both assemblies fail traced-rule attribution in
  `boundary-leftover-in-narrowing-lhs-strrep`. `deep-optional-affix-nesting` crossed its 15 s budget
  in three runs; it has no shared slot tails and takes the original walk.

These two engines measure different things, so do not compare the columns. C#'s "31.4% fewer slot
batches" on Sena and HC-Rust's "31.6% fewer steps" are not the same unit, and neither is HC-Rust's
11.5% less parser work.

**Conformance pins.** The C# 60/60 self-check below is `hc.dll` at `72dbd4f9`, the PR #480 head. The
58/60 above is PR #546's base `aab237b2`. They are different assemblies, and the two counts are not
in conflict.

**What is different from prior attempts.**
- **Row 2.** The battery memo reduces battery *invocations*. C reduces repeated slot batches
  *within one invocation*, with no `(Word, state)` table.
- **Row 18.** C is not a cross-word cache.
- **The research doc's prediction holds.** It said this would not turn 43,641 battery calls into
  4,988. It does not; it shrinks each call.

**Rejected alongside.** A rule-level memo per battery call was no bigger than slot-level sharing on
Sena (33.7M vs 32.9M work units of repeats), and it is a memo.

## Conformance state at the round's close

The run used the PR #480 head, Machine `72dbd4f9`.

- **C# self-check.** `hc-conformance --include-pathological` passed **60/60 fixtures**: 609 entries
  in the `words.yaml` files, none skipped. Upstream fixtures grew from 44 to 60 since the previous
  pin; nothing was removed.
- **HC-Rust.** One fixture failed: `edge-cases/clitic-rule-designation`, 5 words. Machine PR #529
  added `MorphologicalRule@isCliticRule`, which HC-Rust has not yet ported. It is a parity gap, not a
  divergence, and the port is under way. Every other new or modified fixture passes in both engines.
  No fixture passes in HC-Rust and fails in C#.

## How today's measurements bear on the six research reports

The integrated ranking is in the [research doc](hermitcrab-search-asymptotics-research.md). This
section records only what changed.

| Lead (report) | Status after this round |
|---|---|
| **1. Residual viability**: abstract interpretation with staged refinement (R6), lazy residual-lexicon product (R3), magic sets / goal propagation (R4), extension oracle (R5) | **Ceiling measured, rejected for now** (C1). The plain dead-internal bucket is 0-5% on six of eight lists. Reopens only through the Aweti/v1 shadow experiment below |
| **2. Template battery as a shared control trie** (derived from R2/R3) | **Built, kept, ported** (C; Machine PR #546) |
| **3. Trace-class POR with recovery**: Mazurkiewicz traces (R1), sleep/source-set DPOR (R1), critical-pair certificates (R1); verified POR (R6) | Not pursued. No new measurement. The earlier 0.00-0.05% eligibility still stands as the warning |
| **4. Exact finite-state compilation of a verified subgrammar** (R3, R6, R2 #3) | Not pursued as an exact engine. A narrower **FST-derived precheck** is the best remaining lead for single-template grammars (open leads) |
| Fixed-cap root-result cache (R5 #2; R4 aside) | **Rejected**: repeat-lookup work is 0.5-3.3% of parser work untraced, and the cache is a memo. The 97,575/56 premise was a trace-mode artefact |
| Count-vector chart DP with packed provenance (R2 #1); packed trail forest for synthesis (R2 #2); `AnalysisStateKey` graph with ZDD trails (R4 #1); analysis-DAG x synthesis-state product (R5 #1) | Not pursued. They are search-growing tables, a closed family. The finding that 86-98% of synthesis work is root-reachable failure is consistent with the reports' own warning that one output analysis does not bound product states |
| Proven rule-precedence DAG (R4 #2) | Not pursued. It needs complete-path certificates (research doc section 1) |
| Symmetry reduction (R1 #4) | Not pursued. The reports themselves rated it a probable no-fit |
| Meet-in-the-middle (R3, R4), A*, k-best and k-shortest paths, reverse search (R4, R5), Earley/CKY, scalar semirings (R2), e-graphs (R4), bimachines, two-level morphology, compile-replace (R3) | Still not recommended. Unchanged |

**Prediction scored.** The research doc said the trace-mode lookup ratio could not establish an
untraced duplicate rate. That was right: 0.5-3.3%.

**The finding the reports could not have made.** Where it was measured, much of the private
project's cost was two port bugs: 98.5-99.8% less work on the spot words, and 94.7% less on the
deletion variant's held list. No ranked lead addressed that, because each one assumed the port's
search matched the oracle's. It is not the whole story: with fix A in place, v1's held list still
costs 105.8M work units, 45% of it in a one-template battery that C cannot touch (open lead 1).

## Open leads, not pursued

Each lead is tagged **[C# analogue]** where the same question applies to `hc.dll`, and
**[HC-Rust]** where it concerns the port only.

**Work reductions, with a ceiling to measure first:**
1. **FST-derived precheck before slot-rule traversal, for single-template grammars** [C# analogue].
   C cannot help a grammar with one template, and on the private project's v1 variant the battery
   is still 45% of parser work. A necessary condition derived from each slot rule's compiled matcher
   could reject a rule before traversal begins. This differs from row 22's edge-segment prefilter in
   being judged in work units, since a traversal charges per arc, and in being derived from the
   whole matcher rather than its first and last segments. Its ceiling, the work spent in
   zero-match traversals, has not been measured.
2. **Shadow summary experiment on Aweti and v1** [C# analogue]. These are the only lists whose
   plain dead-internal bucket is material (39.8% and 12.2%). Run a sound summary in shadow mode and
   count the plain-bucket work it would actually decide, along with the summary's own cost. Close
   research lead 1 (C1) if the decided share is small.
3. **Mbugwe's step-capped words** [C# analogue]. 86 of 1,645 words hit the 2M step cap and account
   for about 2/3 of Mbugwe's work. They have not been examined individually.
4. **Root-reachable synthesis failure** [C# analogue]. 86% (deletion variant, before fix B) and 98%
   (Mbugwe) of synthesis work goes to candidates whose lookup hits but whose synthesis never matches
   the surface. No lexical oracle reaches this. It is the largest unaddressed bucket on Mbugwe.

**C# port follow-ons to #546** [C#]:

5. **`ParallelApplySlots`** needs its own design for its layered breadth-first queue.
6. **Share compiled slot rules across templates**, in addition to slot batches.
7. **Measure in a deterministic work counter.** The C# gain is measured in slot batches on samples,
   not in a work counter on full lists.

**Time-only costs (no work units charged, so outside the owner metric):**
8. **`ana_allomorph_matches`** deep-clones segments with `segs.to_vec()` and rebuilds
   `format!("p{i}")` part names on every allomorph call [HC-Rust]. About 5.1M segment-vector builds
   are avoidable per private-v1 held run: 5.48M slot-rule attempts against 365K slot batches.
   Hoisting `segs_of` or borrowing segments in `Transduce::new` removes them.
9. **`root_trie::search_segs_ident`** checks its active set with `Vec::contains`, which is quadratic
   [HC-Rust]. A bitset would be a pure time win.

**Identity over-approximations left in HC-Rust** [HC-Rust]. Synthesis re-validates all of them, so
none can change a parse, but each over-explores relative to C# on some grammar:

10. An `InsertContext` (natural-class insertion) in analysis-side `Lhs` still carries no identity
    above 64 character definitions (entry 090's remainder).
11. Narrowing leftovers from `ana_narrow_general` keep no class identity (entry 103's remainder). No
    measured grammar exercises a class LHS there.
12. `union_narrowing_node` checks only `char_def`, so it ignores an LHS class node's member set.
13. RHS class nodes in synthesis (`syn_narrow`, `syn_epenthesis`) lack identity on featureless
    grammars. They were left alone because they affect rendering.

**Unexplained observations:**
14. **The lookup gap** [HC-Rust vs C#]. HC-Rust on C#'s export made 18 lookups on the slowest word
    against C#'s 49. Parses were identical; the cause was not investigated.
15. **Sena SKIPPED rows** [HC-Rust]. A census run with a 30 s wall limit reported 1,012 SKIPPED
    rows, while no-limit A/B runs completed all 6,146 words in about 50 s. Not investigated.

## Data handling

Private-project word lists, per-word logs and exports stay on the measuring machine. This record
carries only aggregate counts. Conformance fixtures are synthetic.
