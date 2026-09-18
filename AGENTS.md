# machine contributor and agent guide

What an agent must do here, and where looking at the tree will mislead you.
Everything else - layout, target frameworks, what a workflow runs - read from
the tree; it is accurate and this file would only rot.

If this file disagrees with `local_check.sh`, `.github/workflows/ci.yml`, or the
code you are changing, prefer the executable behavior and say so.

## Validation

Run `./local_check.sh` from the repository root: it restores, checks CSharpier
formatting, builds Release, and tests. Do not skip a failing step or report
success without fresh output.

Agents must also run `./local_check.sh --agent-strict`, which makes
`scripts/comment-hygiene.ps1` blocking over the lines the branch adds. The
standard it enforces is `.claude/skills/code-comments/SKILL.md`. Do not drop the
flag to get a run through.

CI collects coverage and `local_check.sh` does not, so a local run is never
coverage-equivalent.

## Where the tree misleads

- `ci.yml` triggers on `push`, not `pull_request`. A green check on a pull
  request reflects the pushed head, not the merge result, and not a PR gate.
- The `Comment hygiene` check is advisory and never fails. Its green tick is not
  evidence that the strict scan passed.
- `appveyor.yml` is legacy and names projects that do not exist. Ignore it; do
  not repair it.
- A directory under `src/` or `tests/` is not an active project unless a current
  project file, solution entry, or CI step references it.
- Three different things are called `Word` here: a corpus word position,
  `WordAnalysis` (`src/SIL.Machine/Morphology/WordAnalysis.cs`), and HermitCrab's
  internal `Word`. Say which. Likewise "grammar" is the HermitCrab configuration
  as a whole and has no `Grammar` type - prefer `Language` or `Stratum`; "shape"
  is a phonological form, not geometry; "analysis" is morphological decomposition
  unless you name another domain; and a "reference" is a Scripture or row
  location, never object identity.

## Changing code

- Add or update focused tests with every behavior change. Keep fixtures
  deterministic and platform assumptions explicit.
- Use ordinal comparison for markers, tokens, identifiers, and protocol text;
  culture-sensitive only where the operation is genuinely linguistic. This is the
  defect class that ships here most often.
- Dispose engines, models, trainers, and streams according to their contracts,
  and be explicit about who owns a stream that is passed in.
- Preserve public API semantics unless the change intends otherwise and updates
  the tests. The published libraries are consumed as `netstandard2.0`.
- Prefer an existing abstraction to a parallel one.

## Branch hygiene

Preserve unrelated changes and pre-existing untracked files. Never use
`reset --hard`, `checkout --`, broad deletion, or broad staging as a cleanup
shortcut. Local review notes go in `.review/`, which `.gitignore` excludes.

When a change ports work from `sillsdev/machine.py`, link the source pull
request. A workflow files the porting issue after merge; do not hand-file a
duplicate.

## Agent guidance

`CLAUDE.md` imports this file. Claude workflows live under `.claude/skills/`.
Path-scoped review rules live under `docs/review/`; match the changed path,
first row wins:

| Path glob | Rules file |
| --- | --- |
| `src/SIL.Machine/Corpora/**/*.cs` | `docs/review/corpora-usfm.md` |
| `src/SIL.Machine/PunctuationAnalysis/**/*.cs` | `docs/review/punctuation.md` |
| `src/SIL.Machine.Morphology.HermitCrab/**/*.cs` | `docs/review/hermitcrab.md` |
| any other `src/**/*.cs` | `docs/review/machine-library.md` |
| `tests/**/*.cs` | `docs/review/machine-tests.md` |

`docs/review/devils-advocate.md` is an optional adversarial pass for a high-risk
change. Add a nested `AGENTS.md` only when a subtree needs different rules.
