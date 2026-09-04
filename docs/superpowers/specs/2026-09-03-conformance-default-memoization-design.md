# Conformance Default Memoization Design

## Goal

Run HermitCrab conformance expectations against memoized analysis by default. Treat non-memoized analysis as a diagnostic comparison, never as a fallback oracle.

## Design

Add one conformance-owned factory for the authoritative `Morpher`. It creates a non-tracing, single-threaded morpher because HermitCrab enables its analysis memo only when tracing is off and `MaxDegreeOfParallelism` is `1`. The in-process fixture runner, materialized self-check engine, and counterfactual evaluator will all use this factory. Its diagnostic opt-out creates HermitCrab's established non-memoized configuration instead.

`Runner` still needs traces to verify each fixture's `rules:` declarations. It will therefore parse each ordinary word twice. The first, memoized parse supplies the authoritative result set and per-result morphological-rule history. Only after that result matches the fixture expectation will a separate tracing parse collect word-level phonological and realizational rule evidence. Trace output will not replace or rescue the memoized result. Tracing runs sequentially because `TraceManager` appends to mutable trace-child collections and is not thread-safe; tracing itself keeps memoization disabled.

Tracing-only semantic-coverage sweeps will use the same sequential tracing factory. Memoization remains disabled there because their purpose is trace collection, not conformance result evaluation.

## Failure Behavior

A memoized signature mismatch fails conformance immediately. The runner may write a proposal from that memoized result when proposal mode is active. It will not retry without memoization or turn a non-memoized match into a pass.

Self-check mode accepts `--no-memoization` for a deliberate diagnostic rerun. Memoization remains on when this option is absent. Adapter mode rejects the option because an external engine controls its own execution strategy.

Unexpected engine exceptions keep existing crash handling. Skip and expected-failure words use the authoritative memoized parse and need no trace-only rerun.

## Tests

Add focused tests proving the conformance default factory creates a morpher with tracing disabled and `MaxDegreeOfParallelism` set to `1`, while its diagnostic opt-out selects the established non-memoized configuration. Test that the CLI recognizes `--no-memoization`. Run the existing conformance fixture gate to prove expected signatures and rule attribution still pass. Run the HermitCrab memoization tests to retain direct coverage of memoized/unmemoized equivalence and replayed rule order.
