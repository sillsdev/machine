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

Three short sentences, each doing a different job. Nothing else before them.

1. **What it does.** What a caller can now do, or what stopped being broken.
   Not what you did, not how long it took, not which files moved.
2. **The reviewer's first unknown, answered.** Usually "what breaks?" or "why is
   it this big?" Answer it here; do not make them read for it.
3. **The boundary.** What the change does not touch, or the one condition that
   keeps it safe.

Bad: *This PR refactors the tokenizer and adds some tests.*

Good: *USFM markers now split identically under tr-TR, where the attribute used
to be dropped. No public signature changes - the fix is one comparison, from
culture-aware to ordinal. Nothing outside `UsfmTokenizer` is touched.*

If the change is invisible to callers, lead with what it protects: *Agents can
no longer land a comment that narrates its own history.*

Keep each sentence under about 25 words. If a sentence needs a subordinate
clause to survive, it belongs in the body.

## Body

Keep the top zone under 200 words. Sections, in order, and drop any that are
empty:

```markdown
## Quick summary
<The three-sentence lede. Nothing else in this section.>

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
