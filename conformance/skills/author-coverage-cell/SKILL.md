---
name: author-coverage-cell
description: >-
  Use when asked to cover, satisfy, witness, or close an obligation cell in the HermitCrab
  conformance suite — anything of the form "cover cell X", "satisfy this dataflow obligation",
  "add words so this chain is witnessed", "close a coverage gap", or when handed a cell id from
  conformance/dataflow-obligations.tsv. Authors the minimal-pair words in an existing language
  grammar and the claimed_cells entry that records the evidence, then verifies by severance rather
  than by inspection. Does NOT author new grammars, new fixtures, or grammar.xml content — if the
  construct is absent from every language grammar, this skill reports that and stops.
---

# Authoring a coverage cell

You are given an **obligation cell id** from `conformance/dataflow-obligations.tsv`. Your job is to
make it `Satisfied`: author words in an existing language-family grammar such that severing the
construct changes a parse, then record the claim.

Read `conformance/docs/what-it-claims.md` first if you have not. The distinction it draws between
*present* and *witnessed* is the one this task turns on.

## Phase 1 — Understand the cell

Read its card in `conformance/evidence-cards/` (find it via `index.tsv`). The card states the role in
plain English, the chain, and the current machine status.

A cell id is `payload :: writer.attr -> reader.attr :: kind:role`. The four MC/DC roles:

| role | means |
|---|---|
| `PresentControl` | payload present, the gated rule not applied — the baseline |
| `PresentGatedForm` | payload present, gated rule attempted — the interesting case |
| `AbsentControl` | payload absent, gated rule not applied |
| `AbsentGatedForm` | payload absent, gated rule attempted — proves the rule *can* fire |

Stop and state, in one sentence, what would have to be true of a word for it to occupy this cell. If
you cannot, do not proceed — you will author something plausible and wrong.

## Phase 2 — Find a host grammar

Search `conformance/languages/*/grammar.xml` for one that already declares **both** the writer
construct and the reader construct. `conformance/fold-in-candidates.tsv` marks obligations whose
construct is already structurally present.

- **Never edit `grammar.xml`.** If no language grammar hosts both, report `wrong-grammar` and stop.
  That is a real finding about the corpus, not a failure of yours.
- Prefer a grammar where the construct is already used for something, so the new words sit naturally
  among existing ones.

## Phase 3 — Design the minimal pair

The cell needs a **control** — that is the entire point. From `conformance/docs/`:

> Test every mechanism that can change a parse. A fixture must contain enough cases that exactly one
> explanation of the observed difference survives.

For a gated chain that means two entries and four words: an entry carrying the payload and one not,
each with a bare-root control and a form where the gated rule applies. The ungated arm is what
distinguishes "the gate blocked it" from "the rule never fires at all". A fixture with only the gated
stem exercises the attribute and witnesses nothing.

The pair must differ in **exactly one** condition. Two words that both parse as expected but differ
in two conditions establish nothing — and read perfectly well, which is why this is the easiest
mistake to make.

## Phase 4 — Author the words

Follow the host fixture's existing conventions exactly: form shape, gloss style, `note:` density,
`exercises:` vocabulary.

- **Realistic forms.** These are coherent language-family grammars. A word must be one the grammar
  would plausibly generate, not a minimal string that trips the construct.
- **Synthetic only.** Never actual-language data; never name anything after a real language.
  Typological family belongs in a comment. Cite WALS-style sources in `inspired_by` as the existing
  fixtures do.
- Write the `note:` for every new word. The notes carry the derivation reasoning and are the most
  valuable part of these files.

## Phase 5 — Verify by severance, not by reading

**This is the phase that decides whether you succeeded, and it is the one most often skipped.**

Regenerate the witness ledger and confirm the cell's status actually changed. Adding a word that
*mentions* the construct proves nothing; the sweep is the result and your reasoning is not.

If the cell did not become `Satisfied`, you have not covered it. Report `no-witness` with what you
tried. Do not adjust an expectation, weaken a gate, or claim partial success.

## Phase 6 — Record the claim

Add a `claimed_cells` entry on the word that carries the witness:

```yaml
claimed_cells:
  - cell: <exact cell id>
    severing: <what is removed, in plain English>
    before: "<outcome with the construct intact>"
    after:  "<outcome with it severed>"
    distinct_from: <the counterpart word>
    proof: <why this word demonstrates this cell>
```

`before`/`after` must be the **recomputed** values, copied from the ledger — never predicted. The
gate re-severs and compares; a value you guessed will be caught and reported stale.

Then run the gate and confirm it accepts.

## Reporting

State, in this order: the cell id, the host grammar and why, the words added, the severance result
that proves the witness, and the gate outcome.

If you failed, say which phase and which failure class (`wrong-grammar`, `not-minimal-pair`,
`no-witness`, `unrealistic-word`, `bad-cell-id`, `evidence-mismatch`). A classified failure is useful
data for improving these instructions; an unclassified one is not.

**Never report a cell as covered because you added a word for it.** That conflation has been made
five times on this programme at five different levels, and this phase is where it would happen a
sixth.
