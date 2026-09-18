---
name: pr-review
description: How to write up a code review in sillsdev/machine - short line comments, evidence, severity.
argument-hint: "[optional PR number, branch, or review focus]"
user-invocable: true
---

# Writing a machine review

This is a style guide for the review you publish, not a method for doing the
review. What to look for lives in `docs/review/`; `AGENTS.md` maps a changed
path to its rules file.

A review is read-only. Do not edit, commit, push, or resolve threads.

## Shape

**Many short comments, not one long one.** Anchor each finding on the line it is
about. A reviewer scrolling the diff should meet each point where it applies.

**One finding per comment.** Two problems on one line are two comments.

**Lead with the claim.** First sentence names the defect. Evidence second, fix
third, and only if it fits.

```
Ordinal comparison missing: `marker.IndexOf(":")` is culture-sensitive, so
tr-TR splits this marker differently. Pass `StringComparison.Ordinal`.
```

Three lines is a long comment. If one needs more, the finding is really a
design question - ask it in the summary instead.

## Severity

Prefix each comment: **Critical** (blocks merge), **Important**, or **Minor**.
Critical means demonstrated - a failing command, a broken contract, a missing
gate. A worry is not Critical.

## Evidence

Every comment carries `path:line` and a consequence. Mark anything you did not
confirm as `Unverified`, and never let an unverified concern block a merge.

Do not report pre-existing issues the diff does not touch. Do not ask for a
migration, a modernization, or a benchmark suite the diff gave no reason for. A
search that found nothing proves absence only if you state what you searched.

Name the commands you ran and what they returned. `./local_check.sh` is the full
local sequence; an agent-authored branch also needs `--agent-strict`, and the
advisory `Comment hygiene` check going green does not stand in for it. A
coverage percentage is not evidence that a changed line is tested.

## Summary comment

Five lines at most:

1. Verdict: approve, approve with fixes, or request changes.
2. The one thing that matters most, with its `path:line`.
3. Counts by severity.
4. What you ran, and its result.
5. What you could not verify.

State `None verified` where that is the honest answer. Say which public API,
target framework, package, or parity contract changed, or that none did.

## Closing a thread

Say what happened to each finding: **changed**, **accepted** (the author
answered and you agree), or **unverified** (nobody settled it). Of 140 review
threads in this repository's last three years, 128 have no author follow-up at
all, so the reader cannot tell which findings mattered. Leave nothing implicit.

For an adversarial second pass, apply `docs/review/devils-advocate.md`.
