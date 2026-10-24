# class-deletion-reinsertion-strrep: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# class-deletion-reinsertion-strrep

**Classification: PARITY GUARD for a search-only change (HC-Rust parses were already correct)**

## What it tests

A deletion rule whose target is a segment natural class, `[p b] -> 0 / m _`, in a grammar with no
phonological feature system. Analysis re-inserts an optional node after every `m`. In C# that node
carries the class's feature structure, the union of its members' (`SegmentNaturalClass.cs`), and a
featureless segment's structure is its StrRep alone (`CharacterDefinitionTable.cs`), so root lookup
(`RootAllomorphTrie`, unification) can take `p` or `b` there, or skip it, and nothing else.

`amili`, `uma`, `amuma` and `umuma` need the node to stay optional, to admit both members, and
(`amuma`, `umuma`) to do so at two sites in one shape. `kili` and `umka` put `k` where the deleted
labial would sit. `ampili` and `amkuma` are negative controls.

## Results (hc-conformance self-check, 2026-10-21)

C# founding oracle, sillsdev/machine 72dbd4f9 (PR #480 head): 1 passed, 0 failed, every signature
in words.yaml as recorded.

HC-Rust before divergence entry 103 already produced the same signatures: its re-inserted node had
no identity, so lookup also found `kili` and `umka`, which synthesis then rejected. The fixture is
therefore green on both sides of that change. What it guards is the change itself: an identity
narrower than the class (one member, or a mandatory node) loses `AM+BILI`, `AM+PILI`, `UMPA`,
`AM+PUMA`, `AM+UMPA` or `UMBUMPA`. The extra lookups the old behaviour made are pinned by
`pg-parse` `a_reinserted_class_node_looks_up_only_class_members_on_a_featureless_grammar`, which
names this fixture.

## Provenance

Oracle of record: C# founding oracle, `hc-conformance.dll --fixtures <root>` self-check (in-process
Morpher, memoization on) built from `H:\repos\machine.worktrees\pr480-head` at 72dbd4f9, one run,
2026-10-21. Synthetic data only.

## PanGloss staging

PanGloss conformance replays status and complete parse-signature multisets; it does not compare the
optional rules annotations (recorded from the oracle's trace).

Promotion status: upstream_candidate.
Upstream fixture PR: not opened; candidate destination is machine/conformance/edge-cases/class-deletion-reinsertion-strrep/.
