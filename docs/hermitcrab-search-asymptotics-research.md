# HermitCrab analysis search — asymptotic research

**Disposition: count ceilings before building.** The first two candidates below fit the two open work
items without a search-sized memo. Partial-order reduction has a narrow correctness route and poor prior
coverage. Exact finite-state compilation is a larger, restricted-subgrammar project. None is a measured
optimization, and none establishes a polynomial parser for arbitrary FieldWorks grammars.

This integrates the six supplied reports: **R1** partial-order reduction and rewriting; **R2** packed forests
and subset DP; **R3** finite-state methods; **R4** search and symbolic sets; **R5** enumeration and graphs;
**R6** abstract interpretation and morphology prior art. The reports are research inputs, not independent
measurements. The [ledger](hermitcrab-optimization-ledger.md), its
[September disposition](hermitcrab-perf-2026-09-disposition.md), and the
[probe design](hermitcrab-probe-design.md) remain the measurement record.

## The problem in one page

The motivating case is **a grammar with 16 extra affix processes in one Unordered stratum**. Analysis
starts at the surface, unapplies phonology, then interleaves morphological-rule unapplication with affix
templates. Every candidate can stop for lexical lookup; candidates from intermediate strata also enter
the lookup/synthesis pipeline. A found lexical entry supplies its root allomorphs, each candidate's
alternatives are expanded, and forward synthesis must both pass validity checks and regenerate the surface
form [C1, C4, C5].

There are three different sources of growth:

- **Rule orders.** The combination cascade restarts its rule loop at zero after every successful
  unapplication. With `k` enabled, single-use rules, there can be `k!` full orders and
  `sum(j=0..k) k!/(k-j)!` ordered prefixes. Multiple application is enabled; ordinary affix/compound
  unapplication checks each rule's limit rather than a cascade-wide used-rule mask. C# realizational
  unapplication has no such count guard, so a finite count-vector bound needs an eligibility proof for
  the rule kinds included [C1, C2]. Allomorph branches and compounding add further choices. The 16 added
  processes are not necessarily the total `k`.
- **Optional sites.** Inverse deletion inserts optional nodes into one phonological shape; it does not
  immediately allocate every subset shape. Later matching still has consume/skip choices. The Rust root
  trie already merges equal active trie positions. General morphological matching also returns captures
  and variable bindings, so its branches cannot be merged on position alone [C6, C7]. Optionality can
  therefore multiply matcher work even while the shape representation stays compact.
- **Alternatives.** An analysis-state key deliberately omits trail order. The stratum merge keeps
  alternatives, and expansion grafts subsequent changes onto their source histories. Forward synthesis
  reads the pending rule in order. A shared shape or rule multiset is not a shared synthesis continuation
  [C3–C5]. One final analysis does not imply that only one trail needs checking.

All numbers here are **supplied by the common brief, not reproduced in this job**:

| Aggregate | Reported value |
| --- | --- |
| Baseline parse time | approximately 15–60 ms per input in both implementations |
| Time after the 16 additions | 17–22 s for one expensive input in Rust |
| Lexical lookups / distinct shapes | 97,575 / 56, from trace mode |
| Distinct canonical states | 10,079; they differ in shape or per-rule counts |
| Template-battery runs / distinct input states | 43,641 / 4,988; approximately 2.9 s |
| Lookup + alternative expansion + synthesis | approximately 6.5 s |
| Alternatives synthesized / distinct final analyses | 367,278 / 1 |
| Compounding bound in the case | `MaxStemCount = 2` |
| Deletion-rule variants | 200–1,600 inputs reach a 1 s per-input timeout |

These are ceilings to investigate, not interchangeable denominators. Tracing disables the ordinary
analysis merge and C# memoization [C3, C4]; the trace lookup count cannot establish an untraced duplicate
rate. Nor do 56 shapes establish 56 lookup keys across strata. The apparent battery repetition is at most
38,653 calls (88.6%) under the brief's identity, before checking that identity and the cost of avoiding it.
The 367,278-to-one ratio proves neither order invariance nor a synthesis-sharing opportunity.

## Verified code anchors

