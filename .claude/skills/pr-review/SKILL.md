---
name: pr-review
description: How to write up a code review in sillsdev/machine - short line comments, evidence, severity.
argument-hint: "[optional PR number, branch, or review focus]"
user-invocable: true
---

# Writing a machine review

Post one short comment per finding, anchored on the line it is about, then one
summary comment. A review is read-only: do not edit, commit, push, or resolve
threads.

What to look for is in `docs/review/`; `AGENTS.md` maps a changed path to its
rules file.

## 1. One finding, one comment

Anchor it on the line. Two problems on one line are two comments. A reviewer
scrolling the diff should meet each point where it applies.

## 2. Lead with the claim

First sentence names the defect. Evidence second, fix third, if it fits.

```
Ordinal comparison missing: `marker.IndexOf(":")` is culture-sensitive, so
tr-TR splits this marker differently. Pass `StringComparison.Ordinal`.
```

Three lines is long. A finding needing more is a design question - raise it in
the summary instead.

## 3. Label the severity

**Critical** blocks merge, then **Important**, then **Minor**. Critical means
demonstrated: a failing command, a broken contract, a missing gate. A worry is
not Critical.

## 4. Carry the evidence

Every comment gets a `path:line` and a consequence. Mark what you did not
confirm `Unverified`; an unverified concern never blocks a merge.

- Do not report pre-existing issues the diff does not touch.
- Do not ask for a migration, modernization, or benchmark the diff gave no
  reason for.
- A search that found nothing proves absence only if you state what you
  searched.
- Name the commands you ran and what they returned. `./local_check.sh` is the
  full local sequence; an agent-authored branch also needs `--agent-strict`, and
  a green advisory `Comment hygiene` check does not stand in for it.
- A coverage percentage is not evidence that a changed line is tested.

## 5. Close with five lines

1. Verdict: approve, approve with fixes, or request changes.
2. The one thing that matters most, with its `path:line`.
3. Counts by severity.
4. What you ran, and its result.
5. What you could not verify.

Say which public API, target framework, package, or parity contract changed, or
`None verified`.

Then mark each finding **changed**, **accepted**, or **unverified**. Of 140
review threads here in three years, 128 have no follow-up, so nobody can tell
which findings mattered. Leave nothing implicit.

For an adversarial second pass, apply `docs/review/devils-advocate.md`.
