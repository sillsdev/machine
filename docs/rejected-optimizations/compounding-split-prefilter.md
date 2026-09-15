# Compounding split prefilter

**Status:** rejected before implementation; temporary counters removed.

The candidate was to query lexical reachability before materializing compounding splits. A census on the
heaviest Sena probe counted 35,176 splits, 5,524 root candidates, and 1,121 outputs. That work represented
about 2% of word clones and 9% of FST traversals—too small a ceiling for an additional indexing and correctness
surface, especially because real compounding prevents the simpler grammar-wide lexical gate.

No production change was built. Reopen only if a different grammar shows compounding split construction as a
dominant measured bucket and supplies a sound, stratum-aware reachability predicate.
