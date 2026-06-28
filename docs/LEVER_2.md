# LEVER_2 — forward FST∘FST composition (grammar-sized, ~100% bounded morphology)

The asymptotic fix for "stay fast as features grow." Instead of enumerating word-forms
(`ForwardSynthesisProposer`, O(language)) or un-applying phonology on a boundary-less surface
(`ComposedPhonologyProposer`, which over-generates), build the analyzer the **classical FST-morphology
way**: compile the morphotactics and the phonology each to a transducer and **compose** them into one
surface↔analysis machine. Composition is a graph-algebra operation on the *automata*, not the language —
build cost scales with the **grammar** (arcs), not the number of words, and it shares structure, so a new
affix adds arcs, not a multiplicative blow-up.

## Why composition succeeds where the pivots failed

- **vs. enumeration (`ForwardSynthesisProposer`)**: phonology is applied to the `meN` arc *once*, in the
  network, shared across every root — grammar-sized, not language-sized.
- **vs. inverse (`ComposedPhonologyProposer`)**: the morpheme boundary `+` is an *arc in the lexicon
  network*, so when the phonology transducer composes against it the boundary-conditioned rule sees the
  right context. No bare-surface ambiguity — that is exactly what broke runtime inversion.

Lever 1 (per-morpheme surface precompile) is a *local approximation* of this; Lever 2 is the exact,
global version.

## Target pipeline — LAZY composition (no materialized `Fst.Compose`)

Don't build `Pinv ∘ Lex` as an object. Walk the surface maintaining a frontier of configs
`(pinvState, lexState, tokens)` — an on-the-fly product automaton:

```
   surface segment s ─▶ for each config (pinvState, lexState, tokens):
       for each Pinv arc consuming s with underlying output u:        (substitution / identity)
           if some Lex arc at lexState has input unifying u:
               advance both → (pinvState', lexState', tokens + tokenOf(lexState'))
       for each Pinv ε-input arc with underlying output u:            (DELETION restoration — consumes no surface)
           if some Lex arc at lexState has input unifying u:
               advance both, re-process s
   accept where pinvState and lexState are both accepting; emit accumulated tokens
```

Analyze = this walk; tokens come off the traversed **lex** states exactly as the current
`FstTemplateAnalyzer` walk already does. `VerifiedFstAnalyzer` still confirms every candidate — verify is
the soundness gate, as everywhere.

**The property this must prove:** a `Pinv` ε-arc that "restores a deleted segment" only survives if a
`Lex` arc actually has that underlying segment at that point. That lexicon constraint is *exactly* what
the runtime inverse lacked (it restored everywhere → `ⁿmeⁿnⁿpuⁿlis`). Composition prunes it because the
two machines advance in lockstep.

## The three blockers (and resolution)

