# Corpora and USFM Review

*Review corpus and USFM changes for deterministic marker/token behavior, ScriptureRef
and versification correctness, Unicode handling, and safe file inputs.*

Governs `src/SIL.Machine/Corpora/**/*.cs`.

- Trace changed behavior from source text/file or corpus row through tokenization,
  parsing, ScriptureRef/`ScrVers` conversion, update handling, and emitted text.
- Use ordinal comparison for marker, token, identifier, and protocol identity unless the
  code's contract is explicitly linguistic or user-facing. Do not blanket-replace
  culture-aware comparisons; justify the semantic choice.
- Reference and versification arithmetic is the defect class that recurs most here
  (`4e889539`, `54687760`, `8d924c1a`, `f9ba7bb7`, `78350670`). For any change that
  touches it, add paired input/output or reference assertions over the affected
  book/chapter/verse mapping, marker nesting, empty and malformed input, and the
  relevant Unicode case.
- Check that missing, duplicate, or ambiguous references fail or resolve according to
  the existing contract. Do not treat a parser snapshot as proof of visual rendering
  parity.
- For files, ZIPs, and streams, preserve entry/byte limits, path validation, disposal,
  cancellation, and actionable errors.
