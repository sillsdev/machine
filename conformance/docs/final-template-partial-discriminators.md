# Final-template partial discriminators

This synthetic grammar checks when an ordinary rule may follow a final or non-final template,
and when a partial word survives an applicable template battery with no matching output.

The ten expected rows were derived from the synthesis gates and checked against C# at
`8bad193454a14422a3be59c3156513691ae3da3b` on 2026-09-15. The original PanGloss staged fixture
records earlier C# checks at `4823a05a`; no new parser implementation is included here.

- DAK: a non-partial ordinary rule is blocked after a final template. The same rule works after
  a non-final template when a final template later closes the derivation.
- PIL: a partial ordinary rule is blocked after a non-final template; a non-partial control succeeds.
  The partial rule succeeds after a final template.
- NIB: the partial rule makes `nibgi`; its applicable template's mandatory slot requires a final
  `k`, so the battery has no output. Partial-word pass-through preserves the derivation.

Tag rules are template-only; ordinary rules are stratum-only. Parts of speech isolate the three
branches. This does not test a rule invoked both ways, partial roots, or lower-stratum continuations.

## Verification

The two-fixture scratch-root self-check passes with default memoization and with
`--no-memoization`: 2 passed, 0 failed, 0 skipped, 16 word rows total.

Each independent mutation keeps the original expected rows and makes this fixture fail:
- `mrGate2Partial partial=false`: 2/10 mismatches; `pilnbvbfb` gains a parse and `pilfbvb` loses it.
- `mrGate3Partial partial=false`: 1/10 mismatch; `nibgi` loses its parse.
- `nonFinalTemplateA final=true`: 2/10 mismatches; `daknagafa` loses its parse and `dakna` gains one.

Unmodified grammar remains green. These are grammar-sensitivity tests, not proof that #491's
analysis optimization executes or an engine fix-removed test.
Tracking: [issue #507](https://github.com/sillsdev/machine/issues/507), [PR #491](https://github.com/sillsdev/machine/pull/491).
