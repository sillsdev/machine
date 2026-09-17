---
name: pr-review
description: Evidence-first review of machine pull requests and branch diffs, focused on public library compatibility, deterministic language processing, async contracts, HermitCrab performance, tests, and packaging.
argument-hint: "[optional PR number, branch, or review focus]"
---

# Machine PR Review

Review the current machine branch or named pull request. This is a read-only review. Do
not edit, commit, push, resolve comments, or open a PR unless a separate workflow
explicitly authorizes that action.

## Review contract

- Review the actual branch diff from the merge-base with the PR base, normally
  `origin/master`, not today's commits and not filenames alone.
- Read the changed code, relevant tests, project files, repository instructions, and the
  directly affected public callers or fixtures before making a finding.
- Use only findings grounded in code, a reproducible command, a test/result, a cited
  history change, or a cited consumer contract.
- Every finding must include `path:line` and a concrete consequence.
- Mark each statement as `Verified` or `Unverified follow-up`. An unverified concern is never a blocking finding.
- Do not report a pre-existing issue unless the diff changes its behavior, exposes it
  through a changed contract, or the review explicitly asks for a baseline audit.
- Do not request a broad modernization (nullable migration, async streaming,
  language-version change, analyzer cleanup, or benchmark suite) without a changed-code
  reason.

## Establish scope

1. Confirm the repository, branch, worktree, and base. Use `git status --short
   --branch`, `git merge-base <base> HEAD`, and `git diff --stat <merge-base>...HEAD`.
2. Read `README.md`, `.editorconfig`, `.csharpierrc.yaml`, the relevant solution/project
   files, and any target-local instructions.
3. Classify changed files: shipped API/library, tests, HermitCrab, corpus/USFM,
   SentencePiece/native, tool, workflow/package, or documentation.
4. Apply only the review axes and path-scoped instructions relevant to that classification.
5. Search for public declarations, interface implementations, project references,
   package metadata, tests, fixtures, and downstream references before concluding that a
   contract changed.

## Review axes

### Public API, binary compatibility, and package contract

For changes under shipped library projects, inspect public/protected types and members,
overloads, optional parameters, interface shape, return types, serialized models, target
frameworks, package references, assembly/package versioning, and XML documentation. A
removal, narrowing, incompatible default/behavior, or accidental target/package change
is a finding only when the diff and contract establish the consequence. Check
implementations and tests of changed interfaces. Do not infer downstream breakage from a
public-looking filename.

### Nullable annotations

The current shipped libraries do not opt into nullable reference types, while test
projects do. Review `#nullable`, project settings, `?`, null-forgiving operators, and
generated/public annotations when touched. Flag an annotation that promises a false null
contract or creates inconsistent implementation/interface behavior. Do not require a
repository-wide NRT migration as part of an unrelated change.

### Async, cancellation, and concurrency

The public API uses `Task` and optional `CancellationToken`; corpus/tokenizer APIs are
synchronous, and the current source has no `IAsyncEnumerable` surface. For changed async
code, verify token propagation to every meaningful wait/I/O/dataflow operation, ordering
and partial-result behavior, disposal/pool lifetime, and absence of sync-over-async
deadlocks. Check `ConfigureAwait(false)` where library context capture is not intended.
A new async-streaming API requires an explicit compatibility and consumer decision.

### Determinism and culture

For token, marker, identifier, persisted, protocol, dictionary-order, and serialization
logic, verify explicit comparison/equality/culture choices. Use ordinal comparison for
identity/search where the domain is byte/code-point identity; use invariant or
culture-aware behavior only when the semantic contract requires it. The `418ff225`
`StringComparison.Ordinal` fix and Hindi regression test are precedent, not a blanket
replacement rule. Add a non-English-culture test when the changed behavior can vary by
culture.

### HermitCrab semantic/performance safety

