---
name: measure-authoring-quality
description: >-
  Use when asked to measure, score, or benchmark the conformance authoring harness — "how good is
  the author skill", "what's our first-pass yield", "run the harness eval", "measure before and
  after a skill revision". Runs a batch of obligation cells through author-coverage-cell without
  intervention, scores first-pass yield against the production gate, and attributes every failure to
  a class. Produces a run record; changes no skill and fixes no fixture.
---

The authoritative instructions for this skill live with the fixtures, because they ship to whoever
receives `conformance/`. Read and follow:

`conformance/skills/measure-authoring-quality/SKILL.md`

Do not follow a summary of it from memory; open the file.