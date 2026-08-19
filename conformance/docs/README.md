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

## If you are extending the suite

Start with [decisions-and-lessons.md](decisions-and-lessons.md), not with the code. Several
approaches here were tried, measured, and rejected, and the reasoning is worth more than the
approaches were. In particular, one mistake has been made five times in five different forms —
reporting that something is *present* as though it had been shown to *matter* — and that document
exists mostly to stop it happening a sixth time.
