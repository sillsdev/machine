# The morphological parser conformance suite

A general-purpose, engine-agnostic conformance test suite for what a morphological/phonological
parser needs to get right, usable independently of this repo and of any one engine.

**The goal: any grammar expressible in the HermitCrab XML, parsed correctly — with a named
asterisk.** That is the aspiration this suite is built to reach and to measure progress toward. The
asterisk is short, exact, and machine-proven rather than hand-waved: every surface named below is
proven to have **no** engine consumer — the engine contains no reference to it, so no grammar can make
it influence a parse. That single fact is recorded in two different ledgers depending on how a surface
fails to earn a counterfactual verdict: `no-consumer` in `conformance/semantic-coverage-proofs.tsv` for
a surface a fixture *does* declare but mutating it changes nothing, and `dead-schema` in
`conformance/semantic-coverage-baseline.txt` for a surface no fixture ever attempts to declare because
the engine never reads the owning element at all (recomputed by scanning
`src/SIL.Machine.Morphology.HermitCrab` for the element name, never trusted from the ledger). Both are
the same underlying claim — zero engine references — so both feed this one list.

<!-- exception-surfaces:start -->
Eleven surfaces across three feature areas are expressible in the DTD and consulted by **no** engine
code path:

- **Cyclic strata and simultaneous rule order** (`no-consumer`): `Stratum/cyclicity="cyclic"` and
  `Stratum/phonologicalRuleOrder="simultaneous"`. `SynthesisStratumRule.Apply` and
  `AnalysisStratumRule.Apply` build the rule cascade unconditionally, with no branch point that could
  select either mode.
- **Syntactic subcategorization** (`dead-schema`, six surfaces): the elements `SyntacticRule`,
  `SyntacticRules`, `OutputSubcategorizationOverride`, `OutputSubcategorizationOverrides`, plus the
  `OutputSubcategorizationOverride/isActive` enum (`yes` and `no`). The DTD wires these through the
  `requiredSubcategorizedRules`, `subcategorizations`, `outputSubcategorization`,
  `headSubcategorizedRules`, and `nonHeadSubcategorizedRules` IDREFS attributes on
  `MorphologicalInput`, `MorphologicalRule`, and `CompoundingRule` — the entire feature is
  unimplemented end to end, not just one element in isolation.
- **Cross-word phonological context** (`dead-schema`, three surfaces): `PreviousWord`, `NextWord`, and
  `Null`, the legal children of every `PhonologicalSubrule` describing an adjacent word's shape.
<!-- exception-surfaces:end -->

A grammar may declare any of these; the engine ignores all eleven. Those are unimplemented formalism
features, not coverage gaps, and together they are the entire current exception list. Keeping it a
list — rather than retreating from the claim — is what makes progress toward the goal measurable.
`tests/SIL.Machine.Morphology.HermitCrab.Tests/SemanticCoverage/ExceptionListTests.cs` pins this list
against both ledgers so a newly discovered `no-consumer` or `dead-schema` surface cannot silently drop
out of it.

**What passing means today.** An engine reproduces the reference implementation's observable parse
behaviour — signature, status, pinned crash — on every fixture its declared capability profile
admits. "Complete" is currently an *accounting* claim: every inventory item carries an explicit
status, and uncovered means listed rather than unknown. The inventory is the formalism's surfaces,
not languages. Coverage is tracked at three levels: a surface's existence in the DTD (complete by
construction), whether it is load-bearing (evidenced, or classified impossible to evidence with a
reason), and interactions between surfaces (not yet enumerated) — closing that third level is what
remains between here and the goal.

**Open coverage debt is tracked by exact item and resolution** in `semantic-coverage-gap-baseline.tsv`.
These are active gaps, not accepted parity exceptions or formalism exclusions. New or changed gaps
require explicit review and fail the gate; a row is removed when evidence or a valid proof closes its
item. The shared fixture corpus grew from 62 to 132 fixtures (67 graduated shared grammars, two
rule-order variants and the partial-root probe), expanding the measured inventory from 367 to 389 items. The 22 new ordering
items initially added 12 proof-resolved items and 10 unresolved items. Targeted C# measurements then
found real word-level counterexamples for four of those ten; six remain unresolved. All 26 prior gaps
remain, so the current exact debt is 33 items: 30 unresolved and three rejected proofs.
The 33rd, `PhonologicalRule/multipleApplicationOrder/leftToRightIterative`, times out: its `simultaneous`
mutant does not terminate on `quantified-alpha-bounded-ltr-left` within 180s.

