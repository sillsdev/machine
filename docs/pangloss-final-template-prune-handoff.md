# Handoff: implement the final-template interleaving prune (Machine PR #491) in PanGloss

**Written:** 2026-09-03. **Status:** research complete, nothing implemented in PanGloss. Word forms and glosses are replaced by word-list indices and rule ids (private grammars; see `grammar-privacy-constraint`).
**Supersedes:** section 4 ("PR #491 final-template prune: conditional follow-up") of
`TEMP-PANGLOSS-HC-RUST-SPEEDUPS.md` on Machine branch `perf/hc-optimization-pr`. Its Phase A census is done
(this document is the result); go straight to implementation, with the design changes below.

## 0. One-paragraph brief

HermitCrab analysis explores every interleaving of affix-process rules and affix templates. Synthesis forbids a
non-partial, non-template rule from applying after a *final* template, so every analysis path that unapplies a
non-template rule and *then* a final template is dead unless a partial rule sits inside it. PR #491 (sillsdev/machine,
author jtmaxwell3, head `96c97f76`, open, unreviewed) prunes those paths but disables itself whenever the grammar has
any partial morpheme, which every FLEx export does. With the guard lifted, the prune is **8–33x on Mbugwe and Sena
with identical analysis sets** (Mbugwe 22 words 119 s → 12 s; Sena word-list index 74 84 s → 0.3 s). That is an order of
magnitude larger than the entire retained Machine perf package (1.8x), and it composes with it. The PanGloss job is to
port the mechanism with (a) a provably tighter guard, (b) the whole template battery hoisted on pruned states, (c) the
three defects found in #491 avoided, and (d) an explicit, separately-tested `always_enforce_final_templates` option.

## 1. Sources and pins

| What | Where |
| --- | --- |
| PR #491 head | `sillsdev/machine` `filter-final-templates-in-analysis` @ `96c97f76` (11 commits over base `5d26fac6`, +303/−8 in `src/`) |
| PR #491 scratch worktree with counters + harness | `C:\Users\johnm\Documents\repos\machine\.worktrees\pr491` (branch `pr-491`; uncommitted: `PruneCounters.cs`, counter hooks in 5 rule files, `tests/.../FinalTemplateUpperBound.cs`) |
| Machine re-review record | `C:\Users\johnm\Documents\repos\machine\.worktrees\ledger\docs\pr491-final-template-prune-upper-bound.md` (uncommitted, branch `perf/hc-optimization-pr` @ `179708d2`) |
| Earlier Machine rejection (now superseded in part) | `.worktrees\ledger\docs\rejected-optimizations\final-template-prune.md` (+ addendum) |
| Draft PR comment (not posted) | `docs/pr491-review-draft.md` |
| Raw run logs (session scratchpad only; contain word forms, never commit) | `ft-mbugwe.log`, `ft-mbugwe3.log`, `ft-mbugwe-memooff.log`, `ft-sena3.log`, `ft-amharic.log`, `ft-amharic3.log` |
| PanGloss inspected at | `C:\Users\johnm\Documents\repos\PanGloss` @ `d2717652` |
| Grammars (private, never commit) | Sena `machine\samples\data\sena-hc.xml` + `sena-words.txt`; Mbugwe `machine\Mbugwe.xml` + `PanGloss\samples\data\mbugwe-words.txt`; Amharic `machine\.worktrees\p4-dedup-morphs\samples\data\amharic-hc.xml` + `amharic-words.txt` |

Word-list gotcha: `sena-words.txt` and `amharic-words.txt` contain lines that are not words in the grammar's
character table (`n'nyumba`, English glosses like `break`, `pfv`). Skip `InvalidShape` per word; do not abort.

## 2. The semantics, precisely

### 2.1 Synthesis-side rule (the thing analysis must mirror)

Machine `SynthesisAffixProcessRule.Apply` (PanGloss `pg-rules/src/morph.rs:1336-1343` and `:1448-1453`):

```
if !rule.is_template_rule && word.is_last_applied_rule_final == Some(true)
   && !word.is_partial && !rule.partial  -> reject (NonPartialRuleProhibitedAfterFinalTemplate)
```

Compounding has the same gate without the `rule.partial` clause (`morph.rs:2547`).
`word.is_partial` becomes true from a partial root (Word ctor) or any applied partial affix rule
(`morph.rs:1927-1932`). Templates: `stratum.rs:1590-1610` skips every template when the **root** is partial
(`root_partial`), and marks `is_last_applied_rule_final = is_partial || tmpl.is_final`.

