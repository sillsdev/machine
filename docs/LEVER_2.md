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

## Target pipeline

```
        analysis (morpheme tokens)
              ▲
              │  read tokens off the traversed lexicon states
   surface ──▶│  walk  Pinv ∘ Lex   (one composed transducer, built once)
              │
   Pinv : surface → underlying     (inverse phonology transducer)
   Lex  : underlying → underlying  (morphotactic network, identity tape; tokens on states)
```

Analyze = walk the composed transducer on the surface; the lexicon-component of each traversed state
carries the morpheme token (same token-accumulation the current `FstTemplateAnalyzer` walk already does).
`VerifiedFstAnalyzer` still confirms — composition need not be perfectly sound, only complete-ish; verify
is the soundness gate, as everywhere.

## The three blockers (and resolution)

**Blocker 1 — tokens live in a side-table, not on an output tape.** `FstTemplateAnalyzer` accumulates
morpheme tokens via `_tokenOnEntry[state]` during the walk; it is an acceptor, not a transducer.
*Resolution:* keep tokens **state-based** through composition. A composed state is a pair
`(pinv-state, lex-state)`; it inherits the token of its `lex-state` component. This needs the composition
to preserve which `lex-state` each composed state came from — `Fst.Compose` builds `(s1,s2)` pairs
internally but does not expose the map, so we either (a) extend a composition that records it, or (b)
encode tokens as pass-through **output symbols** on the lexicon's output tape (classical lexc multichar
symbols) and read the output tape. (a) is less invasive here.

**Blocker 2 — HC phonology is not a composable FST.** HC compiles rules to `Matcher` (an acceptor FSA)
+ imperative shape mutation (`AnalysisRewriteRule`/`SynthesisRewriteRule`), *not* to a transducer. There
is no rewrite-rule→transducer compiler in-repo. *Resolution (the real work):* build one. Two routes:
  - **B-direct:** compile each `RewriteRule` to an `Fst<Shape,ShapeNode>` transducer (Kaplan–Kay: a
    bounded rewrite is a regular relation). Hardest; full generality (deletion, epenthesis, environments).
  - **B-probe (reuse HC, tractable):** build a **bounded-context Mealy transducer by probing synthesis** —
    states encode the last *k* segments (incl. the boundary marker); for each (context, segment) emit the
    surface HC synthesizes. Bounded context ⇒ bounded states. Deletion/epenthesis = epsilon/extra output
    arcs detected by length change. Reuses HC's real phonology (no reimplementation); only the
    *unbounded-environment* rules (the census escapes) fall outside and ride the engine.

**Blocker 3 — unification-arc composition.** *Resolved by the library:* `Fst.Compose` already composes
over feature-structure arcs (matches `arc1.output` against `arc2.input`, unifying). Confirmed in
`src/SIL.Machine/FiniteState/Fst.cs`.

## Build plan (spike-first, incremental, verify-gated throughout)

1. ☐ **Spike** — hand-build two tiny `Fst<Shape,ShapeNode>` transducers with input:output arcs and
   `Compose` them; confirm the composed relation and that we can walk it on a surface. De-risks
   Blockers 1 & 3 concretely before any big build.
2. ☐ **Lexicon-as-transducer** — emit `FstTemplateAnalyzer`'s underlying network as an identity
   transducer (input=output=underlying segment), tokens preserved per state.
3. ☐ **Phonology transducer (B-probe, substitution first)** — bounded-context Mealy transducer from
   synthesis probing; substitution rules only (no length change).
4. ☐ **Compose + walk + tokens** — `Pinv ∘ Lex`, walk on surface, recover tokens; demonstrate on the
   `t→d` toy end-to-end (surface→tokens through composition, not the side-table hack).
5. ☐ **Deletion / epenthesis** — extend the phonology transducer to length-changing arcs; target the
   Indonesian `meN-` nasal substitution (the case Lever 1 only reaches by enumeration).
6. ☐ **Measure** on Indonesian: build time (should be ~grammar-sized, not 5 s enumeration) and coverage.

Soundness is never at risk: `VerifiedFstAnalyzer` confirms every candidate, and an uncovered/over-
generated path falls to the engine via the parity gate. The only variables are coverage and build time.
