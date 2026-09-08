# Decisions and lessons

Most of the reasoning behind this suite's design exists only in the commit messages that built it —
long, argued, and never read again once buried in history. This document mines that record. Every
entry names what was believed, what turned out to be true, how that was established, and what now
stops it recurring. Where a thread could not be pinned to a specific commit, that gap is stated
rather than papered over.

## The pattern: presence reported as evidence

The single idea this suite got wrong more than once, in more than one shape, is mistaking *a
construct is declared and loads cleanly* for *a construct was shown to do something*. Four concrete
instances of it are on record with commits; a fifth is referenced only in the documents this one
supports.

**1. The `RequiredToLoad` verdict.** The original counterfactual gate counted a mutation that made a
grammar fail to *load* at the same strength as one that changed a *parse* — 78 of 264 unit surfaces,
40% of everything resolved, credited this way. `3d724023` split it in two: `RequiredByDtd` (the
mutant fails generic DTD content-model validation before any HermitCrab-specific code runs — this
only re-derives the DTD's own content model, proving nothing about the engine) and `RequiredByLoader`
(the mutant passes DTD validation but fails inside the loader itself — an `IDREF`/lookup/coercion
failure, a genuine engine-semantic fact, though still not a word-level witness). The split was
mechanical, not hand-labeled: which of the 78 fell into which bucket was determined from the
mutation's own recorded kind (`GrammarMutator.DeletedElements`/`EmptiedChildren` for `RequiredByDtd`,
`RewroteAttribute` for `RequiredByLoader`), and falsified in the same commit by inverting the
classifying condition and confirming two known cases flip.

**2. The interface inventory's `exercised` column.** `interface-inventory.tsv` originally called an
IDREF/IDREFS attribute "exercised" whenever it resolved to a real reference somewhere in the corpus —
structural presence, nothing about whether severing it changed anything. `47b0c30f` renamed the
column to `present` and built `interface-witness.tsv` to run the real severance test per (element,
attribute, fixture) triple. The gap was not small: of 42 present interfaces, only 15 are ever actually
witnessed. Framed the other way, the fold-in candidate count computed under the old predicate (2) was
mostly wrong once witness was required (1) — one of the two "exercised" interfaces turned out to be
present but permanently inert (`SymbolicFeature.defaultSymbol`).

**3. A fixture design that exercises without witnessing.** `347c54987` corrects a wrong
generalization about how many cases a fixture needs — "two entries" had been stated as a rule rather
than derived from what was being attributed. With only a gated stem, severing the gate changes the
outcome — but so would a rule that never fires at all, and a single-arm fixture cannot tell those
apart. `mpr-gated-exception`'s own notes already called its second, ungated entry "a plain control";
the commit generalizes that vocabulary: the count needed is whatever leaves exactly one explanation of
the observed difference standing (one, when the counterfactual itself supplies the contrast; four for
a gate — two conditions, present and absent, each needing its own control; three for a mutation caught
in transit between a write and a read). A fixture built to the wrong count exercises a construct and
witnesses nothing, indistinguishable from a correctly designed one until someone asks what it would
take to fool it.

**4. An internal design document's own baseline table.** An early version of the data-flow coverage
design measured its own starting point — all-defs, all-uses, kill-paths — using
`interaction-chains.tsv`'s `exercised` column:
the same identifier appearing in both a writer's and a reader's attribute values somewhere in one
fixture, with no ordering, no reachability, and no parse-level evidence. `56ac9773` recomputed it
against the real severance sweep instead: all-uses fell from a published 20/40 to a witness-grade
11/40. The commit names this explicitly as the fourth occurrence of the same mistake, inside the very
document written to abolish it, and states the pattern once rather than just the instance: **the weak
predicate is always the one already computed, and it is always the cheaper reach.**

**5. An agent's headline.** `what-it-claims.md` and the commit shipping it (`ac384820`) both state
that this suite got the presence/evidence distinction wrong five times, the other four being the ones
above. Neither names which commit the fifth is, and it could not be independently located in the
commit range examined for this document. Recorded here as an acknowledged gap rather than a guessed
citation: if a future reader finds it, this line should be replaced with the commit and its evidence.

