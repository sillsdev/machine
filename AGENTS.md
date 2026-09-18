# machine contributor and agent guide

Repository-wide operational guidance for contributors and coding agents. Keep it
short and factual. Shared domain vocabulary lives in `CONTEXT.md`; do not
duplicate that glossary here.

Check the current tree before relying on a rule. If this file disagrees with
`local_check.sh`, `.github/workflows/ci.yml`, a project file, or the code you are
changing, say so and prefer the current executable behavior until this file is
updated.

## Repository shape

- `Machine.sln` contains the source and test projects.
- Production code is under `src/`; tests are under `tests/`.
- Native SentencePiece sources are under `src/sentencepiece4c/`.
- Samples and notebooks are under `samples/`; repository scripts under `scripts/`.
- The published libraries target `netstandard2.0`: `SIL.Machine`,
  `SIL.Machine.Translation.Thot`, `SIL.Machine.Translation.TensorFlow`,
  `SIL.Machine.Morphology.HermitCrab`, and `SIL.Machine.Tokenization.SentencePiece`.
- `SIL.Machine.Tool`, `SIL.Machine.Morphology.HermitCrab.Tool`,
  `SIL.Machine.Plugin`, and the test projects target `net10.0`.
- Shared assembly and package metadata comes from `src/AssemblyInfo.props`.
  There is no `Directory.Build.props` in this repository.
- A directory under `src/` or `tests/` is not an active project unless a current
  project file, solution entry, or CI step references it.

## Local validation

`local_check.sh` is the canonical sequence:

```
dotnet tool restore
dotnet restore
dotnet csharpier check .
dotnet build --no-restore -c Release
dotnet test --verbosity normal
```

Run it from the repository root. Do not skip a failing format, build, or test
step, and do not report success without fresh output.

CSharpier is required for C#. The tool is pinned in `.config/dotnet-tools.json`,
`.csharpierrc.yaml` sets `printWidth: 120`, and `.editorconfig` sets
`max_line_length = 120` plus the repository's analyzer severities.
`.csharpierignore` excludes config, project, props, targets, and XML files.

CI also collects coverage (`--collect:"Xplat Code Coverage"`); `local_check.sh`
does not, so do not describe a local run as coverage-equivalent.

## Branch hygiene

Before opening or updating a pull request, confirm the branch and the range:
`git status --short --branch`, `git merge-base origin/master HEAD`,
`git diff --check <merge-base>...HEAD`, and `git log --check origin/master..`.
The target default is `origin/master`; if the remote default changes, use the
resolved default and say so.

Preserve unrelated changes and pre-existing untracked files. Never use
`reset --hard`, `checkout --`, broad deletion, or broad staging as a cleanup
shortcut. A local review summary may be written to `.review/`, which
`.gitignore` excludes; keep other transient notes outside the repository.

## Comment hygiene

Agents must run `./local_check.sh --agent-strict`. It adds
`scripts/comment-hygiene.ps1` to the sequence above and fails the run on any
comment violation in the lines the branch adds, so you fix your own comments
before they reach review. This flag is required of agents and optional for
humans. Do not drop it to get a run through.

The standard is `.claude/skills/code-comments/SKILL.md`: a 200-character
aggregate budget for a block of implementation comments, the 120-column
`.editorconfig` width for every comment line, ASCII punctuation, and no process
framing, document pointers, historical narration, or provenance claims.

The `Comment hygiene` pull request workflow runs the same scan in advisory mode.
It annotates and never fails, so a green check there is not evidence that the
strict check passed.

## Native SentencePiece boundary

CI builds `src/sentencepiece4c` separately and feeds the platform-specific
artifact to the managed build and package job. If you change it, copy the
current CMake commands from `.github/workflows/ci.yml` rather than from here.

## Tests and changes

- Add or update focused tests with every behavior change.
- Keep test data deterministic and make platform assumptions explicit.
- When a public API or package boundary changes, consider both `netstandard2.0`
  consumer compatibility and `net10.0` tool and test behavior.
- Dispose engines, models, trainers, and streams according to their contracts.
- Preserve public API semantics unless the change intends otherwise and updates
  tests and documentation.
- Use string comparisons that match the domain: ordinal for markers, tokens, and
  other protocol identity; culture-sensitive only where the operation is genuinely
  linguistic.
- Prefer existing abstractions over parallel ones. Keep terminology consistent
  with `CONTEXT.md`.

## CI and legacy CI

`.github/workflows/ci.yml` is the current reference: Ubuntu and Windows, .NET 10,
native SentencePiece build, CSharpier check, Release build, tests with coverage,
and tag-triggered NuGet publishing. It triggers on `push`, not on `pull_request`,
so a green check on a PR reflects the pushed head rather than a PR event.

`appveyor.yml` is legacy and describes projects that are not in the tree.
Ignore it.

## Porting to and from machine.py

`.github/workflows/create-porting-issue.yml` files a porting issue in the sibling
repository when a pull request merges: `machine.py` when running in `machine`, and
the reverse in `machine.py`. It labels the issue `porting` and marks the body
`AUTO-GENERATED-ISSUE`. This is issue-level coordination, not a runtime
dependency. When a change ports work from the sibling repository, link the source
pull request.

## Working with agent guidance

- This file is the shared operational source of truth. `CLAUDE.md` imports it.
- Claude-specific workflows live under `.claude/skills/`.
- Path-scoped review rules live under `docs/review/`. Match the changed path to
  find the rules file:

  | Path glob | Rules file |
  | --- | --- |
  | `src/SIL.Machine/Corpora/**/*.cs` | `docs/review/corpora-usfm.md` |
  | `src/SIL.Machine/PunctuationAnalysis/**/*.cs` | `docs/review/punctuation.md` |
  | `src/SIL.Machine.Morphology.HermitCrab/**/*.cs` | `docs/review/hermitcrab.md` |
  | any other `src/**/*.cs` | `docs/review/machine-library.md` |
  | `tests/**/*.cs` | `docs/review/machine-tests.md` |

  The first matching row wins.

  For a high-risk change, `docs/review/devils-advocate.md` is an optional
  adversarial second pass.
- Add a nested `AGENTS.md` only when a subtree genuinely needs different rules.