Synthesis is a **replay** of the analysis's rule list, not a search (`Word::mrule_apps`, `IsMorphologicalRuleApplicable`
in C#; `guided_synth` in PanGloss). A candidate whose recorded rules cannot all be applied fails with `PartialParse`.

### 2.2 What analysis may prune

Analysis runs outer → inner. Unapplying non-template rule `R` (non-partial) and then final template `T` corresponds to
synthesis `... → T → R`, which is rejected unless the word after `T` is partial. The word after `T` is partial iff
(a) the root is partial, or (b) some rule applied before `R` (inner to `R`: `T`'s own slot rules, other rules in the
same stratum applied earlier, or any rule in a deeper stratum) is partial.

**Theorem (partial roots never rescue).** Case (a) cannot make the pruned path legal: with a partial root, synthesis
applies *no* template at all (`root_partial` gate), so the replay leaves `T`'s slot rules unapplied and the candidate
fails `PartialParse` (`HasRemainingRulesFromStratum` / PanGloss guided-synth residue check). Precondition: slot rules
are not also stratum-level mrules (they would then be applied by the mrule cascade). Verified on the exports:
overlap 0/113 Sena, 0/203 Mbugwe, 0/52 Amharic. Enforce it as a load-time assertion or fall back to the broad guard
for grammars that violate it.

**Corollary (sound guard).** Pruning at template `T` in stratum `k` is sound iff no **rule** with `partial == true`
exists in any stratum of depth ≤ depth(k) (deeper strata apply earlier in synthesis and stay inside). Lexical entries
never enter the guard. This is strictly stronger than #491's `Morpher.IsPartial` (which counts entries) and equal to it
on grammars whose partials are all rules.

**No finer sound engine-side condition was found.** Two attractive ideas are unsound:
- Deferring templates in a poisoned state until a partial rule appears loses parses when the template affixes and the
  rescue chain are on the same edge (outer affixes must unapply first; e.g. `root-P-Y-T-R` all suffixes).
- A "no partial rule's material can match the current shape" check is unsound because analysis unapplication
  *generalizes* segments (inverse of `ModifyFromInput`), so a partial rule that cannot match now may match later.

One exact, sound, but small extra filter exists: at lexical lookup, drop a candidate that is still poisoned and
unapplied no partial rule inside (synthesis would reject it anyway). It saves synthesis replay only, not search.
Amharic word 8: 16 of 36 synthesis affix applications were such rejections. Low priority.

### 2.3 Why the order-independent memo does not already absorb this

PanGloss's memo (`pg-memo`, `AnalysisStateKey` = shape + stratum + syn-FS + real-FS + non-head count + rule
multiset) collapses re-arrivals at the *same* state. The waste here is different: after each distinct cascade state
ending in a non-template rule (`{R1}`, `{R2}`, `{R1,R2}`, …) the **template battery** runs on that state
(`apply_mrules` → `apply_templates` → `run_template_batch`, `stratum.rs:769-830`). Each entry is a full slot-rule FST
sweep over every template. The battery's *outputs* land on states the legal (template-first) order also reaches, so the
continuation is shared, but the battery itself is keyed by its input state and is not. Counters: with the prune,
poisoned template entries fall from ~95% of all entries to 0, and Mbugwe memo hits fall 9,079 → 36 because the
redundant arrivals were exactly the pruned ones.

## 3. Measurements (Machine, PR #491 head + throwaway counters)

