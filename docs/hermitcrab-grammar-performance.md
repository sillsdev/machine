# Writing performant HermitCrab grammars

HermitCrab's engine speedups (see the `hc-rustify` work) and its complexity-cap safety net
(`complexity-cap.md`) both help pathological grammars fail *safely* — bounded runtime, a status
flag, and per-rule evidence when a parse gives up. Neither one makes a pathological grammar fast.
The real fix is always at the grammar level. This guide catalogues the rule shapes that reliably
cause combinatorial blowups, keyed by the stable diagnostic codes `GrammarAnalyzer.Analyze`
(`hc lint`) emits, plus the interaction patterns that only show up empirically.

## Static checks (`GrammarAnalyzer` / `hc lint`)

### HC0001 — Error: no overt exponent + `MaxApplicationCount > 1`

An affix rule whose every allomorph's output is a pure copy of the input (no inserted segments)
*and* whose `MaxApplicationCount` has been raised above 1 (the XML `multipleApplication`
attribute) will unapply to every word, every time, with nothing to ever make it stop. Analysis
keeps "peeling off" a rule that changed nothing, over and over, up to the configured cap.

**Fix:** give the rule a real, overt exponent (an inserted segment or boundary), or drop
`MaxApplicationCount` back to the default of 1.

### HC0002 — Warning: no overt exponent, single application

Same "adds nothing" shape as HC0001, but capped at one application. Still doubles the candidate
count at every cascade position it's considered at, for no linguistic payoff. Often this is an
unintentional gap in a grammar rather than a deliberate zero-exponent rule (e.g. a rule that's
purely feature-changing).

**Fix:** add an overt exponent if one is missing, or confirm the zero-exponent shape is
intentional (e.g. modeling a floating feature) and leave it — HC0002 is Info-adjacent, not a hard
error.

### HC0003 — Warning: `MaxApplicationCount` raised

Flags the opt-in itself, on any affix rule, independent of whether it has an overt exponent. This
is exactly the knob a pathological grammar reaches for. It's not wrong to raise it — some
agglutinative languages need real recursive affixation — but every raised value should be
justified by an actual attested word shape, not left at "big enough."

**Fix:** set it to the smallest value that covers real words in the language, not a round number
picked for headroom.

### HC0004 — Warning: self-feeding rewrite rule

A `Simultaneous`-mode phonological rule whose output can satisfy its own environment again. Before
complexity-cap's Layer 1, this specific shape (`ReapplyType.SelfOpaquing` in `AnalysisRewriteRule`)
had **no reapplication bound at all** — an unconditional infinite loop the first time a grammar
hit it. Layer 1's step budget now catches it, but it's still wasted work every single parse.

**Fix:** add an environment constraint that excludes the rule's own output (so a second
application can't match), or switch to `Iterative` mode if repeated application really is the
intent — iterative mode terminates naturally once the pattern stops matching.

### HC0005 — Warning: unconstrained deletion

A deletion phonological rule (synthesis removes more material than it keeps) with no left or
right environment constraint at all. During analysis, HermitCrab must hypothesize that the deleted
segment could have been anywhere satisfying the (empty) environment — i.e. everywhere — and
`Morpher.DeletionReapplications` governs how many times it's willing to keep re-guessing.

**Fix:** add a left and/or right environment constraint so reinsertion is only considered where
deletion could plausibly have applied.

### HC0006 — Warning: unconstrained compounding

A compounding rule that constrains the part of speech of neither the head nor the non-head. Every
stem in the lexicon becomes a candidate on *both* sides — a cross-product that interacts with
`Morpher.MaxStemCount` and grows fast with lexicon size.

**Fix:** constrain `HeadRequiredSyntacticFeatureStruct` and/or `NonHeadRequiredSyntacticFeatureStruct`
to the parts of speech that can actually compound in the language.

### HC0007 — Info: adjacent optional/iterative lexical patterns

A lexical guess pattern (e.g. `([Seg])([Seg])`) with two or more optional/iterative segments back
to back. `Morpher.LexicalGuess`'s own comments already note this produces spurious ambiguity:
multiple paths through the pattern match the same literal string, multiplying candidates without
adding coverage.

**Fix:** prefer a single Kleene-star class (`[Seg]*`) over back-to-back optional groups when the
intent is "zero or more of these."

### HC0008 — Info: cyclic feeding pair (best-effort)

Two affix rules that each add no overt exponent, where each rule's output syntactic category is
compatible with the other's input requirement. Structurally, this is the shape of an
`A → B → A → B → ...` cycle that never terminates via a shape change — the specific loophole that
`Morpher.MaxRuleApplicationsPerWord` exists to close, since neither rule's own
`MaxApplicationCount` will ever trip on its own.

This check is intentionally conservative (high-confidence pairs only, per an open question in
complexity-cap.md §10) — it will miss cycles that involve an overt exponent that nonetheless still
loops via some other mechanism, and it won't catch cycles longer than two rules.

**Fix:** verify the two rules can't actually chain into each other indefinitely; if they
legitimately can (rare), set a `MaxRuleApplicationsPerWord` cap.

## What static analysis can't catch

Individually reasonable rules can still combine into exponential blowups — this is inherent to
static analysis over a rule *set*, not a specific bug in `GrammarAnalyzer`. When a word breaches
`Morpher.MaxParseSteps`/`ParseTimeout`, use `Morpher.RerunWithDiagnostics` to re-parse that one word
with per-rule counters enabled and get an empirical top-offender report: *"word X exceeded N
steps; rule Y accounted for most of the applications."* That rule is where to start — check it
against the codes above even if the static pass didn't flag it standalone, since the empirical
report is often revealing an *interaction*, not a single bad rule.

## Layered defense, not a substitute for grammar fixes

None of `MaxParseSteps`, `ParseTimeout`, `MaxRuleApplicationsPerWord`, or `MaxAnalysisShapeGrowth`
make a pathological grammar parse faster or more correctly — they bound the damage (a soft-stop
with partial results, never a hang, never an exception) while the grammar gets fixed. A grammar
that regularly needs those caps to fire is a grammar that needs fixing, not a grammar that's
"handled." Treat a budget breach as a bug report against the grammar, using the codes and the
empirical report above to find the specific rule to fix.
