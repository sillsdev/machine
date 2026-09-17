---
name: machine-tests-review
description: Review machine tests as evidence for changed behavior, edge cases, contracts, and resource/cancellation boundaries.
applyTo: "tests/**/*.cs"
---

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
- For HermitCrab changes, compare analysis semantics, not only memo-hit counts or
  execution success. Exercise key completeness, replay, resource caps, and
  parallel/sequential equivalence when touched.
- For public API changes, include compile/use coverage for the changed signature and
  document any consumer or target-framework evidence that was not available.
- Run the focused test command, `dotnet test --verbosity normal`, and coverage
  collection when useful. Record filters, skipped tests, failures, and unverified
  changed lines. A global coverage percentage is not changed-line evidence.
