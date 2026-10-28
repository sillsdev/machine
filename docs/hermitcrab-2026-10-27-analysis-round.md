# HermitCrab analysis-side optimization, 2026-10-27 round

**Disposition: six candidates are PRs (R1 #575, R2 #572, T1 #571, R5 #574, R3 #576, R1 on #546 #577), one is a
dead end (R4), and one is not built (R6).** This round follows the
[synthesis-to-analysis round](hermitcrab-2026-10-27-synthesis-to-analysis-round.md), whose conclusion was that
synthesis is 1.0% (Aweti) to 9.0% (Mbugwe sample) of total work and the dominant cost is analysis. Here the
target is the analysis side: the template battery, rule outputs that cannot reach a root, matcher work per
attempt, and trace memory. Measurements are in steps, never ms.

This record follows the [optimization ledger](hermitcrab-optimization-ledger.md). Ledger rows 38-46 point here.

## Unit and method

- **Work** is analysis events (the census harness's `ITraceManager` callbacks) plus synthesis steps for the same
  words. "% less work" is `(off - on) / off`, per grammar, never pooled. Where a check does work of its own, a
  net figure is given in one unit and the unit is named.
- Harness: `research/analysis-cost-census` @ `38e9622e`, based on master `c2454760` (includes PR #480's
  conformance suite). Each candidate is an internal Off / Shadow / On mode on `Morpher`, product default On.
  The same binary runs both arms; census mode is traced and unmerged, `censusmerge` uses the merged walk.
- Grammars: Indonesian (all 70 words), Aweti (stride 2), Amharic (limit 28), Mbugwe (stride 40, 150 s per
  word), Sena (stride 100), the conformance suite (150 runs), and one synthetic stress fixture per candidate.
  Words that abort in either arm are dropped from both and listed in the result files.
- Soundness: a shadow mode counts violations (a parse below a fired prune). Any violation stops the candidate.
  Parses are compared per word by a signature of root, rule trail and allomorphs.
- Gates for every PR: the HermitCrab test project on the branch and on master (every branch failure also fails on
  master), conformance per-fixture verdicts identical, `dotnet csharpier check .`, comment hygiene. The full
  `local_check.sh --agent-strict` stops at the SentencePiece build, which needs `cmake`; CI is the first full run.

## Decisions

| Candidate | Issue | PR | Decision |
|---|---|---|---|
| R1 required-slot look-ahead | #561 | #575 | **KEEP.** Kill rule waived, see R1 |
| R2 root-substring prune | #562 | #572 | **KEEP**, with a probe skip |
| T1 lean trace | #563 | #571 | **KEEP.** Lands after #534 |
| R5 edge-segment prefilter | #564 | #574 | **KEEP.** The plan's score of 0 was wrong |
| R4 subsequence fallback | #568 | none | **DEAD END** |
| R6 merging while tracing | #565 | none | Not built. Owner product decision |
| R3 ordered look-ahead | #567 | #576 | **KEEP.** Stacks on #575 |
| R1 on #546 | #566 | #577 | **KEEP.** Stacks on #546 and #575 |

Earlier-round PRs: **#560** (E1, allomorph-loop cutoff) and **#570** (MPR and stem-name trail replay). E3 (template
replay) was dropped on cost; its code stays at `perf/hc-lookup-replay-templates` @ `9efd059c`.

## What the census said to aim at

From the [analysis-cost census](hermitcrab-optimization-ledger.md) the plan took these facts:

- The template battery is 69-98% of analysis events on Aweti, Mbugwe and Sena. Mbugwe has 63.8% of its events in
  template partials that never reach lookup (Sena 22.1%, Aweti 11.7%).
- Not-unapplied attempts are 70-91% of events. Skipping one with a cheaper test removes no step; it makes a step
  cheaper. The plan therefore scored prefilters 0 in steps (R5). That scoring turned out to be the lesson of R5.
- The root-substring predicate at rule outputs had 13.6-38.5% ceilings with 0 violations on the five real
  grammars, but 29 success-path nodes on conformance, so it needed static soundness flags (R2).
- PR #546 shares trailing slot batches only in untraced, sequential walks, so it is inert under tracing.

Eight issues were filed in sillsdev/machine (#561-#568), each checked against open and closed issues and PRs for
duplicates. Linked as related, not duplicates: #485, #512, #551, PRs #546, #534, #490, #480.

## R1. Required-slot look-ahead: KEEP (issue #561, PR #575)

**Claim.** A template partial that reaches a required slot where no rule can unapply dies there, after paying for
every optional slot in between. A necessary condition for a required slot to unapply later is that, for some rule
and allomorph in it, every inserted affix segment unifies with some Segment node of the current shape. The check
runs after the final-template guard, before recursing in `ApplySlots`, and before pushing work in
`ParallelApplySlots`. It is used for slot j only when every slot walked after j is pure (copy and insert only, no
part that can match zero segments). Optional nodes count as present. Memory is grammar-sized tables only: no
per-word state, no cache.

**Soundness.** Shadow mode, 0 violations and 0 completions under a fired check:

| grammar | fired | violated |
|---|---:|---:|
| Mbugwe | 803,228 | 0 |
| Sena | 6,402 | 0 |
| Aweti | 5,948 | 0 |
| Amharic | 584 | 0 |
| Indonesian | 0 | 0 |
| conformance (150 runs) | 298 | 0 |
| stress-bantu-tam | 689 | 0 |

Parses were equal per word, traced and merged. Mutation checks: removing the purity gate fails the impurity
tests, and disabling the mode fails the prune tests.

**Event-based result** (analysis events, fewer is better; synthesis steps unchanged):

| grammar | analysis events off to on | % less events | % less work (events plus synthesis) |
|---|---|---:|---:|
| Mbugwe (40 words) | 5,289,909 to 4,940,905 | 6.60% | 6.29% |
| Sena (51) | 110,986 to 76,809 | 30.79% | 30.27% |
| Aweti (99) | 1,596,848 to 1,475,588 | 7.59% | 7.52% |
| Amharic (24) | 8,560 to 7,800 | 8.88% | 7.73% |
| conformance | 38,393 to 37,796 | 1.55% | 1.28% |
| Indonesian (70) | 7,468 to 7,468 | 0% | 0% |
| stress-bantu-tam (60) | 45,342 to 11,497 | 74.64% | 74.35% |

**Unification-work result.** The unit is top-level `FeatureStruct` unification calls (`IsUnifiable` plus `Unify`)
over the whole parse, look-ahead checks included, counted by census-only counters. Positive is less work:

| grammar | calls off to on | % less unification work | direction |
|---|---|---:|---|
| Mbugwe | 21,515,185 to 21,444,795 | +0.33 | break-even, slightly less |
| Sena | 1,308,998 to 1,288,517 | +1.56 | less |
| Aweti | 17,699,599 to 16,902,830 | +4.50 | less |
| Amharic | 411,073 to 413,674 | **-0.63** | **more work** |
| Indonesian | 662,201 to 662,201 | 0.00 | none |
| conformance | 303,738 to 303,313 | +0.14 | break-even |
| stress-bantu-tam | 162,015 to 100,593 | +37.91 | less |

Mbugwe's large event saving is mostly walk and trace work that costs few unifications, so little of it shows in
this unit. The check cost 1,112,761 unifications on Mbugwe against 349,004 events saved. Mbugwe reaches only
10.3% of its no-lookup ceiling, because its required slots mostly insert one segment (`-a`, `a-`), which is nearly
always present somewhere in the shape. R3 is the follow-up.

**Waived kill rule.** The plan said to close R1 if check unifications exceeded 3x the events saved on the grammar
with the largest saving. That fired on Mbugwe at 3.19x. It was **waived**, because it compared unification calls
with walk and trace events, which are unlike units. A same-unit comparison replaced it (table above). The earlier
ratios (1.5x to 12.9x) are withdrawn. The review asked for this to be stated plainly in the PR, with a direction
word on every figure, and it is.

**Tried and rejected.** Skipping slots whose every allomorph inserts a single segment: it did not cut Mbugwe's
check cost (1,119,073 against 1,112,761 check unifications) and halved Sena's saving (17.75% against 30.79% of
events; unification work -0.70% instead of +1.56%). The product keeps the uncut test.

**Gates.** HermitCrab tests 168 passed, 0 failed on the branch (master 151 passed). Conformance 146 passed and 4
failed on both, the four being the nondeterministic `truncate-morphotactic` family. csharpier clean, comment
hygiene clean. Product branch `perf/hc-required-slot-lookahead` @ `0bf9e646` at the time of the result (changed by
review); measurement branch `research/hc-rsl-census` @ `6ad11702`; the unification counters live only there.

**Risks.** Amharic is net more work in unification calls, on 24 words. The stress fixture is synthetic. PR #534
edits the same file; the diff is three hooks and one helper. Aborted words (the heaviest) are dropped from both
arms, so the event saving is a floor for them but unproven in unification calls.

## R2. Root-substring prune: KEEP (issue #562, PR #572)

**Claim.** When a node's shape contains no root allomorph as a contiguous substring, and nothing that can still be
unapplied below it can restore, insert or rejoin stem material, no lookup below it can hit. The probe runs at
the three analysis rule outputs. Three static per-stratum conditions turn it off: a rule that drops a part that
can hold segments, copies a part more than once, copies out of order or modifies a part; deeper phonological
rules; and lexical-pattern roots. A rule type it does not know also turns it off.

**Why it is not row 27.** Row 27 closed root reachability summaries (the prunable bucket was 0-5% on 6 of 8 lists,
and nodes may still compound). A substring test passes a compound, because the compound contains its head root;
it runs at every rule output, including template-slot outputs; and the flags turn it off where stem material can
be restored. Row 27's reopening condition, a shadow summary deciding a meaningful share, is met on Sena (17.7%) and
the stress fixture (67.7%), with 0 violations.

**Where the flag is on** (first failing condition per stratum): Sena all three strata; the stress fixture's main
stratum; conformance the main stratum in 126 of 150 runs. Off: Indonesian (a rule copies a part more than once;
deeper phonology), Mbugwe (a rule drops a part that can hold segments), Amharic (a rule drops a part), Aweti (a
rule drops a part). Census ceilings for those four, from the plan, were Mbugwe 38.5%, Indonesian 29.6%, Aweti 14.8%
and Amharic 13.6%, which are not reachable soundly (see R4).

**Net of probes.** After review, each probe is charged as one event:

| list | gross % less work | net % less work | probes | note |
|---|---:|---:|---:|---|
| Sena (51 words) | 17.7 | **15.3** | 2,698 | parse sets equal |
| stress, early-lexicon agglutinative (60) | 67.7 | **66.5** | 245 | 13,584 events saved |
| conformance | 0.2 | **-15.3** (more work) | 7,237 | buys 92 events; 2 words save more than they probe |
| Indonesian, Mbugwe, Amharic, Aweti | 0 | **0** | 0 | flag off |

The conformance result is the honest cost: it is a suite of tiny words and small lexicons, so the probes exceed the
events saved, and there is no adaptive off switch. Only Sena among real grammars gains.

**Probe skip.** Added after review: the probe is skipped when the output keeps at least as many segments as its
input within the same stratum. It is sound (a skip only keeps a word). It removed only 18 of 7,255 probes.

**Rejected: a root-count gate.** Turning the prune off for strata with fewer than 16 roots would have hidden the
conformance loss. The coordinator rejected it: it is tuned to the toy lexicons in the conformance suite, and no
real grammar would trigger it. Its census was cancelled, so it has no measured result.

**Gates.** HermitCrab tests 161 passed, 0 failed (master 151). 10 new tests in `RootSubstringPruneTests`;
removing each flag condition turns exactly its test red. Conformance 146 passed and 4 failed on both arms, per
fixture identical. Branch `perf/hc-root-substring-prune` @ `d58a8e88`.

**Risks.** Pruned analyses no longer count toward the analysis-side alternative limit, so a parse that threw
`MaxAlternativesExceeded` may now succeed, never the reverse. Try-a-Word traces lose the subtree below a pruned
output. Soundness depends on `AnalysisMorphologicalTransform.GenerateShape` semantics; a future analysis step that
changes shape outside rules and stratum entry would break it silently.

## T1. Lean trace: KEEP, lands after #534 (issue #563, PR #571)

**Claim.** The default `TraceManager` pins about 1 GB on Mbugwe yɔɔna (1,031.95 MiB retained for 21,200 analysis
events and 41,130 synthesis steps, from the trace-memory study) because every node pins a `Word`, either live or
as one of six deep clones. An opt-in `TraceManager.RetainWords = false` keeps shape text (`Trace.InputText`,
`Trace.OutputText`) and no `Word`. Steps are unchanged by construction. The default stays `true`: `Trace.Input`
and `Trace.Output` are public and would be null in lean mode, so flipping is an owner decision. The CLI opts in.

Retained trace memory, same word, same run, product arm against lean arm:

| grammar | words | mean full MiB | mean lean MiB | % less (sum) | max full to lean MiB |
|---|---:|---:|---:|---:|---|
| Mbugwe (stride 40) | 40 | 407.2 | 25.10 | 93.8% | 5,485.6 to 389.3 |
| Aweti (stride 2) | 99 | 35.6 | 2.28 | 93.6% | 656.2 to 29.4 |
| Sena (stride 100) | 51 | 4.35 | 0.35 | 92.0% | 27.5 to 2.0 |
| Amharic (limit 28) | 24 | 6.86 | 0.08 | 98.8% | 17.5 to 0.2 |
| Indonesian | 70 | 2.24 | 0.03 | 98.7% | 18.1 to 0.3 |
| conformance | 1,476 | 0.45 | 0.01 | 97.4% | 329.0 to 7.5 |
| stress deep-derivation | 10 | 200.36 | 4.99 | 97.5% | 343.0 to 7.8 |

yɔɔna: 1,035.0 to 22.6 MiB (97.8% less); peak falls from 1,038 to 89 MiB. The kill criterion (under 50% on
yɔɔna) is not met. What still holds the lean memory is the node objects and two strings per node, about 190 bytes
a node on yɔɔna (an estimate from totals, not a heap count). Allocation is flat: lean adds text formatting and saves
clones.

**Steps are identical.** Parse counts and node counts equal for every compared word; the census rows for Indonesian
(70 words) and yɔɔna are byte-equal (md5) between base and product; conformance per-fixture verdicts are identical;
CLI output is byte-identical to master with one processor apart from the "Parse time" line. With default
parallelism master itself orders some synthesis nodes differently run to run.

**Landing order.** PR #534 rewrites nearly every `TraceManager` callback, `Trace.cs` and `ParseCommand.cs`, so a
textual conflict is certain. Land #534 first, then rebase this onto it, projecting #534's fields in lean mode:
copy strings or ids at the callback (summarise `FailureObject`, keep the allomorph id, the blocking entry name and
the slot rule name) instead of references.

**Gates.** HermitCrab tests 156 passed, 0 failed (master 151). 5 new `LeanTraceTests`; forcing lean mode to store
words fails 2 of 5. csharpier and hygiene clean. Product commit `9f7828e8` on `perf/hc-lean-trace`.

**Risks.** Lean text of an unfrozen word is taken at callback time; full mode formats at read time. An unfrozen
word mutated after the callback would differ. An instrumented run found none: unfrozen words reach callbacks only
as private pre-rule clones, compounding temporaries and phonological outputs the manager clones. Default mode still
pins about 1 GB on yɔɔna.

## R5. Edge-segment prefilter: KEEP (issue #564, PR #574)

**Claim.** Port ledger row 22's analysis affix prefilter to master. A pattern anchored at both ends must match its
first and last segment by unification, so a mandatory edge segment that cannot unify rejects the attempt before
the matcher runs. It skips boundaries, lets optional edge segments through, and emits the existing failed
unapplication trace callback. An internal Off/On switch defaults On.

**The plan scored it 0, and that was wrong.** Census steps (analysis events and synthesis steps) are **unchanged**
on all 1,796 completed pairs per mode, so by the owner's step metric the candidate removes nothing. It makes each
failed attempt cheaper. The first measurement reported gross matcher work and left out the prefilter's own
unifications; review required the net. The headline is **net matcher work**: traversals plus arc checks, with each
prefilter unification charged as one arc check. This is the lesson: a step count is blind to a change that
cheapens each step.

Census mode (unmerged), pairs completed in both arms:

| list | traversals off to on | arc checks off to on | prefilter unifications | gross less | net less |
|---|---|---|---:|---:|---:|
| Aweti | 1,539,228 to 444,856 | 18,959,730 to 9,102,289 | 1,221,061 | 53.43% | **47.47%** |
| Mbugwe | 4,894,285 to 2,464,790 | 18,994,380 to 13,973,576 | 2,763,590 | 31.19% | **19.62%** |
| Sena | 110,368 to 30,893 | 1,297,930 to 1,014,492 | 87,560 | 25.77% | **19.55%** |
| Amharic | 10,143 to 6,931 | 345,463 to 300,000 | 3,305 | 13.69% | **12.76%** |
| Indonesian | 21,107 to 18,767 | 654,511 to 610,183 | 2,682 | 6.91% | **6.51%** |
| stress | 20,284 to 3,589 | 343,290 to 58,511 | 19,154 | 82.92% | 77.65% |
| conformance | 43,682 to 35,667 | 252,488 to 175,788 | 16,248 | 28.60% | 23.12% |

`censusmerge` gives the same ordering (Aweti 49.24%, Mbugwe 21.24%, Sena 19.83%, Amharic 12.94%, Indonesian 6.60%).
Counts are unweighted, not a timing claim. The user-specified kill criterion (net under 5% on every real grammar)
is not met: the smallest net is Indonesian at 6.51%.

**Parity.** All 1,796 completed pairs per mode have identical parse counts and signature sets. The
`edge-cases/truncate-morphotactic` family is excluded (repeated runs differ on master itself). Exclusions are
listed in `edge-prefilter-evidence/census-net-summary.txt` of the working set.

**Memory.** Mbugwe has 874 compiled analysis affix-rule instances, 1,620 allomorph specs and 3,648 list-capacity
slots, giving a conservative 108.4 KiB of retained metadata (below 159 KiB with abandoned list backing arrays).
Counts measured, byte figures are 64-bit layout estimates, not a heap profile. Nothing per word, no cache.

**Gates.** HermitCrab tests 169 passed (master 151); tests cover the exact failure trace on matcher and prefilter
rejection, alpha-variable constraints, a group with an optional leading constraint, and a multi-segment suffix
ending in the `ts` digraph. Conformance verdicts identical (146 passed, 4 failed on both). Both `local_check.sh`
runs fail at the SentencePiece `cmake` step. Branch `perf/hc-edge-segment-prefilter` @ `bb525683`.

## R4. Subsequence fallback and insertion classes: DEAD END (issue #568)

**Claim tested.** R2 is off on four of five real grammars because their rules restore, reorder or duplicate stem
material. R4 asked whether a weaker sound test still prunes: "some root is a subsequence of the shape" for
order-preserving removal, with the segment classes that a dropped part can restore treated as wildcard gaps.
Ceilings from the census, minus any R1 overlap: Mbugwe 38.5%, Indonesian 29.6%, Aweti 14.8%, Amharic 13.6%.

**Built.** `RootPruneRuleShape` classifies each rule as substring-safe, subsequence-safe or order-free-safe, with
the segment classes its dropped parts restore. `RootSubsequenceTrie` holds the roots of the stratum and deeper
strata; root segments that unify with a gap class are skipped, since a restorable segment may be re-inserted
anywhere; the earliest-match search runs depth first; order-free mode ignores positions. Each stratum gets the
tier Substring, else Subsequence, else Order-free, and turns off for rules that modify a part, unknown rules,
deeper phonology, lexical-pattern roots, or when every root is made of restorable segments. Memory is one small
trie per stratum (Mbugwe 165 roots). Product branch `perf/hc-root-subsequence-prune`, stacked on R2's `d58a8e88`,
left unsquashed with the order-free code at **`71cba028`**. No PR.

**Cost accounting.** One probe is 1 step and each trie arc examined is 1 step (`RootSubsequenceSteps`, a hook
that is null in production).

Sound subsequence tier with gaps, net:

| list | status | analysis events off to on | net % less work |
|---|---|---|---:|
| Indonesian | off (REDUP-meN copies parts out of LHS order) | 7,468 to 7,468 | 0.0 |
| Amharic | off (a rule modifies a part) | 8,560 to 8,560 | 0.0 |
| Aweti | off (a rule copies parts out of LHS order) | 1,596,848 to 1,596,848 | 0.0 |
| Mbugwe | off (every root is made of restorable segments) | 3,500,941 to 3,500,941 | 0.0 |
| Sena | on, R2's tier unchanged | 110,986 to 91,010 | 15.3 |
| stress | on | 19,959 to 6,375 | 66.5 |
| conformance | on in many fixtures | 38,393 to 38,225 | -19.1 |

With the order-free tier added (rules that also reorder parts):

| list | status | events off to on | gross % | net % | probes | arcs | shadow violations |
|---|---|---|---:|---:|---:|---:|---:|
| Indonesian | on (Clitics, Surface off: deeper phonology) | 7,468 to 4,956 | 30.8 | **-182.9** (more work) | 631 | 16,779 | 0 |
| Aweti | off (every root is restorable) | unchanged | 0.0 | 0.0 | 0 | 0 | n/a |
| Mbugwe, Amharic | off, as above | unchanged | 0.0 | 0.0 | | | |
| stress | on | 19,959 to 6,375 | 67.7 | 66.5 | 245 | 0 | 0 |
| conformance | on | 38,393 to 38,225 | 0.4 | -19.1 | 7,432 | 1,657 | 0 |

**Kill criterion.** Under 5% net on every one of Mbugwe, Indonesian, Aweti, Amharic: met (0.0, -182.9, 0.0, 0.0).
No parse differences outside the known truncate-morphotactic family. Verdict DEAD-END.

**Why.**

- **The ceilings are unsound.** The flag-ignoring census (fires of the plain subsequence predicate, share of events
  below the topmost pruned nodes) is Indonesian 55.2%, Mbugwe 36.1%, Aweti 21.7%, Amharic 50.9%, Sena 16.8%,
  stress 50.6%. Most of it in the three restoring grammars is unsound: **violation events 334,508 (Mbugwe),
  50,187 (Aweti)**, 2,264 (Amharic) and 1,105 (Indonesian). A raw subsequence prune would lose real parses.
- **The insertion-class (wildcard gap) variant never fires** on Mbugwe, Aweti or Amharic: their dropped parts
  describe classes that cover every root segment, so no word can be ruled out. Position-specific gaps were not
  tried.
- **Amharic is blocked** by a rule that modifies a part, for which there is no sound test.
- **Indonesian is the one grammar where the sound tier works** (30.8% fewer events, 0 shadow violations), and it
  costs 26.6 trie arcs per probe against about 4 events saved per probe: a net loss at one step per arc. Every
  cheaper structure considered (a set of present token classes, a bitmask per root) costs about the same 25-40 steps
  per probe as there are token classes.

**Revival would need** a test that costs well under one step per saved event on Indonesian, position-specific gaps
for the restoring grammars, and a sound treatment of rules that modify a part. None is in sight.

**Known nondeterminism.** Conformance parse signatures differ for the same words between off-arm census and
censusmerge runs (`truncate-morphotactic-identity#default` word 2), and the on arms differ only on words in the
`truncate-morphotactic` family. Parse counts are equal.

## R6, R3 and R1 on #546

- **R6, merging while tracing (#565): not built.** Census merging changes analysis events by only 0.2-2.4%, but
  under tracing lookups are 1.67x (Mbugwe) to 2.19x (Aweti) more frequent. Whether Try-a-Word shows folded
  duplicates is an owner product decision, and it is low in steps. It interacts with #534 and #563.
- **R3, ordered and edge-anchored look-ahead (#567): KEEP, PR #576.** It targets the 90% of Mbugwe's no-lookup
  ceiling that R1 leaves, where required slots insert a single segment. Results are in the section above.
- **R1 on #546 (#566): KEEP, PR #577; results in the section above.** It measures R1 on the shared-slot walk and reports what #546 leaves. The rule
  set in advance: if R1 saves under 3% of slot events on the #546 walk where it saved over 5% on master, the two
  overlap and only one should land. Further sharing of non-trailing slots needs a key on the shape, which is a memo,
  so any residual is reported as a ceiling and not proposed.

## R3 and R1 on #546: KEEP, and the two stack (issues #567, #566; PRs #576, #577)

- **R3, ordered and edge-anchored look-ahead (#567; PR #576, stacked on #575).** Within a rule, the required inserted
  segments must embed in order; the check is anchored at the edge it can reach; and a passing required slot narrows the
  window for the slots behind it. Shadow: 0 violations on every grammar, and parses are equal per word. Events plus
  synthesis steps, less work than master: Sena **40.9% less**, Mbugwe **7.3% less**, Aweti **11.0% less**, Amharic
  **11.9% less**. Unification calls, less than master: Amharic **2.75% less** (R1 alone was 0.63% MORE), Sena
  **5.22% less**, Aweti **9.27% less**, Mbugwe **1.83% less**. Mbugwe gains little over R1 because most of its required
  slots are not testable; the next step is an R1 purity extension with its own soundness argument. PanGloss port:
  sillsdev/PanGloss #65.
- **R1 on #546 (#566; PR #577, stacked on #546 and #575, commit `bbb81a00`).** The look-ahead runs on the shared trie
  walk, so a template whose required slot cannot unapply is dropped for its whole subtree. Reviewed LAND. Shadow: 0
  violations. Events plus synthesis steps, less work than master: Sena **55.64% less**, Mbugwe **32.15% less**, Aweti
  **20.06% less**, Amharic **9.10% less**. Unification calls, less than master: Sena **4.81% less**, Mbugwe **15.19%
  less**, Aweti **12.18% less**; Amharic **0.63% MORE**. Against #546 alone, Mbugwe **1.07% MORE** and Sena **1.40%
  MORE** unification calls. The two stack in events, but on Mbugwe and Sena the check's own unifications cost more than
  the walk work they save. PanGloss port: sillsdev/PanGloss #66 (for #546 and #566).

## Cross-cutting lessons

1. **Count a check's own cost in the same unit as what it saves.** R1, R2 and R5 all first reported gross numbers.
   Net, R1 broke even on Mbugwe and lost on Amharic; R2 lost 15.3% on conformance; R5 fell from 6.91% to 6.51% on
   Indonesian and from 31.19% to 19.62% on Mbugwe. R1's 3x kill rule compared unification calls with walk and trace
   events and was waived for that reason, with a same-unit comparison in its place. R4 was killed by the same
   accounting: 30.8% gross became 182.9% more work.
2. **Truncate-morphotactic conformance fixtures are nondeterministic on master and must be excluded from parity.**
   `truncate-morphotactic`, `-copy-u`, `-identity` and `-subsumption` give different output run to run with no
   change. Their mismatch detail text differs between two base runs, so only per-fixture PASS and FAIL is
   comparable, and the same four fail on both arms of every experiment. PanGloss waives the same family.
3. **Census steps miss work that cheapens each step.** R5 scores 0 in steps and saves 6.5% to 47.5% of matcher
   work. Choose the unit the change acts on, report steps beside it, and do not rank by steps alone.
4. **No memoization (owner rule).** It is why E3's replay was dropped instead of cached, why R1 and R5 keep
   grammar-sized tables only, and why shared work beyond #546 is a reported ceiling and not a proposal.
5. **A port can expose a divergence in the other implementation.** The PanGloss port of machine #548 found and
   fixed a real Rust divergence in alpha-environment selection (PanGloss PR #13). Porting a change to the second
   engine is a cheap second oracle.
6. **A ceiling measured with the flags ignored can be mostly unsound.** R4's census ceilings (up to 55.2%) were
   mostly restoration violations; the sound share on the four target grammars was zero.
7. **A gate tuned to the test lexicons is overfitting.** R2's root-count gate would have removed the conformance
   loss and no real-grammar gain, so it was rejected.

## Open questions

- R3 and R1-on-#546 results (issues #567, #566).
- Whether a sound subsequence test can be made cheap enough for Indonesian-style grammars, where the sound tier
  prunes 30.8% of events but costs 26.6 arcs per probe.
- R6: whether Try-a-Word should show folded duplicates (owner decision).
- Landing order of #571 and #534, and whether the default of `RetainWords` should ever flip (a major-version
  decision).
- R2 on conformance-like workloads: the prune has no adaptive off switch, and loses 15.3% there.
