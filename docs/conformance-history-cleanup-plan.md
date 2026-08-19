# Queued: collapse the conformance history to what we landed on

Not yet executed. This records the intent and the constraint so the operation is a decision someone
takes deliberately, not a rebase someone improvises.

## Goal

The branch history should read as **the decisions we landed on and why**. It should not narrate the
route: the abandoned measurements, the corrected counts, the wrong impossibility proof, the ledger
that was recomputed four times before someone noticed. That material is worth keeping — it is why
`conformance/docs/decisions-and-lessons.md` exists — but it belongs in a document that a reader
chooses to open, not spread across commit subjects that everyone reads whether they want to or not.

The distinction to hold onto: **a lesson is content, churn is not.** Deleting the record of a mistake
from the lessons doc would be a loss. Deleting six commits that walk toward a number we later
corrected is a gain.

## Constraint, and why this is queued rather than done

The branch is 15 commits ahead of `origin/master` and **11 of them are already published** on
`origin/integrate-conformance-framework`. Collapsing below ten therefore requires rewriting published
history and force-pushing. That is a deliberate call, not a tidy-up — anyone who has fetched the
branch gets a divergence — so it waits for an explicit decision and should be announced when it
happens.

The mechanics are settled and safe: set each target commit's tree exactly with
`git reset --hard <boundary>` followed by `git reset --soft <previous>`, then commit. That cannot
throw a conflict, and the result must be verified by comparing the final tree hash against the
pre-squash tip — it has to be identical, and a preserved backup ref makes the operation reversible.

## What the final history should say

One commit per thing we decided, in the order the argument builds:

1. **The suite, its manifest, and the adapter protocol.** The deliverable a consumer actually
   receives: fixtures, per-fixture hashes, and the contract for running an external engine against
   them.
2. **Denominators come from the DTD and the engine, never from the corpus.** A denominator read off
   the corpus measures the corpus.
3. **Presence is not coverage.** A construct can be present, referenced correctly, load cleanly, and
   change no parse. Only severance that changes a parse counts.
4. **Coverage is bounded by three layers, each with an authority.** What the engine does
   (`FailureReason`), what the XML can declare (the DTD), what a real project can produce
   (`HCLoader`). An obligation is worth covering only where all three permit it; failing one is an
   exclusion, not a gap.
5. **MC/DC is keyed to the engine's decision points, not to DTD attribute pairs.** Attribute pairs
   both over-count (four arms per chain, one certifiable) and under-count (six gates no attribute
   reaches).
6. **The authoring harness, and the yield that measures it.** Instructions are the artifact under
   test; first-pass yield against the production gate is how we know whether they work.
7. **The sweep is reproducible and the gates run in CI.** Pinned processor count for determinism,
   failure attribution, and a workflow where there was none.
8. **The published claim is a funnel.** 346 enumerated, 28 certifiable, 18 producible, 4 satisfied —
   stated in the form that tells a reader what remains.

## What must survive the collapse

- Every checked-in ledger and its generating flag.
- `decisions-and-lessons.md`, unabridged. The mistakes are the most transferable thing here.
- The three layer authorities named with citations, since a verdict nobody can re-derive is a verdict
  nobody can trust.
- The measured numbers, with their sources. Not one of them may become an estimate in the retelling.