Line numbers below were read directly, not copied from the reports. **C#** means the requested main
checkout, commit `a4b29742b6274a01c7398ca3e800a19fa6d2c9aa`; paths are relative to
`src/SIL.Machine.Morphology.HermitCrab/` unless qualified. The six central files (`AnalysisStratumRule`,
`AnalysisStateKey`, `MemoizedCombinationRuleCascade`, `AnalysisAffixTemplateRule`, `Morpher`, `Word`) are
text-identical in this PR worktree's starting commit `5bbfa018bdbd5228c3f1aa73db951a4f01d167d3`, ignoring
line endings. The C# anchors are a snapshot at `a4b29742`, before #491 (`b240b7b5`) merged; they were not
re-read at current master. **Rust** paths are relative to the supplied checkout's `rust/crates/`, with
branch tip `fa3bfb186852baaaf1ed28fcc0ba4602b2c19e96` at inspection. This identifies inspected files; it is
not a claim that either external checkout was clean.

| Anchor / fact | C# file:line | Rust file:line |
| --- | --- | --- |
| **C1 — cascade and template interleaving** | `AnalysisStratumRule.cs:24–61,128–137,174–244`; base combination loop: `src/SIL.Machine/Rules/CombinationRuleCascade.cs:25–51` | `pg-rules/src/cascade.rs:219–265`; `pg-rules/src/stratum.rs:811–888`; strata/results: `pg-parse/src/morpher.rs:701–765` |
| **C2 — application/compound guards and feature effects** | `MorphologicalRules/AnalysisAffixProcessRule.cs:39–63`; `MorphologicalRules/AnalysisCompoundingRule.cs:39–77,129–133`; realizational exception: `MorphologicalRules/AnalysisRealizationalAffixProcessRule.cs:40–58` | `pg-rules/src/stratum.rs:707–724,792–805` |
| **C3 — existing sharing and actual keys** | `AnalysisStateKey.cs:31–36,60–85,94–104`; merge: `AnalysisStratumRule.cs:130–167`; inner memo: `MemoizedCombinationRuleCascade.cs:39–94`; battery memo: `AnalysisStratumRule.cs:196–218` | key fields: `pg-rules/src/analysis_state_key.rs:22–47`; key construction: `pg-rules/src/stratum.rs:664–694`; ordered output key: `pg-rules/src/word.rs:309–321,410–433`; stratum fold: `pg-rules/src/stratum.rs:1180–1239` |
| **C4 — lexical lookup, expansion, acceptance, fallback** | `Morpher.cs:167–201,346–367,417–445,634–665`; expansion: `Word.cs:445–486` | `pg-parse/src/morpher.rs:459–499,765–826,933–997`; expansion: `pg-rules/src/word.rs:487–548` |
| **C5 — ordered trails are observable** | `Word.cs:202–215,253–259,339–347,371–382,627–646` | `pg-rules/src/word.rs:318–319`; pending-rule test and consumption: `pg-rules/src/stratum.rs:1546–1571,1606–1609` |
| **C6 — optional nodes and root-trie sharing** | `PhonologicalRules/NarrowAnalysisRewriteRuleSpec.cs:40–59`; optional insertion argument: `src/SIL.Machine/Annotations/Shape.cs:160–174` | deletion sites/insertion: `pg-rules/src/rewrite.rs:2536–2581`; root trie consume/skip/dedup: `pg-parse/src/root_trie.rs:227–277` |
| **C7 — general matcher work carries captures** | `MorphologicalRules/AnalysisAffixProcessRule.cs:24–33`; capture-dependent reconstruction: `MorphologicalRules/AnalysisMorphologicalTransform.cs:61–100`; optional forks/registers/bindings: `src/SIL.Machine/FiniteState/TraversalMethodBase.cs:248–308` | template slot results and their semantic deduplication: `pg-rules/src/stratum.rs:1057–1075` |
| **C8 — template admission and slot semantics** | `AnalysisAffixTemplateRule.cs:18–73`; non-disjunctive union: `src/SIL.Machine/Rules/RuleBatch.cs:52–61`, configured at `AnalysisStratumRule.cs:27–30` | `pg-rules/src/stratum.rs:894–962,990–1075` |
| **C9 — proposer is followed by exact-engine confirmation** | exact confirmation target: `Morpher.cs:346–367` | current emitter: `pg-foma/src/emit.rs:2615–2675,2704–2745,2768–2808`; confirmation filters/reparse: `pg-foma-runtime/src/confirm.rs:498–517` |

