# Two-pass nondeterministic traversal

**Status:** measured negative and removed from production.

This port of the `investigate_linear_traverse` approach first merged traversal instances by state, position,
and variable bindings, then rebuilt captures. HermitCrab's existing traversal already pools instances, and
its patterns do not have the repeated-group shape needed to repay the extra bookkeeping.

On the 45-word Sena battery the alternative used 2.3 times as many traversal instances, reduced arc checks by
only 1.6%, produced more duplicate matches, and increased word clones by 27%. Conformance fixtures pointed in
the same direction. Toggle hooks, alternate algorithms, and counters were removed; only this result remains.

Reopen only with a corpus whose automata have substantial equivalent active-state convergence and a census
showing that capture reconstruction is cheaper than the current pooled traversal.