Two arms per word in one process: `AlwaysEnforceFinalTemplates=false` (= today's behavior on these grammars) and
`true` (= the prune with the guard lifted). Sequential + memo unless noted. `affixApply` = analysis affix-rule
`Apply` calls past the max-count/syn-FS gates; `tmplEntry` = analysis template `Apply` calls; `tmplPoisoned` =
entries in state `FinalTemplateAfterNonTemplate`.

| Grammar | Words | Wall ms off → on | affixApply off → on | tmplEntry off → on | Parity (gloss+allomorph+FS signature) |
| --- | ---: | ---: | ---: | ---: | --- |
| Mbugwe | 22 (idx 0,65,1272,1–19) | 119,059 → 12,066 | 7,801,493 → 442,237 | 190,372 → 28,908 | identical multisets 22/22 |
| Mbugwe, parallel, memo off | 6 (0,65,1272,4,11,14) | 25,694 → 3,189 | 1,153,594 → 171,594 | 11,271 → 11,220 | identical 6/6 |
| Sena | 42 (49,52,74,0–39) | 66,742 → 2,033 | 2,411,519 → 228,865 | 139,283 → 19,401 | identical **sets** 42/42; 6 words doubled multiplicity (defect D1) |
| Amharic | 7 (8,9,267,5,10,12,15) | 12,068 → 11,060 | 3,629 → 3,391 | 364 → 351 | identical 7/7 |

Heaviest per-word rows:

| Word | Wall ms off → on | affixApply off → on | tmplEntry off → on |
| --- | ---: | ---: | ---: |
| Sena index 74 | 84,388 → 283 | 1,532,928 → 40,375 | 84,769 → 3,730 |
| Sena index 52 | 2,850 → 139 | 43,128 → 6,006 | 2,729 → 510 |
| Mbugwe index 17 | 66,031 → 1,280 | 6,523,464 → 141,694 | 171,122 → 9,658 |
| Mbugwe index 11 | 16,096 → 1,012 | 340,436 → 46,054 | 3,006 → 3,006 |
| Mbugwe index 1272 | 11,935 → 2,331 | 90,123 → 21,520 | 1,362 → 1,362 |
| Amharic index 8 | 4,007 → 5,028 | 695 → 644 | 60 → 60 |

Reading: Amharic's cost is not in interleaving (~700 affix applications per 4 s word), so the prune is neutral there.
Bantu grammars are dominated by it. Note `tmplEntry` often does not fall because #491 does not hoist the battery: the
templates are still entered and only their slot rules are rejected. PanGloss should hoist (section 5.3).

In every sample, the partial rules never rescued a parse. Corpus evidence only; the default stays guarded.

### 3.1 The grammars' partial morphemes

| Grammar | Partial rules (all non-template, in the template stratum) | Partial entries | Templates final | Prune under refined guard |
| --- | --- | ---: | --- | --- |
| Sena | 6: mrule15, 16, 19, 20, 21, 23 (none in a template slot) | 25 | 24/24 | off |
| Mbugwe | 1: mrule22, a subject-agreement prefix (requires pos5226) | 0 | 23/24 | off |
| Amharic | 1: mrule11, a definiteness marker (requires pos16747) | 0 | 15/15 | off |
| Indonesian | 4 (mrule12–15) | 0 | no templates | n/a |

FLEx marks an affix `partial` when its MSA is unclassified (`pg-grammar/src/compile/affixes.rs:62`), an entry when
it has no POS (`compile/lexicon.rs:211,515`). Classifying Mbugwe's or Amharic's single unclassified affix turns the
default prune on. **Ship this as a grammar-advisor finding** ("N unclassified affixes disable final-template pruning;
measured cost ~10x on this corpus") regardless of the engine work.

## 4. Defects in PR #491 to avoid in the port

- **D1 Duplicate parses when the prune fires (default on non-partial grammars).** `AnalysisAffixTemplateRule.Apply`
  stamps `FinalTemplateAfterNonTemplate` on the cloned `inWord`. When every slot is optional and none unapplies,
  `ApplySlots` emits that clone. It differs from the template's input only by state, which is now inside
  `Word.ValueEquals`, so `AnalysisStratumRule.ApplyTemplates`'s `!Equals(input, tempOutWord)` "template did nothing"
  filter passes it; with `MergeEquivalentAnalyses` it becomes an alternative of the input's shape and synthesizes into a
  second copy of the same parse. Observed: word-list index 9, 12 → 24 identical analyses, 6 of 42 Sena words, 54 duplicated
  signatures, all `off=1 on=2`. PanGloss equivalent: `template_unapply_slots`'s fall-through
  `out.entry(in_word.dedup_key()).or_insert_with(|| in_word.clone())` (`stratum.rs:937-938`) and `apply_templates`'s
  `changed = t.dedup_key() != in_key` (`stratum.rs:794`). **Rule: the "changed" comparison must ignore the prune
  state, or the poisoned all-skipped clone must not be emitted.**
- **D2 Memo/dedup splitting while disabled.** #491 sets `NonTemplate`/`None` on every affix and compounding
  unapplication regardless of the guard, so `AnalysisStateKey` and `Word` equality split otherwise-equal states on
  every partial grammar (Sena positive memo hits 856 → 768, no prunes). **Rule: compute `enforce` once per stratum; when
  false, never leave `None`.**
- **D3 Guard counts lexical entries** (section 2.2).
- **D4 The result-changing override is undocumented as such and unmeasured.** Treat `always_enforce_final_templates`
  as a semantic option with its own tests and its own conformance expectation, never as a perf A/B arm.

Also from #491's tests: `AffixTemplateTests.EarlyPruningOfFinalTemplate` sets `morpher.IsPartial = false` by hand to
exercise the prune on a partial test grammar; PanGloss tests should construct a genuinely non-partial grammar instead.

## 5. PanGloss design

### 5.1 Grammar-time precomputation (`pg-grammar` compile or `pg-rules` driver construction)

```rust
/// Per stratum: does any *rule* (affix process, realizational, compounding if it ever gains `partial`)
/// in this stratum or any deeper stratum carry `partial == true`? Lexical entries are excluded by proof
/// (partial roots take no templates). Computed once; never rescanned in the cascade.
partial_rule_at_or_below: Vec<bool>   // indexed by StratumId, depth-ordered
/// Load-time check backing the proof: no template slot rule id appears in any stratum's mrule list.
slot_rules_disjoint_from_mrules: bool
```

Only `AffixProcess` has `partial` at `d2717652` (`model.rs:598` says so explicitly); do not infer partiality for
`Compounding`/`Realizational`. Then per stratum:

```rust
enforce = (!partial_rule_at_or_below[k] && slot_rules_disjoint_from_mrules) || cfg.always_enforce_final_templates
```

### 5.2 Word state

```rust
#[derive(Copy, Clone, Debug, Default, PartialEq, Eq, Hash)]
pub enum FinalTemplateState { #[default] None, NonTemplate, FinalTemplateAfterNonTemplate }
```

Add to `WordFlags` (`pg-rules/src/word.rs:24`) as `final_template_state`. Do **not** overload
`is_last_applied_rule_final` (synthesis-side, already in `WordKey`). Transitions, all gated on `enforce`:

1. Successful analysis affix unapplication (`ana_affix*` outputs, `morph.rs`): `NonTemplate` if `!is_template_rule`,
   else `None`. Compounding unapplication (`ana_compound*`): `NonTemplate`. Realizational: `None` (they are
   template-internal in practice; mirror #491 which only rejects there).
2. `analyze_template` (`stratum.rs:875`): if `tmpl.is_final && input.flags.final_template_state == NonTemplate`,
   the walk is poisoned. Prefer **not** to clone-and-stamp the input (see 5.3); carry a `poisoned: bool` argument
   through `template_unapply_slots`/`apply_slot_batch`/`apply_one_mrule`.
3. In `apply_one_mrule` (`stratum.rs:576`) or the `ana_*` entry: if poisoned (or input state is
   `FinalTemplateAfterNonTemplate`), reject before `tick()` and record `NonPartialRuleProhibitedAfterFinalTemplate`
   in trace when tracing. Do it before budget accounting: a gate-rejected rule was never attempted (matches existing
   gate ordering there).
4. Stratum exit (`analyze`, `stratum.rs:1040-1070`): reset to `None` on every output **before** `dedup_key`/
   `merge_equivalent` bookkeeping, so clitic strata and cross-stratum equality behave as today. Also reset on entry to
   synthesis / lexical lookup.

### 5.3 Hoist the whole battery (the part #491 lacks)

In `apply_mrules` (`stratum.rs:769`), before calling `apply_templates(&w)`: if `enforce` and
`w.flags.final_template_state == NonTemplate` and **every** template in the stratum is final
(precompute `all_templates_final[k]`), skip the battery entirely. That removes the remaining `tmplEntry` work
(Mbugwe 1,362 → 0 on index 1272, where #491 leaves it at 1,362) and never enters `run_template_batch`, so no
memo key is built. For mixed-finality strata, enter only the non-final templates; pass `poisoned` to the final ones
so their slot rules reject (or skip them as well, since a poisoned final template can only emit the all-skipped
input, which must be filtered anyway, see D1). Skipping is simpler and equivalent.

### 5.4 Keys, equality, replay

- `AnalysisStateKey` (`pg-memo/src/lib.rs:59`) and `AnalysisDriver::state_key` (`stratum.rs:555`): **include**
  `final_template_state`. Two words equal on every other field but different here make different legal decisions
  (one may enter final templates, the other may not). With D2 fixed (state stays `None` when `enforce` is false)
  this costs nothing on partial grammars.
- `WordKey`/`dedup_key` (`word.rs:424`): include it too; it is already reset before stratum-exit dedup, so cross-stratum
  results are unaffected. Audit `Word::replay_onto` (memo replay reconstructs history) so the replayed word carries
  the *arrival's* state, not the stored word's.
- With 5.3 in place the only words that ever carry `FinalTemplateAfterNonTemplate` are inside a rejected template walk,
  so the state's practical footprint is `None` vs `NonTemplate`.

### 5.5 Option surface

`AnalyzerConfig` / `Morpher` builder: `always_enforce_final_templates: bool` (default false). It also changes the
**synthesis** gates exactly as #491 does: `((!word.is_partial && !rule.partial) || always_enforce)` in
`morph.rs:1336`, `:1448`, and the compounding gate at `:2547`. Expose on `pangloss batch` as a flag for
FLEx-parity experiments. Document it as result-changing.

### 5.6 Counters (permanent, cheap, in `pg-rules/src/stats.rs` / `--stats`)

`template_entries`, `template_batteries_skipped`, `prunes_affix`, `prunes_compound`, `prunes_realizational`,
`poisoned_template_entries`. The acceptance gate below reads them.

## 6. Tests

Unit (synthetic grammars, both memo modes, traced and untraced):

1. Non-partial grammar: final template after non-template rule is rejected; template-first order still parses.
2. Non-final template after non-template is still allowed; final template after a *template* rule allowed.
3. Grammar with a partial **entry** only: prune active (refined guard), results unchanged vs. unpruned engine.
4. Grammar with a partial **rule** in the template stratum: prune inactive by default; identical to today.
5. Same as 4 with `always_enforce_final_templates`: the specific partial-inside parse is dropped; assert it as policy.
6. Partial rule in a *shallower* stratum than the templates: prune active in the template stratum (per-stratum guard).
7. All-optional-slot final template in poisoned state emits nothing new and produces **no duplicate parse** (D1
   regression; assert multiset equality of signatures, not set equality).
8. Compounding unapplication sets `NonTemplate` and participates.
9. Stratum exit resets the state; a clitic stratum's rules apply after a final template in the inner stratum.
10. Memo replay: two arrivals at the same shape/FS/multiset with different `final_template_state` are not conflated.
11. Battery hoist: counters show `template_batteries_skipped > 0` and `template_entries` unchanged from the
    non-hoisted count minus the skipped ones on a mixed-finality stratum.
12. Disabled mode leaves every word at `None` (D2 regression): memo hit counts equal the pre-change engine's.

Corpus (private grammars, `--threads 1`, both `--memo` modes, plus a parallel batch):

- Default policy: analysis **multisets** identical to `d2717652` on the Mbugwe/Sena/Amharic word lists; counters show
  zero prunes on those three grammars (all have partial rules in the template stratum) and nonzero on a copy of
  Mbugwe with mrule22 declassified or on the C#-style non-partial fixtures.
- `--always-enforce-final-templates`: multisets identical on the 22 Mbugwe / 42 Sena / 7 Amharic words measured
  above (they were identical in C#), counters show the prune firing, and steps/FST runs drop by the ratios in
  section 3 within a factor of ~2.
- C# oracle parity: compare against `pr-491` with `AlwaysEnforceFinalTemplates` on **after** D1 is fixed there or
  with multiplicity-insensitive comparison; do not compare against #491's raw default output on non-partial fixtures
  (it duplicates).

## 7. Verification protocol (unchanged from the TEMP handoff; restated)

Pin baseline `d2717652` and the candidate; `cargo build --release -p pg-cli`; run words sequentially; compare
per-word signature **multisets** built from gloss + allomorph index + syntactic FS (never `Morpheme.Id`: FLEx exports
have no `<MorphemeId>`, so an Id-based signature degenerates to a morph count; this caveat applies to every earlier
Machine "exact parity" claim on these grammars); prefer mechanism counts over wall time; run
`cargo test --workspace --release`, clippy `-D warnings`, the conformance fixtures, and a C#-oracle parity pass.

## 8. Expected outcome and what to report

- Default mode on the three private grammars: no behavior change, no measurable cost (D2 avoided), zero prunes.
- Default mode on non-partial grammars (conformance fixtures, declassified Mbugwe): prune fires; report steps, FST
  runs, and wall time off → on with multiset parity.
- Override mode on Mbugwe/Sena: the section 3 ratios, reproduced in PanGloss.
- Grammar-advisor line item: list of unclassified affixes per grammar with the measured cost.
- Feed back to #491: D1–D4 and the refined guard (draft in `pr491-review-draft.md`).

## 9. Open questions

- Should PanGloss's default guard also require `slot_rules_disjoint_from_mrules`, or assert it at load and fail
  loudly? (Recommend: assert with a clear diagnostic; FLEx never produces the overlap.)
- Compounding rules carry no `partial` today; if `pg-grammar` ever adds it, `partial_rule_at_or_below` must include it.
- Whether to implement the lexical-lookup candidate filter (2.2, last paragraph). Measure `synth` rejections first;
  likely not worth the code.
- Realizational rules: #491 rejects them in poisoned state (added in its last commit, 18.75% patch coverage). PanGloss
  should mirror the rejection and add a test; realizational rules are template-internal, so the hoist covers them.
