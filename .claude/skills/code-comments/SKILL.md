---
name: code-comments
description: MUST use before writing or editing any comment in this repository - content rules, budget, width.
---

# Machine code comments

Write for the next reader. A comment earns its place by explaining a contract,
an invariant, a compatibility requirement, a performance tradeoff, or a
non-obvious reason. If the code and its names already say it, delete it.

Say WHAT the code guarantees and WHY it matters, in the present tense. Do not
narrate HOW it works - the comment should survive an equivalent rewrite. A
member summary describes that member's own contract, not its caller's.

## Banned content

`scripts/comment-hygiene.ps1` fails on these over the lines a branch adds:

- **Process framing** - `Phase 1`, `later we'll`, `we'll eventually`.
- **History** - `it used to`, `previously returned`, `was removed`,
  `renamed from`, `no longer used`.
- **Provenance** - `extracted from`, `shared by X and Y`, `the only caller`.
- **Pointers** - to a Markdown file, a numbered section, a review note, or
  another file's comment.
- **Non-ASCII punctuation** - use `--`, `->`, `...`, `-`, `x`, and plain quotes.
  Typography only; comment text may use any script the language data needs.

A present-tense statement of current state is not history: "Returns null when
the stratum has no rules" is a contract. A compatibility note about behavior
that must stay true is welcome. An issue reference that is part of the current
contract may stay.

## Budget and width

A run of consecutive whole-line `//` or `#` comments is one block, ended by a
blank line, code, or a doc comment. **One block gets 200 characters total**,
markers and indentation excluded. `///` blocks and PowerShell block comments are
exempt from the budget, not from the content rules or the width limit.

Every comment line fits 120 display columns, per `.editorconfig`.

A block that wants more than 200 characters usually belongs in an XML summary,
or is explaining something the code should express directly.

## XML documentation

One `<summary>` above the member, including a public constructor with
parameters. Omit `<param>` and `<returns>` that only restate a name or type;
keep them for units, nullability, ownership, or real result semantics. Document
every parameter or none. No file headers, no divider comments, no fact repeated
in both the summary and the parameters.

A test comment explains a non-obvious fixture or setup constraint. It does not
restate the test name.

## Running the check

`pwsh ./scripts/comment-hygiene.ps1` scans the lines your branch adds; add
`-Full -Advisory` to size existing debt, or `-SelfTest` to check the rules
themselves. Agents run `./local_check.sh --agent-strict`, which makes the scan
blocking. The pull request check is advisory, so its green tick is not evidence
that the strict scan passed.
