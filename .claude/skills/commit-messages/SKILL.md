---
name: commit-messages
description: How to write a commit message in sillsdev/machine - imperative subject, short body.
---

# Commit messages

Name the change in an imperative, sentence-case subject under about 72
characters, with no terminal punctuation:

- `Fix bug in MergeEquivalentAnalyses (#493)`
- `Port changes from sillsdev/machine.py#336 (#498)`

A body, when there is one: blank line after the subject, wrapped at about 80
columns, saying what changed and why. Reference a GitHub issue when one exists.

## Two traps in the history

1. The `(#N)` suffix is added by GitHub when a pull request is squashed. Never
   type it into a local commit.
2. Older commits carry Jira identifiers such as `LT-22605`. That is historical;
   use a GitHub issue reference.

The 72-character limit is not enforced and longer subjects exist. Do not rewrite
shared history to satisfy it, or to fix a message on a pushed branch - add a
corrective commit unless the author asks for the rewrite.