In particular, at `a4b29742` the C# key contains shape, stratum, syntactic and realizational features,
non-head count, and exact per-rule unapplication counts. It contains neither final-template state nor a
separate morphology history; #491 later added final-template state to the key, so current master lacks only
the morphology history. Rust adds both, saturates counts at each rule's maximum, and clears its analysis
final-template state at the stratum fold (`pg-rules/src/stratum.rs:1196–1200`). The same key name does not
imply the same equivalence relation [C3]. Neither key is a synthesis key. This inspection verifies these
fields and call sites; it does not certify a new key as complete for every continuation.

There is a second relevant implementation difference: Rust's common dispatch applies the count gate and
pushes a pending-rule entry for every morphological rule kind. C# realizational unapplication has no count
gate and records counts without adding a pending-trail entry [C2, C5]. These are verified bookkeeping
differences, not a demonstrated output divergence here. Any proof using bounded counts or trail identity
must address the C# semantics explicitly; copying Rust's control vector is insufficient.

## Ranked candidates

Rank is the order in which to spend research effort under the memory constraint, not a promised speedup.
The reference labels point to the bibliography below; **every literature citation is from memory and was
not checked against a publication or online**.

| Rank / technique | Home field + reference | Mapping in C# / Rust | Exact-parity condition; check and fallback | Expected asymptotic change | Cheapest ceiling experiment | Difference from closed rows |
| --- | --- | --- | --- | --- | --- | --- |
| **1. Grammar-compiled residual viability**; includes lexical goal propagation, abstract interpretation, and residual lexical coaccessibility | Program analysis / demand-driven databases / automata; [L1, L2, L5] | Before recursive expansion in C1/C2, with summaries of templates C8, optional nodes C6, and lexical targets C4; Rust `combination_rec` and `apply_one_mrule` | **Static:** sound transfer summaries for all remaining operations and reachable strata/roots, including compounds. **Runtime:** prune only a proved-empty abstract continuation. Unsupported operation, budget exhaustion, guessing/overlay not covered: unknown, ordinary search | Fewer explored prefixes; a certified precedence restriction can change `k!` orders to `L` viable linear extensions. No universal worst-case improvement | Shadow checks at C1/C2: count provably dead branches and descendant rule/template/lookup work, unpooled; count summary size and query work | Rows 10/11 reopen only with residual transformations **and compounding** covered; root-reachable synthesis failures remain. This is neither row 8's bare-root length bound nor row 19's feature-only synthesis replay |
| **2. Compile the template battery into a shared-prefix control trie** | Grammar factoring / parsing / finite-state compilation; [L3, L5]. A specialization of the reports' compiled-control ideas, not a reported measurement | C8: replace repeated reverse slot walks inside C1's battery. Rust `run_template_batch`, `template_unapply_slots`, `apply_slot_batch` | **Static:** share only identical slot-batch operations and optionality, with per-template admission and terminal labels retained. **Runtime:** identical input to a shared prefix; preserve all branch outcomes and existing dedup semantics. Trace, unsupported effects, or control-size budget: original battery | For `t` templates sharing prefix work `W_p`: `t·W_p` becomes `W_p`, plus divergent branches and terminal bookkeeping. For a deterministic `p`-slot prefix, this is `t·p` to `p`. No reduction of unrelated rule permutations | Compile-only census of distinct reversed slot prefixes; at C8 count attempts/outcomes at common prefixes, admitted templates, skip-only terminals, clones, and maximum DFS depth. Separate per-battery savings from avoiding battery calls | No `(Word,state)` result cache: sharing is in the grammar's control trie. Does not repeat rows 1/2/18 or the alternatives memo/hoist. Deferred cloning and the final-template filter already have separate records |
| **3. Trace-class POR with complete ordering recovery**; critical-pair analysis supplies certificates | Concurrency / term rewriting; [L4] | C1 unordered recursion; class readout at C4/C5. Same boundaries in Rust `combination_rec` and the expansion/synthesis consumer | **Static:** strong independence with enabledness and branch/effect correspondence, not just equal final shapes. **Runtime:** stack-local sleep sets; retain every class's ordered histories and all stopping/template points. Uncertified pair is dependent; unhandled rule kind uses ordinary search | Ordered-prefix exploration becomes trace-class exploration; fully independent single-use, single-outcome rules give `2^k` classes and `O(k·2^k)` transition attempts. Recovery/synthesis can still require `k!` trails | Count certified pairs **where enabled**, classes, and prefix/template work avoided; separately count recovered trails and synthesis. Earlier eligibility was only 0.00–0.05% of attempts in the supplied research context | Recovers the orders discarded by the rejected commuting-order canonicalization. Skipping class members without a full synthesis/output-invariance proof repeats row 6; merging synthesis steps repeats row 5 |
| **4. Exact finite-state compilation of a verified subgrammar** | Computational morphology / transducers; [L5] | Compile C1/C2/C6/C8 and the lexicon/forward checks C4 into a labelled relation; Rust emitter C9 is a starting point for coverage inspection, not an exact replacement | **Static:** exact regular encoding of features, annotations, root/non-head identity, all ordered outputs, copying and rule limits; unsupported construct or compile-size overflow rejects eligibility. **Runtime:** budget checked before allocating input-product storage; ordinary parser on refusal | Control alone: `prod(i)(m_i+1)` states, at most `k` outgoing choices per state; for single use `2^k` states / `O(k·2^k)` arcs. Full relation and output enumeration may still be exponential/factorial | Coverage audit and compiled states/arcs first; then accepted labelled paths, confirmation work actually removed, distinct complete analyses, and bytes. Compare against 367,278 trails, not just one output | Shares a **grammar-compiled** relation without erasing order or adding a per-search chart. A permissive proposer that still reparses has not removed the cascade |

