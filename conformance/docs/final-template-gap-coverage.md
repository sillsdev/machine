# Final-template gap coverage

This synthetic grammar covers the three gaps `final-template-partial-discriminators.md` names in its
own text: "a rule invoked both ways, partial roots, or lower-stratum continuations." Each is a separate
branch (posA, posB, posC) sharing one grammar so the fixture stays small; none of the three branches
interacts with either of the others.

- **Branch A (posA, dual-role rule).** `mrDualA` is named BOTH in the Root stratum's own
  `morphologicalRules` list (an ordinary rule) AND inside `templateA`'s one Slot (a template-slot rule) --
  the same rule ID, two nominal invocation roles, the case `template-category-sharing`'s own commentary on
  a rule that is both stratum-listed and template-reachable was pointing at without a fixture behind it.
  `dukom` (mrDualA fires once) is the one surviving derivation; `dukomom` (mrDualA fired twice, once from
  each nominal role) has none, because `MorphologicalRule`'s default `multipleApplication` is 1 and it is
  the RULE's own application count that is capped, not one count per invocation role. Removing the dual
  listing entirely (mrDualA left template-only) changes nothing observable here -- see Verification --
  which is itself the finding: for a single mandatory Slot holding the stratum's only ordinary rule, the
  ordinary-cascade role never contributes a surviving derivation of its own; the template is the only role
  that ever actually closes a word.
- **Branch B (posB, partial root).** `eTikPartial` sets `partial="true"` on the LEXICAL ENTRY itself.
  `SynthesisAffixTemplatesRule.cs`'s `!input.RootAllomorph.Morpheme.IsPartial` gate skips the entire
  template battery outright for a partial root, before any Slot rule is even tried -- a different code
  path than `final-template-partial-discriminators`' NIB case, which used a partial RULE and an
  inapplicable battery (the battery IS tried there; it just produces no output). `tik` ends in "k", the
  same shape that lets the identical Slot rule `mrSlotB` fire on the non-partial root `nok`, so the
  contrast is real: the affix is structurally available and still never attempted.
- **Branch C (posC, lower-stratum continuation).** The grammar has two Strata for the first time in this
  fixture family: `Root` (Depth 0, holds all the roots) and `Outer` (Depth 1, holds posC's only template).
  `mrRootPrefixC` applies at Root; the resulting stem passes through Root's own pass-through (posC has no
  template there) into Outer, where `templateC` applies `mrOuterSuffixC`. That Slot rule is anchored on
  the surface's FIRST segment being "z", true only because the Root-stratum prefix already fired, so
  Outer's derivation cannot close without it. Analysing `zaboteshi` must therefore genuinely continue
  analysis from the Outer stratum back down into the lower-depth Root stratum to reach `BOTC` -- not a
  single-stratum derivation using two rule names.

## Verification

The self-check passes all 60 fixtures on this branch, including this fixture's 12 word rows. Three independent runs of
the grammar through the C# founding oracle (`hc-dotnet-wrapper.sh`) produced byte-identical signatures and
counts each time.

Each mutation below keeps the fixture's original expected rows and makes it fail:

- Removing `mrDualA` from the Root stratum's own `morphologicalRules` list (leaving it template-only):
  produces IDENTICAL output for every word in this fixture. This is not a "the mutation had no effect"
  null result to worry about -- it IS branch A's finding, stated as a grammar-sensitivity check rather than
  asserted in prose: the ordinary-cascade role of a dual-listed rule contributes nothing a template-only
  listing would not already give you, for a mandatory single-slot template whose only slot rule is that
  same rule.
- `eTikPartial partial="false"`: 2/12 mismatches; `tik` loses its parse (the template battery is now
  tried and its mandatory Slot fires, so the bare root no longer closes) and `tikun` gains one
  (`TIKPARTIAL+SLOTB|tikun`).
- Dropping `mrOuterSuffixC`'s first-segment-"z" anchor (making its context `ncAny` like `mrDualA`'s own
  pattern): 1/12 mismatch; `boteshi` gains a parse (`BOTC+OUTERSUFFIXC|boteshi`), because the Outer
  template's mandatory Slot can now fire on the bare, unprefixed root, which defeats the branch's whole
  point (that the Root-stratum prefix is a de facto prerequisite for Outer's derivation to close).

Unmodified grammar remains green. These are grammar-sensitivity tests, not proof that PR #491's analysis
optimization executes; the analysis-side `FinalTemplateState` pruning this fixture is meant to feed
evidence to does not exist on this fixture's own base branch at all (confirmed: zero references in
`AnalysisStratumRule.cs`), so it cannot be exercised from this repository state. See the task report for
the corresponding prune-execution measurement taken against PR #491 in a separate, disposable worktree
-- not committed here.

Tracking: [issue #507](https://github.com/sillsdev/machine/issues/507), [PR #491](https://github.com/sillsdev/machine/pull/491).

This fixture does NOT test: multiple dual-role rules interacting with each other; a dual-role rule with
`multipleApplication` raised above 1 (which could let both nominal roles fire on the same derivation and
would need its own signature); a partial root combined with a non-mandatory (optional) Slot; or more than
two strata. Those remain open.
