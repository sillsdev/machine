---
name: pr-authoring
description: How to write a pull request in sillsdev/machine - strong lede, short body, honest evidence.
argument-hint: Optional branch purpose, issue number, or PR number
user-invocable: true
---

# Writing a machine PR

Write the body to a file, then `gh pr edit <n> --body-file`. Do not push, open,
or edit a PR unless the author asked.

What to check and what to run is in `AGENTS.md` and `docs/review/`. This is the
write-up.

## 1. Write the lede

Three sentences, each with a different job, before anything else:

1. **What it does** - what a caller can now do, or what stopped being broken.
2. **The first unknown, answered** - usually "what breaks?" or "why so big?"
3. **The boundary** - what it does not touch.

Under 25 words each. A sentence needing a subordinate clause belongs in the
body.

Bad: *This PR refactors the tokenizer and adds some tests.*

Good: *USFM markers now split identically under tr-TR, where the attribute used
to be dropped. No public signature changes - the fix is one comparison, from
culture-aware to ordinal. Nothing outside `UsfmTokenizer` is touched.*

Invisible to callers? Lead with what it protects: *Agents can no longer land a
comment that narrates its own history.*

## 2. Fill the top zone

Under 200 words. Drop any section that would be empty.

```markdown
## Quick summary
<The lede. Nothing else.>

## Where to look
- <risk> -- <the test, invariant, or gate that pins it>

## Deliberately not included
- <deferred path, and what would unblock it>

## Validation
- <exact command> -- <exact result>

## Issue / porting context
<Fixes #N only for a real issue. For machine.py work, link the source PR.>
```

## 3. Put the reasoning below the rule

Everything longer goes under a `---`, in closed `<details>` blocks: *Reading
this a year from now*, *Decisions, and why*, *Paths not taken*, *Deferred, and
what would unblock it*. Long reasoning is welcome there and nowhere above.

No preamble, no apology, no "should be fine", no recap.

## 4. Check the claims

Every count, path, type, and test name must match the tree. A wrong number in a
PR body outlives the PR.

Validation lines carry the command and its result, nothing else. Never list a
command you did not run. Never call a local run CI-equivalent - CI collects
coverage and `local_check.sh` does not. Name any check you skipped.

## Replying to review comments

Reply in the thread, on the line, in two or three sentences. Classify first:

- **Fix** - sound and unambiguous; make the smallest change.
- **Clarify** - ask the one specific question.
- **Reply only** - state the verified behavior; change nothing.
- **Defer** - name the follow-up and why it is outside this PR.

Resolve only a thread that is fully answered and that you did not dispute.