## 1. Residual viability: prune only a proved-empty continuation

Let `S` be a concrete HermitCrab internal `Word` plus cascade/stratum control. Let `Reach(S)` include
every possible stopping candidate after the remaining inverse operations, including lookup in intermediate
strata and every permitted compound split. Choose a finite abstract domain, an abstraction `alpha`, and
transfers satisfying `alpha(T(S)) <= T#(alpha(S))` for **sets** of results. Compute a conservative closure
of the remaining transfers. If that closure has empty intersection with an over-approximation of all
compatible lexical targets, no concrete continuation can find a root. Removing that branch therefore
removes no successful synthesis and no reported analysis. This follows by induction over each concrete
continuation; it needs no assumption that rule order commutes.

The target set must include everything ordinary lookup could supply. Compound non-head lookup is already
inside unapplication [C2]; it cannot be ignored or treated as a single final root. Root guessing must
either have its own sound target abstraction or retain the original candidates for the fallback branch
[C4]. Rust supplied-root overlays also affect targets (`pg-parse/src/morpher.rs:459–499`). A finite list of
ordinary root spellings is insufficient in either situation.

Start with a grammar-compiled coarse domain: mandatory/optional segment-length intervals, safe segment
classes, and only feature constraints whose transfer is proved. Inverse truncation can **grow** a shape:
`MorphologicalRules/AnalysisMorphologicalTransform.cs:54–80,108–132` reconstructs uncaptured input parts.
Optional nodes require both skip and consume possibilities [C6]. An unsupported transform maps to top,
never to failure. Staged
refinement is useful only if every stage remains an over-approximation; an inconclusive expensive stage
returns unknown. This is the safe part of R6's abstract interpretation, R4's magic sets, R3's lazy product,
and R5's extension-oracle proposals. They are one pruning family, not four independent wins.

R4's precedence idea is a special case with a stricter obligation. To install `r before s`, prove that
**every successful complete path containing both occurrences** respects that order, including paths with
intervening enabling rules/templates. Failure of the adjacent two-step sequence `s;r` is insufficient.
Rules are optional: `s` without `r` must remain allowed. A runtime implementation can forbid a later `r`
after `s` only under that complete-path certificate; it cannot require `r` before admitting `s` altogether.
Begin with single-use rules, no certified edges for unknown constructs, and no inference from prefix/suffix
direction alone. A cycle says that the affected combination is impossible, not that individual rules
should be deleted.

