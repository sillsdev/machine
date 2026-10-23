# infix-interdigitation: provenance and regression rationale

Graduated from PanGloss staging in Machine PR [#480](https://github.com/sillsdev/machine/pull/480).
The ordered words, statuses and complete analysis-signature multiplicities are preserved.
Grammar preparation adds missing external DTD declarations and, where needed, consistently
renames alpha-variable labels `a`/`b` to the DTD vocabulary `α`/`β`; variable IDs and references
are unchanged. Required word notes and explicit rule-trace annotations are supplied separately.

The original rationale below is historical. References to `STAGING.md` mean this preserved
rationale; claims that a then-current oracle was unavailable or unfixed describe that earlier
revision, not current verification. PanGloss-specific assertions remain in PanGloss.

---

# STAGING: infix-interdigitation

## Why this fixture exists

Mimics two of the **templatic-Semitic** pathologies named in `docs/conformance-staging-plan.md`'s
pathology catalog:

1. **Infix interdigitation rules (`InsertSegments` around a root Copy).** `mrPfv`/`mrConv` each
   split the root after its first consonant and insert a literal marker in the gap — the same
   mechanism `machine/conformance/languages/austronesian-phase`'s `mrInfixUm` (that fixture's own
   infix, `-um-`) already exercises, here modeling templatic perfective/converb marking instead.
2. **A merged letter-series (two unifiable `CharDef`s sharing a phoneme).** `cSTz`'s
   `SegmentDefinition` carries TWO `<Representation>` strings (U+1338 ጸ, U+1268 ፀ) for the SAME
   segment — the general "two graphemes, one phoneme" render-variant trap the project's own
   pathology catalog names via a templatic-Semitic reference corpus's real ጸ/ፀ pair. No existing `machine/conformance` fixture
   declares more than one `<Representation>` per `SegmentDefinition` (checked: all 130 existing
   `SegmentDefinition`/`BoundaryDefinition` blocks across every language/edge-case fixture carry
   exactly one), so this is new coverage — distinct from `edge-cases/strrep-identity`, which is
   about single-representation `StrRep` **identity** as a matching dimension on a feature-less
   grammar, not a many-representations-to-one-segment merge.

## What it pins

- `kpfotab` / `kcvotab`: the two independent infixes each produce their own distinct signature —
  the interdigitation mechanism working correctly, and not conflating the two markers.
- `ጸa` and `ፀa` (the two spellings of the SAME underlying segment sequence) parse to the
  **identical** signature — the load-bearing assertion. An engine that treats a `SegmentDefinition`'s
  representations as anything other than fully interchangeable spellings of one segment would
  either fail to parse one of the two, or (worse) parse them to different, non-matching signatures.
- `ጸan` exercises the merged segment inside a real derivation (root + suffix), not just a bare-root
  tokenization smoke test.

Empirically confirmed (see "Verification"): `ጸa` and `ፀa` both render as `TSU|[ጸፀ]a` — the bracketed
alternates class, not a literal `ጸ`/`ፀ`, because `cSTz`'s two representations are feature-identical
and no other segment in this small grammar shares that bundle. That bracket rendering IS the pin
holding (both spellings collapse to "matches either representation of this one segment"), not an
artifact to fix.

## Oracle discipline

**Oracle: `pangloss` (this repo's own Rust engine), NOT the C# founding oracle.** Authored fresh for
this task; `words.yaml` signatures captured by driving `pg_parse::Morpher::parse_word` directly (a
throwaway in-repo test — see "Verification" below), no C# tool run available in this environment.
Per `docs/conformance-staging-plan.md`'s oracle-discipline note, machine acceptance must re-verify
against the C# founding oracle before graduation — the Unicode-representation-merge behavior in
particular is worth an explicit C#-side re-check, since it touches character-table tokenization, an
area where engine-specific implementation choices could plausibly diverge.

## Verification

Signatures were captured via a throwaway test driving `pg_parse::Morpher::parse_word` directly over
every word in `words.yaml` (equivalent to `pangloss batch grammar.xml words.txt out.tsv`'s signature
column, without needing a release build of the `pg-cli` binary — a from-scratch release build in
this task's environment took over 30 minutes under heavy concurrent load and was abandoned in favor
of a debug-profile `pg-parse` test driving the same engine). Output transcribed into `words.yaml`
above. Cross-checked in-repo by `rust/crates/pg-parse/tests/conformance_fixtures_gate.rs`'s
`all_discovered_fixtures_match_oracle` test (dual-root discovery, default `cargo test --workspace`
suite) — that test is what actually gates CI; the throwaway dump test was deleted after transcription.

## Graduation

Not yet proposed upstream. Candidate destination:
`machine/conformance/edge-cases/infix-interdigitation/`. On acceptance, delete this staged copy in
the same change (graduation guard enforces this mechanically).

## Oracle provenance (reconciled 2026-08-31)


ust/tools/oracle-conformance.ps1 ran hc-conformance.exe self-check (C# founding oracle,
machine commit caa4ddde8782557c6fb58cac57e4761ffcafc2a6) directly against this fixture's
grammar.xml + words.yaml: PASS -- every word's signature and traced 
ules: list matched. The
fixture's words.yaml now carries # oracle-provenance: founding-oracle. Any "Oracle discipline"
section below describes how this fixture was originally authored, not its current verification
status.
