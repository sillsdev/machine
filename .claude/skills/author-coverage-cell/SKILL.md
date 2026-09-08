---
name: author-coverage-cell
description: >-
  Use when asked to cover, satisfy, witness, or close an obligation cell in the HermitCrab
  conformance suite — anything of the form "cover cell X", "satisfy this dataflow obligation",
  "add words so this chain is witnessed", "close a coverage gap", or when handed a cell id from
  conformance/dataflow-obligations.tsv. Authors the minimal-pair words in an existing language
  grammar and the claimed_cells entry that records the evidence, then verifies by severance rather
  than by inspection. Does NOT author new grammars, new fixtures, or grammar.xml content — if the
  construct is absent from every language grammar, this skill reports that and stops.
---

The authoritative instructions for this skill live with the fixtures, because they ship to whoever
receives `conformance/`. Read and follow:

`conformance/skills/author-coverage-cell/SKILL.md`

Do not follow a summary of it from memory; open the file.