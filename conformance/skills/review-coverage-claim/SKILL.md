---
name: review-coverage-claim
description: >-
  Use when asked to review, verify, certify, or check a coverage claim in the HermitCrab conformance
  suite — "review this claimed_cells entry", "does this word really demonstrate that cell", "check
  the role attribution", or when handed an evidence card from conformance/evidence-cards/. Judges
  ONLY role attribution: whether the word occupies the cell it claims. Never sets a status, never
  edits a claim, never edits a grammar or an expectation. Declining is a valid outcome.
---

# Reviewing a coverage claim

The machinery has already established a **flip**: severing an attribute changed a word's outcome.
That is recomputed every run and you cannot affect it.

You are judging **role attribution** — whether this word occupies the cell it claims. That needs
three facts the flip does not carry: the stem really carries the payload, the named rule really is
the gated one, and the observed polarity is the one that reader implies.

**Your verdict never sets a status.** It is recorded as an opinion beside machine-established facts.
If you approve and recomputation disagrees, recomputation wins and your approval is discarded.
Nothing you write can make a cell count as covered.

## The four checks

**1. Predict before you read.** From the grammar and the word, state what the outcome will be with
the payload intact and with it severed — *before* reading the card's `before`/`after`. Then compare.

Confirming a visible answer is nearly free, and you will do it convincingly whether or not you
understand the gate. Predicting requires deriving the behaviour. **If your prediction was wrong, say
so and do not approve** — a wrong prediction means you did not understand the claim well enough to
certify it.

**2. Verify the citations; do not merely read them.** The card carries the `grammar.xml` lines where
the payload is declared and where the reader gates on it, extracted mechanically. Open the grammar
and confirm each cited line really declares what the chain names. The numbers are handed to you, so
the test is semantic: does that line carry that payload, on that element.

**3. Check the counterpart.** `distinct_from` asserts a minimal pair differing in **exactly one**
condition. Verify it. Two words that both parse as expected but differ in *two* conditions establish
nothing — and read perfectly well, which makes this the easiest defect to wave through and the one
worth spending your attention on.

**4. Read the prose against the values.** `proof:` must describe the same event the `before`/`after`
record. Prose that is true but about something else is a failure, not a nitpick.

## How to decline

Say which check failed and why. **A claim you cannot verify is not approved, and that is a normal
outcome.** "I could not determine whether the counterpart differs in one condition or two" is a
useful review; a confident yes covering that uncertainty is worse than no review at all.

Some batches contain deliberately wrong claims. Approving one voids your other verdicts in the same
batch, so guessing costs more than declining.

## Never

- **Never edit** a claim, word, expectation, or grammar to make a claim true. Report the discrepancy.
  An adjusted expectation destroys the finding.
- **Never review the authored file against itself.** The grammar and the recomputed severance are
  ground truth; `words.yaml` is the claim under test, never evidence for itself.
- **Never infer a status.** `Satisfied`/`NotSatisfied`/`Unknown` are machine-established and appear
  on the card already labelled as such.