**Rule order is part of the contract, not an implementation detail.** A parse runs in two passes:
the word is **torn down** to find candidate stems, then **built back up** to check the rebuild
reproduces the surface it started from. This is analysis-by-synthesis — propose by stripping,
confirm by regenerating — and the engine's own vocabulary is *unapply* for the first pass and
*apply* for the second.

| Pass | Direction | Per-stratum order |
|---|---|---|
| **Analysis** — tearing down (`unapply`) | surface inward | phonological rules unapplied **first**, then affix templates and morphological rules |
| *lexical lookup* | the hinge | each stripped form is looked up for real lexical entries; `Morpher.Synthesize` calls this, so the engine places it at the **start of the build-up** |
| **Synthesis** — building up (`apply`) | stem outward | morphological rules and affix templates first, *mutually recursive* — each may invoke the other — then phonological rules **last** |

Strata run in declaration order building up, and reversed tearing down. Note the two passes are
mirror images: whatever runs last on the way up runs first on the way down.

Two consequences a consumer must reproduce. **Within one stratum pass a phonological rule cannot
feed a morphological rule or template** — phonology runs after morphology in synthesis and before it
in analysis — so that class of interaction does not exist and needs no coverage. **Across strata it
can**: stratum *N*'s phonological output is stratum *N+1*'s morphological input. And because
analysis is the inverse of synthesis, an interaction that *feeds* in one direction *bleeds* in the
other, so an interaction witnessed only in synthesis is untested in the direction a proposer runs.

The order is fixed by the engine, not by the grammar: `SynthesisStratumRule.Apply` and
`AnalysisStratumRule.Apply` build it as an unconditional cascade, which is also why
`Stratum/cyclicity` and `phonologicalRuleOrder` are inert — there is no branch point for them to
select.

`PROTOCOL.md` specifies the adapter contract in engine-agnostic terms;
nothing in `conformance/` depends on Machine's build, layout, or C#. `SIL.Machine.Morphology.HermitCrab`
supplies the seed construct vocabulary (`constructs.txt`) and its C# engine is the **founding
oracle** that generated every fixture's ground truth, but it is not a hard boundary: any engine that
implements `PROTOCOL.md`'s contract can be validated here, declaring a partial **capability
profile** (e.g. morphotactics-only, no phonological rules) to be measured fairly against just the
subset it claims to support.

## Shape

```
conformance/
  languages/<name>/    grammar.xml + words.yaml   -- 8 typologically-selected synthetic languages
  edge-cases/<name>/   grammar.xml + words.yaml   -- focused micro-grammars for things no shared grammar hosts
  coverage.csv         GENERATED: language x word x construct
  rules.csv            GENERATED: language x grammar rule id x exercising words
  HermitCrabInput.dtd  the published grammar DTD (byte-identical to the library's embedded copy)
  schema/              Draft 2020-12 contracts for the authoring and product formats
  generated/           GENERATED: the versioned JSON manifest a consumer imports
```

## Consuming the suite from another repository

`words.yaml` is the single canonical authored format — the only place case data (inputs, parses,
rejections, skips, `neutralizes`) lives. There is no generated JSON restatement of it: an earlier
`hc-conformance-corpus/v1` product duplicated that ground truth into a 409KB file, and it carried
less than the 383KB of authored `words.yaml`/`grammar.xml` sources it was generated from, because
those sources hold 1011 comment lines of reasoning — why a fixture exists, what it pins — that JSON
cannot express. Two representations of the same ground truth also meant a permanent drift gate. It
has been deleted, along with `schema/conformance-corpus.schema.json`.

What that product genuinely contributed was provenance, not case data, so that is what survives as
`generated/hc-conformance-manifest.v1.json` (`hc-conformance-manifest/v1`, `schema/conformance-manifest.schema.json`,
13KB): one entry per fixture carrying `fixtureId`, `category`, `displayLanguage`, `grammarPath`,
`grammarSha256`, `wordsPath`, `wordsSha256`, `caseCount`, and `expectedCrash`, plus top-level
`dtdPath`, `dtdSha256`, `sourceHash`, and the three format-version identifiers
(`hc-conformance-manifest/v1`, `hc-conformance-words/v1`, `sil-machine-hermit-crab-input-xml/v1`).
A consumer reads the manifest to discover fixtures and verify integrity, then reads `grammar.xml`
and `words.yaml` directly for the actual grammar and cases. Paths inside the manifest are
repository-relative, never relative to the manifest itself.

Regenerate with `hc-conformance --generate-manifest`; `--check-manifest` verifies the checked-in
bytes and exits nonzero on drift. Both validate every grammar against the DTD and every
`words.yaml` against `schema/words.schema.json` before writing anything.

