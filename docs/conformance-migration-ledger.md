# Conformance v1 → v2 migration ledger

Every v1 fixture maps to a v2 destination. Rules (spec §5): no construct's coverage may regress;
a pin that can't embed faithfully stays an `edge-cases/` micro-grammar; `manifest.json.provenance`
moves into per-word `provenance:` fields; the v1 tree is deleted only after the G4 parity proof.

**XAmple floor** (column X): fixtures with `requires: []` in v1. Their construct coverage must land
in a `requires: []` destination — `suffixing-quechua`, `fusional-latin` (deliberately authored
phonology-free: ablaut/stem alternants via `ModifyFromInput`/lexical allomorphs, which derive
morphotactic), or a phonology-free edge-case. Where the primary destination is a phonology grammar,
a second requires-[] home is listed (words are cheap; the grammar is the expensive part).

| v1 fixture | X | Destination | Notes on faithful embedding |
|---|---|---|---|
| affix-shapes/circumfix | ✓ | austronesian-phase (`ke-…-an`) **+ fusional-latin** (`ge-…-t`) | Latin home preserves XAmple floor |
| affix-shapes/infix | ✓ | austronesian-phase (`-um-`) **+ fusional-latin** (nasal infix `vi-n-c-`) | Latin nasal infix is genuinely attested |
| affix-shapes/noncontiguous | — | austronesian-phase | already phonology; noncontiguous affix + rewrite interplay |
| affix-shapes/truncate | ✓ | austronesian-phase (Rotuman deletion phase) | XAmple floor: no natural requires-[] home → **edge-cases/truncate-morphotactic** duplicate |
| allomorphy/discontinuous-env | ✓ | agglutinative-turkic | env across intervening segments ≈ harmony-adjacent conditioning |
| allomorphy/disjunctive-recheck | ✓ | **edge-cases/disjunctive-recheck** | deferred-recheck engine behavior too specific to embed (spec §5 names it) |
| allomorphy/strrep-identity | ✓ | **edge-cases/strrep-identity** | feature-less-grammar StrRep identity pin (spec §5 names it) |
| compounding/nonhead-not-root | ✓ | polysynthetic-inuit **+ fusional-latin** (Latin/Germanic compounds) | Latin home preserves XAmple floor. Landed in fusional-latin: the compounding join marker "+" must be a `BoundaryDefinition` (CharacterDefinitionTable), not a plain `SegmentDefinition` — declaring it as a segment produced zero parses for every compound word (v1's own grammar.xml already does this correctly; worth checking before polysynthetic-inuit's own compounding lands). |
| compounding/prefix-commute | ✓ | polysynthetic-inuit **+ fusional-latin** | same. Also landed in fusional-latin: if two `CompoundingRule`s coexist in one grammar with unrestricted head/nonhead wildcards, each must be scoped to a disjoint `headPartsOfSpeech`/`nonHeadPartsOfSpeech` pair or they double-count each other's words. |
| cooccurrence/allomorph-basic | ✓ | suffixing-quechua | allomorph co-occurrence in suffix chain |
| cooccurrence/and-semantics-pin | ✓ | suffixing-quechua | AND-not-OR semantics (LT-22156) — provenance must carry |
| cooccurrence/morpheme-adjacency | ✓ | suffixing-quechua | all 5 adjacency kinds across the evidential chain |
| cross-cutting/disjunctive-tense-simultaneous-epenthesis | — | agglutinative-turkic | disjunctive tense slots + simultaneous epenthesis, Turkic-plausible |
| loader/n1-isactive | ✓ | **edge-cases/loader-isactive** (G1, done) | XML-loader semantics, not language facts |
| loader/n2-default-symbol | — | **edge-cases/loader-default-symbol** | same |
| loader/n3-pattern-shapes | ✓ | **edge-cases/loader-pattern-shapes** | same |
| metathesis/simple_rule | — | austronesian-phase | Rotuman/Leti phase alternation |
| metathesis/complex_rule | — | austronesian-phase | fallback edge-case if exact rule shape won't embed |
| metathesis/not_unapplied | — | austronesian-phase | pins non-unapplication; fallback edge-case |
| mpr-groups/output-overwrite | ✓ | bantu-verbal **+ fusional-latin** (conjugation classes as MPR) | Latin home preserves XAmple floor. Landed in fusional-latin: `MorphologicalPhonologicalRuleFeatureGroup` genuinely needs `morphologicalRuleOrder="linear"` to be observable (per v1's own note) — if that same stratum also needs `"unordered"` for some other construct (e.g. a CompoundingRule's head-recursion), split into two `Stratum` elements sharing one `CharacterDefinitionTable` rather than forcing one order on both; worth checking before bantu-verbal's own MPR-groups land alongside its other constructs. |
| mpr-groups/required-all | ✓ | bantu-verbal **+ fusional-latin** | same |
| negative/obligatory-tense-slot | ✓ | agglutinative-turkic | the F3 example is already Turkic-shaped (tun-di/tun-ta) |
| pathological/deep-optional-affix-nesting | ✓ | **edge-cases/deep-optional-affix-nesting** | budget_ms fixture, isolation required |
| realizational/family-blocking | ✓ | fusional-latin | LexFamily blocking is fusional-paradigm territory |
| realizational/realizational-rule | ✓ | bantu-verbal **+ fusional-latin** | core mechanics in both; Latin preserves floor |
| realizational/stem-name | ✓ | templatic-semitic (binyan grades) **+ fusional-latin** (principal parts) | Latin preserves floor |
| rewrite/deletion-reinsertion | — | polysynthetic-inuit | seam deletion under reinsertion |
| rewrite/disjunctive | — | agglutinative-turkic | disjunctive subrules ≈ gradation contexts |
| rewrite/expand | — | templatic-semitic | segment expansion ≈ spreading/gemination; **LT-22613 provenance must carry verbatim** |
| rewrite/longdistance | — | agglutinative-turkic | harmony is the canonical long-distance rule |
| rewrite/merge | — | agglutinative-turkic | vowel coalescence at seams (Uralic-plausible); history row 1 provenance |
| rewrite/multiplemerge | — | agglutinative-turkic | same |
| rewrite/multiplesegment | — | polysynthetic-inuit | multi-segment seam alternations |
| rewrite/multiplesegment-deletion-composition | — | polysynthetic-inuit | group-capture composition pin; fallback edge-case |
| rewrite/quantifier | — | agglutinative-turkic | quantified contexts = harmony transparency spans |
| rewrite/required-pos-subrule | — | bantu-verbal | verb-only phonology is Bantu-plausible |
| rewrite/simultaneous-epenthesis | — | templatic-semitic | Arabic-style epenthesis, Simultaneous mode |
| rewrite/simultaneous-epenthesis-cascade | — | **edge-cases/simultaneous-epenthesis-cascade** (G1, done) | expect_crash isolation |
| rewrite/simultaneous-feeding | — | templatic-semitic | paired Simultaneous/Iterative contrast — embed as two rules on distinct segments; fallback: keep pair as one edge-case |
| rewrite/simultaneous-feeding-control-iterative | — | templatic-semitic | control sibling of the above, same fallback |
| rewrite/word-initial-epenthesis | — | templatic-semitic | Arabic prothesis (ʔi- before clusters) — perfect family fit |

