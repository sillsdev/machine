---
name: code-comments
description: MUST use before writing or editing any comment in this repository - content rules, budget, width.
---

# Machine code comments

Before you write a comment, delete it. If the code and its names already say it,
it was noise. What survives explains a contract, an invariant, a compatibility
requirement, a performance tradeoff, or a non-obvious reason.

Say WHAT the code guarantees and WHY, in the present tense. Do not narrate HOW
it works - the comment should survive an equivalent rewrite. A member summary
describes that member's own contract, not its caller's.

## Never write these

`scripts/comment-hygiene.ps1` fails on them over the lines your branch adds:

1. **Process framing** - `Phase 1`, `later we'll`, `we'll eventually`.
2. **History** - `it used to`, `previously returned`, `was removed`,
   `renamed from`, `no longer used`.
3. **Provenance** - `extracted from`, `shared by X and Y`, `the only caller`.
4. **Pointers** - to a Markdown file, a numbered section, a review note, or
   another file's comment.
5. **Non-ASCII punctuation** - use `--`, `->`, `...`, `-`, `x`, plain quotes.
   Typography only; comment text may use any script the language data needs.

Present tense about current state is not history: "Returns null when the stratum
has no rules" is a contract. A compatibility note about behavior that must stay
true is welcome, as is an issue reference that is part of the contract.

## Fit the budget

One block - a run of whole-line `//` or `#` comments, ended by a blank line,
code, or a doc comment - gets **200 characters total**, markers and indentation
excluded. Every line fits **120 display columns**.

`///` blocks and PowerShell block comments are exempt from the budget, not from
the content rules or the width limit.

Over budget? It belongs in an XML summary, or the code should express it
directly.

## XML documentation

One `<summary>` above the member, including a public constructor with
parameters. Omit `<param>` and `<returns>` that only restate a name or type;
keep them for units, nullability, ownership, or real result semantics. Document
every parameter or none. No file headers, no divider comments, no fact repeated
in both the summary and the parameters.

A test comment explains a non-obvious fixture or setup constraint. It does not
restate the test name.

## Run the check

`pwsh ./scripts/comment-hygiene.ps1` scans the lines your branch adds. Add
`-Full -Advisory` to size existing debt, or `-SelfTest` to check the rules
themselves. Agents run `./local_check.sh --agent-strict`, which makes the scan
blocking; the pull request check is advisory, so its green tick proves nothing.
