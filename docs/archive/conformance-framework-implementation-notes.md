# Conformance framework F0-F3 — grounded implementation scoping (post-recon)

**Postscript (2026-07-10, review-fix pass):** several mechanics this note describes as planned or
provisional have since changed and this historical record is left as-is rather than rewritten —
see `conformance/PROTOCOL.md` and `conformance/README.md` for current behavior. In short: crash
fixtures are pinned via manifest-declared `expectCrash` (not inferred from an empty `expected.tsv`);
`category: pathological` is the sole authoritative signal for pathological gating (the manifest's
separate `pathological` boolean, mentioned below as the planned mechanism, is now a redundant
cross-check the harness FAILs on if it disagrees); the per-word write-up file is `words.yaml`, not
`words.md`; and `manifest.json.requires` is mechanically re-derived from `grammar.xml` and enforced
by the harness on every run, not just checked once at authoring time.

**Status of this note:** written after actually doing F0 and the F1 pure-move (both committed —
see commits `81677820` and `0d8f90f6`), per an explicit instruction to stop and produce a reviewed
scoping checkpoint before continuing the bulk of implementation. Nothing below has been implemented
yet except what those two commits already did.

**Post-write correction (same day): the branch this work sits on was rebuilt on top of
`origin/master` instead of the `rust` branch** — per John, the Rust port will never land in this
repo, so the framework must not be built on top of it. `81677820` (F0) cherry-picked cleanly onto
`master` unchanged. `0d8f90f6` (F1)'s `git mv` could NOT replay onto `master` (master never had a
`rust/` directory to move from) — it was redone as a cross-branch content copy instead (new commit
`b5e4aa07`: same end-state file content, verified byte-identical, but via `git checkout
<rust-branch-commit> -- rust/conformance` + `git mv` into place, not a same-branch rename). This
note's step 2 below and the "no content changes" / "rename-tracked" wording describe the original
(now-superseded) commit; the content and BatchCommand verification are unaffected and still hold —
re-verified against the new commit before writing this correction.

## What's actually done (committed, verified)

