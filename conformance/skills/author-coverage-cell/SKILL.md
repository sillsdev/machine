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

**Before anything else, run the feasibility check.** Most of an authoring budget has more than once
been spent proving an obligation impossible:

```powershell
conformance\tools\check-obligation-feasibility.ps1 -Writer <Element.attr> -Reader <Element.attr> [-ControlElement <Element>]
```

It answers mechanically whether the attribute can be severed at all, whether one fixture declares both
ends of the chain, and whether a Control arm can be attributed. If it exits non-zero, this is not an
authoring task -- record the blocker and stop.

Then read `conformance/docs/severance-mechanics.md`. It states what severance can and cannot do, which
attributes are unsevereable, and three engine behaviours that each cost an author most of a budget to
rediscover. Do not reason about severance without it.

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

- **Default: never edit `grammar.xml`.** If no language grammar hosts both, report `wrong-grammar`
  and stop. That is a real finding about the corpus, not a failure of yours.
- Prefer a grammar where the construct is already used for something, so the new words sit naturally
  among existing ones.

### The one exception: a small, engine-justified grammar edit

Some assignments (bucket-2 coverage obligations) explicitly license editing a grammar when the
construct is absent from every fixture but HCLoader can genuinely produce it and real morphology
exhibits it. That relaxation is narrow and directional — it does not turn Phase 2 into "invent
whatever makes the cell green."

**The rule.** An edit is legitimate only when all three hold:

1. **HCLoader demonstrably emits the construct**, cited by file and line (e.g. `HCLoader.cs:2057`
   for `PhonologicalSubrule.RequiredMprFeatures`).
2. **Real morphology exhibits it** — the shape you are adding is a genuine, nameable linguistic
   phenomenon (a minor rule, a lexically-conditioned exception, a category-changing derivation),
   not a string manufactured to trip a check.
3. **The reasoning runs from engine behaviour TO a grammar shape — never from a red cell to
   whatever turns it green.** Write down the HCLoader citation and the engine mechanism *before*
   deciding what to add. If you cannot state both before touching `grammar.xml`, do not make the
   edit.

The edit must still leave the grammar a coherent description of a plausible language: synthetic
data only, never named after a real language, typological family in a comment only if at all.

**The tell.** The direction of reasoning is everything. "This cell is red; if I add X the severance
will flip" is illegitimate even if X is grammatically well-formed and even if it works — the
grammar became a proof artifact for the cell instead of an independent thing the cell happens to
measure. "HCLoader.cs:969/1717 read/write MPR features on an ordinary affix; a derivational suffix
that marks a stem as already-derived and blocks a further derivation is ordinary morphology; let me
add that pair and see what it demonstrates" is legitimate, because the construct and its
justification exist before any cell's colour is consulted.