This must remain grammar-sized: use finite transfer tables and stack-local query work, not a table keyed
by every visited `S`. A lazy lexical product that needs an unbounded visited-state set fails the memory
constraint; use a statically bounded abstraction or return unknown. Count dead **subtrees and their work**
without double-counting descendants. The ledger's 23.5% pooled dead-step result and zero on one expensive
input are reasons to stop if the stronger oracle still mostly accepts. No lexical oracle can reject a
root-reachable path merely because later synthesis will fail.

## 2. Make the template battery cheaper by factoring its control

The code provides a useful, smaller target than a whole-search chart. C# builds a non-disjunctive battery
and non-disjunctive batches for individual slots. Each template walks slots from last to first; an optional
slot permits the unchanged input to continue, while a required slot stops that skip path [C8]. Templates
are therefore finite control programs over **set-valued** morphological transformations.

Compile a trie of those reversed control programs. Share a prefix only when its slot operation has the
same ordered rule identities, application role, optionality, and relevant compiled settings. Keep terminal
template identities and their entry predicates. At each battery invocation, evaluate each template's
selector/required-feature predicate on the **original input**, as today; a shared edge is eligible only
for the admitted descendant templates. Preserve per-template finality policy in Rust. Do not move entry
predicates to a transformed intermediate input [C8].

For an admitted template, the original slot path has an identical path through the trie. Conversely,
every trie terminal has an admitted original template. At a shared edge the inputs and operations are
identical, so applying the operation once yields exactly the result set each original execution would
have produced. Induction over prefix length preserves each template's ordered rule trail and complete
internal `Word` effects. Branches split at differing slots; required slots retain no skip edge, optional
slots retain both routes, and all-skipped terminals remain. The union of terminal results is consequently
the original battery result set. This is control factoring, with no identification of two independently
arrived search states and no commutation assumption.

Two implementation boundaries matter. First, retain the existing slot/battery deduplication and representative
semantics: C# uses `Word.ValueEquals`, which omits syntactic features; Rust explicitly generalizes syntactic
features on collisions [C5, C8]. A new global semantic-key merge is not justified by the trie proof. The
first eligible fragment should exclude differing duplicate-result representatives unless their equivalence
is certified. Second, keep template-specific effects at terminals; if an effect changes a subsequent slot
operation, it belongs in the prefix identity or prevents sharing. Tracing follows the ordinary battery,
because template-specific trace events observe separate executions.

The new trie has at most one node per original slot-prefix position: `O(B)` grammar storage for `B` total
slots, plus rule lists and terminal membership. Evaluate it depth-first with only the current internal
`Word`, branch iterator, and admitted-template information on the stack. No completed `(trie node, Word)`
results are retained for later arrivals. Live traversal storage is bounded by grammar depth and the current
rule-result enumeration, not total states visited. Existing result accumulation is still required. If
implementation instead introduces a growing active-state chart, it has left this proposal.

This can reduce the factor due to duplicated template control. It does **not** turn the 43,641 battery calls
into 4,988 calls; that would need different search structure or the ruled-out memo. No supplied report
measures common slot prefixes, so a compile-only prefix census is the first gate. This specialization
combines R2's structural sharing and the reports' finite-state compilation ideas at a grammar-sized
boundary. It is a derived proposal, not an experiment any report already ran.

## 3. Partial-order reduction: preserve the trace class, then recover its orders

R1 supplies the essential correction to canonicalization. A class contains paths related by swapping
adjacent independent **occurrences**, not by sorting a multiset and deleting its other orders. With repeated
applications, occurrence identity includes its count/position; nondeterministic allomorph outcomes must
also have a proved correspondence. Templates, compounding, and unsupported operations initially act as
dependent barriers.

For every certified pair and reachable eligible state, require (1) either transition preserves the other's
enabledness and limits; (2) both two-step result sets have a branch-by-branch bijection; (3) matched endpoints
agree on everything subsequent unapplication, templates and lookup can read; and (4) occurrence effects can
be replayed in each recovered order with all trail, non-head and feature history preserved. Disjoint-looking
string edits, critical-pair joinability, or local confluence alone prove none of these for the complete
internal `Word`. C2 and C7 show why feature effects and captured reconstruction are part of the obligation.
Mark uncertain pairs dependent; a conservative restricted certificate is preferable to runtime speculation.

