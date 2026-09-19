# `ExpandAlternatives` memoization and hoisting

**Status:** rejected and removed from production.

The experiment memoized `Source` recursion during `Word.ExpandAlternatives` and computed an analysis word's
expansion once outside the per-root-candidate loop. It was exact, but on the 45-word Sena battery it left word
clones unchanged at 4,747,013: deeper strata did not branch, so it saved only small temporary list work.

A later change moved work back behind successful lexical lookup, but after the experimental memo/hoist was
removed the final product path is the same ordering as the pinned production baseline: each lexical candidate
expands its synthesis word. There is therefore no retained alternatives optimization in this branch, and the
earlier 75% “alternatives expanded” reduction must not be attributed to the final diff.

The broader deduplication avenue remains closed by the complete-key census in the main ledger: once pending
trail content is included, only 11% of alternatives duplicate, for a 2.28% wall-time ceiling on Sena.