**What now prevents a sixth.** No single gate closes this off, because the failure is a habit of
reasoning rather than a bug in one ledger. What exists instead is the vocabulary and the standing
question, applied at every later layer: the inclusion rule — *test every mechanism that can change a
parse; do not test one that cannot, unless it sits in a chain that does* — and the requirement that a
human "yes" on a coverage cell carries a hash of the machine evidence it was shown and lapses the
moment that evidence moves, so
witnessed and reviewed can never quietly collapse into one status again.

## The rule-interaction-pair ledger's demotion

`13ea28c2` introduced `rule-interaction-pairs.tsv` to answer a real question: coverage measured per
surface cannot see a defect that needs several surfaces at once (the MPR-overwrite defect below is
exactly such a case), so interactions needed their own denominator — every pipeline-permitted ordered
pair of rule-bearing units within a stratum, 1,290 pairs across 39 strata at introduction.

The ledger looked like a coverage denominator and was initially treated like one, but it has a
property no denominator can have: it grew every time a fixture was added. `0f0a32f5c` records the
count moving to 1,305 when four more fixtures landed; `b22c038e` names the consequence plainly — it is
"not a coverage denominator," because 1,217 of those 1,305 rows are `Undetermined` by construction
(the disjointness check that would resolve a row only applies to phonological-rule pairs; every other
unit-kind combination cannot be classified at all), and a number that rises when the corpus grows is a
statistic about the corpus, not a bound on the engine. `6b6f7ae4` separately corrects three arithmetic
errors that had crept into the pinning comment along the way (a wrong stratum count, a stale total, and
a citation of the wrong estimate), restating the whole thing as "a corpus statistic, not a bound."

The ledger was not deleted — it does a real job, just a smaller one. It was demoted explicitly to
**per-grammar pruning**: given one grammar, which adjacent rule swaps are even
worth running a counterfactual on, and which are provably inert. It is good at that question and is
kept for it; it simply stopped being asked the coverage question once that question had a real answer
(the interaction-chain and obligation-cell layers, layers 3 and 4 of `how-it-is-computed.md`).

## Name-prefix heuristics failing twice

Two different classification questions in this suite were first answered by guessing from an
attribute's name, and both guesses were wrong in ways only a real grammar shape exposed.

**Write/read direction.** `5a78c69` built the first interface inventory and classified each
IDREF/IDREFS attribute's direction — write, read, or bare reference — from its name (does it start
with `output`, `assigned`, `required`, and so on). `86d3bbf4` replaced it with
`SemanticInterfaceDirection`, a table checked against the engine's actual read/write sites for all 60
declared interfaces, after a real field grammar showed the naming convention wrong:
`LexicalEntry.ruleFeatures` reads as a bare reference under the "ref" prefix rule, but the loader
unions it straight into the same live MPR-feature set every gate reads from — it is the lexicon's own
*write* of a lexical entry's initial feature set, not a reference to one. Fixing the one reported case
and then applying the same engine-checked test to everything else surfaced two more misclassified
attributes (`LexicalEntry.partOfSpeech`, `Allomorph.stemName`) and one entirely new junction
(`StemName`) the prefix heuristic could never have produced, since it doesn't correspond to any
"write"-looking prefix at all. Even after the replacement shipped, the same bug resurfaced one call
site over: `cc4dc0eb` found that `interface-inventory.tsv`'s own junction-counting code still
classified direction by prefix internally, so `Allomorph.stemName` never got asked the junction
question in that code path either. The fix was structural, not another patch: point that code at the
same `SemanticInterfaceDirection` table instead of re-deriving its own answer, so the two ledgers
cannot disagree about direction again by construction.