Under these conditions, adjacent swaps neither lose a path nor invent an impossible one. Their transitive
closure partitions executions into trace classes. A sleep-set traversal can explore one representative
of each class, **including classes of every stopping prefix**; the dependency ordering of occurrences
retains its other linearizations. Template opportunities at intermediate prefixes must remain represented,
not just the terminal class. Lazy enumeration of each class's linear extensions, with exact occurrence
effects restored, reconstructs the original ordered synthesis inputs. Running ordinary synthesis on each
restores precisely the original analysis set. This is the completeness argument missing from merely
keeping the representative.

Store the grammar's conservative pair matrix (`O(k^2)`) and current path/sleep/linearization stacks. The
occurrence bound `D` must come from actual grammar limits; a current class can use `O(D^2)` ordering data.
Replace that class's explicit histories or stream them to the existing consumer; do not add a search-wide
class registry or DPOR backtracking graph. If the certificate or stack budget cannot be satisfied, use
the ordinary traversal. A streaming consumer is an architectural change to C4, not a drop-in set key.

For fully independent, single-use rules with one outcome per occurrence there is one class per subset,
hence `2^k` classes rather than factorially many ordered prefixes. Allomorph branching can add further
classes. **That bound is on analysis traversal, not end-to-end parsing.** If
forward synthesis or output identity distinguishes class members, all relevant orders still need exact
validation/readout. Skipping their synthesis needs a separate proof of identical complete acceptance and
reported analysis, for every root and allomorph. C5 and ledger rows 5/6 rule out assuming it globally.
R6's stronger output-invariance condition is right for **discarding** orders; R1's recovery construction
is right for avoiding repeated pre-synthesis work while preserving them. The earlier 0.00–0.05% eligibility
is a warning, not evidence that better certificates exist.

## 4. Finite-state compilation: exact only for a certified fragment

An exact compiler would encode a relation from surface input to complete accepted analysis labels. It
must represent the inverse/forward rule behavior, lexical alternatives, template paths, stratum control,
features, annotations and non-heads that affect validity, with every observable ordered identity retained.
Compile bounded rule-use counts as a control vector: there are `prod(i)(m_i+1)` vectors and at most `k`
choices out of one vector. The vector alone describes permission to try rules, not their semantic outputs.

If each primitive's encoding is exact, relational union preserves all branches and composition preserves
all legal sequences. Intersecting with the input and lexical/validity constraints then preserves exactly
the accepting paths. This gives a structural induction proof for the compiled fragment. Output labels
must carry sufficient allomorph/morpheme order, root position, non-head provenance and feature information
to reconstruct the same reported analyses. If compilation is only an over-approximation, it may reject
proved-impossible branches as in candidate 1, but its surviving paths still require ordinary confirmation.

The compiler must refuse constructs with no established regular encoding. In particular arbitrary copying
of an unbounded captured string is not an ordinary finite-state relation; finite alphabets alone do not
fix that. Bounded copying, finite feature domains and bounded compound/rule counts need explicit encodings,
not assumptions. Unknown annotation or constraint behavior also rejects exact eligibility. Compile-time
state/arc/byte limits bound grammar storage, with fallback if composition expands too far.

For an accepted finite relation with `Q` states and `E` arcs and an input of length `n`, recognition can
use a grammar/input-bounded product, rather than materializing each complete trail. Epsilon behavior must
be handled exactly, and productive zero-input cycles must be excluded or proved bounded. An `O(nQ)`
reachability/coaccessibility structure is acceptable only with its byte bound checked before allocation;
otherwise use ordinary parsing. Recognition sharing does not bound distinct labelled outputs or the
cost of eliminating duplicate output identities. Both the compiled relation and its readout can still
be exponential; `2^k` is the **control-state** bound, not the number of analyses.

R3 is right that a compact proposer plus exact reparse does not solve the confirmation cascade. Its
blanket `O(k^2)` description of the present proposer is too broad: the actual emitter has a subset-mask
path for eligible unordered underlying-token zones, a dedicated-level fallback, and a legacy every-rule
per-level path [C9]. The comment in `pg-foma/src/unordered.rs:44–61` describes the latter, not all current
execution. No claim about the proposer's universal completeness or exact parity is established here.

## Where the six reports disagreed