1. **F0 — `BatchCommand` ported.** `git show b9b77bfd:.../BatchCommand.cs` copied verbatim into
   `src/SIL.Machine.Morphology.HermitCrab.Tool/BatchCommand.cs`, registered in `Program.cs`'s
   `commands` array. `dotnet build src/SIL.Machine.Morphology.HermitCrab.Tool` succeeds clean on
   both the original `rust`-based commit and after cherry-picking onto `master`.
   Verified by running `batch` against `conformance/rewrite/simultaneous-epenthesis/` and diffing
   word/status/signature columns (ms and the `STARTED` sentinel line excluded from comparison, same
   convention as b9b77bfd's own verification note and `rust/tools/parse_compare.py`) — exact match,
   re-confirmed again after the F1 redo at the new `conformance/` path.
2. **F1 — cross-branch copy done** (see correction above — no longer a "pure move", since this
   branch and `rust` share no history for this content). All 38 fixture directories +
   `HISTORY-MATRIX.md` now live at `conformance/` on this `master`-based branch, byte-identical to
   `rust`'s copy. Nothing inside any fixture has been touched yet: no `script.txt` backfilled, no
   `manifest.json`/`words.md` written, no `PROTOCOL.md` written, no Rust test path fixes applied yet
   (and now that this branch doesn't contain `rust/` at all, those Rust test path fixes are moot for
   THIS branch — they'd only matter wherever the Rust port ends up consuming `conformance/` from,
   which is now an explicitly separate, deferred concern per §9 of the plan doc, not something to
   fix here).

## Concrete facts learned during recon that refine/correct the original prompt's assumptions

- **`BatchCommand`'s real CLI shape differs from the design doc's stated adapter protocol.**
  Design doc §4.3 says the protocol is one command: `<engine-binary> batch <grammar.xml>
  <words.txt> <output.tsv>`. That's exactly what `hc-rs`'s CLI does
  (`rust/crates/hc-cli/src/main.rs` — `batch` subcommand takes 3 positionals: grammar, words,
  output). **C#'s `hc.dll` does not** — grammar loading is a *global* `Program.Main` flag
  (`-i <grammar.xml>`), separate from the `batch` subcommand, which only takes 2 positionals
  (`<wordlist-file> <output-tsv>`) plus `--start=N`. The normal invocation is
  `hc.dll -i grammar.xml -s script.txt` where `script.txt` contains the line
  `batch words.txt expected.tsv` (this is exactly what the existing 19 fixtures' checked-in
  `script.txt` files already look like — confirmed by reading
  `conformance/allomorphy/discontinuous-env/script.txt`). **Decision needed, not blocking F0/F1**:
  PROTOCOL.md (§4.3) should document the *engine-agnostic* contract as the design doc states
  (single command, 3 positional args) since that's what `hc-rs` already conforms to and what any
  third engine should implement — and should separately note, as an aside, that C#'s own
  reference tool needs a one-line script-file wrapper to conform to that same contract (or, F2's
  self-check mode simply never shells out to `hc.dll` at all — it calls the `Morpher` API
  in-process, sidestepping this mismatch entirely, which is what the design doc already specifies
  for self-check mode). Recommend: PROTOCOL.md documents the 3-arg single-command contract as
  canonical; a short note explains C#'s tool needs a trivial wrapper script to act as an
  `--adapter` if anyone ever wants to point `--adapter` at `hc.dll` itself (not required by F0-F3's
  acceptance criteria, which only requires self-check-mode + `--adapter` against `hc-rs`).

- **Exactly 19 of 38 fixtures lack `script.txt`** (confirmed by direct listing, not estimate):
  all 4 of `affix-shapes/*` (`circumfix`, `infix`, `noncontiguous`, `truncate`) and all 15 of
  `rewrite/*` (`deletion-reinsertion`, `disjunctive`, `expand`, `longdistance`, `merge`,
  `multiplemerge`, `multiplesegment`, `multiplesegment-deletion-composition`, `quantifier`,
  `required-pos-subrule`, `simultaneous-epenthesis`, `simultaneous-epenthesis-cascade`,
  `simultaneous-feeding`, `simultaneous-feeding-control-iterative`, `word-initial-epenthesis`).
  The other 19 (`allomorphy/*`, `compounding/*`, `cooccurrence/*`, `loader/*`, `metathesis/*`,
  `mpr-groups/*`, `realizational/*`) already have one, in the form
  `batch <abs-path>/words.txt <abs-path>/expected.tsv` — **note these existing ones use absolute
  paths baked in from whatever worktree generated them** (e.g.
  `discontinuous-env/script.txt` reads
  `batch C:\Users\johnm\...\phase2-w3\rust\conformance\allomorphy\discontinuous-env\words.txt ...`
  — a path that no longer exists anywhere, let alone at the new location). Backfilled/rewritten
  `script.txt` files should use **relative paths** (`batch words.txt expected.tsv`, run with cwd
  set to the fixture dir, or paths relative to it) so they stay valid across moves — this is a
  fix-forward improvement over the existing convention, not a faithful copy of it, and should be
  called out as such (existing 19 should probably be normalized to relative paths too while
  touching them, for consistency, rather than leaving 19 fixtures on an absolute-path convention
  that's already stale).

- **`manifest.json`'s `constructs` vocabulary must be pinned once, up front**, using §6's exact
  key strings verbatim (e.g. `"AffixProcessRule: prefix/suffix/circumfix/infix"`, `"RewriteRule
  Simultaneous"` — copy the literal table-cell text from the design doc, not a paraphrase), because
  the coverage report (F2) does an exact cross-reference against that checklist. If this pass
  writes 38 manifests with inconsistent phrasing, the coverage report is wrong from day one.

- **Rust test path fix — out of scope for THIS branch, but recorded here for whoever updates the
  `rust` branch itself.** Since `conformance-framework` is now based on `master` and carries no
  `rust/` directory at all, there is nothing on this branch for these 14 files to be fixed in. The
  fix below still needs to happen eventually, just on the `rust` branch (or wherever the Rust port
  ends up), once it's ready to point its own tests at the new `conformance/` location instead of its
  own `rust/conformance/` copy — a separate, later coordination step, not part of this PR.
  Mechanical but touches 14 files, not "a couple." Grep confirms 14
  files under `rust/crates/hc-parse/tests/` reference `../../conformance` (relative to
  `CARGO_MANIFEST_DIR` = `rust/crates/hc-parse`, so `../../conformance` = `rust/conformance`).
  Moving to repo-root `conformance/` means every one of those needs `../../../conformance`
  (one more level up). Full list already collected via Grep:
  `simultaneous_conformance.rs`, `strrep_identity_gate.rs`, `rewrite_conformance.rs`,
  `mpr_groups_gate.rs`, `realizational_gate.rs`, `loader_n3_pattern_shapes_gate.rs`,
  `metathesis_conformance.rs`, `loader_n1_isactive_gate.rs`, `loader_n2_default_symbol_gate.rs`,
  `disjunctive_recheck_gate.rs`, `discontinuous_env_gate.rs`, `compounding_conformance.rs`,
  `cooccurrence_gate.rs`, `affix_shapes_conformance.rs`. This is a pure string substitution
  (`../../conformance` → `../../../conformance`) across these 14 files, doable as one sed-style
  pass, followed by `cargo test --workspace` to confirm no regression. Doc comments in these files
  that *mention* `rust/conformance/...` as prose (not as a path literal) are stale after the move
  but harmless to leave as historical citations, OR could be updated to `conformance/...` for
  accuracy — low priority, cosmetic.

- **No step-count/rule-stats mechanism exists on this branch** (confirmed: `--rule-stats` was
  explicitly stripped from `BatchCommand` per b9b77bfd/this port, because it depends on
  `Morpher.AccumulateRuleStats` etc. which only exist on unmerged perf-optimization PRs). The F3
  pathological fixture's manifest must use a **wall-clock bound only**, not a step-count ceiling —
  the design doc permits this ("wall-clock with a generous margin is fine").

- **38 fixtures, not "about 38"** — confirmed exact count via `find conformance -name grammar.xml
  | wc -l` = 38, matching the design doc's number.

## Revised step-by-step plan (small, ordered, independently verifiable, each its own commit)

**F1 remainder (fixture-content work):**

1. Write `conformance/PROTOCOL.md` (design doc §4.3), documenting: the 5-column `expected.tsv`
   signature format precisely (column meanings, the `STARTED` sentinel-line convention, the
   sort-order/`;`-join signature algorithm — cite `BatchCommand.cs`'s `BuildSignature` by
   paragraph, not by reproducing code), the single-command 3-positional-arg adapter contract
   (`<engine> batch <grammar.xml> <words.txt> <output.tsv> [--start N]`), and the aside about C#'s
   own tool needing a script-file wrapper to conform (see discrepancy above). One commit.
2. Pin the `constructs` vocabulary: extract §6's table as a literal string list (a small constant,
   either inline in this note or as a `conformance/CONSTRUCTS.md` companion — recommend the latter
   so F2's coverage report and any future contributor can both point at one file). One commit.
3. Write ONE fixture's `manifest.json` + `words.md` by hand as the template
   (recommend `conformance/rewrite/simultaneous-epenthesis/` — already has a rich README with a
   confirmed live-oracle bug worth describing accurately in `words.md`). Commit this alone so it
   can be reviewed as the pattern before the other 37 follow it.
4. Backfill `script.txt` (relative-path form) + write `manifest.json` + `words.md` for the
   remaining 37 fixtures, grouped by category directory (9 groups: `affix-shapes`, `allomorphy`,
   `compounding`, `cooccurrence`, `loader`, `metathesis`, `mpr-groups`, `realizational`,
   `rewrite` — `rewrite` is the largest at 15). Each group is its own commit. Every `words.md`
   entry must be traceable to something read (the fixture's `README.md` and/or `grammar.xml`) —
   no fabricated per-word descriptions; if a README genuinely doesn't say enough, that fixture's
   entry says so explicitly rather than guessing, and gets flagged for follow-up in the final
   report rather than silently invented.
5. Self-check-verify all 38 (see F2 step 2 below — this can't fully happen until the harness's
   self-check mode exists, so step 5 is really "re-run after F2 lands," not a separate F1 step).

**F2 (harness project):**

1. Scaffold `src/SIL.Machine.Morphology.HermitCrab.Conformance/` as a new C# console project,
   referencing `SIL.Machine.Morphology.HermitCrab`; wire into the solution file. Verify
   `dotnet build` succeeds with a trivial `Main` before adding logic. One commit.
2. Implement self-check mode: for each fixture under `--fixtures <path>`, load `grammar.xml` via
   `XmlLanguageLoader`, build a `Morpher`, parse every word in `words.txt` in-process (same logic
   `BatchCommand` uses — consider factoring `BatchCommand`'s `BuildSignature`/`ParseOneWord` into
   a small shared internal helper referenced by both, rather than copy-pasting, since they must
   stay byte-identical), diff against `expected.tsv` using parse_compare.py's comparison semantics
   (read `rust/tools/parse_compare.py` first to port the exact rules: reorder-only and
   dup-count-only differences are NOT failures, real signature/status differences ARE). Run
   against all 38 migrated fixtures; this is the actual verification that F1's migration didn't
   corrupt anything. One commit, plus fixing any fixture the self-check reveals as broken.
3. Implement `--adapter "<command template>"` mode: shell out per fixture, same diff logic. Verify
   against `hc-rs batch` (need to locate/build the `hc-cli` binary — `rust/crates/hc-cli`). One
   commit.
4. Implement coverage report (plain text, per design doc — no report UI): parse every
   `manifest.json`, cross-reference `constructs` against the pinned checklist from F1-step-2,
   report per-construct fixture counts + negative/cross-cutting flags + zero-coverage constructs.
   One commit.
5. Implement `pathological` skip-by-default + `--include-pathological` + per-fixture
   timeout/wall-clock bound read from manifest. One commit (can land before F3's pathological
   fixture exists, just needs the flag to be inert until then).
6. Fix the 14 Rust test files' path constants (`../../conformance` → `../../../conformance`),
   `cargo test --workspace`. One commit, separate from the C# harness commits.

**F3 (one fixture per new category):**

1. Negative fixture: design a small grammar + a word that fails one specific constraint (not
   random gibberish). Generate `expected.tsv` for real via the now-working `BatchCommand`/harness
   self-check (build the fixture, run it through the live oracle, confirm it reports zero parses
   for the target word — don't hand-write "0 parses" and assume). `manifest.json`
   (`category: negative`) + `words.md` + `script.txt`. One commit.
2. Cross-cutting fixture: per advisor input during recon, prefer an interaction where **both
   halves already have a working single-feature fixture to crib grammar structure from** (lower
   risk than reduplication×compounding, since reduplication has zero existing fixtures/no proven
   grammar skeleton to start from) — e.g. a disjunctive affix-template slot combined with
   Simultaneous-mode epenthesis (both already have single-feature fixtures: `rewrite/disjunctive`
   and `rewrite/simultaneous-epenthesis`/`-cascade`). Read both constructs' C# source together
   before designing the grammar (per the original task brief), generate `expected.tsv` against the
   live oracle. `manifest.json` (`category: cross-cutting`) + `words.md` + `script.txt`. One
   commit. (Open decision to flag in the final report either way — reduplication×compounding is
   also acceptable per the design doc; this note records the lower-risk choice as the
   recommendation, not a unilateral final decision.)
3. Pathological fixture: pick one real complexity driver (deeply nested optional affixes is the
   cheapest to build convincingly). Manifest records a wall-clock bound (generous margin) instead
   of a parse signature, `pathological: true`. Verify the harness's `--include-pathological` flag
   actually gates it (runs excluded by default, included when passed). One commit.
4. Final pass: update `docs/conformance-framework-plan.md` §8 acceptance criteria with DONE status
   per item + evidence citations (file:line style, matching `rust-optimizations-phase2.md`'s
   convention). One commit.

## Open items / discrepancies to flag when reporting back (not resolved yet)

- PROTOCOL.md's contract-vs-C#'s-actual-CLI-shape mismatch (see above) — recommend documenting the
  3-arg single-command contract as canonical and noting C#'s wrapper-script caveat, but this is a
  judgment call worth a second opinion before writing PROTOCOL.md.
- Whether to normalize the 19 existing `script.txt` files' absolute paths to relative while
  touching them (recommended: yes, for consistency and because the existing absolute paths are
  already dangling/stale).
- `words.md` vs `words.yaml` — design doc already defaults to YAML-in-Markdown (`words.md`
  containing YAML); no new information changes that default.
- Cross-cutting fixture choice (reduplication×compounding vs. disjunctive-slot×Simultaneous
  epenthesis) — recommendation above, not yet decided.
