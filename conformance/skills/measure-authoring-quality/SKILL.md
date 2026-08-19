---
name: measure-authoring-quality
description: >-
  Use when asked to measure, score, or benchmark the conformance authoring harness — "how good is
  the author skill", "what's our first-pass yield", "run the harness eval", "measure before and
  after a skill revision". Runs a batch of obligation cells through author-coverage-cell without
  intervention, scores first-pass yield against the production gate, and attributes every failure to
  a class. Produces a run record; changes no skill and fixes no fixture.
---

# Measuring authoring quality

You are measuring the **instructions**, not the agent and not the corpus. The output is a number and
a histogram that tell someone which sentence to rewrite.

## The metric

**First-pass yield: the fraction of attempted cells that pass the production gate on first
submission, with zero reviewer feedback.**

The gate is the arbiter. Not your judgment, not a reviewer's, not the authoring agent's self-report.
A cell counts only when `DataflowClaimGate` accepts it against a fresh recomputation.

## Procedure

1. **Pick the set.** Reference cells while iterating; golden cells only when reporting a headline
   number. Never inspect golden cells while revising — that is what makes them golden.
2. **Run each cell through `author-coverage-cell` cold.** No hints, no clarifications, no
   mid-attempt corrections. The moment you help, you are measuring yourself.
3. **Score against the gate.** Pass or fail, nothing in between. A cell that reaches `Unknown` is a
   fail.
4. **Classify every failure** into exactly one class:

| class | signature |
|---|---|
| `wrong-grammar` | chose a fixture that cannot host the construct |
| `not-minimal-pair` | the cases differ in more than one condition |
| `no-witness` | the authored word does not flip under severance |
| `unrealistic-word` | form or gloss does not fit the grammar's shape |
| `bad-cell-id` | claimed a cell that does not exist |
| `evidence-mismatch` | inline `before`/`after` disagree with recomputation |

If a failure fits none, add a class rather than forcing it — a new class is a finding.

5. **Append to the run record**, one row per attempt:

```
attempt_id  cell_id  skill_version  set  phase_reached  outcome  failure_class  notes
```

`skill_version` is mandatory. Yield is only comparable across runs when you know which instructions
produced it.

## Reporting

Report FPY as a fraction with its denominator (`4/12`, never "33%"), the failure histogram, and the
dominant class. Name the single class you would fix first and why.

Also report **run-to-run stability**: if the same cell passes on one run and fails on the next with
identical instructions, the measurement is unreliable and that outranks the yield number. Say so
prominently — a metric that moves without a cause is worse than no metric.

## Never

- **Never help mid-attempt.** A rescued attempt is not a first-pass, and recording it as one
  destroys the only thing this measures.
- **Never tune the measurement.** If yield is low, that is the finding. Accepting `Unknown` as a
  pass, or letting a reviewer correct before the gate runs, produces a better number and a worse
  suite.
- **Never revise a skill here.** Measuring and revising in one pass means you cannot attribute the
  change. Hand the record to `revise-coverage-harness`.
- **Never fix the fixture.** A failed attempt is data. Repairing it by hand loses the data and
  inflates the next run.