| Issue | Resolution after reading code and the ledger |
| --- | --- |
| **Is the cascade already subset DP?** R2/R4/R5 emphasize a new inner chart; R6 says that would repeat memoization | R2 is right about **Rust**: C1 uses recursive enumeration and C3 merges later. R6 is right about **C#**: the sequential inner cascade and battery already replay state-keyed results. A count vector is not a sufficient semantic key. A capped chart still grows with search and is outside the hard constraint |
| **What is in `AnalysisStateKey`?** The brief/R2 describe more than the older probe-design key | R2 matches Rust's morphology-history/finality fields. Applying that description to C# is wrong; C3 lists the actual different keys. Neither warrants synthesis merging |
| **Every rule is bounded by its application count**, assumed in the brief and count-vector proposals | Correct for Rust's shared gate and C# ordinary affix/compound rules; C# realizational unapplication is an exception and does not extend the pending trail [C2, C5]. The general bound is conditional, not established for every rule kind |
| **Can commuting orders be reduced?** R1 permits recoverable classes; R6 requires invariant output | Both conditions have a different scope. Recovery can preserve every path; discarding an order requires R6's stronger condition. This resolves the ledger's parse-signature objection, but does not establish a synthesis saving |
| **Packed forest/ZDD/product graph is the best lead** in R2/R4/R5, versus R6's memory objection | R5 is right that an exact product needs full synthesis state and ordered continuation/provenance; one final analysis supplies no small-state bound. R6 is right under this job's constraint. Generic product charts are not recommended. A ZDD encodes sets; ordered trails need position/occurrence encoding, whose size none of the reports bounds |
| **Optional deletion is already compact**, emphasized by R3/R6 | Correct for shape construction and Rust trie active states [C6]. It does not establish cheap general morphological matching: C7 needs captures/bindings, and ledger row 23 closes position-only deduplication |
| **The proposer is `O(k^2)`**, R3 | Verified only for the legacy branch. C9's executed mode dispatch and mask loop take precedence over the older descriptive comment |
| **Simple static precedence could remove impossible orders**, R4 | Conditional only: adjacent-pair failure does not rule out an intervening enabler, and an optional predecessor cannot become mandatory. Candidate 1 states the stronger complete-path certificate |
| **A 64-key root cache bounds walks by `min(U,64)`**, R5; R4 also proposes bounded reuse | The raw search boundary is reusable under fixed grammar/overlay [C4], but the bound is wrong when `U > 64`: uncached keys are searched on every revisit. A byte cap must also cover shapes and result lists. Under the no-memo instruction this is not recommended |

## Considered and not recommended

- **Count-vector charts, generic packed analysis forests, ZDD trail tables, analysis-DAG × synthesis-state
  products:** search-growing tables; rows 1/4/5/6/16 already test the necessary convergence, and a fixed cap
  with restart does not make these a different allowed technique.
- **Packed synthesis readout keyed only by trail position or current shape:** omits ordered continuation
  and root-dependent state; rows 5/6/16 close it. `O(Q+T+A)` only helps after proving `Q` and provenance
  readout small, which no report does.
- **State/position-only matcher deduplication or two-pass capture reconstruction:** row 23 and the
  [two-pass record](rejected-optimizations/two-pass-nondeterministic-traversal.md) already settle these;
  the safe group-free fragment is existing prior work, not a new research result.
- **Fixed-cap exact root-result cache:** useful only for raw lookup, still memoization, and neither
  56 shapes nor 64 entries bounds all `(stratum, shape)` result bytes or repeated misses.
- **Root-only lexical gates, edge prefilters, current-length bounds, feature-only abstract replay, and
  nogood subsumption:** rows 8–11/14/19/22 already cover them; candidate 1 survives only with its stronger
  residual relation and a new measured dead-subtree ceiling.
- **Commuting-order canonicalization without recovery, symmetry/orbit counts:** distinct rule/morpheme
  identities and ordered trails remain observable; no output-fixing grammar automorphism was established.
- **Meet-in-the-middle:** large midpoint frontiers violate the memory constraint; unordered rule splits
  do not automatically turn factorial work into two square-root searches.
- **A*, k-best/k-shortest paths, reverse-search enumeration alone:** all valid analyses are required;
  rearranging or ranking trail enumeration does not eliminate failed trails without a sound extension oracle.