The DTD is published at `conformance/HermitCrabInput.dtd` so a consumer that receives `conformance/`
alone — PanGloss sparse-checks-out exactly that path — can resolve it. The library keeps its own
copy at `src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd` because `XmlLanguageLoader`
reads it as an embedded resource by manifest name; a test holds the two byte-identical. When
resolving a grammar's DOCTYPE, admit only the system identifier `HermitCrabInput.dtd` and map it to
the published DTD path the manifest records. Any other external entity, network fetch, or
filesystem fallback should be refused rather than attempted.

Two files are required per fixture: `grammar.xml` and `words.yaml`. An optional `README.md`
preserves the fixture's rationale and oracle history; optional FieldWorks witnesses are described
in the protocol. `words.yaml` is the single ground truth: front matter (`language`,
`inspired_by`, `sources`, `requires`) plus one entry per word, each carrying a `note` and either
`parses` (`signature`/`gloss`/`rules`/`exercises`, one per distinct analysis), `expect_fail` +
`blocked_by` (well-formed input, zero valid analyses), or `expect_skip` (the oracle throws
`InvalidShapeException`, e.g. an undeclared segment — status "SKIPPED", not "ok"). Optional fields:
`provenance` (bug/PR reference), `guess` (a `LexicalGuess` parse),
`budget_ms`/`expect_crash` (edge-cases only). Signatures are authored-and-verified, never blindly
regenerated — self-check FAILs on mismatch; `--propose` prints a patch for a human to accept, never
writes. Full schema: `schema/words.schema.json`.

## The eight languages

| Directory | Grammar name | Simulates |
|---|---|---|
| `suffixing-vowel-harmony` | SuffixingVowelHarmony | Long-distance backness harmony, long agglutinative suffix chains, consonant-gradation allomorphy |
| `templatic-root-modification` | TemplaticRootModification | Templatic root-and-pattern morphology, stem names, OCP co-occurrence, simultaneous rewrite |
| `suffixing-extension-slot-ordering` | SuffixingExtensionSlotOrdering | Realizational rules, MPR groups, verbal reduplication, post-nasal mutation, CARP-style extension ordering |
| `metathesis-phase-isolation` | MetathesisPhaseIsolation | Infix, circumfix, reduplication, metathesis, subtractive truncation |
| `polysynthetic-stratal-derivation-chain` | PolysyntheticStratalDerivationChain | Derivation-then-inflection recursion across strata, incorporation-style compounding, guessed stems |
| `suffixing-evidential-adjacency-chain` | SuffixingEvidentialAdjacencyChain | Evidential suffix chain, morpheme-adjacency co-occurrence, `requires: []` — the XAmple-eligible, phonology-free grammar |
| `prefixal-discontinuous-slot-dependency` | PrefixalDiscontinuousSlotDependency | Left-edge position-class template, fused/discontinuous slot dependencies |
| `fusional-realizational-morphology` | FusionalRealizationalMorphology | Inflection classes, syncretism, ablaut as sole exponent, realizational blocking/suppletion |

Each is a dense, synthetic grammar (invented lexemes and segment inventories, no orthographic claim
about any real language) where ordinary words exercise several constructs at once, rather than a
one-mechanism probe. `edge-cases/` holds the things no shared grammar can host faithfully:
loader/XML-semantics probes, an `expect_crash` pin, a `budget_ms` pathological stress case, and
allomorphy/rewrite pins whose exact rule shape a naturalistic grammar wouldn't contain.

## Truncation identity companions

Three edge cases derive from the published Machine #480 truncation grammar at
`a48b899a456b4d7165cc53e1f76d6ef5dc357dd0` (whose original grammar remains unchanged):
`truncate-morphotactic-identity` adds explicit `MorphemeId` labels; `truncate-morphotactic-subsumption`
uses root `ag` plus prefix rule `X` with the original leading truncation rule `B` and insertion rule
`C`; `truncate-morphotactic-copy-u` adds a copying suffix rule `U` to the explicit-ID A/B/C grammar.
These cases preserve the distinction between a parse's allomorph order in its signature and its
forward rule application order in `words.yaml`'s `rules` list. Repeated signature strings are
intentional and remain repeated rows.

