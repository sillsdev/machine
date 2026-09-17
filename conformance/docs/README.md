# The conformance suite: documentation

These documents ship **with** the fixtures. That is deliberate: a consumer receives the
`conformance/` directory and nothing else — PanGloss, the first external consumer, sparse-checks out
exactly this path — so anything explaining the suite that lives outside it may as well not exist.

Read in this order.

| | |
|---|---|
| [what-it-is.md](what-it-is.md) | the suite, the fixture contract, the two-file format |
| [what-it-claims.md](what-it-claims.md) | the honest numbers, the named exceptions, and what is **not** claimed |
| [how-it-is-computed.md](how-it-is-computed.md) | the four coverage layers and how each denominator is derived |
| [how-it-works.md](how-it-works.md) | every ledger, its generator, its gate, and how to regenerate |
| [decisions-and-lessons.md](decisions-and-lessons.md) | what was tried, what was wrong, and how we know |
| [PROTOCOL.md](../PROTOCOL.md) | the adapter contract an engine must implement |

## If you are implementing an engine

You need **`PROTOCOL.md` and nothing else here.** Your entire obligation is to produce the same
parses for each fixture's `grammar.xml` and words. If you do, you inherit whatever this suite covers.

Everything else — the counterfactual census, the interface inventory, the severance witness sweep,
the interaction chains, the data-flow obligations — is internal to HermitCrab. It exists so this
suite can justify the claim that its fixture set is complete. You never run it, never regenerate it,
and are never measured against it.

The ledgers are shipped anyway, because a claim you cannot inspect is a claim you have to take on
trust.

## Status: what is live, what is frozen, what nobody should extend

This suite is deliberately stopped at a known point rather than finished. The measurement apparatus
did its job -- it established what the fixtures witness, and it produced the map of what remains --
and its marginal return then fell to roughly nothing. Freezing it is a decision, not neglect.

**Live, cheap, and worth keeping working.** The self-check, `--check-manifest`, and the drift gates
that compare a checked-in ledger against a fresh recompute. These run in seconds and they are what
stop the published numbers from quietly going stale.

**Frozen, and honest as they stand.** The ledgers. They record 14 of 46 gate arms evidenced, 5 of 23
gates with both MC/DC arms, and 8 defensible obligation cells of 18 worth covering. Those are
findings, not a backlog. `obligation-triage.tsv` classifies every unmet obligation by the blocker its
own ledger records, and most of the remainder is either impossible or strained -- read it before
assuming a number should be higher.

**Do not extend.** The missing severance primitives (whole-element removal, element-content
severance) exist only to feed a campaign that has ended. The authoring-yield measurement loop
optimises a process whose output has no consumer. `fieldworks-producibility.tsv` is a hand-researched
snapshot of an external repository with a demonstrated non-composing failure mode, and patching it
further buys less than replacing its role would.

**Where the next real gain is, and it is not here.** Two engines already exist and both already speak
`PROTOCOL.md`. Running them against each other -- on generated grammars, or better, on the grammars
real projects actually produce -- measures the half of the engine this apparatus does not: not
whether a rule was correctly declined, but whether the parse it produces is the same one. The three
defects this suite has found were all of that kind.

## If you are extending the suite

Start with [decisions-and-lessons.md](decisions-and-lessons.md), not with the code. Several
approaches here were tried, measured, and rejected, and the reasoning is worth more than the
approaches were. In particular, one mistake has been made five times in five different forms —
reporting that something is *present* as though it had been shown to *matter* — and that document
exists mostly to stop it happening a sixth time.
