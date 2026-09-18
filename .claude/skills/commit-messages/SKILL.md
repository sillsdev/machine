---
name: commit-messages
description: How to write a commit message in sillsdev/machine - imperative subject, short body.
---

# Commit messages

A concise, imperative, sentence-case subject naming the actual change, under
about 72 characters, with no terminal punctuation:

- `Fix bug in MergeEquivalentAnalyses (#493)`
- `Port changes from sillsdev/machine.py#336 (#498)`

If there is a body, leave a blank line after the subject, wrap at about 80
columns, and say what changed and why. Reference a GitHub issue when one exists.

Two things the history will mislead you about:

- The `(#N)` suffix is added by GitHub when a pull request is squashed. Never
  type it into a local commit.
- Older commits carry Jira identifiers such as `LT-22605`. That is historical;
  use a GitHub issue reference for new work.

The 72-character limit is not enforced, and longer subjects exist. Do not
rewrite shared history to satisfy it, or to fix a message on a pushed branch -
add a corrective commit unless the author asks for the rewrite.