- **Generic Earley/CKY, scalar semirings, equality saturation/e-graphs:** no proved semantics-preserving
  span/equivalence boundary for these transforms; adding full transformation state/provenance restores
  the unresolved search size.
- **Bimachines, two-level morphology, compile-replace, or parser names alone:** constrained morphology
  formalisms supply candidate 4's tools, not a correctness proof for arbitrary HermitCrab behavior.

## Implications for the two work items

**Lexicon-driven pruning during unapplication.** Start with candidate 1 in shadow mode at C1/C2. The
deliverable is a per-input census of proved-dead subtrees, saved descendant work, unknown cases and
abstraction cost. Include remaining morphology, template transitions, intermediate-stratum lexical goals,
optional sites and compound non-head roots. Disable pruning where guessing/overlays are uncovered. Close
it if the result merely reproduces row 10's no-op or row 11's weak root-only ceiling. Exact product
coaccessibility is a possible stronger oracle only after a grammar-sized representation exists; building
a whole search graph to ask whether that graph has a successful leaf is not an allowed shortcut.

**Making the affix-template battery cheaper without memoization.** Start with candidate 2's static
prefix census and admission/slot work counts. Its target is repeated control **within one invocation**.
Candidate 1 can also avoid an invocation whose entire continuation is proved dead; candidate 3 might
avoid repeated prefix work if independence coverage is real. Neither the 43,641/4,988 ratio nor the
historical battery memo authorizes reinstating a cache. Keep deferred cloning, safe edge dispatch and
the final-template policy distinct from this research: their evidence and limits already have records.
In particular, the [final-template record](rejected-optimizations/final-template-prune.md) has an addendum
reopening a narrower guard; its old zero-prune result is not a new lead to rediscover here.

For either item, compare complete analysis sets with ordered morpheme/allomorph identities, root position,
features and non-head provenance. Do not use the legacy count-degenerate signature called out in the
September disposition. Tests support a proof; a finite corpus cannot prove parity for every export.
Tracing and stateful selectors use the existing path unless their invocation/event semantics are preserved.
Configured alternative/work limits also use the existing path unless the same limit behavior is reproduced
[C1, C4]; a resource-limited optimized query returns unknown and falls back, never a partial exact result.

## Evidence limits and references

**Not verified:** the brief's timings/counters, the earlier POR eligibility percentage, exact applicability
of any proposed certificate/compiler to the motivating grammar, common template-prefix coverage, a small
packed product/ZDD bound, and any end-to-end speedup. No real-grammar benchmark or new optimization was
run. Historical measurements are attributed to the supplied brief or the existing records, not fresh
results. Code inspection verifies the listed control/data facts; it does not establish universal parity
of the current Rust port or the current FST proposer. All future experiments need the stronger output
signature and ordinary-parser fallback.

All entries below are **[memory; bibliographic details and applicability not independently verified]**.
No network or publication lookup was available. They identify home fields, not proofs of these proposals.

- **L1.** P. Cousot and R. Cousot (1977), “Abstract Interpretation: A Unified Lattice Model for Static
  Analysis of Programs by Construction or Approximation of Fixpoints.”
- **L2.** C. Beeri and R. Ramakrishnan (1991), “On the Power of Magic.”
- **L3.** B. Billot and B. Lang (1989), “The Structure of Shared Forests in Ambiguous Parsing.” Structural
  sharing background; the grammar-prefix specialization here does not adopt a runtime parse chart.
- **L4.** A. Mazurkiewicz (1987), “Trace Theory”; P. Godefroid (1996), *Partial-Order Methods for the
  Verification of Concurrent Systems*; F. Baader and T. Nipkow (1998), *Term Rewriting and All That*.
- **L5.** R. Kaplan and M. Kay (1994), “Regular Models of Phonological Rule Systems”; M. Mohri (1997),
  “Finite-State Transducers in Language and Speech Processing”; K. Beesley and L. Karttunen (2003),
  *Finite State Morphology*.

The reports also invoke Held–Karp sequencing DP, Tomita-style shared forests, Minato's ZDDs, graph
coaccessibility, and PC-KIMMO/AMPLE/XAmple. These are useful vocabulary for the considered designs;
none supplies the missing state-equivalence, memory, or arbitrary-HermitCrab encoding proof.
