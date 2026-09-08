# Final-template interleaving prune

**Status:** rejected and removed from production. **Portable:** the proof is portable where its guard holds.

The experiment mirrored synthesis's rule that a non-template affix process normally cannot follow a final
template. Analysis could skip the inverse interleaving when every relevant template was final and the grammar
contained no partial morphemes.

The available real grammars defeated the useful guard: Sena has 25 partial lexical entries and six partial
rules; Amharic and Mbugwe each have a partial rule. The prune fired zero times on all three. Synthetic tests
proved that the rule could fire safely, but that establishes correctness, not value. Carrying production
branches, state, toggles, and counters for a zero-effect optimization violated the final minimum-surface goal,
so all of them were removed.

Reopen only for a corpus with the reported pathological final-template interleavings and no partial
morphemes, or with a proved narrower per-state partiality condition. Measure real prunes before shipping.

## Addendum 2026-09-03
Superseded in part: `docs/pr491-final-template-prune-upper-bound.md` measures the same prune with the partiality
guard lifted at 8-33x on Mbugwe and Sena with identical analysis sets. The guard, not the mechanism, is what made
it inert, and partial lexical entries can be excluded from the guard by proof. Reopen per that document.
