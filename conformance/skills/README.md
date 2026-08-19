# The authoring harness

Four skills, one measurement, one feedback loop. The point is not that an agent *can* author coverage
— it is that we can say **how often it gets it right unaided**, and improve that number deliberately
rather than by intuition.

Skills live here rather than only in `.claude/skills/` because they ship with the fixtures. A
consumer or a future maintainer receiving `conformance/` gets the instructions that produced it.

## The four skills

| skill | role | phase |
|---|---|---|
| `author-coverage-cell` | given an unsatisfied obligation cell, author the words and the claim that satisfy it | do |
| `review-coverage-claim` | judge role attribution on an authored claim | check |
| `measure-authoring-quality` | run a batch, score first-pass yield, attribute failures | measure |
| `revise-coverage-harness` | diagnose which instruction failed and revise the skills | improve |

They form a loop: author → review → measure → revise → author. That shape is borrowed rather than
invented; it is the compile/evaluate/improve cycle DSPy uses for prompts, and the
capability-eval-to-regression-eval progression the agent-skill literature describes.

## The quality signal: first-pass yield

**FPY = the fraction of attempted cells that pass the production gate on first submission, with zero
reviewer feedback.**

That is the number this harness exists to move. Not "can an agent do it with enough hand-holding" —
with enough hand-holding the answer is always yes and the measurement is worthless. FPY asks whether
the *instructions* are sufficient.

The gate is the arbiter, not a reviewer and not the authoring agent's own judgment. A cell counts as
first-pass only when `DataflowClaimGate` accepts it against a fresh recomputation.

### Failure attribution is the actionable part

A bare pass rate tells you the harness is weak; it does not tell you which sentence to fix. Every
failure is classified:

| class | what it means | which instruction is at fault |
|---|---|---|
| `wrong-grammar` | chose a fixture that cannot host the construct | grammar-selection guidance |
| `not-minimal-pair` | the two cases differ in more than one condition | the MC/DC explanation |
| `no-witness` | the authored word does not flip under severance | the witness requirement |
| `unrealistic-word` | form or gloss does not fit the grammar's shape | the realism constraint |
| `bad-cell-id` | claimed a cell that does not exist | the cell-id vocabulary |
| `evidence-mismatch` | inline before/after disagree with recomputation | the evidence-recording step |

A class with a high rate is a documentation defect, not an agent defect. That is the whole premise
of the revise step.

### Reference set and golden set, held apart

Tuning the harness on the cells you measure it with makes FPY meaningless — the number goes up
because the instructions memorised those cells.

- **Reference cells** — used while iterating. Look at them freely.
- **Golden cells** — held out. Only ever run to report FPY, never inspected while revising.

A revision that raises reference FPY but not golden FPY has overfitted, and that gap is the signal
that it has.

## Output format

Every run appends to a machine-readable record so the revise step consumes data, not recollection:

```
attempt_id  cell_id  skill_version  set(reference|golden)  phase_reached  outcome(pass|fail)  failure_class  notes
```

`skill_version` matters: FPY is only comparable across runs if you know which instructions produced
it. Bump it on every revision.

## The loop, and when to stop

1. Run `measure-authoring-quality` over the reference set. Record FPY and the failure histogram.
2. Run `revise-coverage-harness` on the dominant failure class. One class at a time — revising three
   at once means you cannot attribute the change.
3. Re-measure on reference. If it moved, measure golden.
4. Stop when golden FPY is high enough that reviewing is cheaper than re-authoring.

There is no target number here yet, because none has been measured. Setting one before the first
measurement would be inventing a threshold and then discovering it.

## What this harness must never do

**The gate stays the authority.** No skill here may mark a cell covered, weaken a gate, or edit an
expectation to make a claim true. An authoring agent that cannot satisfy a cell reports that it could
not — a failed attempt is data, and a fabricated success poisons both the corpus and the measurement.

**Do not tune the measurement.** If FPY is low, that is the finding. Lowering the bar — accepting
`Unknown` cells as passes, or letting the reviewer pre-correct before the gate runs — produces a
better number and a worse suite.

## Prior art

- **DSPy** — declarative signature, explicit metric, optimizer that compiles instructions against it.
  The discipline worth copying: engineer the metric first, because it is what everything else
  optimises toward.
- **Agent-skill evaluation practice** — goldens as input/expected-output pairs; capability evals that
  start with a low pass rate and graduate into regression evals once they approach 100%.
- **SkillAxe** (arXiv 2606.10546) — decomposes skill quality into quality impact, trigger precision,
  instruction compliance with fault attribution, and solution-path coverage. Our failure-class table
  is the fault-attribution idea applied to this domain.
