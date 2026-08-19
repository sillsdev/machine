---
name: revise-coverage-harness
description: >-
  Use when asked to improve, revise, or fix the conformance authoring harness after a measurement —
  "first-pass yield is low, fix the skill", "most failures are not-minimal-pair, revise the
  instructions", "improve the harness based on the run record". Consumes a measurement run record,
  diagnoses which instruction produced the dominant failure class, and revises exactly one thing.
  Never revises without a measurement, and never touches the golden set.
---

# Revising the harness

You are fixing **instructions**, on evidence. The premise: a failure class with a high rate is a
documentation defect, not an agent defect. Somewhere a sentence is missing, ambiguous, or buried.

## Preconditions

- A run record from `measure-authoring-quality` exists, with a failure histogram and a
  `skill_version`.
- You have **not** looked at golden cells. If you have, the next golden measurement is compromised
  and you must say so.

**Never revise without a measurement.** A revision based on a hunch cannot be shown to have helped,
and it makes the next number uninterpretable — you will not know whether it moved because of your
change or the one you made on a hunch.

## Procedure

**1. Take the dominant class only.** One class per revision. Fixing three at once means you cannot
attribute the change, and the histogram stops being able to guide you.

**2. Find the instruction at fault.** For each class there is a sentence that should have prevented
it:

| class | where to look |
|---|---|
| `wrong-grammar` | the host-selection phase — is the "both constructs present" test stated as a test, or as advice? |
| `not-minimal-pair` | the MC/DC explanation — is "exactly one condition" prominent, or buried in prose? |
| `no-witness` | the verification phase — is severance framed as *the* result, or as a final check? |
| `unrealistic-word` | the realism constraint — is "plausible for this grammar" defined, or assumed? |
| `bad-cell-id` | the cell-id vocabulary — is the id's structure explained, or only exemplified? |
| `evidence-mismatch` | the evidence-recording step — does it say values are copied from the ledger, never predicted? |

**3. Diagnose before editing.** State which sentence failed and *why an agent following it in good
faith would still have got it wrong*. If you cannot articulate that, you have not found the defect
and adding emphasis will not help.

Three common shapes:
- **Buried** — correct but positioned where it is read after the decision it governs.
- **Advisory** — phrased as a preference where it needed to be a test with a failure mode.
- **Assumed** — a term used without definition ("realistic", "minimal") that the author must guess.

**4. Revise minimally.** Change the one instruction. Resist rewriting the surrounding sections while
you are there — a large revision cannot be attributed either.

**5. Bump `skill_version` and hand back for re-measurement.** You do not measure your own revision;
that is `measure-authoring-quality`'s job and keeping them apart is what makes the number credible.

## Reading the result

- **Reference yield up, golden yield up** — a genuine improvement.
- **Reference up, golden flat** — you have overfitted to the reference cells. Revert or generalise;
  the gap is the evidence, not a coincidence.
- **Neither moved** — the diagnosis was wrong. Say so and pick the next class rather than adding
  emphasis to the same sentence, which is the most common failure of this loop.

## Never

- **Never make the task easier to make the number better.** Weakening what the skill demands raises
  yield and lowers coverage quality. The measurement exists to improve instructions, not to be
  satisfied.
- **Never touch the gate.** If a class of failure looks like a gate defect rather than an instruction
  defect, report it — that is a valuable finding and it is out of scope here.
- **Never revise the measurement skill** to reclassify failures more favourably.