**Blocker 1 — tokens in a side-table, not an output tape. → DISSOLVED by lazy composition.** `lexState`
is in the config, so tokens stay state-based; no token-map to recover, no output-tape hack. The walk is
a product-automaton extension of the existing `EpsilonClosure`/NFA walk. `Lex` stays the acceptor
`FstTemplateAnalyzer` already builds — use the **default ctor** (underlying-only arcs, no surface
precompile, so phonology isn't double-applied).

**Blocker 3 — unification-arc composition. → MOOT.** We never call `Fst.Compose`; the lazy walk unifies
`Pinv` output against `Lex` input directly (same `IsUnifiable` the walk already uses).

**Blocker 2 — HC phonology is not a transducer. → THE REAL WORK.** Build `Pinv` (surface→underlying).
HC compiles rules to `Matcher` + imperative mutation, not a transducer; no rewrite→transducer compiler
exists in-repo. Routes:
  - **B-probe (reuse HC):** a bounded-context Mealy transducer built by probing HC synthesis — states
    encode the last *k* segments (incl. the boundary marker); for each (context, segment) record the
    surface HC produces; invert. Deletion/epenthesis = ε arcs from length change. **Risk (advisor):** HC
    phonology is a multi-rule cascade with feeding/bleeding; a per-context probe only reproduces it if
    the combined effect stays in the window, and deletion breaks clean underlying↔surface alignment.
    *Must be validated on a two-interacting-rule case, not one rule.*
  - **B-direct:** compile each `RewriteRule` to a per-rule `Fst` transducer and lazy-compose the cascade
    (Kaplan–Kay). Classically safe; more work (per-rule compilation).

## Build plan (spike-first; the spike targets DELETION)

1. ☑ **Deletion spike (algorithm-level)** — `LeverTwoSpikeTests`: symbol-alphabet lazy composition of a
   hand-built `Pinv` (with an ε-input arc restoring a deleted `t`) ⊗ a tiny lexicon. Proven:
   `"sad" → [sat, -d]`; with a bare root `sad` added, **exactly** `{sat+-d, sad}` (restoration is
   lexicon-constrained — no `ⁿmeⁿnⁿpuⁿlis` garbage); non-word → nothing. Targets deletion, not
   substitution.
2. ☑ **Lazy-compose walk, REAL HC types** — `InversePhonology` (surface→underlying transducer with
   ε-input restoration arcs) + `FstTemplateAnalyzer.AnalyzeComposed` (product walk over
   `(pinvState, lexState, tokens)`; lexicon ε-arcs and Pinv ε-restorations both handled in the closure).
   Proven by `LeverTwo_LazyComposition_RecoversBoundaryDeletion_RealTypes`: a `kd`-suffix whose `k`
   deletes before `d` surfaces as `d`; `"sagd"` recovers `[sag, KD]` by restoring the `k` (lexicon-
   constrained), sound (⊆ engine), non-word → nothing. **Blockers 1 & 3 resolved; Blocker 2's consuming
   engine built and proven, including deletion.**
3. ☐ **`Pinv` compiler** — the remaining Blocker 2 work: auto-build `InversePhonology` from a grammar's
   phonological rules (the spikes use a *hand-built* `Pinv`). B-probe or B-direct; **must be validated on
   a two-interacting-rule cascade** (assimilation + deletion) — the advisor's gate, since feeding/bleeding
   + deletion break clean alignment. This is the genuine frontier.
4. ☐ **`ComposedLexiconProposer` + measure** — wrap the walk as an opt-in `IConstructProposer`,
   verify-gated; measure on Indonesian `meN-` (build should be ~grammar-sized, not the 5 s enumeration).

## Status (what is proven vs. the frontier)

**Proven (committed, passing):** the Lever 2 *architecture* is real in this codebase. Lazy composition
recovers boundary **deletion** — the exact case that broke the runtime inverse — and the lexicon prunes
the over-restoration, with real HC types end-to-end. Blockers 1 (tokens state-based) and 3 (no
`Fst.Compose`) are dissolved by the lazy walk; Blocker 2's consuming side is done.

**Frontier (the honest wall):** the general `Pinv` *compiler* — turning a grammar's phonological rules
(substitution, deletion, assimilation, and their feeding/bleeding cascades) into the `InversePhonology`
transducer automatically. The spikes prove that *given* the right `Pinv`, everything downstream works;
building that `Pinv` for arbitrary cascades is the multi-week subsystem. Until it exists, Lever 1
(guided forward-synthesis, 42→69 on Indonesian) remains the pragmatic accelerator. Soundness is never at
risk either way — `VerifiedFstAnalyzer` + the parity gate gate everything.

## Honest gate
Work the deletion spike for real. If end-to-end recovery + pruning hold, generalize `Pinv` with
confidence. If it resists after genuine effort, that is the recorded finding — "Blocker 2
deletion/cascade is the wall" — and Lever-1 guided enumeration is the documented pragmatic fallback, not
a silent retreat. Soundness is never at risk either way (verify + parity gate).
