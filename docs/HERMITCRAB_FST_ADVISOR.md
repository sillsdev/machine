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

## 7. Engine extension — the *regularity* axis (added, kept orthogonal to the warning)

The advisor answers one question — **"is this slow in today's engine?"** — and the user keeps
asking exactly that ("which rule blew up the grammar", "which cases are still slow"). The
extension adds a *second, independent* question — **"does an FST exist for this in principle?"**
(regular vs non-regular) — **without letting the answer soften the slow-today warning.**

Why the two must not be merged: the engine that turns "regular" into "fast" is the FST compiler,
and **it does not exist yet** (gated on the unbuilt spike, `HERMITCRAB_FST_PLAN.md` §7). So
"regular" today means *fast eventually, slow now*. If a vowel-harmony rule reported as
`Cost / Tier-1-reachable`, a non-expert reads "fine" — when in the only engine that ships it is
the worst case (harmony on a common segment ⇒ ~every word on the slow path). The severity must
keep telling the truth about **today**.

So **severity is unchanged** — it means *escapes the finite-state fast path in today's engine*
(forces the combinatorial search). Harmony, infixation, and reduplication (bounded or not) all
stay `Escape`: all are slow now. We only *add* a `Regular` axis that says whether an FST could
reclaim it later, and we report it as a **separate reclaim-path line that never upgrades the
tier**.

The theory behind the new axis is **Kaplan & Kay (1994)**: a context-sensitive rewrite rule
`φ → ψ / λ _ ρ` with regular `φ, ψ, λ, ρ`, applied obligatorily/directionally (not recursively
into its own unbounded output), **denotes a regular relation — however long `λ`/`ρ` are.** HC's
`RewriteRule` is this form, and its `Rhs` is a *bounded segment specification*, not a copy (copy
lives only in morphological `CopyFromInput`). So:

- **Unbounded-environment rewrite (harmony/spread): `Regular = true`** — *iff* the rule's own
  `Lhs`/`Rhs` are bounded (only the environment is unbounded). Reclaim later by **state-encoding**
  the spreading feature (or two-level pre-image arcs). If the `Lhs`/`Rhs` themselves are unbounded
  we cannot confirm regularity → `Regular = false` (conservative). Stays `Escape` (slow today).
- **Reduplication splits by boundedness of the copied part.** Look up the copied part's defining
  `Lhs` pattern by name: a **length-bounded** reduplicant (fixed CV/CVC) is a finite copy →
  `Regular = true` (reclaim by bounded fold). Copying an **unbounded** part (whole stem,
  `Annotation(any).OneOrMore`) is the one genuinely non-regular operation (`{ww}` is not regular)
  → `Regular = false`. **If the part can't be resolved, default `Regular = false` (warn).** Stays
  `Escape` either way.
- **Infixation** at a pattern-defined slot: `Regular = true` (the split is a regular pattern;
  reclaim by bounded fold / the per-word probe). Stays `Escape`.

### The reclaim map (how a `Regular` case *would* be made fast — once the compiler exists)

| Construct | `Regular` | Slow today? | Reclaim path (needs the FST compiler) |
|---|---|---|---|
| Unbounded-environment rewrite (harmony/spread) | ✅ (bounded Lhs/Rhs) | **yes** | state-encode the spreading feature / two-level pre-image arcs |
| Bounded reduplication (fixed CV reduplicant) | ✅ | **yes** | bounded fold — emit the finite copy as arcs |
| Infixation (pattern-defined slot) | ✅ | **yes** | bounded fold / per-word strip-and-reparse probe |
| Deletion | ✅ | **yes** | inverse probe — re-insert candidate deleted segments, re-parse |
| Unbounded-copy reduplication | ❌ | **yes** | per-word probe only (when surface-invariant); else search |

`Regular` and `Probeable` (§5a) are both *paths forward*, never excuses: `Regular` = "an FST
could reclaim it (compiler pending)", `Probeable` = "a runtime strip-and-reparse is sound". The
severity and tier keep warning about today.

### Implementation of the extension

- Add `GrammarAdvisory.Regular` (`bool?`): true = an FST exists in principle (reclaim by
  compiling), false = genuinely non-regular / unconfirmable, null = N/A. **Severity is not
  changed by it.**
- Reduplication: resolve the copied part's `Lhs` pattern by name; bounded → `Regular=true`,
  unbounded or unresolved → `Regular=false`. Severity stays `Escape`.
- Infixation: `Regular=true`; severity stays `Escape`; keep the per-word-probe advice.
- Unbounded-environment rewrite: `Regular = !(unbounded Lhs or Rhs)`; severity stays `Escape`;
  advice = Kaplan–Kay + state-encoding, explicitly "regular in principle but slow in today's
  engine".
- Report: count `RegularEscapeCount` vs `NonRegularEscapeCount`; emit a **reclaim-path line**
  ("N of M escapes are FST-reclaimable once the compiler exists; all M are slow in today's
  engine"). **The tier verdict is unchanged** — no "Tier 1-reachable" upgrade.
- Tests: a non-expert sanity check — a grammar whose only complex rule is harmony must still
  report a slow-path warning (escape present), with `Regular=true` only as the reclaim note.
  Unbounded-copy reduplication ⇒ `Regular=false`; bounded reduplicant ⇒ `Regular=true`;
  infixation ⇒ `Escape` + `Regular=true` (the committed infix test keeps its severity). Sena
  unchanged (Tier 1).
