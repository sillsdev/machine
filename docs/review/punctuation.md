# Punctuation Analysis Review

*Review quotation-mark and punctuation analysis for Unicode safety, malformed
input, and chapter/verse bookkeeping.*

Governs `src/SIL.Machine/PunctuationAnalysis/**/*.cs`.

This area's history is almost entirely crash fixes, so review it for the input
that should not have reached the code rather than for the happy path.

- Index by text element, not by `char`. A surrogate pair, a combining mark, or a
  multi-byte quotation mark must not split - `03621d14` fixed a crash from
  exactly that.
- Assume the chapter or verse is missing, out of range, or unparsable. The
  resolver runs over real Paratext projects: `36a24b57` fixed a crash on an
  invalid chapter and `f9ba7bb7` fixed chapter numbers that came back wrong.
- A depth or state machine that tracks open and close marks must terminate on
  unbalanced input and say what it saw, rather than running to the end of the
  text.
- Treat quotation-mark identity as ordinal. Denormalization maps one code point
  to another; a culture-aware comparison here is a bug.
- Add a fixture for the malformed case with the fix, in
  `tests/SIL.Machine.Tests/PunctuationAnalysis/`. Every fix above was reported
  from live data, not found by review.
