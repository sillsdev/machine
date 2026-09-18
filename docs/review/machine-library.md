# Machine Library Review

*Review shipped library code for compatibility, deterministic comparison, async
contracts, and disposal.*

Governs any `src/**/*.cs` no more specific rules file claims.

- This is shipped library code, consumed as `netstandard2.0`. Check public and
  protected API shape, overloads, optional parameters, and return types when they
  change; do not introduce an API that silently drops existing consumers.
- Use ordinal comparison for markers, tokens, identifiers, and protocol text.
  Reserve culture-sensitive comparison for genuinely linguistic operations. This
  is the defect class review misses most often here: `075c6ea1`, `dac2d895`, and
  `418ff225` all shipped it.
- Existing async APIs use `Task` and `CancellationToken`; corpus and tokenizer
  APIs are synchronous. In changed async code verify cancellation propagation,
  ordering, and disposal.
- Dispose engines, models, trainers, and streams according to their contracts,
  and check who owns a stream that is passed in - `5a488c01` and `37e13b79` were
  both ownership bugs.
- Test projects enable nullable reference types and the shipped libraries do
  not. Review a changed annotation for a false promise; do not ask for a
  repository-wide migration.
- Add focused tests for the decisions the diff changes. Report the commands you
  ran; a coverage percentage is not evidence.
