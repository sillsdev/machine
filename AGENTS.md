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

## Native SentencePiece boundary

CI builds `src/sentencepiece4c` separately and feeds the platform-specific
artifact to the managed build and package job. The verified forms are:

```
cmake -S src/sentencepiece4c -B src/sentencepiece4c/build -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build src/sentencepiece4c/build --config Release --target sentencepiece4c
```

Windows CI configures with `-A x64` instead of the Ninja generator. This is the
only native boundary in the repository; it is not a general native-before-managed
build policy.

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

`appveyor.yml` is legacy. It still references `SIL.Machine.WebApi` projects that
are absent from the tree. Do not add projects to satisfy it, and do not treat its
VS2019 assumptions as the current target matrix.

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
- `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md`
  exist for GitHub Copilot compatibility and must not restate rules from here.
- Add a nested `AGENTS.md` only when a subtree genuinely needs different rules.
