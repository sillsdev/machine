# Grammar FST Advisor — plan

A grammar evolves; one new rule can quietly push it from the fast finite-state path into the
slow combinatorial search. This plan adds a **grammar advisor/linter** that, for any HermitCrab
`Language`, flags the rules that make parsing expensive or block FST compilation, and gives the
grammar engineer **actionable write-ups**: *why* a rule is costly, how to **constrain** it back
into fast territory, and an **alternative formulation** to try.

It is the front-end to the FST work (`HERMITCRAB_FST_PLAN.md`): the same per-rule classification
that decides the FST tier also drives the warnings.

## 1. What it does

Input: a compiled `Language`. Output: a `GrammarFstReport` — a list of per-rule advisories plus
an overall **tier verdict**. Each advisory has:
- **rule name + kind** (affix / phonological / compounding / template),
- **severity**: `Escape` (breaks FST → forces search), `Cost` (inflates the search fan-out), or
  `Info`,
- **issue**: one sentence on what's expensive and why,
- **advice**: "constrain it like this" and/or "try this instead".

## 2. The classifier (what flags what)

Detected from the object model (`AffixProcessRule.Allomorphs` → `Rhs` actions; `RewriteRule`
Lhs/Subrule environments; `MorphologicalOutputAction.PartName`; `Quantifier.Max/MinOccur`):

| Signal | Severity | Issue | Advice |
|---|---|---|---|
| **Reduplication** — a part copied ≥2× by `CopyFromInput` | **Escape** | copying an unbounded span isn't finite-state; forces search for any word it could apply to | "If the reduplicant is a fixed size (e.g. one CV syllable), bound the copied part's length → finite-state. If only a few forms reduplicate, list them as lexical entries. Else the grammar stays in the hybrid/search tier." |
| **Infixation / stem split** — ≥2 `CopyFromInput` of *different* parts | **Escape** (unless bounded) | the stem is split at a content-determined position | "If the infix position is fixed, encode it as a bounded split; a variable split blocks FST." |
| **Process modification** — `ModifyFromInput` present | **Info/verify** | FST-able only if the modification is local/bounded | "Local feature change in a fixed context = fine; non-local/agreement = blocks FST — try a bounded reformulation." |
| **Phonological rewrite rule** present | **Info/verify** | FST-able iff its environment is a bounded window | "Bound the left/right environment to the actual window (usually 1–2 segments); unbounded context blocks FST." |
| **Deletion rule** — Lhs longer than Rhs | **Cost** | analysis must guess where deleted segments were and re-insert them (× `DeletionReapplications`) | "Keep `DeletionReapplications` as low as the language needs; bounded deletion context is still FST-able." |
| **Unbounded environment** — a `Quantifier` with infinite `MaxOccur` in an environment | **Escape** | matches an arbitrary-length span | "Replace the `+`/`*` context with the fixed window the rule really needs." |
| **Many allomorphs** on one rule (> threshold) | **Cost** | each allomorph multiplies un-application branching | "Consolidate via environment conditioning where possible." |
| Compounding rule | **Info** | bounded by `MaxStemCount`, so finite | — |

## 3. Tier verdict (static; corpus refines it)

- **0 Escape advisories** → **Tier 1 candidate** (fully FST-able) — confirm with the FST compile
  + corpus parity check.
- **a few Escapes** → **Tier 2 candidate** (hybrid: escapes fall back to search) — run the corpus
  fallback-rate measurement to confirm it's worth it vs. Tier 3.
- **pervasive Escapes** → **Tier 3** (search only).

The static report can't compute the corpus-weighted fallback rate, so it reports the tier
*candidate* + the escape list; the FST pipeline's corpus pass (`HERMITCRAB_FST_PLAN.md` §1)
confirms it.

## 4. The "one new rule blew up the grammar" workflow

Run the advisor before/after a grammar change (or in CI). A new `Escape` advisory that flips the
tier (e.g. Tier 1 → Tier 2) is the warning: it names the offending rule, says it moved the whole
grammar off the fast path, and gives the constrain/alternative write-up. Grammar engineers get
"this rule made parsing slow, here's how to keep it fast" at authoring time.

## 5. Implementation

- `GrammarFstAdvisor.Analyze(Language) → GrammarFstReport` in the HermitCrab library (pure static
  analysis of the object model; no parsing, no corpus needed).
- `GrammarFstReport.Format()` for a readable dump.
- Tests: a normal concatenative grammar → Tier 1, no escapes; add a reduplication rule → the
  advisor flags it `Escape` with the reduplication write-up and downgrades the tier.
- Run on the real Sena grammar and report the advisories + tier.

## 6. Validate on Sena

Census already showed Sena is concatenative + no rewrite rules + no productive reduplication →
expect **Tier 1, zero escapes**, possibly a few `Cost`/`Info` notes (allomorph counts,
compounding). That both validates the classifier (no false escapes) and confirms Sena is the
fast-path case.
