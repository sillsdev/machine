# Partial Morpheme Health Check

## Goal

Extend `GrammarHealthChecker` on pull request #475 to identify every partially
analyzed HermitCrab morpheme. The finding should tell grammar authors to finish
the incomplete analysis and explain that partial morphemes can broaden search
and prevent safe final-template pruning.

This is a production-readiness diagnostic. It does not change grammar loading,
parsing, synthesis, or the conservative final-template correctness guard.

## Diagnostic contract

Add the stable code `hc-partial-morpheme` with warning severity. Emit one
finding for each distinct `Morpheme` whose `IsPartial` property is true.

The check covers:

- lexical entries in every stratum;
- ordinary morphemic morphological rules in every stratum; and
- morphemic rules referenced by affix-template slots.

The same rule object can be referenced more than once, so enumeration must use
reference identity and report it once. Each finding's first and only subject is
the partial `Morpheme`, allowing a host to navigate to the original object.

The message identifies whether the subject is a lexical entry or morphological
rule, names it using the best available identifier, and recommends supplying
its missing category or template/slot analysis. It also states that leaving the
morpheme partial can broaden analysis and disable safe final-template pruning.

## Placement

`GrammarHealthChecker.Check(Language)` will invoke a private partial-morpheme
check alongside the two existing checks. The implementation stays inside the
`netstandard2.0` HermitCrab library and remains diagnostic-only.

No new parser option or model field is introduced. `Morpheme.IsPartial` remains
the owner of the decision; the health checker reports that published fact and
does not re-derive partiality from POS, slots, or feature structures.

## Tests

Tests will be written and observed failing before production code changes. They
will prove that:

1. a partial lexical entry produces one actionable warning and exposes the
   entry as its subject;
2. a partial ordinary affix rule produces one warning;
3. a partial template rule produces one warning even if referenced by multiple
   slots or templates;
4. non-partial morphemes produce no partial-morpheme warning; and
5. the existing checks continue to compose with the new check.

The targeted HermitCrab suite and formatting check must pass before the branch
is pushed.

## Relationship to pull request #491

Pull request #491 keeps its safe default: final-template pruning remains
disabled wherever partial morphemes make the stronger conclusion unsafe. The
new health finding gives grammar authors an actionable route to remove that
performance blocker instead of weakening the correctness guard or silently
forcing the optimization.
