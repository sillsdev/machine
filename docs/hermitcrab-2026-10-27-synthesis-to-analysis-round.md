# HermitCrab synthesis-to-analysis pruning, 2026-10-27 round

**Disposition: round finished; no product code is proposed for this PR.** This round answers John
Maxwell's question about which synthesis tests could move into analysis. It records a failure-reason
census, gives a direct answer for each of his four candidates, and ranks six experiments. Five were
built and measured on throwaway branches (none pushed); E6 stays research-only. The results are in
[Results of the round](#results-of-the-round). Measurements are in steps, never time.

This record follows the [optimization ledger](hermitcrab-optimization-ledger.md). Ledger rows 31-37
point here.

## Answer to John Maxwell's questions

**Failure-reason statistics.** They exist now (census below, harness `research/failure-reason-stats`
@ `af97348b`). On the five real grammars, pooled, 71.8% of failed synthesis branches and 43.5% of
synthesis steps end in `PartialParse / ApplicableTemplatesNotApplied`, and 25.6% of steps end in
`SurfaceFormMismatch`. Your four reasons together are 0.1% of failed branches and 0.1% of steps.
Caveats from the method section that bind every number: the counts come from traced parses (tracing
turns merging off, so more analyses are explored); a step is a count, and `RequiredMprFeatures`
events are mostly allomorph selection, not failure; branch outcomes are not candidate outcomes; 22.1%
of steps have no terminal event and are unattributed; the samples are strides of five grammars, with
heavy words dropped by the per-word budget; and the census counts synthesis steps only, so it does
not see analysis work.

**Excluded co-occurrence (E4): sound, cheap, effective only where exclusions are dense.** The check
is sound: parses are identical on every corpus, and the zero-width "other" trap you might expect does
not occur for affix-process allomorphs. It is the only experiment of the round that removes analysis
events. On the five real grammars it removes 0% (none has an affix-only exclude rule), on the
conformance suite 0.23% of total work. On the brief's own stress fixture it gives 3.2%, and on two
deliberately dense synthetic grammars 31.3% (Athabaskan-style prefix complex) and 19.8%
(Bantu-style extension chain). Verdict PARK: land it, if at all, as a cheap safety net for grammars
with many ad hoc co-prohibitions.

**DisjunctiveAllomorph: already mostly early; no experiment.** The environment-free `break` in the
synthesis allomorph loop already stops later allomorphs, and what remains needs the surface form.
Disjunctions conditioned by MPR features are covered by E2, and the loop itself is shortened by E1.
Real-grammar terminal share is 0.0%.

**Environments (E5, the input-word prefilter): dead end as specified.** The soundness scan has to
disable the prefilter on 5 of 5 real grammars (four have ambiguous digraph segmentation, one has
non-uniform feature sets) and on 145 of 150 conformance runs. Forced on against that scan it is net
negative on two of three stress fixtures once matcher invocations are counted, and about neutral on
Aweti (net -0.4%). See E5 below for what would have to change.

**ExcludedMprFeatures (covered by E2, the MPR and stem-name trail replay): sound; worth keeping,
but small in total work.** Excluded MPR features are replayed with the required ones at lexical
lookup. The prune is sound (a shadow census found no successful candidate that it rejects), and it
saves 56.9% of synthesis steps on Aweti and 11.4% on Mbugwe. Those are shares of synthesis, which is
a small part of the whole parse (next paragraph).

**What the data says matters instead.** Synthesis steps are a small fraction of total work, which is
analysis events plus synthesis steps. On the Aweti sample synthesis is 1.0% of the total (15,940 of
1,560,180). On the Mbugwe sample it is 9.0% (130,647 of 1,449,301). E1, E2 and E3 save synthesis
steps only, so their "% less work" is a share of synthesis. In total work they save at most 1.1% on a
real grammar (table below). The dominant cost is analysis. The next round should target analysis: for
example, extending E4-style monotone checks earlier, and the `ApplicableTemplatesNotApplied` analysis
paths that the census ranks first.

## The question

HermitCrab is generate-and-test. Analysis unapplies rules to generate candidate analyses, and
synthesis re-applies them to test each candidate. A test that runs in analysis prunes a candidate
before it is expanded, looked up and synthesized. [PR #519](https://github.com/sillsdev/machine/pull/519)
did this for reduplication copy agreement. Maxwell asked whether other tests could move. His
candidates were:

- excluded `MorphemeCoOccurrenceRules` (monotone: once a morpheme is present it stays);
- `DisjunctiveAllomorph`;
- `Environments`: check a required environment against the input word in linear time, finding the
  left context and then the right context after it. An excluded environment would need its left and
  right contexts to be present everywhere;
- `ExcludedMprFeatures`.

His principle is that an excluded property can be checked early if nothing later can remove it, and
a required one usually cannot. He also asked whether there are statistics on failure reasons. There
were none: `FailureReason` does not appear in any document in PR #490. This round adds them.

## Scope: the target is the space of grammars, not the five samples

The owner set the scope during the round. The five sampled reference grammars (Sena, Amharic,
Aweti, Mbugwe, Indonesian) rarely use co-occurrence rules, environments, disjunctive allomorphs or
MPR features. A census of their XML finds 0-2 co-occurrence rules each, required MPR features on 0-9
allomorphs, and no `Overwrite` MPR groups. The target is every grammar HermitCrab can represent that
some language family would plausibly use. Each candidate is therefore ranked by soundness and by its
benefit across that space. The measured samples are one data point, and they do not decide the
ranking. For every candidate this record names:

- (a) the language types that lean on the feature;
- (b) the worst-case blow-up when the check runs late compared with early, in steps;
- (c) a stress fixture that models such a language at realistic density.

Every experiment's measurement must include its stress fixture as well as the conformance suite and
the five real grammars.

## Failure-reason census

**Reproduce.** Branch `research/failure-reason-stats` @ `af97348b` is master `c2454760` with PR #480
merged. Its harness is test-only:
`tests/SIL.Machine.Morphology.HermitCrab.Tests/Research/{FailureReasonStatsHarness.cs,
FailureStatsTraceManager.cs, run_failure_stats.sh, make_failure_stats_report.py}`. Run
`bash tests/SIL.Machine.Morphology.HermitCrab.Tests/Research/run_failure_stats.sh OUT_DIR` from the
repository root. It writes one TSV per corpus and a `tables.md`. No product code changed.

**Definitions.**
- A *step* is one rule-level synthesis trace event: a rule applied, or one subrule attempt that did
  not apply. Steps are counted, never timed.
- A *terminal outcome* is a synthesis branch that ends in `Failed(reason)`, `Successful` or `Blocked`.
- *Steps wasted per reason* attaches each step to its word through `Word.CurrentTrace` lineage, then
  splits it equally among that word's terminal descendants.
- *Rule-attempt reasons* (`RequiredMprFeatures`, `Pattern`, `AllomorphNotApplicable`, ...) mean one
  subrule did not fire. The word goes on, so these are never terminal by themselves.

**Corpora.** The real grammars are samples, not whole corpora: Indonesian all 70 words, Aweti every
2nd (103), Amharic the first 28, Mbugwe every 40th (42), Sena every 100th (62). The conformance suite
has 150 fixture runs and 1,492 words.

### Headline numbers

Real grammars, pooled (27,912 failed and 422 successful terminal outcomes; 279,872 synthesis steps):

| Terminal reason | % of failed branches | % of synthesis steps |
|---|---:|---:|
| PartialParse / ApplicableTemplatesNotApplied | 71.8% | 43.5% |
| SurfaceFormMismatch | 4.9% | 25.6% |
| (no terminal event) | - | 22.1% |
| (success) | - | 3.8% |
| PartialParse / MorphologicalRulesPending | 14.8% | 2.1% |
| PartialParse / NonFinalTemplateAppliedLast | 5.2% | 1.6% |
| PartialParse / StratumRulesRemaining | 3.3% | 1.2% |
| RequiredStemName, Environments/Require, BoundRoot, DisjunctiveAllomorph | 0.1% together | 0.1% together |

Rule-attempt events, real grammars: `RequiredMprFeatures` 41.4%, `Applied` 21.7% (morphological)
plus 1.7% (phonological), phonological `Pattern` 17.0%, `AllomorphNotApplicable` 11.6%, morphological
`Pattern` 5.2%, and `ExcludedMprFeatures` 2 events (0.0%).

Per grammar, the step shares differ widely. Unpooled:

| Grammar | Largest wasted-step buckets |
|---|---|
| Mbugwe | ApplicableTemplatesNotApplied 46.3%, SurfaceFormMismatch 27.2%, no terminal 22.8% |
| Aweti | MorphologicalRulesPending 30.4%, NonFinalTemplateAppliedLast 28.5%, success 16.0% |
| Amharic | MorphologicalRulesPending 60.0%, SurfaceFormMismatch 16.9% |
| Indonesian | success 51.9%, SurfaceFormMismatch 37.9% |
| Sena | no terminal 56.3%, MorphologicalRulesPending 13.3%, ApplicableTemplatesNotApplied 6.8% |

The conformance suite is dominated by one pathological fixture. `deep-optional-affix-nesting`
contributes 5,556 of 6,789 failed branches. Pooled, StratumRulesRemaining has 40.7% of the suite's
steps, success 44.1% and SurfaceFormMismatch 6.9%. Maxwell's four reasons together have 0.9% of
conformance steps.

### Caveats on the method

These are read before any number above is used as a ceiling.

1. **Tracing changes the search.** `Failed(...)` is raised only while tracing. Tracing turns off
   equivalent-analysis merging (`AnalysisStratumRule.cs:142`), so traced parses explore more
   analyses than production parses. The counts describe a traced parse.
2. **A step is a count, not a cost.** A `RequiredMprFeatures` event is one set lookup
   (`SynthesisAffixProcessRule.cs:144-160`). An `Applied` or `AllomorphNotApplicable` event is a full
   allomorph pattern match plus an output shape (`:179`). Both count as one step. Ledger row 7 is the
   precedent: 100% of a rejection histogram cost 0.2% of wall time.
3. **Analysis work is outside the step total.** On Mbugwe the sampled words had 4,598,689
   analysis-side morphological rule events (unapplied plus not unapplied) against 260,070 synthesis steps. A
   synthesis-only saving of N% is not N% of the work to parse the words. Every brief reports
   analysis events and synthesis steps separately.
4. **Most `RequiredMprFeatures` events are allomorph selection, not failure.** Mbugwe has
   `requiredMPRFeatures` on only 9 allomorphs, yet it logged 113,419 such events against 56,405
   morphological applications. The allomorph loop logs one event for each gated allomorph it skips
   on the way to the one that fires. These events show where the loop spends time. They are not why
   candidates die.
5. **Branch outcomes are not candidate outcomes.** `ApplicableTemplatesNotApplied` fires on one path
   through a stratum. `SynthesisStratumRule.Apply` (`:58`, `:104-140`) runs the templates both on the
   input and after each morphological-rule output, so a candidate can lose one path and succeed on
   another. A prune that drops whole candidates needs a candidate-level census: the steps spent in
   candidates with zero successes. That census was not run. The 2026-10-21 round measured the
   HC-Rust analogue at 86-98% of synthesis work (ledger row 27, "root-reachable synthesis failure").
6. **The 22.1% "no terminal event" share is unattributed.** It is steps on branches that ended with
   no terminal callback. Template slot paths that die inside `SynthesisAffixTemplateRule.ApplySlots`
   while a sibling path survives are one source. Silently dropped words are another: the realizational
   gate at `SynthesisAffixTemplatesRule.cs:27-28` and `HashSet` duplicates. This split is an
   inference and was not measured. Treat the share as mostly template-path failure.
7. **Samples and budgets.** A 30 s per-word budget dropped 2 Mbugwe words, so the heaviest words are
   under-represented. `run_failure_stats.sh` runs the five grammars concurrently, so which words hit
   the budget can vary between runs. A/B runs must be sequential and must drop a word that aborted
   in either arm. Amharic stops at word 28 (word 29 hangs).

## Direct answers to Maxwell's four candidates

All four are sound to move in at least a restricted form, and the census predicts little benefit
from any of them on the samples. On the samples the work goes to template-path failures and surface
mismatches.

| Candidate | Sound early? | Where | Terminal share on the samples | Who needs it |
|---|---|---|---|---|
| Excluded `MorphemeCoOccurrenceRules` / `AllomorphCoOccurrenceRules` | Yes for `Anywhere`, with a zero-width caveat; not for `Somewhere*` or `Adjacent*` without linear order | Analysis, per rule (H1) | Real 0; conformance 6 branches (0.1% of steps) | Polysynthetic and Bantu morphotactics; FLEx "ad hoc co-prohibitions" written to kill spurious parses |
| `DisjunctiveAllomorph` | Mostly already early; the rest needs the surface | Synthesis loop | Real 13 (0.0%); conformance 13 (0.2%) | Romance and Semitic suppletion, IE stem allomorphy |
| Required `Environments` (input-word prefilter) | Yes, under two conditions (below) | Analysis, per allomorph (H1) | Real 8 (0.0%); conformance 15 (0.4%) | Turkic and Uralic harmony, Bantu, Austronesian nasal substitution |
| `ExcludedMprFeatures` | Yes, by trail replay after lexical lookup | After lookup (H4) | Real 0 terminal, 2 rule events; conformance 11 rule events | IE declension and conjugation classes, Bantu noun class |

Hook points (origin/master `c2454760`): **H1**, per analysis rule, after `MorphologicalRuleUnapplied`
at `MorphologicalRules/AnalysisAffixProcessRule.cs:92`. **H3**, end of analysis,
`Morpher.cs:179-193`. **H4**, after lexical lookup and `ExpandAlternatives`, once per alternative, just
before `_synthesisRule.Apply` at `Morpher.cs:353-358` (sequential) and `:386-395` (parallel). The filter
must see each alternative, not the canonical word. With merging on, which is the production setting,
`ExpandAlternatives` (`Word.cs:440-482`) rebuilds merged analyses with their own rules and
allomorphs, so dropping the canonical would drop analyses with different trails. Traced runs hide
this trap, because tracing turns merging off.

**One principle covers all four.** A successful candidate applies every entry of its trail
(`Word._mruleApps`) in a fixed order, with the allomorph analysis recorded for it
(`Word.IsAllomorphApplicable`, `Word.cs:254-261`, added by PR #548). Any quantity that depends only
on the root, the trail and the recorded allomorphs therefore has one value on every successful path.
A predicate that is false for that value on every successful path can drop the candidate without
losing a parse. Maxwell's "cannot be removed later" is the special case where analysis can compute
that value before the root is known.

### Excluded co-occurrence

`MorphCoOccurrenceRule.IsWordValid` (`MorphCoOccurrenceRule.cs:82-87`) rejects an `Exclude` rule
when every listed other co-occurs with the key (`:92-170`). Under `Anywhere` that is set membership,
and adding morphs never undoes it, so it is anti-monotone in the Apriori sense. There is one trap the
monotonicity argument misses. The "others" are read from `Word.AllomorphsInMorphOrder` (`Word.cs:127`),
which is built from morph annotations. `MarkMorph` (`Word.cs:274-296`) creates no annotation for a
zero-width allomorph. A null affix on the trail is therefore invisible to the rule in synthesis. An
early check may count an "other" only when its recorded allomorph always leaves a morph annotation.
`SomewhereToLeft/Right` need surface linear order, which the trail gives only for simple prefixes
and suffixes, so they stay late. `Require` and `Adjacent*` are not monotone and stay late. The key is
always checked, because `Morpher.IsWordValid` iterates `word.Allomorphs` (`Morpher.cs:661`), which
includes zero-width allomorphs.

Benefit: zero on the samples. In a grammar that uses co-prohibitions it is the only candidate here
that prunes analysis work as well as synthesis. Experiment E4.

### DisjunctiveAllomorph

Most of this check already runs early. An environment-free earlier allomorph with no required
syntactic FS blocks later ones at rule-application time, through the `break` at
`SynthesisAffixProcessRule.cs:234-241`. The test left in `Allomorph.IsWordValid` (`Allomorph.cs:127-151`)
covers earlier allomorphs whose environment holds on the final surface. That needs the morph's
surface position, which analysis does not have. Where the disjunction is conditioned by MPR features
(Romance conjugation classes, for example), the MPR replay in E2 covers it. No separate experiment.
E1 shortens the same loop.

### Environments

Environments are checked on the **final** synthesized word, after every stratum's phonology
(`Morpher.cs:358` runs `IsWordValid` on `_synthesisRule.Apply(...)`, and `Allomorph.cs:110-125` checks
the morph's neighbours there). For any success, `IsMatch` (`CharacterDefinitionTable.cs:274-282`)
has accepted that shape against the input word. A1's worry, that later rules could change the
context, therefore does not apply here, because no rule runs after the check. Maxwell's prefilter is
sound only under two conditions that the code imposes:

1. **Boundaries.** The environment matchers accept `Boundary` and `Anchor` annotations
   (`AllomorphEnvironment.cs:38-41`, `:55-58`), which the input shape lacks. A boundary node in a
   context must match the empty string in the prefilter.
2. **Underspecified surface segments.** `IsMatch` compiles the synthesized shape to a regex, so an
   underspecified node (an archiphoneme such as N that no rule fills in) can match an input character
   whose features conflict with the context. For example, a [+cons] node unifies with a [+voice]
   context and also matches input `t`. Run against the input, the prefilter would then reject a valid
   parse. The prefilter is sound only when every surface node is fully specified. Featureless
   grammars meet that by `StrRep` identity. Grammars without underspecified segments in lexical
   entries, inserted affix material or rule outputs also meet it. Otherwise the prefilter must stay
   off.

Excluded environments cannot be prefiltered this way, as Maxwell said. Benefit: on the samples, 23
terminal branches across all corpora. The real case is a zero allomorph with an environment: analysis
can unapply it anywhere in any word, and today only `IsWordValid` disposes of it. Experiment E5.

### ExcludedMprFeatures

On a successful path, MPR features evolve deterministically along the trail. They start from the
root's set (`Word.cs:152-153`), and each applied allomorph calls `AddOutput` (`MprFeatureSet.cs:29-44`;
`SynthesisAffixProcessAllomorphRuleSpec.cs:209`). This is exact except where something outside the
trail touches the set: realizational rules (not on the trail, `Word.cs:340`), compounding
(`SynthesisCompoundingRule.cs:178-179`), `ChooseInflectionalStem` swapping in a relative's entry
(`SynthesisAffixTemplatesRule.cs:83-115`), and `Overwrite` groups (which remove features, so a
feature can be lost). Excluded MPR features can therefore be checked at H4, but not before lookup,
because the root contributes MPR features. Required MPR features can be checked at the same point,
which matters more: required MPR features are how FLEx inflection classes gate affix allomorphs.
Benefit on the samples: no terminal failure is attributed to either reason. Required failures may be
hidden inside the PartialParse buckets, and E2's shadow census measures that. Experiment E2.

## Literature that bears on this

The full survey is in this round's working notes, and only the parts that change a decision are kept
here. Unless marked, the sources were checked at the abstract level only.

- **Anti-monotone pruning** (Agrawal & Srikant 1994, Apriori). A set that contains a forbidden subset
  is forbidden. This justifies E4 and the excluded half of E2. It does not apply to required
  properties, which are monotone in the other direction.
- **Nogoods and forward checking in CSP search** (Dechter; Schiex & Verfaillie 1993, cited
  second-hand). A failed partial assignment is a nogood. Ledger row 14 already found 0 subsumable
  nogood pairs, so nogood *learning* is closed. Forward checking ("can this still be satisfied?") is
  the model for E2 and E3. Both ask whether a trail can still succeed once the root is known.
- **Constraint automata intersected with the input** (Koskenniemi 1990; Bird & Ellison 1994; Kaplan &
  Kay 1994). This is the principled form of Maxwell's environment check, and E5 is its cheapest
  instance: a greedy left-then-right scan, linear in word length for each context. Baeza-Yates (1991)
  subsequence automata pay off only when one word is tested against many contexts.
- **Maxwell (1994), "Parsing Using Linearly Ordered Phonological Rules"** (arXiv cmp-lg/9411015, full
  text not read). It is HermitCrab's own ancestor: generate-and-test with ambiguity encoded into the
  form being parsed. Reading where it rejects candidates is open.
- **Billot & Lang (1989)** warn that look-ahead added to chart parsing can cost more than it saves.
  This is why every experiment here has a kill criterion in steps, including the cost of the check.

No published source was found on tracking morph spans through rule inversion. That is what a
position-aware environment check would need.

## Ranked experiments

Ranked by soundness plus benefit across the grammar space, with cost as the tiebreak. "Ceiling" is
the census share that the change could at most remove. It is not a prediction. Every experiment is
measured in steps: synthesis steps, analysis events and candidates reaching synthesis, each reported
as "N% less work" per corpus, unpooled. Every experiment's parses must be identical, per word, on the
conformance suite, the five real grammars and its stress fixture.

### E1. Stop the synthesis allomorph loop after the recorded allomorph

- **Hypothesis.** `SynthesisAffixProcessRule.Apply` keeps applying allomorphs after the one analysis
  recorded. Each of those applications is a full pattern match and output shape, and its result is
  thrown away at `:199` (`AllomorphNotApplicable`). Breaking the loop once `i` passes the recorded
  index removes them, along with their MPR checks.
- **Soundness.** An output from allomorph `i > r` fails `IsAllomorphApplicable` and is never added.
  `appliedAllomorphIndices` (`:138`, `:196-197`) is copied into the outputs at the moment each is
  built (`Word.MorphologicalRuleApplied`, `Word.cs:374-375`, `UnionWith`). Indices added after `r`
  therefore never reach `r`'s output. When the recorded allomorph is null, nothing changes. Earlier
  allomorphs (`i < r`) are untouched, because their application decides the disjunctive `break` and
  the recorded indices.
- **Hook.** `MorphologicalRules/SynthesisAffixProcessRule.cs:140-247`. It needs a `Word` accessor for
  the recorded allomorph at the current trail position (`Word.cs:254-261`).
- **Language types, and blow-up.** Any affix with an ordered allomorph list. Examples are Turkic
  -DI/-lAr sets written as 4-8 environment-conditioned allomorphs (environment-bearing allomorphs
  never `break`, so today every one is applied), Bantu class prefixes, and IE case endings by
  declension class. Late: each rule application costs `A` allomorph applications. Early: `r + 1`.
  The waste is linear, up to a factor of `A`, for every synthesis of every candidate, including the
  successful ones.
- **Stress fixture.** New synthetic fixture `stress-allomorph-list-density`. It has a harmony-style
  suffix grammar with four suffix slots, each with 8 environment-conditioned allomorphs, and with the
  recorded allomorph spread across positions in the lists. `languages/suffixing-vowel-harmony` and
  `edge-cases/disjunctive-recheck` are the soundness controls.
- **Expected savings.** The ceiling is the `AllomorphNotApplicable` events with `i > r` plus the
  `i > r` part of `RequiredMprFeatures`. `AllomorphNotApplicable` is 11.6% of real-grammar synthesis
  steps (Mbugwe 12.3%, Sena 11.4%, Aweti 2.6%) and 1.0% of conformance steps. The census did not split
  by `i > r`. On the stress fixture the ceiling approaches (A-1)/A of allomorph applications.
- **Cost.** Very low: about 10 lines plus a `Word` accessor.
- **Kill.** Less than 2% fewer synthesis steps on every real grammar *and* less than 20% on the
  stress fixture, or any parse difference.

### E2. Replay MPR features and stem names along the trail at lexical lookup

- **Hypothesis.** Once the root is known (H4), the MPR set that each trail entry will see on a
  successful path can be computed without shapes. A candidate whose recorded allomorph fails its
  required MPR features (or matches its excluded ones), or whose rule's `RequiredStemName` differs
  from the root's, can be dropped before synthesis.
- **Soundness.** Replay in synthesis order (trail from last to first). Start from the root's
  `MprFeatures` and add each recorded allomorph's `OutMprFeatures`. On a successful path this gives
  the set the synthesis check sees (`SynthesisAffixProcessRule.cs:144-177`), except for contributions
  the replay can only bound. The required test runs against an upper bound: that set plus every
  realizational and compounding output, with `Overwrite` removals ignored. The excluded test runs
  against a lower bound, and only when the grammar has no `Overwrite` group and the trail has no
  compounding entry. If the root's entry has a `Family`, the prune stays off, because
  `ChooseInflectionalStem` replaces the root. With a null recorded allomorph, the candidate is dropped only if every allomorph
  of the rule fails. Stem names: `SynthesisAffixProcessRule.cs:107` compares only with
  `input.RootAllomorph.StemName`, which is known exactly at H4.
- **Hook.** H4, per alternative (`Morpher.cs:353-358` and `:386-395`), after the `MaxAlternatives`
  count so that the exception behaviour does not change. Both paths call it.
- **Language types, and blow-up.** IE declension and conjugation classes (Latin and Russian
  declensions; Romance -ar/-er/-ir conjugations), Bantu noun class written as MPR features, and
  Australian conjugation classes. With `c` inflection classes and endings that are homophonous
  across classes, each ending's analysis meets roots of every class. Late, each wrong-class root
  costs a synthesis up to the gated rule, plus every template path. Early, it costs a set replay of
  length |trail|. With agreement prefixes gated in `k` slots, late synthesis grows as `c^k`
  candidates. The early check is still `c^k` replays, but each one is trivially cheap.
- **Stress fixture.** New synthetic `stress-mpr-inflection-classes`: five declension classes as MPR
  features on 20 roots, case and number endings gated by required MPR features, deliberate homophony
  across classes, one excluded-MPR exception and one `Overwrite` group (which must disable the
  prune). Soundness controls: `edge-cases/mpr-overwrite-order-dependence`,
  `edge-cases/mpr-group-overwrite-without-realizational`, `edge-cases/mpr-gated-exception`, and
  `languages/fusional-realizational-morphology`.
- **Expected savings.** On the samples the floor is near 0: no terminal failure has an MPR reason.
  The ceiling is the MPR-caused part of PartialParse (48.4% of real-grammar steps across all its
  causes). That part is unknown, and phase 1 (a shadow census) measures it. Mbugwe and Aweti are the
  only samples with gated allomorphs (9 and 6 required, 41 and 26 excluded). On the stress fixture,
  expect most of the wrong-class syntheses, roughly (c-1)/c of candidates.
- **Cost.** Low to medium: one replay function and a `Morpher` switch defaulting on, following PR
  #519's `PruneDisagreeingCopies`.
- **Kill.** The shadow census decides less than 3% of synthesis steps on every real grammar *and*
  less than 30% on the stress fixture. A shadow run in which any successful candidate fails the
  predicate is a soundness bug: stop and fix it, do not tune it.

### E3. Replay the template and final-state outcome at lexical lookup (after a dead-candidate census)

- **Hypothesis.** Most synthesis steps on the template-heavy samples go to candidates that die on
  shape-free conditions. The conditions are an applicable template whose required slot no pending
  rule fills (`SynthesisAffixTemplatesRule.cs:57-63`), a non-final template applied last
  (`SynthesisStratumRule.cs:70-74`), rules left in a stratum (`:75-87`), and trail rules never
  consumed (`Morpher.cs:639`). An abstract replay at H4 can follow the trail pointer, template
  finality, `IsPartial` and the syntactic FS while assuming every pattern match succeeds. That
  replay over-approximates success, so it can prove a candidate dead.
- **Soundness.** Abstract interpretation of `SynthesisStratumRule.Apply` and
  `SynthesisAffixTemplatesRule.Apply`. Assuming every allomorph pattern matches only adds successful
  paths, and so does treating each realizational rule as both applied and not applied. If the
  abstract replay finds no path, the concrete synthesis finds none either. Template applicability
  (`:37`) needs the syntactic FS at that point. That FS is exact on successful paths: the root entry's
  FS (`Word.cs:151`) is unified with each `Required` and then `PriorityUnion`-ed with each `Out`
  (`SynthesisAffixProcessRule.cs:123`, `:182-183`). It is not exact when realizational FS output or
  `ChooseInflectionalStem` intervene, and the replay must bail out there.
- **Phase 0 (must come first).** A candidate-level census: synthesis steps in candidates (root
  allomorph lookups) with zero successes, split by the first shape-free reason the replay would
  give. This settles caveats 5 and 6 above.
- **Hook.** H4, as in E2. E3 extends E2's replay.
- **Language types, and blow-up.** Every grammar with affix templates and required slots. FLEx
  templates are the normal way to write Bantu verbs, Athabaskan and Algonquian position classes,
  Mayan and Quechuan inflection. Late, each dead candidate pays lookup, `ExpandAlternatives` and
  synthesis of every stratum below the failing one, plus each template path. Early, it pays a replay
  of length |trail| times the number of templates per stratum. On Mbugwe, 19,734 candidates reached
  synthesis and 149 branches succeeded.
- **Stress fixture.** New synthetic `stress-required-template-slot`. It models a Bantu-like verb:
  one stratum, a template with a required TAM slot and optional subject and object slots, noun and
  verb roots with overlapping shapes, and words whose bare-stem and partial analyses find roots.
  `languages/prefixal-discontinuous-slot-dependency` and `edge-cases/final-template-partial-*` are
  the soundness controls. `edge-cases/deep-optional-affix-nesting` is the pathological control.
- **Expected savings.** The branch-attributed ceiling is PartialParse at 48.4% of real-grammar
  synthesis steps plus part of the 22.1% unattributed share. Mbugwe has 46.3% plus 22.8%, Aweti 58.9%
  (MorphologicalRulesPending plus NonFinalTemplateAppliedLast) and Amharic 60.0%. Ledger row 19 is
  the counterweight: shape-free die points were 0.0-6.0% of cost on heavy Sena probes and 0.4% on 28
  Amharic words. Sena and Amharic are therefore expected low and Mbugwe and Aweti decide.
- **Cost.** Medium to high. It mirrors two synthesis classes' control flow for Linear and Unordered
  strata. It is the riskiest experiment for soundness, and the one with the largest measured target.
- **Kill.** Phase 0 shows that shape-free reasons decide less than 10% of dead-candidate synthesis
  steps on both Mbugwe and Aweti *and* less than 30% on the stress fixture. Or any successful
  candidate fails the abstract replay.

### E4. Excluded co-occurrence during analysis (Maxwell)

- **Hypothesis.** An `Exclude` + `Anywhere` morpheme (or allomorph) co-occurrence rule whose key and
  others are all on an analysis word's trail rejects that word and everything analysis derives from
  it. Checking it at H1 prunes the analysis subtree as well as the lookups and syntheses.
- **Soundness.** As argued above (anti-monotone under `Anywhere`), with three restrictions. An
  "other" counts only if its recorded allomorph always leaves a morph annotation. A rule whose key or
  others include a root morpheme is checked at H4, not H1. Allomorph rules count only entries whose
  allomorph is recorded (not null). `Somewhere*`, `Adjacent*` and `Require` stay late.
- **Hook.** `MorphologicalRules/AnalysisAffixProcessRule.cs:92` (after `MorphologicalRuleUnapplied`,
  before `output.Add` at `:96`). Root-involving rules use H4.
- **Language types, and blow-up.** Polysynthetic prefix incompatibilities (Athabaskan, Iroquoian),
  Bantu negation against tense, Quechua and Aymara suffix incompatibilities, and any FLEx project
  that uses ad hoc co-prohibitions to suppress spurious parses. Take `k` optional slots with `m`
  fillers each. Late, every analysis that holds a forbidden pair is expanded below that point, looked
  up and synthesized. Early, the subtree is cut where the pair completes. The worst case is
  `m^(k-d)` analysis nodes saved per cut at depth `d`, so it is exponential in the slots that remain.
- **Stress fixture.** New synthetic `stress-excluded-cooccurrence`: six optional prefix slots with
  four fillers each, about ten `Exclude`/`Anywhere` rules, and a zero-width affix as an "other" in one
  rule. That word must still parse. `languages/suffixing-evidential-adjacency-chain`,
  `languages/templatic-root-modification` and `edge-cases/morphotactic-attribute-breadth` are the
  soundness controls (they hold 3, 1 and 8 exclude rules).
- **Expected savings.** Samples: about 0. Conformance: 2 `MorphemeCoOccurrenceRules/Exclude` and 4
  `AllomorphCoOccurrenceRules/Exclude` branches, 14 steps (0.2%). The stress fixture should show a
  reduction in analysis events, which no other experiment here can produce.
- **Cost.** Low. Rule lists can be indexed per morpheme at `Morpher` construction.
- **Kill.** Less than 25% fewer analysis events plus synthesis steps on the stress fixture, or any
  parse difference. A zero effect on the samples is expected and is not a kill.

### E5. Required-environment prefilter on the input word (Maxwell)

- **Hypothesis.** For each allomorph whose environments are all `Require`, compute once per input
  word whether any of them can match anywhere: the left context ending at some position `p`, and the
  right context starting at some `q >= p`. Allomorphs that cannot match are skipped during analysis.
- **Soundness.** Environments are checked on the final shape, and `IsMatch` ties that shape to the
  input, as argued above. Three restrictions apply. Boundary nodes match empty. The prefilter is
  enabled only when the grammar cannot produce underspecified surface segments (a static scan of
  lexical entries, inserted affix material and rule outputs; featureless grammars qualify). An
  allomorph with any `Exclude` environment is never skipped. Skipping analysis allomorph `i` is sound
  only because PR #548 restricts synthesis to the recorded allomorph.
- **Hook.** Compute the set per word in `Morpher.ParseWord` (`Morpher.cs:167-179`). Consult it in the
  allomorph loop of `AnalysisAffixProcessRule.Apply` (`:61-102`), skipping `_rules[i]`.
- **Language types, and blow-up.** Environment-conditioned allomorphy is everywhere: Turkic and
  Uralic harmony, Bantu, Korean, Austronesian nasal substitution. It matters most for **zero
  allomorphs with environments**, which analysis can unapply in any word. With `z` optional
  zero-allomorph affixes, late checking multiplies the analysis tree by up to `2^z` before
  `IsWordValid` disposes of the extras. The prefilter removes each one whose context is absent from
  the whole word. It is weak when contexts are common, because it does not know positions.
- **Stress fixture.** New synthetic `stress-env-zero-allomorphs`: three affixes, each with an overt
  allomorph and a zero allomorph conditioned by an environment (for example ∅ / _ [+nasal]), with
  words that mostly lack the conditioning contexts. Controls: `edge-cases/discontinuous-morph-environment`,
  `edge-cases/diacritic-segments` and `languages/suffixing-vowel-harmony`. Sena (72 required-environment
  sets) is the real grammar most likely to move.
- **Expected savings.** Samples: `Environments/Require` has 98 real steps (0.0%) and 32 conformance
  steps (0.4%). Pruning at H1 also removes analysis events, which the terminal counts do not show.
  The stress fixture is the real test.
- **Cost.** Low to medium. The left matcher runs right to left from each position, with the existing
  compiled patterns reused on the input shape.
- **Kill.** Less than 20% fewer analysis events plus synthesis steps on the stress fixture, or the
  grammar condition disables it on four of the five real grammars and on most conformance languages.
  Any parse difference is a soundness bug.

### E6. Intermediate-shape check for SurfaceFormMismatch (research only, no brief)

`SurfaceFormMismatch` is the largest terminal reason that no experiment above touches: 25.6% of
real-grammar steps, 37.9% on Indonesian and 27.2% on Mbugwe. It fails last, after a full synthesis. A
check at each stratum boundary that the synthesized shape still matches the analysis shape the
candidate came from could stop a candidate early. There is no soundness argument yet. Analysis
shapes are over-approximations with optional and underspecified nodes, and ledger row 8 (a
synthesis-side length bound) lost valid parses. Run a census only: at each boundary, record whether
the check would fire, and whether any successful candidate ever fails it. **Kill** if any success
fails it on any corpus.

### Not an experiment

- **Cheaper `RequiredMprFeatures` rejection** as such: each rejection is one set lookup (caveat 2),
  and ledger row 7 is the precedent. E1 removes the ones past the recorded allomorph as a side effect.
- **Nogood learning over candidates**: ledger row 14 found 0 subsumable pairs.
- **Moving constraints into the analysis merge key**: ledger row 15. Every synthesis rejection depends
  on the root, so H4, not the analysis key, is the place.

## Results of the round

Five experiments were built, each on its own unpushed branch from `origin/master` `c2454760`, with the
harness commit `af97348b`. Every result is steps only. "Synth" is synthesis steps, "total" is
analysis events plus synthesis steps for the same words in the same arm pair, and "% less work" is
the relative drop. Parses were identical per word in every arm pair on every corpus, except that the
four `truncate-morphotactic*` conformance fixtures are nondeterministic on master (see Open problems).

### Decisions

| Experiment | Branch @ SHA | Decision |
|---|---|---|
| E1 allomorph-loop cutoff | `perf/hc-allomorph-loop-cutoff` @ `d21af37e` | **KEEP.** Candidate for its own PR |
| E2 MPR and stem-name trail replay | `perf/hc-trail-mpr-replay` @ `4fbbfb14` (product `84f76cdf`) | **KEEP.** Candidate PR, to combine with E3: both use the same post-lookup hook |
| E3 template replay | `perf/hc-template-replay` @ `6edc4375` | **KEEP, without memoisation.** See E3 |
| E4 excluded co-occurrence | `perf/hc-excluded-cooccurrence` @ `02873608` (product `b07fc4d6`) | **PARK** |
| E5 required-environment prefilter | `perf/hc-env-input-prefilter` @ `9d55dade` (product `1af79e20`) | **DEAD END** as specified |
| E6 intermediate-shape check | none | Not run (research only) |

None of the branches is pushed. A branch name and SHA is the citation.

### Synthesis share of total work

Synthesis steps and analysis events for the identical words, switch off:

| Corpus | Synth | Analysis events | Synth share of total |
|---|---:|---:|---:|
| Aweti stride 2 | 15,940 | 1,544,240 | 1.0% |
| Mbugwe stride 40 (E2-E4 word set) | 130,647 | 1,318,654 | 9.0% |
| Sena stride 100 | 1,908 | 103,277 | 1.8% |
| Amharic limit 28 | 1,277 | 7,403 | 14.7% |
| Indonesian | 677 | 6,552 | 9.4% |
| Conformance | 8,246 | 27,765 | 22.9% |

The E1 run used a Mbugwe word set with 3,451,016 analysis events (a different drop set), which is
why its Mbugwe row differs below. Analysis events never changed in E1, E2, E3 and E5-off arms.

### E1. Allomorph-loop cutoff: KEEP

`SynthesisAffixProcessRule.Apply` now stops after the recorded allomorph (`Morpher.CutAllomorphLoopAtRecorded`,
default on). Kill criterion not met. The soundness argument held: no parse changed.

| Corpus | Synth off to on | % less synth | % less total work |
|---|---|---:|---:|
| Mbugwe stride 40 | 151,505 to 138,737 | 8.43% | 0.35% |
| Sena stride 100 | 1,908 to 1,772 | 7.13% | 0.13% |
| Aweti stride 2 | 15,940 to 15,831 | 0.68% | 0.007% |
| Amharic limit 28 | 1,277 to 1,271 | 0.47% | 0.07% |
| Conformance (minus 4 flaky) | 8,150 to 8,109 | 0.50% | 0.12% |
| Indonesian | 677 to 677 | 0% | 0% |
| `stress-allomorph-list-density` (Turkic-style) | 566 to 337 | 40.46% | 8.45% |
| `stress-prefix-class-density` (Bantu-style) | 341 to 190 | 44.28% | 9.49% |

Cost not in the counters: one `IndexOf` over the rule's allomorphs per rule application. Unaffected:
analysis events and candidates (equal in both arms).

### E2. MPR and stem-name trail replay: KEEP

`TrailMprReplay` and `Morpher.PruneByTrailReplay` (default on) drop a candidate at lexical lookup
when its trail cannot satisfy required or excluded MPR features or the stem name. Two refinements to
the brief's predicate: the excluded test is disabled only when an `Overwrite` group is reachable from
a rule output, and the stem-name check stops after a compounding entry. Family roots are never
pruned. The shadow census agreed exactly with the on arm and found no successful terminal under a
rejected candidate anywhere. Kill criterion not met.

| Corpus | Synth off to on | % less synth | % less total work | Replay checks |
|---|---|---:|---:|---:|
| Aweti stride 2 | 15,940 to 6,873 | 56.9% | 0.58% | 14,064 |
| Mbugwe stride 40 | 130,647 to 115,747 | 11.4% | 1.03% | 26,299 |
| Conformance | 8,246 to 8,218 | 0.3% | 0.08% | 251 |
| Sena, Amharic, Indonesian | unchanged | 0% | 0% | 0 |
| `stress-mpr-inflection-classes` (IE declensions) | 494 to 278 | 43.7% | 6.45% | 180 |
| `stress-mpr-bantu-concord` (three gated slots) | 3,376 to 382 | 88.7% | 31.85% | 652 |

Replay checks are set operations, a different unit from synthesis steps (a step is a pattern match),
so they are reported beside the savings and not netted. The Aweti and Mbugwe gain comes from required
and excluded gates (Mbugwe 41 excluded, Aweti 26 excluded).

### E3. Template replay: KEEP, without memoisation

`TemplateReplay` and `Morpher.PruneByTemplateReplay` (default on) walk the stratum, template and rule
control flow over abstract states (trail pointer, partial, final), assuming every pattern match
succeeds. It bails out on family roots, non-affix-process rules on the trail, any realizational rule
in the grammar and a null stratum. The shadow census found no successful terminal under a rejected
candidate. Phase 0 census (steps in candidates with no success and shape-free): Mbugwe 40.0%, Aweti
73.4%, so the kill rule for the census was not met.

| Corpus | Synth off to on | % less synth | % less total work | Replay states |
|---|---|---:|---:|---:|
| Aweti stride 2 | 15,940 to 5,112 | 67.9% | 0.69% | 22,308 |
| Mbugwe stride 40 | 130,647 to 114,833 | 12.1% | 1.09% | 133,606 |
| Amharic limit 28 | 1,277 to 532 | 58.3% | 8.58% | 718 |
| Sena stride 100 | 1,908 to 1,790 | 6.2% | 0.11% | 4,166 |
| Conformance | 8,246 to 8,181 | 0.8% | 0.18% | 10,622 |
| Indonesian | 677 to 667 | 1.5% | 0.14% | 389 |
| `stress-derivation-chain-homophone-roots` (Turkic-like) | 300 to 176 | 41.3% | unmeasured | 948 |
| `stress-bantu-extension-required-slot` | 279 to 225 | 19.4% | unmeasured | 763 |
| `stress-required-template-slot` (control) | 155 to 155 | 0% | 0% | 329 |

The three stress rows do not give analysis event counts in the result files, so their total-work
share is unmeasured. The control has no gain because dead candidates there cost 0 steps.

The brief's blow-up kill rule says to kill if walk states exceed steps saved, and it fired on every
corpus (Mbugwe 133,606 states against 15,814 steps saved; Aweti 22,308 against 10,828). The result
file therefore said PARK. The owner overrode the literal rule: a replay state is an integer set
operation and a saved step is a pattern match, so the two are different units and cannot be compared
by count. **Decision: KEEP, without memoisation.** Memoising verdicts by root entry and trail is
ruled out: the owner will not accept a memory footprint above a few MB, and earlier memo rows (18, 28,
29) show how such tables grow. Next step: cost the replay walk in units comparable to a step, then cut
the walk itself without a cache, for example by skipping it when the grammar has no required slots or
by stopping at the first violated constraint.

### E4. Excluded co-occurrence during analysis: PARK

`ExcludedCoOccurrencePruner` indexes active `Exclude` + `Anywhere` morpheme and allomorph
co-occurrence rules at `Morpher` construction. Rules that name only affixes are checked in analysis,
right after unapplying (H1). Rules that name a root are checked per alternative after lookup (H4).
It is sound and it is the only experiment that cuts analysis events.

| Corpus | Analysis events off to on | Synth off to on | % less total work |
|---|---|---|---:|
| Aweti, Mbugwe, Sena, Amharic, Indonesian | unchanged | unchanged | 0% |
| Conformance (1,492 words) | 27,765 to 27,705 | 8,246 to 8,223 | 0.23% |
| `stress-excluded-cooccurrence` (brief's fixture) | 4,690 to 4,605 | 326 to 250 | 3.2% |
| `stress-athabaskan-prefix-complex` | 8,937 to 6,347 | 891 to 404 | 31.3% |
| `stress-bantu-extension-chain` | 10,552 to 8,548 | 515 to 332 | 19.8% |

The brief's kill criterion (under 25% on its own fixture) fires on 3.2%. The two dense fixtures were
added at the owner's request. **Decision: PARK.** Land it only as a cheap safety net for
co-occurrence-heavy grammars. Findings that affect any landing: the "zero-width other is invisible"
trap does not occur for affix-process allomorphs (synthesis marks the morph on the last output node);
merged analyses must be filtered per alternative, not on the canonical word; with pruning on,
`MaxAlternatives` counts fewer candidates, so its exception can stop firing; and the rule index is
read at construction, so a rule added later is not pruned early.

### E5. Required-environment input prefilter: DEAD END as specified

The code is recoverable at `1af79e20`. The sound static scan disables the prefilter on 5 of 5 real
grammars (four with ambiguous digraph segmentation, one with non-uniform feature sets) and on 145 of
150 conformance runs, so it gives 0% on every real grammar. Forced on, with the scan bypassed (an
unsound licence), Sena saves 4.0% of synthesis steps and 7.3% of analysis events (net 5.7% after
counting matcher invocations) and Aweti is net -0.4%.

| Fixture | Synth | Analysis | Gross % less total | Net % less work (matcher calls counted) |
|---|---|---|---:|---:|
| `stress-env-zero-allomorphs` | 87.4% less | 74.1% less | not given | 65.2% |
| `stress-env-bantu-concord` | 1,344 to 732 | 2,036 to 1,207 | 42.6% | -3.8% |
| `stress-env-austronesian-nasal-prefix` | 936 to 883 | 2,206 to 1,471 | 25.1% | -37.4% |

Forced on, it is net negative on two of the three fixtures. Soundness findings: the brief's
counterexample direction (an archiphoneme `[+cons]` against a `[+voice]` context) does not hold under
subsumption. The real unsound case is a synthesized node more specific than the input character
definition (test word "atpa": `D` against `t`); forcing the prefilter on loses that parse, and the
static scan is what prevents it.

**What would have to change to revive it.** (1) A scan that stops rejecting digraph grammars, which
needs a segmentation of the input that is unambiguous per grammar, or a prefilter that tests all
segmentations. (2) A treatment of non-uniform feature sets and of synthesized nodes more specific
than the input segment definitions. (3) A prefilter cost small enough that matcher invocations do not
cancel the saving, since two of three fixtures go net negative. (4) Optionally, root allomorph
environments (67 in Sena) prefiltered at lookup, which was not built. Absent (1) and (2) the
eligible set on real grammars is empty (indonesian 0 of 13, aweti 47 of 394, amharic 0 of 93, mbugwe
0 of 415, sena 5 of 117).

### E6. Intermediate-shape check: not run

No soundness argument yet; `SurfaceFormMismatch` (25.6% of real synthesis steps) is still untouched.

### Conclusion

Each of E1-E3 is sound on every corpus and cuts synthesis steps by a large amount on grammars that
lean on the feature (inflection classes, ordered allomorph lists, template slots). But synthesis is
1.0% of total work on Aweti and 9.0% on the Mbugwe sample, so on real grammars each removes about 1% or less of
total work (E2: 0.58% Aweti, 1.03% Mbugwe; E3: 0.69% and 1.09%; E1: 0.007% and 0.35%). E2 and E3 overlap
and were not measured together. **The dominant cost is analysis-side.** The next round should
target analysis, for example by extending E4-style monotone checks earlier, and the
`ApplicableTemplatesNotApplied` analysis paths. E4 shows an analysis check can cut work, but only
where the grammar has the constraint; the question is which constraints real grammars have.

### Open problems found at baseline

1. The four `truncate-morphotactic*` conformance fixtures (`truncate-morphotactic`, `-copy-u`,
   `-identity`, `-subsumption`) are nondeterministic on master: their output varies run to run with no
   change. They fail in both arms of every experiment, and `EveryConformanceFixturePassesSelfCheck`
   fails on them.
2. `FieldworksProducibilityLedgerTests.CoversExactlyEveryFailureReasonAndEveryInterfaceAttributeInThisRepo`
   fails on master because PR #548 added `AllomorphNotApplicable` without updating that ledger.
3. The stress fixtures live under `tests/SIL.Machine.Morphology.HermitCrab.Tests/Research/stress-fixtures/`,
   not under `conformance/`, because registering a fixture needs ledger and coverage regeneration
   (a full test cycle takes 34 to 53 minutes under load). E5's branch registered three fixtures and
   broke 8 pinned ledger tests (row totals, fixture count, layer counts, interaction and inventory
   ledgers, real-corpus pair counts); E2's registration attempt broke 7. Moving the fixtures is a
   ledger-regeneration job.
4. Full-suite evidence: E3's run finished at 215 of 217 passing; the two failures are the truncate
   flake and a SIGTERM under load. E4's run was 723 of 725, with the truncate flake and the ledger
   failure of item 2. `local_check.sh --agent-strict` could not finish on these machines because the
   SentencePiece step needs `cmake`; comment hygiene and formatting passed.

## Open questions

- The candidate-level dead-step census (E3 phase 0) was run: shape-free dead steps are 73.4% on Aweti
  and 40.0% on Mbugwe. What is left is the cost of the replay in comparable units, cut without a memo (E3).
- Which analysis-side constraints real grammars carry, and whether they can be checked before a
  candidate is expanded. This is the next round's target.
- Whether synthesis phonological deletion removes morph annotations. E4's zero-width restriction may
  need to cover deleted morphs too. The E4 brief requires a test.
- Whether `ChooseInflectionalStem` families occur in FLEx exports at all. None of the five samples has
  one. E2 and E3 bail out on them.
