# Deferred per-template clone in `AnalysisAffixTemplateRule`

**Status:** retained. **Portable:** the observation is; the saving is allocation, so the Rust port gains
little unless it also clones eagerly.

## The observation

`AnalysisAffixTemplateRule.Apply` made an internal engine clone and froze it once per template before trying any
slot, then ran the slot DFS on the clone. The ledger measured 66% of those clones discarded unused
(2,505 created, 1,659 discarded, Amharic) and closed the "defer" option because it missed a 70% gate by
four points, choosing "make a clone cheaper" instead.

Reading the code shows the clone had exactly one load-bearing job. The input is already frozen at every
call site; slot rules never mutate their input (`ApplyRhs` clones `match.Input`); intermediate `Source`
pointers are overwritten by the stratum. The only path that emits the input object itself is the
all-slots-skipped terminal, and `Apply` then mutates every emitted word's `SyntacticFeatureStruct`, so
that one path needs a private copy.

## The change

Run the DFS on the frozen input directly (untraced path). At the two emission points that can carry the
original input through unmodified (`ApplySlots` terminal, `ParallelApplySlots` push), emit a frozen
clone when the word is reference-equal to the input. The traced path keeps the eager clone because the
trace manager stamps `CurrentTrace` onto the words it is handed. A naive "no clone anywhere" version was
tried first and failed the new tests immediately (`FeatureStruct.Add` threw on a frozen structure).

## Measured and verified

Alternating wall-time A/B against the pinned baseline, 45 Sena words, min of two interleaved rounds:
1.054x overall, heavy words 1.02x to 1.08x; parity clean on all words and both rounds. Smaller than the
ledger's framing suggested: the eager clone was not where the heavy words' time went.

Focused template tests cover required/optional slots, traced execution, and both sequential and parallel
paths. The complete conformance-inclusive HermitCrab run at the final code tip passed 603 tests with one
expected host-privilege skip.
