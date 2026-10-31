# expansion-interposed-optional: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: expansion-interposed-optional

## Why this fixture exists

Issue 8 tracks expansion-rule unapplication when an earlier deletion unapplication can insert an
Optional segment between the expansion's RHS target positions. The pinned C# regression is
`ExpandRuleUnappliesAcrossInterposedOptionalSegment` in Machine's
`RewriteRuleTests.cs` at commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`.

This fixture reduces that shape to one root and two rules. Forward synthesis maps `ROOT/u` to `ii`
with `prExpand`; `prDelete` then deletes `z` only in `i_z_i`, so it is inert for this root. During
analysis, unapplying deletion may leave an optional `z` between the two `i` target segments. The
expansion must still recover `u`. `ii` is the positive complete-identity check; `u`, `i`, and `iz`
are negative controls.

## Oracle and status

The expected complete identity is `ROOT|ii`; `words.yaml` is verified against the pinned C#
`hc-conformance.exe` from Machine commit `aab4c59819adb7b366915e09df2fb28c62b8c03a`. Its self-check
passed the fixture with the positive identity and traced rule `[prExpand]`; `u`, `i`, and `iz` each
had no analysis.

The published PanGloss v0.10.0 Windows binary was run before the fix through
`rust/tools/pg.ps1 -Mode run -Exe ... -- batch ... --threads 1`. Its SHA-256 was
`90bc73e2a9154db0bc6bfd3fdb518fd134a722f7e75f20a3b3940f261f7b5729`. The batch completed all four
words without skips, caps, or timeouts, but `ii` had no analysis. This is the fixture's baseline RED.

The direct Rust regression also failed before the implementation change:
`rewrite_gate::expansion_unapply_crosses_interposed_optional_in_uncached_traced_and_cached_paths`
returned zero results for uncached expansion unapplication where one was expected. After changing
the matcher, the same test passed across uncached, traced-uncached, and `RuleCache` paths (1 passed,
132 skipped). `pg-parse`'s `conformance_fixtures_gate::all_discovered_fixtures_match_oracle` then
passed with this staged fixture in the discovered set (1 passed, 127 skipped). The focused
`pg-rules` managed check also passed.

Upstream PR: not yet proposed.