**Gate-ness.** The first version of the data-flow obligation plan classified a chain as "gated" (and
therefore subject to MC/DC) only when its reader was a `required*`/`excluded*`-named attribute — the
same shape of naming-convention shortcut, applied one layer up. `56ac9773` and `bede48be` replace it:
`SemanticInterfaceDirection`'s own definition of a *read* already means "the engine makes a
control-flow decision on this value," which is gating by definition, so **all 40 chains gate**, not
just the ones with a suggestive name. Twelve chains have `head*`/`nonHead*` readers that gate in engine
code exactly as much as any `required*` one does; the name told nothing about it either way.

Both fixes point at the same underlying rule, now load-bearing across this suite: a classification
that could instead be checked against engine behavior must be, and a naming convention is only ever a
first guess, never the answer.

## A tie-break that hid 22 obligations

`CounterfactualLedger` recorded, per surface, only the single best-verdict fixture — and on a tie
between two fixtures both reaching `Evidenced`, it kept whichever was discovered first.
`Fixture.DiscoverAll` sorts by id, and `edge-cases` sorts before `languages` alphabetically, so on
every such tie the fabricated fixture was recorded and a language-family grammar witnessing the exact
same surface was silently dropped. The ledger could answer *which fixture was recorded first*; it
could never answer *which fixtures witness a surface*, which is the question the fold-in strategy
actually needed.