**Worked legitimate example.** Bucket-2 item D (`MorphologicalOutput.MPRFeatures ->
MorphologicalInput.excludedMPRFeatures`, "affix-conferred blocking"): HCLoader.cs:969
(`AffixProcessAllomorph.OutMprFeatures`) writes an MPR payload onto a rule's own output;
HCLoader.cs:1717 (`AffixProcessAllomorph.ExcludedMprFeatures`) reads it as an exclusion on a later
rule's input. This is ordinary derivational morphology — a suffix that marks its own output as
"already derived," making a further derivational step ineligible, exactly the shape English
`-ize`/`-ization` chains resist re-`-ize`-ing. The engine mechanism (`AddOutput`'s MPR-feature
write, `IsMatchExcluded`'s membership check) was read and cited *first*; only then were two new
rules (`mrConferExcl`, `mrExclReader`) and a witness word (`topdori`) added, on an isolated root
chosen specifically because it carries no family or co-occurrence constraints that could disturb
anything already tested in that fixture.

**Worked illegitimate example (do not do this).** Suppose `LexicalEntry.ruleFeatures ->
PhonologicalSubrule.requiredMPRFeatures` is red, and the temptation is: "add a root with
`ruleFeatures="mprX"` and a `PhonologicalSubrule` with `requiredMPRFeatures="mprX"`, run the
severance sweep, and see if it flips." That is illegitimate even before checking whether it works,
because the starting point was the red cell, not an engine citation — and in this specific case it
provably cannot work regardless: `SynthesisRewriteSubruleSpec.IsApplicable`
(`PhonologicalRules/SynthesisRewriteSubruleSpec.cs:47-49`) checks
`_subrule.RequiredMprFeatures.IsMatchRequired(input.MprFeatures, ...)` — a plain set-membership
test with no accumulation or overwrite semantics for a root-sourced feature. Severing
`LexicalEntry.ruleFeatures` can only ever *remove* a feature that was satisfying the requirement
(turning a pass into a fail — the wrong direction) or remove one that was already going to be
destroyed by some other write (a no-op). There is no grammar shape that flips this specific pairing
from fail to pass, so authoring words for it is `false-impossibility`'s mirror image: instead of
wrongly declaring victory impossible, it would be wrongly declaring victory achieved by adding
words to a chain the engine's own comparison logic forecloses. The fix when you hit this is to
write down the proof (as this paragraph does) and report `no-witness`, not to keep trying words.

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

### Severance is fixture-wide, and that is usually what you are missing

`Sever()` removes an attribute from **every** element in the fixture at once -- not from one entry.
So removing a payload does not merely turn one match into a mismatch. It can make a gate vacuous, or
delete a competing analysis, and thereby take a word from failing to parsing. A writer severance
CAN flip a word from fail to pass on a `required*` reader; the ledger already contains cells proving
it.

This matters because the paired witness you need is exactly a same-word fail-to-pass flip on both
sides, and reasoning about severance as if it were per-word makes that look impossible when it is
not.

### Before you conclude a cell cannot be witnessed, falsify that

A structural argument that a cell is unreachable is a claim like any other, and it is the easiest
claim to believe because it ends the task. So it carries the heaviest burden of proof here:

**Test your impossibility argument against a cell that is already `Satisfied`.** Read the satisfied
rows out of `conformance/dataflow-obligations.tsv`, take your argument, and ask whether it would also
forbid those. If it would, your argument is wrong -- discard it and author the words. This check has
already caught a confident, well-written, entirely wrong proof that five separate cells were
unreachable.

`wrong-grammar` is for a construct **absent from every language grammar** -- a fact you establish by
searching the grammars, not by deriving it. An argument that the words cannot be *made to flip* is
not that, and reporting it as a corpus finding when you never authored a word is the
`false-impossibility` failure class. You are expected to try, and a witnessed flip beats a proof.

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

### Regenerating fast enough to iterate

The full sweep re-parses every interface in every fixture and takes about seven minutes. You do not
need it while iterating. Point the tool at a throwaway root holding only your fixture:

```powershell
$root = "$env:TEMP\cellwork"
New-Item -ItemType Directory "$root\conformance\languages" -Force | Out-Null
Copy-Item conformance\languages\<your-fixture> "$root\conformance\languages\" -Recurse
Copy-Item conformance\HermitCrabInput.dtd,conformance\constructs.txt "$root\conformance\"
dotnet <hc-conformance.dll> --fixtures "$root\conformance" --repository-root $root `
    --write-coverage-traceability
```

That runs in seconds and its rows for your fixture are byte-identical to the ones the full sweep
produces -- severance is evaluated per fixture, so nothing outside yours can change them.

What it does **not** do is decide the obligation's status. `Satisfied` is a judgement across every
fixture, and a scoped root cannot see the others. So use the scoped run to answer "do my words flip
under severance", which is the question you are actually iterating on, and run the full sweep and the
gate once at the end to answer "is the cell now covered".

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

If you failed, say which phase and which failure class (`wrong-grammar`, `false-impossibility`, `not-minimal-pair`,
`no-witness`, `unrealistic-word`, `bad-cell-id`, `evidence-mismatch`). A classified failure is useful
data for improving these instructions; an unclassified one is not.

**Never report a cell as covered because you added a word for it.** That conflation has been made
five times on this programme at five different levels, and this phase is where it would happen a
sixth.
