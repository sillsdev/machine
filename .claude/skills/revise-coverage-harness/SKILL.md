---
name: revise-coverage-harness
description: >-
  Use when asked to improve, revise, or fix the conformance authoring harness after a measurement —
  "first-pass yield is low, fix the skill", "most failures are not-minimal-pair, revise the
  instructions", "improve the harness based on the run record". Consumes a measurement run record,
  diagnoses which instruction produced the dominant failure class, and revises exactly one thing.
  Never revises without a measurement, and never touches the golden set.
---

The authoritative instructions for this skill live with the fixtures, because they ship to whoever
receives `conformance/`. Read and follow:

`conformance/skills/revise-coverage-harness/SKILL.md`

Do not follow a summary of it from memory; open the file.