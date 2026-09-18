---
name: code-comments
description: MUST use before writing or editing any comment in this repository - content rules, budget, width.
---

# Machine code comments

Write comments for the next reader. A comment must explain a current contract,
invariant, constraint, compatibility requirement, performance tradeoff, or
non-obvious reason. If the code and its names already make the behaviour
obvious, delete the comment.

`scripts/comment-hygiene.ps1` enforces the mechanical parts of this standard
over the lines a branch adds. It cannot judge whether a comment is accurate or
worth keeping; that is still the author's and reviewer's job.

## Scope

The checker examines whole-line comments in:

- C# under `src/` and `tests/`;
- the C/C++ wrapper under `src/sentencepiece4c/`;
- PowerShell, shell, and Python under `scripts/`, plus `local_check.sh`.

It examines `//`, `///`, and whole-line `#` comments. It does not examine
trailing comments, `/* ... */` blocks, Python docstrings, string literals, or a
shebang line. A bare `#` in a C# file is a preprocessor directive, not a
comment.

## Content rules

- Be accurate before being brief. Three or four sentences is usually enough.
- Explain WHAT the code guarantees and WHY the choice matters. Do not narrate
  HOW the current implementation works; the comment should survive an equivalent
  rewrite.
- A member summary describes that member's own contract, not a caller's or a
  collaborator's.
- A comment must stand on its own. Delete restatements of the adjacent code.
- Private comments are for non-obvious behaviour, invariants, compatibility,
  performance, or a subtle bug fix.
- Keep a compatibility comment about behaviour that must stay true, for example
  "Matches the legacy normalization so persisted data stays interoperable". Do
  not say that code was ported, changed in a commit, or used to behave
  differently.
- Use ASCII punctuation: `--` for an em dash, `->` for an arrow, `...` for an
  ellipsis, `-` for a bullet or en dash, `x` for a multiplication sign, and
  plain quotes. This is about typography only; comments may contain any script,
  IPA, or orthography the language data requires.
- Prefer a clear word to an abbreviation the next reader cannot resolve.

## Banned comment content

Do not write:

- process framing such as `Phase 1`, `later we'll`, or `we'll eventually`;
- pointers to a Markdown document, a numbered section, or a review note;
- historical narration such as `it used to`, `used to be`, `previously
  returned`, `was removed`, `renamed from`, `first shipped`, or `no longer
  used`;
- provenance claims such as `shared by X and Y`, `the only caller`, or
  `extracted from`;
- cross-file pointers such as `see X's note` or `as documented in X`.

A present-tense statement about current state is fine: "Returns null when the
stratum has no rules" is a contract, not history. A GitHub issue reference that
is part of the current contract may stay.

## XML documentation

The shipped library projects set `GenerateDocumentationFile` and suppress
CS1591/CS1573, so the compiler does not require documentation. This standard
supplies the contract the compiler does not enforce.

- Put one `<summary>` directly above the documented member. Give a public
  constructor with parameters a summary too.
- Omit `<param>` and `<returns>` when they only restate a name or a type. Keep
  them when they add units, nullability, ownership, constraints, or real result
  semantics.
- Be all-or-nothing for parameters: document every parameter, or fold the
  explanation into the summary.
- Use `<value>` for a property contract, `<exception>` for errors a caller is
  expected to handle, and `<see cref="..."/>` for contract-relevant symbols.
- Do not repeat the same fact in the summary, the parameters, and the returns.
- Do not add decorative file headers or section-divider comments.

## Length and width

An implementation-comment block is a consecutive run of whole-line `//` or `#`
comments; a blank line, a line of code, or a doc comment ends it. **The combined
trimmed text of one block must be at most 200 characters.** This is a single
aggregate budget, not 200 characters per line, and neither the indentation nor
the `//` marker counts toward it.

A `///` block and a PowerShell block comment are exempt from that budget. They
are exempt from how much they may say, not from how they are written: the
content rules and the width limit still apply.

Every comment line must fit `max_line_length` from `.editorconfig`, currently
120 display columns, counting indentation and the marker. A tab advances to the
next four-column stop.

If a block needs more than 200 characters, the usual answer is that it belongs
in an XML summary on the member, or that it is explaining something the code
should express directly.

## Tests

A test comment should explain a non-obvious fixture, fake, or setup constraint.
It should not restate the test name or say that the test exists for coverage.

## Running the check

```
pwsh ./scripts/comment-hygiene.ps1              # lines this branch adds; fails on a violation
pwsh ./scripts/comment-hygiene.ps1 -Advisory    # same scan, always exits 0
pwsh ./scripts/comment-hygiene.ps1 -Full -Advisory  # size the existing debt
pwsh ./scripts/comment-hygiene.ps1 -SelfTest    # verify the rules themselves
```

Agents must run `./local_check.sh --agent-strict`, which runs the blocking scan
before the format, build, and test steps. Do not drop the flag to get a run
through; fix the comments instead. The pull request workflow is advisory, so a
clean CI check is not evidence that the strict check passed.
