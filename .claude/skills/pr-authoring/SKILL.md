---
name: pr-authoring
description: Use when preparing, opening, updating, or responding to a GitHub pull request in sillsdev/machine; verify branch hygiene and actual validation evidence, then compose a concise reviewer-ready PR body.
argument-hint: Optional branch purpose, issue number, or PR number
user-invocable: true
---

# PR Authoring

Use this as the single entrypoint for preparing or updating a pull request in
sillsdev/machine. It prepares evidence and copy; it does not claim that a check
ran when it did not. Do not push, create, edit, or close a PR unless the user
explicitly requested that operation or confirmed a readiness offer.

## 1. Announce the stages

Tell the author that this run will:

1. inspect the branch and target diff;
2. review contracts, implementation, tests, CI/dependencies, and packaging risk
   appropriate to the changed files;
3. ask about Important/Critical risks and validation gaps;
4. run or record the exact applicable checks; and
5. prepare the PR body and review handoff.

If a finding is ambiguous, ask for the missing product or design decision. If the
author cannot explain a changed mechanism, record the area as an understanding
gap instead of silently treating the change as safe.

## 2. Branch and working-tree gate

Run these read-only checks first:

~~~text
git branch --show-current
git status --short
git symbolic-ref --short refs/remotes/origin/HEAD
git fetch origin --quiet
git merge-base origin/master HEAD
git diff --name-status origin/master...HEAD
git log --check --pretty=format:"---% h% s" origin/master..
git diff --check origin/master...HEAD
~~~

The verified target default is origin/master. If the remote default changes, use
the resolved remote default and state it in the report; never silently use main.
Stop if the current branch is master and the request is to open a feature PR.
Preserve unrelated changes and pre-existing untracked files. Never use
reset --hard, checkout --, broad deletion, or broad staging as a cleanup
shortcut.

Record merge base, HEAD, changed-file count, branch purpose, and pre-existing
work. A local review summary may be written to .review/, which .gitignore
excludes. Never commit it, and keep other transient notes outside the repo.

## 3. Review the diff against the target's actual shape

Use the smallest relevant set of passes:

* **Contract/API:** public signatures, serialization, compatibility, package
  metadata, and behavior visible to callers.
* **Implementation:** correctness, error handling, resource bounds,
  cancellation/concurrency, determinism, and edge cases.
* **Tests and fixtures:** regression coverage, expected/actual behavior,
  deterministic fixtures, test isolation, and platform assumptions.
* **Build/CI/dependencies:** project references, native SentencePiece boundary,
  dependency changes, CI matrix, and NuGet packing when relevant.

For a change under src/sentencepiece4c, include the exact matrix-specific CMake
commands from .github/workflows/ci.yml in required validation. For package or
project-file changes, inspect the package job's eight explicit dotnet pack
projects and say which package outputs are affected. Do not import FieldWorks
desktop, COM, installer, localization, or Jira rules into this review.

Verify every named type, method, project, test, path, and count against the
current tree before putting it in the body. If a draft document conflicts with
code, correct the document or report the uncertainty; do not make code fit
stale prose.

Classify findings as Critical, Important, or Minor. Keep positive observations
and evidence gaps separate. Deduplicate only identical concerns.

## 4. Author interview

Ask one Critical or Important question at a time:

* What caller-visible behavior or contract changes?
* What makes the changed path safe for existing callers?
* Which test or fixture proves the reported bug or acceptance criterion?
* Which OS/runtime/package path is at risk?
* What was deliberately left out, and what would unblock it?

For a vague answer, ask one focused follow-up and then record the concern as
unresolved if it remains unclear. For a small low-risk diff, do not manufacture
an interview; state that no interview was needed and why.

## 5. Validation contract

The authoritative managed CI sequence is:

~~~text
dotnet tool restore
dotnet restore
dotnet csharpier check .
dotnet build --no-restore -c Release
dotnet test --verbosity normal --collect:"Xplat Code Coverage"
~~~

Run the sequence for a normal code/project change unless the author explicitly
chooses a narrower check and the report names omitted checks. local_check.sh is
a useful shortcut, but it does not collect coverage; do not call it
CI-equivalent. For native SentencePiece changes, also run the applicable Linux
or Windows CMake commands exactly as shown in ci.yml. For packaging changes, run
or record the affected dotnet pack ... -c Release -o artifacts command and
distinguish local results from the CI package job.

An agent must also run `./local_check.sh --agent-strict`, which adds the
comment-hygiene scan and fails on any violation in the lines the branch adds.
It is required of agents and optional for humans; do not drop it to get a run
through. The advisory `Comment hygiene` pull request check going green is not
evidence that the strict scan passed.

For documentation/skill/template-only changes, run dotnet csharpier check .
only if C# files are touched; otherwise use git diff --check and available
YAML/Markdown diagnostics. Always state skipped checks and why. Never claim
manual validation unless it was directly performed or explicitly confirmed by
the author.

## 6. PR body

Write the body from a file. Keep the top zone concise:

~~~markdown
## Quick summary

<What the user or caller can now do, and why this change matters.>

<The reviewer's main unknown and its answer.>

## Where to look

- <risk> -- <test, invariant, or gate that pins it>
- <risk> -- <test, invariant, or gate that pins it>

## Deliberately not included

- <deferred or intentionally narrow path>

## Validation

- <exact commands run and their result>
- <exact checks not run and why>

## Issue / porting context

<Use Fixes #N only for a real GitHub issue. For machine.py work, link the
source PR or generated porting issue.>
~~~

The top zone should normally be 200-400 words for a substantive PR, but clarity
beats an artificial count for a tiny change. Do not open with process narration,
apology, or "should be fine." Do not duplicate proof in every section.

When durable reasoning matters, put it below a horizontal rule in closed details
sections: Reading this a year from now; Decisions, and why; Paths not taken;
What this does NOT authorize; Deferred, and what would unblock it; and Evidence.

Synthesize reasoning; do not paste scratchpads. Do not automatically delete
markdown from the branch. If temporary research is intentionally removed, list
the files and obtain confirmation before deletion.

## 7. Machine.py porting

For a change ported from sillsdev/machine.py, identify the source PR/commit,
state which behavior is relevant to this repository, and include affected
tests. create-porting-issue.yml creates the opposite-repository issue after a
merged PR, with title Port '<PR title>', label porting, and an
AUTO-GENERATED-ISSUE marker. Do not create a duplicate by hand when that
workflow has already created one.

## 8. Review comments

For each review comment, human or automated, classify it as:

* **Fix** -- technically sound and unambiguous; make the minimum change.
* **Clarify** -- missing context or a design decision; ask the specific question.
* **Reply only** -- explain verified behavior without changing code.
* **Defer** -- record the follow-up and why it is outside this PR.

Validate fixes with applicable commands. Reply in the anchored GitHub thread.
Resolve only an unresolved thread that the API permits resolving and that is
fully answered with no open question. Do not resolve disputed, ambiguous,
unverified, or deferred comments.

## 9. Readiness handoff

Before offering to publish, report branch, base, merge base, changed files,
findings/status, exact checks run/skipped/red, issue/porting links,
commit-message/whitespace status, and reviewer focus.

Only after confirmation may the workflow stage intended files, commit, push,
create/update the PR, or update its body. Preserve unrelated staged changes and
ask before including them.
