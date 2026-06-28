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

1. ☐ **Deletion spike** — toy grammar with a *boundary deletion* rule we fully control (e.g. root `sat`
   + suffix `-d`, rule `t→∅ / _ d`, so `sat+d = satd → "sad"`). Build `Lex` = `FstTemplateAnalyzer`
   (default ctor). Hand-build the smallest `Pinv` that restores the deleted `t`. Lazy-compose-walk
   `"sad"` and **recover `[sat, -d]`**, and prove the restoration is *pruned* everywhere the lexicon
   lacks the `t`. Substitution would pass and lie — deletion is where every prior approach died, so the
   spike must hit it. If hand-building `Pinv` for one deletion case is too fiddly, that fiddliness *is*
   the signal about the general compiler.
2. ☐ **`Pinv` compiler** — generalize the hand-built `Pinv` into a builder (B-probe or B-direct per what
   the spike teaches), validated on a **two-interacting-rule** cascade (assimilation + deletion).
3. ☐ **`ComposedLexiconProposer`** — the lazy-compose walk as an `IMorphologicalAnalyzer`/`IConstructProposer`
   over `Lex` + `Pinv`; verify-gated; wired opt-in like the others.
4. ☐ **Measure** on Indonesian `meN-`: build time (should be ~grammar-sized, not the 5 s enumeration) and
   coverage vs. the Lever-1 forward-synth baseline (42→69).

## Honest gate
Work the deletion spike for real. If end-to-end recovery + pruning hold, generalize `Pinv` with
confidence. If it resists after genuine effort, that is the recorded finding — "Blocker 2
deletion/cascade is the wall" — and Lever-1 guided enumeration is the documented pragmatic fallback, not
a silent retreat. Soundness is never at risk either way (verify + parity gate).
