# Stress fixtures

These grammars are for timing and optimization work on the analysis pass. They are not conformance
fixtures: they live outside `conformance/`, so no ledger, manifest, coverage report or default runner
sees them. Each `words.yaml` starts with `# kind: stress`.

Expected output is a parity check: the parses are what `master` produces (three identical runs were
required), so an optimization must return the same parses. The measure of a change is the number of
analysis steps, not wall-clock time.

Run them only on request:

```
dotnet test tests/SIL.Machine.Morphology.HermitCrab.Tests --filter TestCategory=Stress
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance-stress --include-pathological
```

The test fixture is `[Explicit]` and `[Category("Stress")]`, so a plain `dotnet test` skips it.

| Fixture | What it exercises | Built for |
|---|---|---|
| `bantu-tam-templates` | Bantu slot templates with required slots (subject, object, extension, TAM, final vowel); 12 of 60 words have no parse | #575, #576 (required-slot look-ahead) |
| `bantu-tam-templates-shared` | the same words with three trailing slots shared across templates | #575, #576 (required-slot look-ahead) |
| `bantu-tam-fusion` | the same Bantu slot templates, but each object and subject rule has a plain and a vowel-fusion allomorph (synthetic, not a real language; 28 of 60 words have no parse) | the impure-slot look-ahead, stacked on #576 |
| `truncating-slot-chain` | slot templates whose required and optional suffix slots each delete the vowel before them, so required slots are always missing a segment a later slot restores (synthetic; the look-ahead prunes next to nothing; 12 of 60 words have no parse) | the supply-set look-ahead, stacked on #579 (its worst case) |
| `early-lexicon-agglutinative` | agglutinative words whose root is found early; root-substring and edge-segment prunes | #572, #574 (root-substring prune, edge-segment prefilter) |
| `bantu-extension-chain` | chained verbal extensions | excluded co-occurrence check (parked; #490 ledger) |
| `bantu-extension-required-slot` | extension chain with a required slot | template replay at lookup (dead end; #490 ledger) |
| `required-template-slot` | a required template slot with decoy branches | template replay at lookup (dead end; #490 ledger) |
| `athabaskan-prefix-complex` | a dense prefix complex | excluded co-occurrence check (parked; #490 ledger) |
| `excluded-cooccurrence` | dense morpheme co-occurrence exclusions | excluded co-occurrence check (parked; #490 ledger) |
| `allomorph-list-density` | many allomorphs per morpheme | #560 (allomorph-loop cutoff) |
| `prefix-class-density` | many prefix classes | #560 (allomorph-loop cutoff) |
| `derivation-chain-homophone-roots` | derivation chains over homophonous roots | template replay at lookup (dead end; #490 ledger) |
| `mpr-bantu-concord` | noun-class concord through MPR features | #570 (MPR replay) |
| `mpr-inflection-classes` | inflection classes through MPR features | #570 (MPR replay) |
| `env-bantu-concord` | allomorphs conditioned by environment | environment input prefilter (dead end; #490 ledger) |
| `env-austronesian-nasal-prefix` | nasal-prefix allomorphs conditioned by environment | environment input prefilter (dead end; #490 ledger) |
| `env-zero-allomorphs` | zero allomorphs with rare-class environments | environment input prefilter (dead end; #490 ledger) |

`early-lexicon-agglutinative` omits the word `polundanasak`: engines before #548 (synthesis
filtered by the allomorphs used) trace an extra phonological rule for it that master does not, so no single expectation passes on both.
`deep-derivation-trace` is not included: it measures trace memory and
its words have up to 864 parses each.