The checked-in expectations were captured from the combined #500+#506 C# source state: Machine #556
commit `d98d12a5d514ce589d7495c71d055830e061a418` plus the Machine #500 `ApplyRhs` relocation at
`157c031ef272d07c900db41b2d4a8852b13f64b0`. The #556 `SIL.Machine.dll` SHA-256 is
`b265a40a44292614f51f7a65afc445ae34dde685cb40ce8c2d2de5521a0c299d`; the combined
`SIL.Machine.Morphology.HermitCrab.dll` SHA-256 is
`4cd980453f51fd3cf29cf38a99f342f542e631eb23be6ba87b9433aab6c3d57f`. Reproduction is: start from
the #480 truncation grammar; add the identity labels or stated companion rule/root; use the #556
Machine library and the #500 relocation in HermitCrab's copy/annotation transfer; parse every
`words.yaml` word; and retain every returned signature and forward trace. These source revisions,
assembly hashes, and the recipe are the provenance; the expected rows do not depend on a temporary
staging file.

The published #480 base lacks both fixes, so its copied annotations do not satisfy these complete
identity rows reliably. Keep the full combined-oracle multisets; do not weaken them to match the
pre-fix base output.

The corrected C# traces report the same analysis features for these rows:
`pos=[posV]`, `syntactic=[POS:v]`, `realizational=ANY`, and the default MPR feature set.
Each root token is its entry's single root XML allomorph, and each rule token is that rule's
single allomorph; every observed allomorph has index 0 and count 1. Signature token order is
the final morph occurrence order, while the tables below retain the separate forward rule
application order. All positive words have status `ok`; `bag` and `bau` have status `ok` with
zero parses, as declared by `expect_fail`. None of the fixture words is skipped.

Stable source attribution is `ROOT` = `root32`/`root32_1` in the identity and copy-U fixtures;
`ROOTAG` = `root32`/`root32_1` in subsumption; `A` = `mruleTruncTrail`, `B` =
`mruleTruncLead`, `C` = `mruleTruncOptIns`, `X` = `mrulePrefixX`, and `U` = `mrU`. The
corresponding `MorphemeId` is explicit in each companion grammar; runtime GUIDs are not used as
fixture identities.

`truncate-morphotactic-identity` expected parses:

| Word | Signature | Forward rule order |
|---|---|---|
| `sag` | `ROOT|sag` | root |
| `sas` | `ROOT33|sas` | root |
| `a` | `ROOT+B+A|a` | B, A |
| `a` | `ROOT+A+B|a` | A, B |
| `ga` | `ROOT+B+C+A|ga` | B, A, C |
| `ga` | `ROOT+A+C+B|ga` | A, B, C |
| `ga` | `C+B+ROOT+A|ga` | B, C, A |
| `ga` | `ROOT+C+A|ga` | A, C |
| `ga` | `C+ROOT+A|ga` | C, A |
| `bag` | no parses | — |

`truncate-morphotactic-subsumption` expected parses:

| Word | Signature | Forward rule order |
|---|---|---|
| `ag` | `ROOTAG|ag` | root |
| `ag` | `ROOTAG+X+B|ag` | X, B |
| `sag` | `X+ROOTAG|sag` | X |
| `gag` | `X+C+ROOTAG|gag` | X, C |
| `gag` | `X+C+ROOTAG+B|gag` | X, B, C |
| `gag` | `X+C+ROOTAG+B|gag` | C, X, B |
| `gag` | `C+ROOTAG|gag` | C |
| `bag` | no parses | — |

`truncate-morphotactic-copy-u` expected parses (the repeated `ROOT+C+A+U|gau` identity is retained):

| Word | Signature | Forward rule order |
|---|---|---|
| `gau` | `ROOT+A+C+B+U|gau` | A, B, C, U |
| `gau` | `ROOT+B+C+A+U|gau` | B, A, C, U |
| `gau` | `B+A+C+ROOT+U|gau` | B, C, A, U |
| `gau` | `C+B+ROOT+A+U|gau` | A, B, U, C |
| `gau` | `C+A+ROOT+B+U|gau` | B, A, U, C |
| `gau` | `ROOT+U+C+A+B|gau` | A, U, B, C |
| `gau` | `ROOT+C+A+U|gau` | A, C, U |
| `gau` | `C+A+ROOT+U|gau` | C, A, U |
| `gau` | `ROOT+C+A+U|gau` | A, U, C |
| `au` | `B+ROOT+A+U|au` | A, B, U |
| `au` | `A+ROOT+B+U|au` | B, A, U |
| `au` | `ROOT+A+U+B|au` | A, U, B |
| `sagu` | `ROOT+U|sagu` | U |
| `bau` | no parses | — |