## Cross-cutting findings from G2a (agglutinative-turkic, fusional-latin)

Two engine/harness facts surfaced while authoring the first two multi-construct v2 grammars that
will affect every language still to come (templatic-semitic, bantu-verbal, austronesian-phase,
polysynthetic-inuit, prefixal-athabaskan) — not fixture-specific, so recorded here rather than on
any one row:

1. **Any phonological subrule (or `ModifyFromInput`) that CHANGES a segment's identity needs a
   fully-specified `PhonologicalFeatureSystem`** where every segment in the `CharacterDefinitionTable`
   has an explicit value for every declared feature, and every segment's combination is unique. A bare
   `SegmentNaturalClass`/literal `Segment` target or output for such a subrule is analysis-unreachable
   in self-check (confirmed by isolated probing: always zero parses, regardless of anchoring or an
   explicit `Environment`); a partially-specified feature table renders ambiguous bracketed character
   classes even for segments no rule ever touches. A `SegmentNaturalClass`/literal segment remains fine
   for IDENTITY subrules (no actual change) and for `Environment` conditions (never the position being
   changed). See `agglutinative-turkic/words.yaml`'s header and `fusional-latin/grammar.xml`'s
   `PhonologicalFeatureSystem` comment for the full derivation.
2. **Self-check's traced `rules:` verification originally could not distinguish "this phonological
   rule fired with effect on this word" from "this phonological rule's stratum pass merely ran"** —
   confirmed by an isolated 2-rule probe where a word containing neither rule's target segment still
   traced both rules as applied. **Fixed in the G2a review pass:** the engine's `TraceManager` records
   both applied (`FailureReason.None`) and not-applied (`FailureReason` set) attempts as
   `PhonologicalRuleSynthesis` trace nodes, and `TraceRuleAttributor` was counting both kinds;
   filtering to `FailureReason.None` makes per-word phonological-rule attribution exact.
   `agglutinative-turkic`'s `rules:` lists were narrowed to the truthful per-word sets, which matched
   its header's hand derivations exactly. Later grammars declare only the rules that actually fire.

## Floor summary (G4 parity check keys)

- Per-construct: every construct with ≥1 v1 fixture must have ≥1 v2 word exercising it.
- XAmple floor: every construct covered by a requires-[] v1 fixture must be exercised by ≥1 word in
  a requires-[] v2 grammar (quechua, latin, athabaskan, or a phonology-free edge-case).
- Provenance: every v1 `manifest.json.provenance` string must appear in some v2 `provenance:` field
  (mechanically greppable).

## G2e parity proof (mechanical, run before deleting the v1 tree)

`conformance/parity-check.py` mechanically enforces all three floors above plus an absolute
construct-coverage check, and exits nonzero on any violation. It self-validates that it discovered
exactly 41 v1 fixtures before certifying. Run it any time with `python conformance/parity-check.py`.
The passing run that gated the v1 deletion:

```
parity-check: discovered 41 v1 fixture(s), 15 v2 grammar(s) in coverage.csv, 41 v2 provenance string(s), 20 constructs.txt entries
[PASS] A. provenance: 41/41 v1 strings carried verbatim
[PASS] B. per-construct floor: 14/14 v1 constructs covered in v2
[PASS] C. XAmple floor: 11/11 requires:[] constructs have a requires:[] v2 home
[PASS] D. absolute coverage: 19/19 in-scope constructs covered ('Tracing (TraceType)' out of scope by design)
parity-check: ALL CHECKS PASSED -- v1 coverage is fully preserved in v2; safe to delete the v1 tree.
```

The proof caught one real gap during authoring: the G1 pilot `suffixing-quechua` (authored before the
provenance discipline) had never carried the three `cooccurrence/*` fixtures' provenance and did not
genuinely exercise their sub-behaviours; it was enriched (AllomorphCoOccurrenceRule, all five
adjacency kinds, the LT-22156 two-rule AND pin — all morphotactic, `requires: []` preserved) until the
proof went green. After deletion the v1→v2 checks (A/B/C) report "migration complete" and only the
absolute check D runs; D is the permanent G4 mechanism (also emitted by `--coverage-report`).
