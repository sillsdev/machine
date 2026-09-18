---
name: pr-authoring
description: How to write a pull request in sillsdev/machine - strong lede, short body, honest evidence.
argument-hint: Optional branch purpose, issue number, or PR number
user-invocable: true
---

# Writing a machine PR

A style guide for the PR text. The work itself - what to check, what to run - is
in `AGENTS.md` and `docs/review/`.

Do not push, open, or edit a PR unless the author asked for it.

## The lede

First sentence: what a caller can now do, or what stopped being broken. Not what
you did, not how long it took, not which files moved.

Bad: *This PR refactors the tokenizer and adds some tests.*
Good: *USFM markers now split identically under tr-TR; they used to lose the
attribute on a Turkish locale.*

If the change is invisible to callers, say what it protects instead: *Agents can
no longer land a comment that narrates its own history.*

## Body

Keep the top zone under 200 words. Sections, in order, and drop any that are
empty:

```markdown
## Quick summary
<The lede, then the reviewer's main unknown and its answer. Two short paragraphs.>

## Where to look
- <risk> -- <the test, invariant, or gate that pins it>

## Deliberately not included
- <deferred path, and what would unblock it>

## Validation
- <exact command> -- <exact result>

## Issue / porting context
<Fixes #N only for a real issue. For machine.py work, link the source PR.>
```

Everything else goes below a `---`, in closed `<details>` blocks: *Reading this
a year from now*, *Decisions, and why*, *Paths not taken*, *Deferred, and what
would unblock it*. Long reasoning is welcome there. It is not welcome above the
rule.

No preamble, no apology, no "should be fine", no recap of the section above.

## Validation lines

Write the command and its result, nothing else. Never list a command you did not
run, and never call a local run CI-equivalent - CI collects coverage and
`local_check.sh` does not. If a check was skipped, say which and why.

Verify every count, path, type, and test name against the tree before it goes in
the body. A wrong number in a PR body outlives the PR.

## Replying to review comments

Reply in the thread, on the line. Classify first, then act:

* **Fix** - sound and unambiguous; make the smallest change.
* **Clarify** - ask the one specific question.
* **Reply only** - state the verified behavior; change nothing.
* **Defer** - name the follow-up and why it is outside this PR.

One reply per comment, two or three sentences. Resolve only a thread that is
fully answered and that you did not dispute.