Every listed allomorph is index 0; the named roots and rules in the signatures are their explicit
`MorphemeId` values. The rules use the existing `AffixProcessRule: subtraction/truncation`
construct label; the fixtures do not introduce new coverage categories. All three declare
`morphologicalRuleOrder="unordered"`, which HCLoader emits. Their words are intentionally synthetic,
but that does not make the grammar constructs FieldWorks-unproducible, so all three correctly carry
`fieldworks_producible: true`.

The combined #500+#506 corrected-engine audit that flips those copied grammars from `unordered` to
`linear` loses analyses in all three companions; the exact changed words and signatures are recorded
in `docs/morphological-rule-order-measurement.md`. The checked-in grammars remain `unordered`,
matching HCLoader's emitted order.

The ordinary conformance runner checks signature multiplicity and the set of rule IDs associated
with each signature. It currently canonicalizes rule IDs before comparing them, so the ordered
`rules` lists preserve the observed application histories but do not independently gate their order.

Fixture and case totals are summed from `generated/hc-conformance-manifest.v1.json`, not
hand-copied here: two prose totals in this file have already drifted from the manifest twice, so
this file no longer states them at all -- read the manifest (`--generate-manifest`/
`--check-manifest`), whose per-fixture `caseCount` is what those flags verify.

## What the coverage numbers claim, and what they do not

Every coverage figure here is **per surface** — one DTD element, or one enumerated attribute value.
A surface counts as covered when a fixture exercises it and, in the counterfactual ledger,
neutralizing it changes a real word's parse. That is a strong claim about each surface on its own.

It is **not** a claim about surfaces in combination. Nothing in this suite enumerates or measures
interactions between surfaces, so a green run means "no uncovered surface is known", never "no
uncovered behaviour is known". A defect needing two or more surfaces together can sit behind
fully-covered ingredients indefinitely; one does today. Enumerating every pairwise interaction is
not the fix, since the space is combinatorial while the coverage claim is per-surface; closing this
gap needs selective, named interaction coverage instead, tracked as `conformance/interface-inventory.tsv`
(integration/edge, landed) plus a capped hand-crafted integration/chain set.

## Coverage philosophy

`constructs.txt` is the flat, cross-grammar checklist of grammar-model constructs every fixture's
`exercises:` values are drawn from — the coverage report cross-references against it and flags both
zero-coverage constructs and **dead rules** (a `grammar.xml` rule id no word exercises). A rule named
in an `expect_fail` word's `blocked_by` counts as exercised, but `blocked_by` is **asserted, not
verified**: the report fails when a label is not a rule id in that fixture's grammar, and does not
check the parse trace to confirm the rule blocked the word (tracked in sillsdev/machine#552). `rules:`
per parse is a verified dimension, not aspirational: self-check runs the oracle with tracing on and
FAILs if the traced rule applications diverge from the declared list. Analyses with identical
signatures are retained: self-check compares `(signature, rule-id set)` as a multiset, so two
equal signatures with different valid traces can match independently of result order. A missing,
extra or incorrectly attributed trace still fails. As of this writing, 31/31
in-scope constructs are covered (`Tracing (TraceType)` is out of scope — it was never in
`expected.tsv`'s domain).

## Running it

```
# self-check: in-process C# oracle against every fixture
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --include-pathological

# adapter mode: materializes words.txt/expected.tsv to temp, runs an external engine
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --adapter "<command template>"

# coverage + dead-rule report: writes coverage.csv and rules.csv
dotnet run --project src/SIL.Machine.Morphology.HermitCrab.Conformance -- --fixtures conformance --coverage-report

# migration parity proof (v1->v2 floors + the permanent absolute construct check)
python conformance/parity-check.py
```

## Growth policy: every bug fix lands with a conformance addition

> **Every bug fix in a conforming engine lands together with a conformance addition** — a new word,
> a new grammar, or a new edge-case.

The cheap path, in order of preference:

1. **Add a word.** One `words.yaml` entry (word + `note` + `parses` + `provenance`) in the language
   whose grammar already covers the territory. No harness or grammar changes.
2. **Extend a grammar** when no existing language's grammar can express the trigger but the
   phenomenon is typologically plausible for one of the eight (e.g. a new backness-harmony
   interaction belongs in `suffixing-vowel-harmony`).
3. **Add an edge-case** when the pin is engine-internal or too specific to embed naturalistically
   (an XML-loader quirk, a crash, a rule shape no real grammar would contain).
4. **Add a new `languages/` member** only for genuinely new typological territory not covered by any
   of the eight, not for an incremental variant of one that already exists.

## See also

- `PROTOCOL.md` — the adapter contract: CLI shape, TSV format, signature algorithm, capability
  profiles, per-engine grammar representation.
