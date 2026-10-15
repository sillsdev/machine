---
name: port-pr
description: Port a machine.py (Python) PR into machine (C#). Given a GitHub issue number for a porting task in sillsdev/machine, finds the linked machine.py PR, ports its changes to the C# codebase, runs ./local_check.sh, and opens a PR that closes the issue. Use when asked to port a PR/issue from machine.py, complete a "porting" issue, or sync a machine.py change into machine.
---

# Port a machine.py (Python) PR into machine (C#)

`machine` (C#) and `machine.py` (Python) are direct, intentionally-synced ports of each
other. This skill ports a change that already landed in `machine.py` into `machine`,
driven by a "porting" issue in `sillsdev/machine`.

**Required argument:** the GitHub issue number in `sillsdev/machine` (always exists).

## Repos

- C# target repo: the current working directory (`sillsdev/machine`).
- Python source repo: the sibling clone at `../machine.py` (`sillsdev/machine.py`).
  Use the local clone for reading surrounding context; use `gh ... --repo sillsdev/machine.py`
  for authoritative PR data.

## Step 1 - Read the porting issue

```bash
gh issue view <ISSUE> --json title,body,labels
```

- The body looks like: `Port any relevant changes in https://github.com/sillsdev/machine.py/pull/<PR> from machine.py to machine.`
  Extract `<PR>` - the machine.py PR number - from that URL. An issue filed with the
  porting form carries the URL in its `machine.py source PR or commit` field instead.
- The issue title looks like `Port '<Title>'`. Keep `<Title>` for the branch and PR.
- If the body has no machine.py PR link, stop and ask the user for the source PR.

## Step 2 - Understand the source change

```bash
gh pr view <PR> --repo sillsdev/machine.py --json title,body,files,commits,mergeCommit
gh pr diff <PR> --repo sillsdev/machine.py
```

Read the full diff. For each changed Python file, read the corresponding file(s) in
`../machine.py` as of the merge commit, with `git -C ../machine.py show <mergeCommit>:<path>`,
to understand the surrounding context. The working tree may have moved on since the PR
merged. First check the commit with `git -C ../machine.py cat-file -e <mergeCommit>`. If it
fails, the clone predates the merge: run `git -C ../machine.py fetch`. Identify the C#
counterpart (see mapping below). Read the existing C# code you're about to change so
the port matches local idiom.

Note: not every change ports. Skip Python-only concerns (`pyproject.toml`/`poetry.lock`
dependencies, `__init__.py` re-exports, black/flake8/isort/pyright configuration, PyPI
packaging, and `machine/jobs`, which has no C# counterpart here). The issue says "any
*relevant* changes" - use judgment and call out anything you intentionally skip.

## Step 3 - File & API mapping

| machine.py (Python) | machine (C#) |
|---|---|
| `machine/<area>/<snake_case>.py` | `src/SIL.Machine/<PascalArea>/<PascalCase>.cs` (or the matching `SIL.Machine.*` project) |
| `tests/<area>/test_<snake_case>.py` | `tests/SIL.Machine.Tests/<PascalArea>/<PascalCase>Tests.cs` (or the matching `*.Tests` project) |
| `snake_case` functions/vars | `PascalCase` methods / `camelCase` locals / `_camelCase` fields |
| `Sequence[T]`/`list` / `Mapping`/`dict` / `Set`/`set` etc. | `IReadOnlyList<T>` / `IReadOnlyDictionary<,>` / `IReadOnlyCollection<T>` etc. - match the neighbors |
| pytest plain `assert` | NUnit `Assert.That(...)` (check neighboring test files) |
| `pyproject.toml` `version` | `src/AssemblyInfo.props` `<Version>` |

Python modules usually map one-to-one onto a folder of `src/SIL.Machine` (`corpora` ->
`Corpora`, `punctuation_analysis` -> `PunctuationAnalysis`, and so on), but a module may
hold several types that C# splits into one file each. Find the C# counterpart by searching
for the type/method name (translated to PascalCase) before assuming a path:
`grep -rn "<TypeOrMethodName>" src tests`.

Port the **behavior**, not the syntax. Match existing C# patterns in the neighboring
code, and apply the `AGENTS.md` rules Python never makes you think about: ordinal string
comparison for markers, tokens, and identifiers; disposal and stream ownership; and
`netstandard2.0` compatibility for the published libraries. Port the tests too.

## Step 4 - Branch & apply

Create a branch off `master` (do not commit to `master`):

```bash
git switch master && git pull && git switch -c port-<slug>
```

where `<slug>` is a short kebab-case form of the issue title (e.g.
`port-fix-unclosed-style-marker-crash`).

Apply the ported changes with Edit/Write.

## Step 5 - Verify locally

```bash
dotnet csharpier format .
./local_check.sh --agent-strict
```

`local_check.sh` restores, checks CSharpier formatting, builds Release, and tests;
`--agent-strict` adds the blocking comment-hygiene scan. Fix any failure before
proceeding. Report the test results plainly (pass/fail counts); don't claim success if
anything failed.

## Step 6 - Commit, push, open PR (pause first)

Show the user a summary of the diff and the proposed PR title/body, and **confirm before
pushing**. Then commit with a message following the commit-messages skill
(`Port '<Title>' from machine.py PR #<PR>`), push, and open the PR:

```bash
git push -u origin port-<slug>
gh pr create --title "Port '<Title>' from machine.py" --body-file <file>
```

Write the body with the pr-authoring skill. Link the source PR in its porting
context section, name anything deliberately not ported, and end with
`Closes #<ISSUE>`.

## Notes

- Keep the two codebases as similar as is reasonable for a C#-vs-Python port.
- If the source PR spans multiple commits, the squashed PR diff is the source of truth, but
  reading individual commits can clarify intent.
- If a change has no sensible C# counterpart, say so in the PR body rather than forcing it.