For `src/SIL.Machine.Morphology.HermitCrab/**`, verify that analysis-state keys include
every field read by rules, frozen/mutable objects cannot invalidate keys, replay
preserves analysis results and prefixes, memo entries are complete before storage, and
resource/parallelism caps remain effective. Review allocations and retained lifetimes in
inner loops when changed. Require focused semantic tests for behavior changes and a
benchmark or measured artifact when the PR claims performance improvement. Do not accept
hit-count improvement as proof of semantic equivalence.

### Python parity

For a port or algorithm explicitly shared with `machine.py`, require a cited Python
source/commit/issue and paired behavior evidence. The post-merge porting workflow is
follow-up coordination, not proof. If the Python source is unavailable, record parity as
`Unverified follow-up`; do not invent a comparison or block without a stated parity
contract.

### Tests and coverage

Map changed branches, guards, error paths, ordering, resource limits, and side effects
to tests. Prefer focused tests in the affected test project, including Unicode/culture,
cancellation, malformed input, and boundary cases where relevant. Run the smallest
meaningful command first and then the repository check when practical. Report exact
commands, results, filters, and exclusions. Coverage percentage alone is not evidence
that changed decisions are covered.

### Formatting and repository checks

Run `dotnet csharpier check .`, `dotnet build --no-restore -c Release`, and `dotnet test
--verbosity normal` as applicable. `local_check.sh` is the canonical combined local
sequence. Respect `.editorconfig` and CSharpier's 120-column configuration. Formatting
is normally an Important/Minor issue, never a Critical issue by itself.

### USFM, ScriptureRef, and versification

For `src/SIL.Machine/Corpora/**` changes involving USFM, ScriptureRef, `ScrVers`,
tokenization, or update handlers, trace input -> parse/tokenize -> reference mapping ->
output. Require paired fixtures or assertions for valid, empty, malformed, nested,
Unicode, and cross-versification cases appropriate to the change. Verify marker and
token identity comparisons are deterministic. Do not claim render parity; this library
has semantic text behavior, not FieldWorks desktop rendering.

### Input, resource, and native boundary safety

For changed streams, archives, paths, subprocess/tool inputs, or SentencePiece native
loading, check size limits, path/entry validation, disposal, error propagation, platform
selection, and secrets. Cite the changed boundary and test/evidence. Do not apply this
checklist to unrelated pure algorithms.

## Validation

- Prefer `./local_check.sh` (or `bash local_check.sh`) for the full local check when the environment supports it.
- Otherwise run the equivalent `dotnet tool restore`, `dotnet restore`, `dotnet
  csharpier check .`, `dotnet build --no-restore -c Release`, and `dotnet test
  --verbosity normal` commands.
- For focused behavior, run the relevant test project with an explicit filter and then
  state whether the full matrix was run.
- Treat the visible GitHub `CI Build` result as evidence only when its commit SHA is the
  reviewed head. This workflow is push/tag-triggered, not a `pull_request`-triggered
  gate.
- Do not treat Codecov upload as a pass/fail threshold; inspect changed-line tests directly.
- If a command cannot run, state why and what remains unverified. Never convert unavailable evidence into a pass.

## Output format

### Contract/API Changes Summary

State whether shipped public APIs, target frameworks, package metadata, native
artifacts, serialized formats, or parity contracts changed. Say `None verified` when
appropriate.

### Findings

#### Critical (must address)

- `[Verified] path:line -- consequence; evidence/command; required correction.`

Use only for demonstrated correctness, security/resource, compatibility, build/package,
or test-gate failures that block a safe merge.

#### Important (should address)

- `[Verified] path:line -- concrete risk or missing contract evidence; evidence/command; requested action.`

#### Minor (consider)

- `[Verified] path:line -- scoped maintainability/style/test improvement.`

#### Unverified follow-ups

- `[Unverified] concern -- exact evidence still needed; why it is not a finding.`

### Positive Observations

List concrete safeguards or tests that the diff preserves or adds.

### Required Validation

List commands with status (`passed`, `failed`, `not run`, or `blocked`), exact filters, artifacts, and remaining gaps.

### Suggested Review Focus

Name the one to three highest-value areas for a human reviewer. If an adversarial pass
is requested, use the separate library devil's-advocate role and keep its objections
evidence-linked.
