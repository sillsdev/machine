---
name: pr-review
description: Review a pull request in sillsdev/machine - find and verify with code-review, then post short numbered line comments with evidence and severity.
argument-hint: "[optional PR number, branch, or review focus]"
user-invocable: true
---

# Writing a machine review

Post one short comment per finding, anchored on the line it is about, then one
summary comment. A review leaves the code alone: do not edit, commit, or push.
The only threads it resolves are its own findings, once addressed, withdrawn,
or, unless Critical, rejected or deferred by the author.

Unless verified findings are already in hand, get them first with
`/code-review high <target>`, without `--comment`: it finds and verifies, and
this skill decides what gets posted.

What to look for is in `docs/review/`; `AGENTS.md` maps a changed path to its
rules file.

## Start from the last round

A PR is reviewed again on every push. Each round is a follow-up: settle the
open findings first, then add only what is new.

1. Load the review threads, which carry the resolved state REST lacks:

   ```
   gh api graphql -F owner={owner} -F repo={repo} -F n=<n> -f query='
     query($owner: String!, $repo: String!, $n: Int!) {
       repository(owner: $owner, name: $repo) { pullRequest(number: $n) {
         reviewThreads(first: 100) { nodes { id isResolved path line
           comments(first: 50) { nodes { databaseId author { login } body } } } } } } }'
   ```

   Load summaries from
   `gh api --paginate repos/{owner}/{repo}/issues/<n>/comments`. An earlier
   finding is a thread whose first comment's author is `claude`, as GraphQL
   spells `claude[bot]`, numbered `F<n>` or not. It is open while unresolved,
   even when `line` is null because the diff moved past it. The latest summary
   names the commit it reviewed.
2. Check every open finding against the head commit and reply in its thread,
   using its first comment's `databaseId`, with
   `gh api repos/{owner}/{repo}/pulls/<n>/comments/<id>/replies -f body=...`:
   - Fixed: `F3 addressed in <sha>:` and what fixed it, then resolve the
     thread by its `id`:
     `gh api graphql -F id=<id> -f query='mutation($id: ID!) { resolveReviewThread(input: {threadId: $id}) { thread { isResolved } } }'`
   - Author rejected or deferred it: weigh any reason given. If it holds,
     `F3 withdrawn:` and why, then resolve the thread. If not, `F3 accepted:`
     with your evidence or where it was deferred, once, then resolve it. An
     accepted Critical finding stays open for a maintainer to dismiss.
   - Still applies and its code changed: `F3 still applies at <sha>:` and why.
   - Still applies and its code is untouched: stay silent; the summary counts
     it.
3. Treat each finding `/code-review` returns as a duplicate when any thread,
   open or resolved, from anyone, already raises the same defect, even if the
   line has moved. Drop duplicates: a resolved thread is a settled one.
4. Post a new finding when it is on code changed since the reviewed commit, or
   when it is Critical. Diff with `git diff <reviewed-sha> <head-sha>` if
   `gh api repos/{owner}/{repo}/compare/<reviewed-sha>...<head-sha> --jq .status`
   prints `ahead`; otherwise history was rewritten, so treat the whole PR as
   changed. Number new findings on from the highest `F` in the thread.

Every earlier finding has a status when this is done.

## 1. One finding, one comment

Anchor it on the line. Two problems on one line are two comments. A reviewer
scrolling the diff should meet each point where it applies.

Number findings `F1`, `F2`, ... in the order you post them, and put the number
right after the keyword, so a reply or a later review can refer to one without
quoting it. Not `#1`: GitHub links that to issue 1. Numbers are stable - a
withdrawn finding keeps its number, and a later round continues the sequence.

## 2. Lead with the claim

After the keyword and number, the first sentence names the defect. Evidence
second, fix third, if it fits.

```
Minor: F1. Ordinal comparison missing: `marker.IndexOf(":")` is
culture-sensitive, so tr-TR splits this marker differently. Pass
`StringComparison.Ordinal`.
```

Three lines is long. A finding needing more is a design question - raise it in
the summary instead.

## 3. Label the severity

Every finding is **Critical**, **Important**, or **Low**. Critical means
demonstrated: a failing command, a broken contract, a missing gate. A worry is
not Critical. Important needs an answer from the author; Low is worth knowing
and needs none.

A finding comment posted to the PR starts with the Reviewable keyword for its
severity, followed by a colon. Reviewable reads it and sets the discussion's
disposition:

| Severity | Comment starts with | Disposition in Reviewable |
| --- | --- | --- |
| Critical | `Major:` | Blocking, until a maintainer dismisses it |
| Important | `Minor:` | Discussing, open until the author answers |
| Low | `FYI:` | Informing, starts resolved |

Use the keywords there and nowhere else. The summary, replies, and a review
that is not posted use the severity names: `Minor` reads as trivial, and an
Important finding is not. A finding comment that starts with any other word gets
Reviewable's default, which for a reviewer is Blocking.

## 4. Carry the evidence

Every comment gets a `path:line` and a consequence. Mark what you did not
confirm `Unverified`; an unverified concern is never Critical.

- Do not report pre-existing issues the diff does not touch.
- Do not ask for a migration, modernization, or benchmark the diff gave no
  reason for.
- A search that found nothing proves absence only if you state what you
  searched.
- Name the commands you ran and what they returned. `./local_check.sh` is the
  full local sequence; an agent-authored branch also needs `--agent-strict`, and
  a green advisory `Comment hygiene` check does not stand in for it.
- Reproduce before you report. The environment can build and test: a finding you
  tried and failed to reproduce is worth more than one you only reasoned about.
- A coverage percentage is not evidence that a changed line is tested.

## 5. Close with five lines

1. Verdict: approve, approve with fixes, or request changes.
2. The one thing that matters most, with its number and `path:line`.
3. Counts by severity.
4. What you ran, and its result.
5. What you could not verify.

Say which public API, target framework, package, or parity contract with
`sillsdev/machine.py` changed, or `None verified`.

Then mark every finding in the thread, earlier rounds included, by number:
**new**, **open**, **addressed**, **accepted** (the author keeps it knowingly),
or **withdrawn**, adding **unverified** where it applies. Of 140 review threads
here in three years, 128 have no follow-up, so nobody can tell which findings
mattered. Leave nothing implicit.

End with `Reviewed at <head-sha>`, the PR head from
`gh pr view <n> --json headRefOid`, not the merge commit checked out, so the
next round knows where this one stopped.

Once the new summary is posted, minimize each earlier one as outdated, so only
the latest shows. An earlier summary is a top-level comment by `claude[bot]`
containing `Reviewed at`; leave its other comments, such as replies to an
`@claude` mention, visible. Take its `node_id` from the comments listing:

```
gh api graphql -F id=<node_id> -f query='mutation($id: ID!) { minimizeComment(input: {subjectId: $id, classifier: OUTDATED}) { minimizedComment { isMinimized } } }'
```

For an adversarial second pass, apply `docs/review/devils-advocate.md`.
