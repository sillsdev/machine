---
name: commit-messages
description: Use before writing a commit message in sillsdev/machine; apply the repository commit conventions and check the new commit range before pushing.
---

# Commit messages

Write a concise, imperative, sentence-case subject that names the actual change.
Recent history is descriptive and GitHub-native, for example:

- `Fix bug in MergeEquivalentAnalyses (#493)`
- `Use StringComparison.Ordinal when locating token indices in PlaceMarkersUsfmUpdateBlockHandler (#496)`
- `Port changes from sillsdev/machine.py#336 (#498)`

## Conventions

- Keep the subject under about 72 characters when you can. This is not enforced
  by CI, and existing history contains longer subjects, so do not rewrite shared
  history to satisfy it.
- No trailing period or other terminal punctuation on the subject.
- No leading, trailing, or interior tab and trailing-whitespace damage.
- If a body is present, leave one blank line after the subject and wrap body
  lines at about 80 characters.
- Explain what changed and why. Reference a GitHub issue when one exists.
- The `(#N)` suffix is added by GitHub when a pull request is squashed. Do not
  add it by hand to an ordinary local commit.
- Older commits use Jira identifiers such as `LT-22605`. That convention is
  historical; use a GitHub issue reference for new work unless a maintainer asks
  otherwise.

## Check the range before pushing

```
git fetch origin --quiet
git log --check --pretty=format:'--- %h %s' origin/master..HEAD
```

`git log --check` reports whitespace damage in the commits you are about to
push. A failed check is not a pass. Do not rewrite a pushed or shared branch to
fix a message; add a corrective commit unless the author explicitly authorizes
the rewrite.
