# Copilot guidance for machine

Read `AGENTS.md` at the repository root for operational rules and `CONTEXT.md`
for domain vocabulary. Those are the shared sources of truth; do not restate
their rules here. Path-scoped review rules live in
`.github/instructions/*.instructions.md` and attach automatically to matching
files.

Before making or reviewing a change, inspect the current source, project files,
tests, `local_check.sh`, and `.github/workflows/ci.yml`. Use actual code and
current configuration as evidence. A search that finds nothing is not proof that
a concept is absent unless you state the scope you searched.

Pay particular attention to:

- `netstandard2.0` library compatibility versus `net10.0` tools and tests;
- corpus row, tokenization, Scripture-reference, and alignment semantics;
- explicit string-comparison and culture choices in marker, token, and
  identifier logic;
- the SentencePiece4c native boundary and its platform artifacts;
- disposal and lifetime of engines, models, trainers, and streams;
- deterministic tests and stable public API behavior;
- the current CI workflow rather than the stale `appveyor.yml` WebApi paths.

Keep review comments concise, actionable, and anchored to a real file and line.
Avoid unrelated refactors and speculative claims. If a source or workflow fact
contradicts the documentation, report the discrepancy and prefer the verified
current behavior until the documentation is corrected.