`138559e2` fixed it by keeping winner selection untouched (so no existing consumer's answer moves) and
adding a `witnessed_by` column naming every fixture reaching the best verdict, not just the recorded
one. The effect on the number that was actually driving strategy: the count of obligations witnessed
*only* by a fabricated edge case fell from 30 to 8. Twenty-two obligations had a language grammar
witnessing them the whole time, with words already sitting in the repository, and the ledger's own
tie-break was the only thing standing between that fact and the fold-in plan.

The same commit also removed a `TEMP-BEFORE-MEASUREMENT` marker that had sat unchanged since the
founding commit of the whole coverage apparatus, guarding a `bestSoFar` dictionary that was written and
never read — a short-circuit that, on inspection, skipped nothing at all. The reason it had to go
rather than simply be renamed: exhaustive evaluation of every fixture against every surface is the only
correct behavior here, because a short-circuit would suppress exactly the ties this fix depends on
being able to see. A marker that says "temporary, revisit before trusting this" is only useful if
someone eventually revisits it; this one sat for the suite's entire history without being read for the
thing it was guarding.

## Why all-uses, not all-du-paths — and a citation that had to be withdrawn

`f961c321` adopted data-flow coverage (Rapps and Weyuker, 1985) and MC/DC (DO-178C) as the criteria
for the interaction layer, and set the target at **all-uses plus MC/DC on every gate plus every
kill-path witnessed** — deliberately short of **all-du-paths** (every distinct path from a write to a
read, not just one def-clear path per pair). The commit's original justification for stopping short
cited NIST SP 800-142, reading it as evidence that most faults come from a small number of interacting
factors and that the marginal yield of full path coverage doesn't justify its cost.

`56ac9773` withdrew that citation and replaced it with the honest one. SP 800-142 is a combinatorial-
testing standard about *input-parameter* interaction, not path coverage, and read on its own terms it
argues the *opposite* direction for this exact case: a (writer, mutator, reader) triple is a 3-factor
interaction, squarely inside the 1-to-3-factor band the standard says accounts for most faults — which
would mandate covering the *full* kill-path space, not license a short list from it. The corrected
reasoning states it in the terms that actually hold: the real justification is the classical one
already inside the data-flow literature itself — path explosion, and the cost of proving an infeasible
path infeasible — not a borrowed citation from an adjacent field that does not transfer. The correction
also records, without hiding it, that the criterion was chosen *after* the baseline measurement was
already in hand (`interaction-chains.tsv` landed a day before the corrected reasoning was written),
which does not make all-uses the wrong bound but means it should never be presented as though it
preceded the number it explains.

## Taking shape, never scale, from a real grammar

`ea42ff499` measured one real grammar — a Bantu language converted to `HermitCrabInput` XML — against
the entire 33-fixture synthetic corpus: 224 morphological rules, 415 subrules, and 93 slots in that one
grammar against 137, 152, and 46 across everything this suite had built, at roughly 1MB against an
11KB median fixture size. That one grammar also exercised two interfaces the whole synthetic corpus
never had — both on the MPR-to-phonology gate. **One real grammar outweighed the whole corpus**, in
both what it revealed and in sheer size.

The size difference is exactly why it was never imported wholesale. `99754f4b` measured the
consequence the other way: of the 42 interfaces the corpus exercises at all, 40 are already reached by
one of the eight `languages/` grammars, and only two depend solely on a fabricated `edge-cases/`
fixture — the fabricated fixtures are pins for specific hazards, a real job, but they are not what
makes the coverage claim true. So the procedure this pair of commits establishes is to mine a real
grammar for **shape** — which constructs genuinely co-occur, in what configuration — and reproduce that
configuration at the **minimal scale** that still witnesses the obligation: where the real grammar
needed 12 MPR features, 14 tagged entries, and 4 gated phonological rules to exhibit a gate, the fixture
needs one feature, one gated rule, and (per the control-count rule above) two entries — one the gate
applies to, one it does not. Importing the real grammar's scale directly would have dominated every
gate that walks the fixture set — several of which regenerate a ledger per fixture — trading a suite
that runs in minutes for one that does not, to buy configuration information a two-entry fixture
already captures in full.

## Other findings worth carrying forward

**The MPR-overwrite defect is the founding example, not a hypothetical one.** `13ea28c2` is where the
whole interaction/data-flow apparatus starts, because per-surface coverage genuinely cannot see this
class of bug: an MPR feature group with `outputType="overwrite"` evicts its whole group instead of
accumulating into it, so on an unordered stratum, two rules writing that group leave different state
depending on which ran last — a rule gated on the evicted member then succeeds under one derivation
order and fails under the reverse, with every individual ingredient already fully evidenced on its own.
The append-mode mirror (the same two orders through a non-overwriting group, both succeeding) is what
makes this attributable to the overwrite semantics specifically rather than to ordering in general.
Every later layer in `how-it-is-computed.md` exists to generalize past this one pinned case.

**Mechanical is not the same as meaningful.** The very first semantic census in this suite's history
was mechanically generated, honestly gated against drift, and
almost useless — complete over a denominator that was roughly 90% XML scaffolding no grammar could
meaningfully vary. Deriving a number does not make it worth measuring; every layer that followed was
chosen specifically so each member is semantically load-bearing, not merely countable.

**A gate and a measuring instrument are different things, and conflating them produces something too
slow to trust and too weak to run always.** `faa092519` separates the deterministic, always-on
dataflow-claim gate (cell exists, before/after values match a fresh recomputation, ledger status is
`Satisfied`) from a separate calibrator that measures whether an AI reviewer can be trusted near that
gate at all, by perturbing known-good claims into deliberate mutants and scoring the reviewer blind per
mutant class. The same commit records why this suite's earlier, unrelated rejection of mutation testing
for Rust code does not transfer here: that rejection held because the mutants there did not correspond
to real defects, whereas here the mutants *are* the defect class under study — a rare case where the
same tool that was rightly rejected for one purpose is exactly right for a different one.

**Closing one hole in a review process can open a different one, and the fix has to name the new
hole rather than pretend it isn't there.** `9f1840b9` moved the evidence-card generator's grammar-line
citations from human-produced to machine-extracted, which is more reliable but costs a real anti-
faking property: a fabricating reviewer used to give itself away with wrong line numbers, and a
machine now hands the correct numbers to it. The replacement is a semantic check instead of a
structural one (the cited line must actually carry the payload the cell names, on the element the
chain names) plus a new calibrator mutant class built specifically to test whether that replacement
catches a citation repointed at a line that doesn't declare the claimed payload. The lesson generalizes:
a fix to a known weakness should be evaluated for what it removes as well as what it adds.
