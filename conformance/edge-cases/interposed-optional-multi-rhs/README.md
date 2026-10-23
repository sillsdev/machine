# interposed-optional-multi-rhs: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# Interposed Optional multi-node RHS

T4 from the narrowing-unapplication experiment. It pins analysis of a multi-node narrowing RHS
when a prior deletion unapplication inserts an Optional segment between the RHS positions.

The expected positive is independently justified by forward synthesis: root `ABC` has shape `abc`;
the merge rule maps `abc` to `de`; the `x -> 0 / d _ e` deletion rule has no `x` to delete, so the
surface remains `de`, with signature `ABC|de`. In analysis, deletion unapplication may insert
`(x)` between `d` and `e`. The merge matcher must recover `d` and `e` as its two target positions,
leave `x` Optional, and reconstruct `abc`.

The C# implementation has the same pre-existing defect and loses this parse, as reported for this
task. The fixture is intentionally red against that result. The C# oracle was not run locally in
this lane; the expected signature comes from the forward derivation above. The staged words include
four negative controls (`abc`, `dex`, `dxe`, `dd`) so the grammar does not turn the lost parse into
unrestricted acceptance.

The PanGloss baseline reproduction used this command:

```sh
/home/johnm/work/narrow539/bin/pangloss-main batch \
  conformance-staging/edge-cases/interposed-optional-multi-rhs/grammar.xml \
  /home/johnm/work/narrow539/f3-words.txt \
  /home/johnm/work/narrow539/f3-main.tsv \
  --threads 1 --word-timeout-ms 5000
```

It completed all five rows with no skipped words, caps, or timeouts. `de` had signature `-`; the
four negative controls also had signature `-`. The binaries and TSV are outside the worktree under
`~/work/narrow539/`.

No boundary-inside-RHS variant is included: analysis target matching calls `MutShape::segs(false)`,
which omits physical boundary nodes. Such a node is not an interposed RHS capture position in this
matcher; adding one would probe boundary matching rather than this Optional-skip defect.

Upstream report: [Machine PR #540](https://github.com/sillsdev/machine/pull/540).
