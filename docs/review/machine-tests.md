# Machine Tests Review

*Review machine tests as evidence for changed behavior, edge cases, contracts, and
resource/cancellation boundaries.*

Governs `tests/**/*.cs`.

- A test must exercise the changed behavior, not merely execute the changed method.
  Identify branches, guards, ordering, error paths, cancellation, limits, and side
  effects in the production diff.
- Prefer focused NUnit tests in the affected test project. Keep fixtures deterministic
  and avoid sleeps, ambient machine state, or localized selectors.
- For string/token/marker/USFM behavior, include relevant Unicode, empty, malformed,
  nested, and non-English-culture cases. Do not use a culture-sensitive assertion helper
  when the contract is ordinal identity.
- For async code, assert cancellation and completion behavior where the change promises
  it; do not hide unobserved tasks.
- For HermitCrab changes, compare analysis semantics, not only execution success.
  Exercise state-sensitive merging, resource caps, and parallel/sequential
  equivalence when touched.
- Name the test that proves the change. "Where is the test?" is the single most
  common review question in this repository; answer it before it is asked.
