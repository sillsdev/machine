# `(State, AnnotationIndex)` traversal deduplication

**Status:** rejected in its general form; a narrowed form is sound and was measured effective.

The idea, proposed independently as PR #511: inside `Traverse`, skip any newly produced traversal
instance whose `(State, AnnotationIndex)` pair was already pushed, on the grounds that the surviving
instance differs only in its registers and "registers only record the match, they do not filter the
traversal."

## The motivating pathology is real

The cost driver is not the state count. `TraversalMethodBase.Advance` forks a copy of the instance for
every `Optional` annotation at the next offset, so instance count grows exponentially in the number of
optional annotations along the input and is independent of `|States|`. A synthetic microbenchmark on a
**2-state** FSA measured 2,097,150 instances popped at 20 optional annotations (~2^(k+1)); at k>=24 it
exceeds a 5,000,000 budget. The originally reported "200,000 traversals on a 4-state fsa" is reproduced
and exceeded on fewer states. With the skip in place, the same cells are bounded at `N * |States|`
(measured ratio 0.5-1.0x), turning non-terminating cells into instant ones.

## Why the general form is unsound

A differential fuzz — 20,000 random pattern/input pairs, fixed seed, identical generator compiled against
both arms, comparing success, range, every group range and every variable binding — found **112 cases
where `Match()` differs from production**, while `AllMatches().First()` was identical in all 20,000. That
control pins the divergence to exactly the path the skip changes.

- **108 cases: variable bindings.** Instances carry live `VariableBindings`, which filter later arcs
  through `CheckInputMatch`; the key omits them. `Matcher.Compile` determinizes only variable-free
  patterns, so every alpha-variable rule runs the nondeterministic method where the key is blindest.
  Minimal case: pattern `high=$v0+` anchored both ends over 5 annotations, production matches `[0,5)`,
  the skip returns no match at all.
- **4 cases: capture registers, on the deterministic path.** All are alternation plus capture groups and
  all shorten the match *range*, e.g. `(g0(back=back+)|g1(high=high+back=back+))` going from `[0,4)` to
  `[0,1)`. So restricting to the deterministic method is not by itself a sufficient guard.

Both committed suites stay green under the unnarrowed change (826/0/3 and 98/0/0), so the existing tests
cannot see any of this.

## The narrowed form that is sound

Gate the skip on the deterministic method **and** the absence of capture groups, and leave the
nondeterministic method untouched:

```csharp
bool dedupeByState = !allMatches && Fst.GroupNames.Count() <= 1;
```

Measured: fuzz divergences **112 -> 0**; the group-free microbenchmark sweep cell-for-cell identical to
the unnarrowed change across all 18 cells, so the full bound is preserved; grouped patterns fall back to
production behaviour exactly; both suites unchanged. `GroupNames.Count()` should be hoisted out of
`Traverse` — it is O(1) via the `ICollection` fast path but is evaluated once per call.

## Where it cannot be extended, and why

Two censuses attributed every FSA traversal call to its constructing matcher and counted, per category,
how many pushes a `(State, AnnotationIndex)` key would have collapsed — and critically, whether the
collapsed instance's registers were identical to the surviving one's.

*Environment matchers* (the subject of issue #515) read only `.Success` and `.VariableBindings` from their
matches, never a range or group, so they looked like the ideal target. They are 0.20% of traversal
instances on Amharic and 1.18% on Mbugwe. Removing every redundant environment push would be worth 0.12%
and 0.004% of total pushes respectively. Not built.

*Analysis-side morphological rules* hold nearly all the volume and all the apparent opportunity:
2,801,478 of Amharic's 2,808,088 pushes, 93.95% collapsible; Mbugwe 14.44%. Labelling the remaining
construction sites showed the entire figure is exactly `AnalysisAffixProcessRule` and
`AnalysisCompoundingRule` — both built with `AllSubmatches = true` and calling `AllMatches` precisely to
enumerate every distinct morph-boundary placement. Register comparison at each would-be collapse:

| matcher | identical registers | differing registers |
| --- | --- | --- |
| Amharic `analysis-affix` | 7,446 (0.29%) | 2,573,312 (99.71%) |
| Amharic `analysis-compound` | 16 (0.03%) | 50,920 (99.97%) |
| Mbugwe `analysis-affix` | 3,022 (5.49%) | 52,056 (94.51%) |
| Mbugwe `analysis-compound` | 1,886 (2.93%) | 62,396 (97.07%) |

The genuinely free slice is 0.28% of collapses on Amharic and 4.11% on Mbugwe — at most 0.6% of total
traversal instances, reachable only by adding a per-instance register-equality guard. `RootAllomorphTrie`'s
raw FST collapses 0% despite ~62k pushes, which is correct rather than a missed opportunity: a prefix trie
is already maximally state-shared at construction.

Adding `VariableBindings` to the key costs nothing on this evidence: `collapsedWithBindings == collapsed`
on every row of both grammars, with a distinct-signature counter confirming the signature function
discriminates. The information that differs lives in the registers, which is exactly what `AllSubmatches`
consumers read.

## Relationship to `two-pass-nondeterministic-traversal.md`

That experiment merged on state, position **and bindings** and then rebuilt captures, and was measured
negative largely on the rebuild cost. This one merges on state and position only and keeps the first
instance's captures, which is cheaper and unsound rather than expensive and sound. The two are not
alternatives to the same design; they are the two ways of getting the key wrong in opposite directions.

**Reopen only** with a grammar where the identical-registers split looks materially different from the
four rows above, or for the narrowed group-free form, which needs no further evidence to be safe.
