# Two systems: the production gate, and the review calibrator

Checking coverage claims splits into two systems with different subjects, different costs, and
different lifetimes. Conflating them produces something both too slow to run always and too weak to
trust — the first draft of this document did exactly that.

| | **Gate** | **Calibrator** |
|---|---|---|
| subject | the claims | the review harness |
| decides | does this claim hold | can an agent judge claims reliably, given our docs and format |
| runs | every build | when the harness, docs, or artifact format change |
| AI in the loop | none | that is the point |
| output | pass or fail | a per-defect-class catch rate |

## System A: the gate

Deterministic, always-on, no AI. It checks everything mechanically checkable and nothing else:

- The claimed cell id exists in `conformance/dataflow-obligations.tsv`. Catches typos and ids left
  behind when a chain changes.
- The claim's inline `before`/`after` match a fresh recomputation. Disagreement means **stale**:
  reported loudly, cell reverts to unreviewed. Not a build failure — evidence moving is a review
  task, not a defect.
- The cell's ledger status is `Satisfied`. A claim on an unwitnessed cell fails outright: the author
  intended a witness and did not get one.
- `distinct_from` names a word that exists, claims a cell of the same chain, and has a **different**
  outcome. This is the MC/DC independence check, and it is mechanical.
- Cited grammar lines resolve, and the cited elements really carry the payload and the gate.

What it deliberately does not check is **prose**. `proof:` is for a human or a reviewing agent; no
gate can judge whether an explanation is *right*. Prose can therefore rot — mitigated only by sitting
directly beside inline values that are checked, so a drifted explanation is visible next to evidence
that is not.

The gate is the production system. It is the whole production system.

## System B: the calibrator

Its subject is the **harness** — the reviewer prompt, `docs/coverage-strategy.md`, the artifact
format, the cell-id scheme. Claims are only the test material.

Method: take known-good claims, generate **mutants** by perturbing exactly one thing, run the
reviewer blind, and score it per mutant class.

Mutant classes, each probing a different capability:

| mutant | what it tests |
|---|---|
| role swapped (`PresentGatedForm` → `AbsentGatedForm`) | does the reviewer check polarity against the reader |
| `distinct_from` repointed to a word differing in two conditions | does it check independence, not just existence |
| `before`/`after` altered to a plausible but wrong outcome | does it derive the outcome or read it |
| `proof` prose swapped in from a different cell | does it read prose against values, or against itself |
| a card citation repointed to a line that does not declare the payload | does it open the grammar, or trust the card |
| a genuinely correct claim | false-positive rate |

The score is the deliverable: catch rate per class. A class the reviewer misses is not a reviewer
problem to scold — it is a **documentation or format problem to fix**. If bad counterparts get through,
the counterpart requirement needs to be more prominent, or the artifact needs to make the differing
conditions visible rather than requiring them to be inferred.

Then re-run and see whether the number moved. That is the loop, and it is why this system exists: it
converts "the guidance feels unclear" into a measurement.

### Rule that keeps the two apart

**A measured catch rate is the precondition for letting a reviewer near production, not a substitute
for the gate.** If AI review is ever added to the pipeline, it enters as an advisory signal that
flags claims for human attention — never as something that can mark a cell covered. The gate stays
the authority regardless of how well any reviewer scores.

## Note on mutation testing

This repository previously rejected mutation testing for its Rust code, on the grounds that it finds
missing *assertions* while the real defects were missing *cases*, and that cost-only mutations
survive a semantics suite. That reasoning does not transfer here, and the difference is worth being
explicit about: mutating **claims** is a direct test of the thing under measurement, because catching
wrong claims is precisely the reviewer's job. The earlier objection was that mutants did not
correspond to real defects; here the mutants *are* the defect class.

## The reviewer's own instructions

Used by System B, and by any future advisory use. Four checks:

**1. Predict before reading.** From the grammar and the word, state the outcome with the payload
intact and with it severed — before reading the claim's recorded values. Then compare. Confirming a
visible answer is nearly free and an agent will do it convincingly either way; predicting one
requires deriving the behaviour. A wrong prediction means "do not approve", stated plainly.

**2. Verify the citations — do not merely read them.** The evidence card already carries the
`grammar.xml` line where the payload is declared and the line where the reader gates on it, extracted
mechanically. Open the grammar and confirm each cited line really declares what the cell's chain
names.

This is a change from an earlier draft, which asked the reviewer to *produce* the citations. Machine
extraction is more reliable, but it removes an anti-faking property: a fabricating agent used to give
itself away with wrong line numbers, and now the numbers are handed to it. The replacement test is
semantic — the cited line must carry the payload the cell names, on the element the chain names — and
it still requires opening the file, so the calibrator can probe it by corrupting a citation and
seeing whether the reviewer notices.

**3. Check the counterpart.** `distinct_from` asserts a minimal pair differing in exactly one
condition. Two words that both parse as expected but differ in *two* conditions establish nothing —
and read perfectly well, which is what makes this the easiest defect to wave through.

**4. Read prose against values.** `proof:` must describe the same event the `before`/`after` values
record. Prose that is true but about something else is a failure, not a nitpick.

Declining is a normal outcome. "I could not determine whether the counterpart differs in one
condition or two" is a useful review; a confident yes covering that uncertainty is worse than no
review at all. Never edit a claim, word, expectation, or grammar to make a claim true — report the
discrepancy, because an adjusted expectation destroys the finding.
