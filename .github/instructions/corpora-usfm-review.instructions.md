---
name: corpora-usfm-review
description: Review corpus and USFM changes for deterministic marker/token behavior, ScriptureRef and versification correctness, Unicode handling, and safe file inputs.
applyTo: "src/SIL.Machine/Corpora/**/*.cs"
---

- Trace changed behavior from source text/file or corpus row through tokenization,
  parsing, ScriptureRef/`ScrVers` conversion, update handling, and emitted text.
- Use ordinal comparison for marker, token, identifier, and protocol identity unless the
  code's contract is explicitly linguistic or user-facing. Do not blanket-replace
  culture-aware comparisons; justify the semantic choice.
- For USFM/versification changes, add paired input/output or reference assertions
  covering the affected book/chapter/verse mapping, marker nesting, empty/malformed
  input, and Unicode case relevant to the change.
- Check that missing, duplicate, or ambiguous references fail or resolve according to
  the existing contract. Do not treat a parser snapshot as proof of visual rendering
  parity.
- For files, ZIPs, and streams, preserve entry/byte limits, path validation, disposal,
  cancellation, and actionable errors.
- Use existing corpus/USFM test helpers and run focused tests plus the normal test/build
  checks. Report any fixture, full-suite, or culture/platform evidence not run.
