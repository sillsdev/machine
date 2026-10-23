# guesser-pattern-root-fallback: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: guesser-pattern-root-fallback

## Why this fixture exists

Closes HC-rust port gap G3 (`docs/hermitcrab-rust-port-audit.md` sec 2 "Partially ported" /
sec 3 item 1): the Guesser (`guessRoot`/`LexicalGuess`) engine logic was ported and verified
against the C# unit test's literal expected values (`docs/p11-guesser-api-design.md`), but had no
oracle-verified conformance fixture, no CLI flag, and no FFI wire-format bit. This fixture pins the
*engine* behavior (a total lexical miss, guessed via a lexical pattern) as a committed, discoverable
conformance probe; the CLI/FFI opt-in surface it exists to justify is exercised separately by
`rust/crates/pg-cli`'s own tests (`src/main.rs`'s `guess_tests` module,
`tests/guesser_conformance_gate.rs`) and `rust/crates/pg-ffi`'s `tests/parse_opts_gate.rs`, both of
which load this exact fixture directly.

## What it pins

- **`kad`** (control): an ordinary lexical root, ordinarily trie-indexed. Analyzes identically with
  guessing on or off, and is never marked guessed -- the load-bearing negative control proving the
  guesser only ever fires on a genuine total miss of normal lexical lookup (never overrides or
  duplicates a real hit).
- **`gag`**: no lexical entry of its own; the grammar's only other lexical item is a lexical
  PATTERN (`[Any]*`, an empty-FS natural class matching every segment). A lexical pattern is
  partitioned out of ordinary root lookup entirely at load time (`RootAllomorphDef::is_pattern`,
  P11 chunk 1/2) -- guessing OFF must find nothing (signature `-`); guessing ON must fabricate a
  root from the rendered match and mark the result guessed.
- **`gagd`**: same missing-lexicon situation, but the surface also matches the grammar's one
  suffix rule's output shape, so unapplying that rule leaves a residual stem for the guesser --
  TWO guesses coexist (a 2-morph guess through the suffix rule, and a 1-morph guess of the whole
  surface as a bare root), matching the C# founding oracle's own canonical Guesser test
  (`MorpherTests.AnalyzeWord_CanGuess_ReturnsCorrectAnalysis`) structurally (root position, morph
  counts, both guesses coexisting) -- see `words.yaml`'s note for why this port's own join format
  isn't a byte-for-byte match of the C# test's `ToString()` output (that's a display-formatter
  difference already documented at `rust/crates/pg-parse/tests/csharp_port_morpher.rs`'s module
  doc, not a fixture bug).

All three `gag`/`gagd` parses are self-check-only (`guess: true`, PROTOCOL.md section 3): the
`BatchCommand`-equivalent plain adapter contract this repo's `conformance_fixtures_gate.rs` replays
against (`pg_parse::Morpher::parse_word`, guessing hardcoded off) has no way to request guessing at
all, so `WordEntry::adapter_visible()` correctly omits these words from that generic replay -- they
are asserted directly, with guessing explicitly turned on, by this repo's own CLI/FFI gate tests
named above.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** The C# founding
oracle (`SIL.Machine.Morphology.HermitCrab.Tool`) has no CLI surface for `guessRoot: true` at all
(`docs/p11-guesser-api-design.md` sec 6's "oracle caveat" -- no `hc.dll` command exposes the
3-argument `Morpher.ParseWord`/`AnalyzeWord` overload), so there is no upstream tool to generate a
golden TSV against; the guesser engine port was instead verified directly against the C# unit
test's own literal expected outcomes (`MorpherTests.AnalyzeWord_CanGuess_ReturnsCorrectAnalysis`,
ported at `rust/crates/pg-parse/tests/guesser_gate.rs`). Per `docs/conformance-staging-plan.md`'s
oracle-discipline note, upstream acceptance of this fixture would need the C# founding oracle's
self-check harness (`Runner.cs`, which *does* call the 3-argument overload directly for `guess:
true` words) to re-verify these signatures before graduation.

## Verification

Signatures were captured by driving `pg_parse::Morpher::parse_word_opts` directly over every word
in `words.yaml`, both with `ParseOptions::default()` (guess off) and with
`ParseOptions::default().with_guess_root(true)` (guess on) -- a throwaway test loading this exact
`grammar.xml` and printing each outcome's `signature()`/`guessed`/`structured` fields, equivalent to
what `pangloss batch`/`parse --guess` (see `rust/crates/pg-cli/src/main.rs`) and
`hc_parse_word_opts`/`hc_parse_batch_opts` (see `rust/crates/pg-ffi/src/parse.rs`) now expose.
Confirmed:
- `kad`: `KAD|kad`, guess on or off, never guessed.
- `gag`: guess off -> `-`; guess on -> `gag|gag`, guessed.
- `gagd`: guess off -> `-`; guess on -> `gag+PAST|gag+?d;gagd|gagd` (two analyses), guessed.

The throwaway probe was deleted after transcription (its assertions now live permanently in
`rust/crates/pg-cli/tests/guesser_conformance_gate.rs`, which loads this exact fixture directory).
Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite): that test replays `kad` normally and correctly SKIPS `gag`/`gagd` (every one of their
parses carries `guess: true`) rather than asserting on them.

## Grammar-loadability fix (update)

hc.dll originally could not even LOAD this grammar: `mrPast`'s `<MorphemeId>` sat right after
`<Name>`, but the DTD's `MorphologicalRule` content model is `(Name, MorphologicalSubrules, ...,
MorphemeId?, Gloss?, Properties?)` -- `MorphemeId` must come AFTER `MorphologicalSubrules`. Both
`LexicalEntry`s also had `<MorphemeId>` before `<Allomorphs>`, but `LexicalEntry`'s content model is
`(Allomorphs, ..., MorphemeId?, Gloss?, Properties?)` -- `Allomorphs` must come first. Both were pure
element-reordering fixes, no linguistic content change. This does not change the "Oracle discipline"
section above (hc.dll still has no CLI surface for `guessRoot: true`, so it remains
`oracle-provenance: rust-only`), but confirms the grammar itself is now valid HC XML, loadable and
re-checkable by hc.dll's own loader/schema.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/guesser-pattern-root-fallback/`. On acceptance, delete this staged
copy in the same change (graduation guard enforces this mechanically).
