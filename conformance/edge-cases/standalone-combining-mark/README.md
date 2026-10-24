# standalone-combining-mark: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: standalone-combining-mark

## Why this fixture exists

Pins a dormant bug in `vendor`/crates.io `foma`'s apply-time tokenizer: it merges a base
character's trailing combining marks not only WITHIN one lexc char-def's own representation (the
ordinary "é as one `SegmentDefinition`" case every existing diacritic-bearing fixture, e.g.
`languages/*`'s `g_dia.xml`-style grammars, already exercises and `pg_foma::emit::combining_run_symbols`
already fixes) but also ACROSS the boundary between two DIFFERENT adjacent char-defs. A grammar
that models a standalone combining mark as its own char-def — e.g. an autosegmental tone mark, as
opposed to folding a diacritic into its base letter's own segment — would have that mark silently
swallowed into whatever base char-def precedes it in the emitted lexc surface text, forcing
`IDENTITY` and a silent zero-parse. The fix, `pg_foma::emit::boundary_combining_run_symbols`
(`rust/crates/pg-foma/src/emit.rs`), declares every such cross-boundary run in `Multichar_Symbols`
so foma's tokenizer sees it as one atomic unit instead of merging across the boundary unasked.

This fixture is not one of the four named reference-corpus-shaped pathology mimics in
`docs/conformance-staging-plan.md`'s catalog — it is a narrower,
construct-specific probe for this one tokenizer bug, in the same spirit as
`docs/fst-plan/foma-fst-plan.md`'s "non-Latin/non-ASCII robustness follow-up" note (combining marks,
NFC vs NFD inputs, as a named future-fixture target).

## What it pins

- `grammar.xml` has two segmentally-near-identical roots on `posN`: `ePa` ("pa", `MorphemeId` PA,
  plain) and `ePah` ("pa" + U+0301 COMBINING ACUTE ACCENT, `MorphemeId` PAH) — the tone mark is
  authored as its own standalone `SegmentDefinition` (`cHigh`), concatenated after `cA`'s own "a"
  representation within `ePah`'s `PhoneticShape`, NOT folded into one precomposed char-def the way
  an ordinary accented-letter grammar would author it. This is the exact shape
  `boundary_combining_run_symbols` exists to cover: two DIFFERENT char-defs' surface text sitting
  adjacent within one root's own literal spelling.
- `words.yaml`'s load-bearing pin is the pair `pa` (`PA|pa`) vs. the boundary-case word `pá`
  (`PAH|pá`, written `"pá"` in `words.yaml` using a YAML unicode escape — see that file's own
  header warning against accidentally substituting a precomposed character there): both parse to a
  SINGLE, DIFFERENT analysis each. Without the boundary fix, `pá` foma-fails to parse at all
  (the merged token has no arc in the compiled network), which the pre-fix engine's
  `pg_foma::emit`-based path would have shown as a recall loss on this word specifically — the
  bare-root case already exercises the boundary merge (no rule/affix involved), and
  `pán`/`"pán"` (PAH + the `mrSuf` "-n" suffix) additionally confirms the fix survives when
  more surface text (`mrSuf`'s own inserted segment) follows the boundary run, not just when the
  boundary sits at the end of the whole word.
- `pan` (`PA+SUF|pan`) is the plain-root positive control carried through the same derivation, so a
  reader can see the PA/PAH pair differ by exactly the tone mark and nothing else, bare and affixed.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** `words.yaml`
signatures were captured by driving `pg_parse::Morpher::parse_word` directly (a throwaway in-repo
test — see "Verification" below) rather than a release build of `pg-cli`/`pangloss`, or a
`SIL.Machine.Morphology.HermitCrab.Tool` run (no `dotnet` toolchain set up in this environment). Per
`docs/conformance-staging-plan.md`'s oracle-discipline note, this is an accepted staging-time
substitute; **machine acceptance must re-verify against the C# founding oracle**, and any divergence
found there is itself a finding, not assumed to match by construction.

Note also that `pg_parse::Morpher` (the engine driven here) is the STRUCTURAL/HC-native parser, not
the `pg_foma`/`FomaAnalyzer` propose+confirm path the boundary bug actually lives in — the signatures
below describe what a CORRECT engine must return for these words; the separate end-to-end regression
coverage that the `boundary_combining_run_symbols` fix actually closed a real (foma-path) recall gap
lives in `rust/crates/pg-foma/tests/f5_diacritics_gate.rs` (which drives `FomaAnalyzer` and diffs it
against `pg_parse::Morpher` directly on `boundary-mark-affix-hc.xml`, a near-identical but separate
fixture used only by that crate's own test suite — this `conformance-staging` fixture is the
committed, dual-root-discovered pin; that crate-local one is the closer-to-the-bug regression test).

## Verification

Signatures were captured via a throwaway test (`rust/crates/pg-parse/tests/zz_throwaway_standalone_mark.rs`,
deleted after use) driving `pg_parse::Morpher::parse_word` directly over `pa`, `pá`, `pan`,
`pán`, printing `word`/`invalid_shape`/`outcome.signature()` — equivalent to
`pangloss batch grammar.xml words.txt out.tsv`'s signature column without needing a release build of
the `pg-cli` binary (which depends on `pg-foma`, which drags in the vendored `foma` C library).
Output:

```
word="pa" invalid_shape=false signature="PA|pa"
word="pa\u{301}" invalid_shape=false signature="PAH|pa\u{301}"
word="pan" invalid_shape=false signature="PA+SUF|pan"
word="pa\u{301}n" invalid_shape=false signature="PAH+SUF|pa\u{301}n"
```

Transcribed into `words.yaml` above verbatim. Cross-checked in-repo by
`rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s `all_discovered_fixtures_match_oracle`
test (dual-root discovery, runs in the default `cargo test --workspace` suite) — that test is what
actually gates CI; the throwaway dump test was deleted once transcription was done.

## Graduation

Not yet proposed upstream (no `sillsdev/machine` PR opened). Candidate destination:
`machine/conformance/edge-cases/standalone-combining-mark/` — same two files (`grammar.xml`,
`words.yaml`), re-verified against the C# founding oracle before acceptance. On acceptance, delete
this staged copy in the same change (the graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.

## Shared construct coverage note

The shared tags record only positive mrSuf affix analyses. The cross-character-definition
combining-mark tokenizer case remains described above and has no current shared construct key.
