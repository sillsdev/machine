# HermitCrab Optimization Round Completion Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish, minimize, validate, benchmark, document, and publish the September 2026 HermitCrab optimization branch.

**Architecture:** Keep only optimizations whose correctness follows from HermitCrab's search invariants and whose measurements justify their cost. Preserve on/off paths in the benchmark harness for same-binary comparisons, but remove production mechanisms that are experimental, inert on available grammars, or unrelated to proven wins. Validate behavior against the conformance-framework branch and representative real grammars before publishing.

**Tech Stack:** C#/.NET, NUnit, PowerShell, Git/GitHub CLI.

---

### Task 1: Finish copy-on-write syntactic feature structures

**Files:**
- Modify: `src/SIL.Machine.Morphology.HermitCrab/Word.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/Morpher.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/AnalysisAffixTemplateRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AnalysisAffixProcessRule.cs`
- Modify: `src/SIL.Machine.Morphology.HermitCrab/MorphologicalRules/AnalysisCompoundingRule.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/WordSyntacticFsSharingTests.cs`
- Create: `tests/SIL.Machine.Morphology.HermitCrab.Tests/ShareSyntacticFsSetUpFixture.cs`
- Modify: `tests/SIL.Machine.Morphology.HermitCrab.Tests/PerfBench.cs`

- [ ] Audit all in-place `SyntacticFeatureStruct` mutations; assignments do not need copy-on-write guards.
- [ ] Add the minimal `EnsureOwnSyntacticFeatureStruct()` guards at the three analysis-rule mutation sites.
- [ ] Add suite and benchmark toggles so sharing-on and sharing-off are testable in the same source tree.
- [ ] Run focused tests, then the HermitCrab suite with `HC_SHARE_SYNTACTIC_FS=false`; require zero failures.
- [ ] Commit only the task files after reviewing the diff.

### Task 2: Adversarially minimize and correct the full branch

**Files:**
- Review: every file in `git diff origin/master...HEAD`
- Modify: only files needed to resolve confirmed review findings

- [ ] Pin `origin/master...HEAD` and this plan/user goal as the review range and specification.
- [ ] Run independent spec and quality reviews asking whether every production change is necessary, minimal, and correct under parallel and tracing paths.
- [ ] Prefer deleting measured-negative experiments and inert production changes; retain their research records as rejected leads.
- [ ] Add focused regression tests for correctness fixes and re-review until no critical or important findings remain.

### Task 3: Refresh research records

**Files:**
- Modify: `docs/hermitcrab-perf-2026-09-counts.md`
- Modify/Create: `docs/optimizations/*.md`
- Modify: `docs/hermitcrab-optimization-ledger.md`
- Modify: `docs/hermitcrab-probe-design.md` when its statuses or measurements change

- [ ] Give every retained successful optimization one document covering its invariant, implementation, evidence, parity coverage, portability, and limits.
- [ ] Record compounding analysis and removed experiments as rejected leads without calling them shipped wins.
- [ ] Synchronize counts, grammar facts, statuses, and scope across all research documents.

### Task 4: Run the full conformance gate

**Files:**
- Integration worktree: `C:/Users/johnm/Documents/repos/machine/.worktrees/hc-conf`
- Conformance source: branch `integrate-conformance-framework`

- [ ] Bring the exact minimized optimization tip into the conformance integration worktree.
- [ ] Run the complete conformance-inclusive suite with unfiltered output captured to a log; confirm all expected tests and fixture words ran.
- [ ] Resolve failures in the owning branch and repeat the full gate until it exits zero.

### Task 5: Benchmark representative samples from five languages

**Files:**
- Use: `tests/SIL.Machine.Morphology.HermitCrab.Tests/PerfBench.cs`
- Use: `scripts/hc-bench-compare.ps1`
- Modify: optimization documents with final results

- [ ] Reuse compatible historical baselines or select a recorded small mix of cheap and heavy words from Sena, Amharic, Indonesian, Mbugwe, and Aweti.
- [ ] Run alternating same-binary A/B measurements with sequential parsing, a discarded warm-up, and at least two measured rounds.
- [ ] Check signature parity and asymmetric timeouts before accepting timings.
- [ ] Report sample size, baseline/candidate time, ratio, and limitations per language without extrapolating to the full corpus.

### Task 6: Final verification and pull request

**Files:**
- Verify: complete feature branch and documentation diff

- [ ] Run fresh release tests, formatting checks, and the conformance-inclusive gate on the final commit.
- [ ] Re-read this plan and verify every requirement against concrete artifacts.
- [ ] Commit, push `perf/hc-optimization-pr`, and open a PR against `master` with retained wins, rejected leads, measurements, and exact verification commands.
