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
not languages. See `docs/coverage-levels.md` for the levels, the admissibility preconditions, and
what remains between here and the goal.

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
  edge-cases/<name>/   grammar.xml + words.yaml   -- 21 micro-grammars for things no shared grammar hosts
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

Exactly two files per fixture. `words.yaml` is the single ground truth: front matter (`language`,
`inspired_by`, `sources`, `requires`) plus one entry per word, each carrying a `note` and either
`parses` (`signature`/`gloss`/`rules`/`exercises`, one per distinct analysis), `expect_fail` +
`blocked_by` (well-formed input, zero valid analyses), or `expect_skip` (the oracle throws
`InvalidShapeException`, e.g. an undeclared segment — status "SKIPPED", not "ok"). Optional fields:
`provenance` (bug/PR reference), `guess` (a `LexicalGuess` parse),
`budget_ms`/`expect_crash` (edge-cases only). Signatures are authored-and-verified, never blindly
regenerated — self-check FAILs on mismatch; `--propose` prints a patch for a human to accept, never
writes. Full schema: `docs/conformance-language-suite-plan.md` §2.1.

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
one-mechanism probe. `edge-cases/` holds the 21 things no shared grammar can host faithfully:
loader/XML-semantics probes, an `expect_crash` pin, a `budget_ms` pathological stress case, and
allomorphy/rewrite pins whose exact rule shape a naturalistic grammar wouldn't contain.

29 fixtures and 424 cases in total. Those counts are not maintained by hand — the manifest carries
them per fixture, and `--check-manifest` fails on drift.

## What the coverage numbers claim, and what they do not

Every coverage figure here is **per surface** — one DTD element, or one enumerated attribute value.
A surface counts as covered when a fixture exercises it and, in the counterfactual ledger,
neutralizing it changes a real word's parse. That is a strong claim about each surface on its own.

It is **not** a claim about surfaces in combination. Nothing in this suite enumerates or measures
interactions between surfaces, so a green run means "no uncovered surface is known", never "no
uncovered behaviour is known". A defect needing two or more surfaces together can sit behind
fully-covered ingredients indefinitely; one does today, and is described in `docs/coverage-levels.md`
along with the level model and why the missing measurement is not simply "enumerate the pairs".

## Coverage philosophy

`constructs.txt` is the flat, cross-grammar checklist of grammar-model constructs every fixture's
`exercises:` values are drawn from — the coverage report cross-references against it and flags both
zero-coverage constructs and **dead rules** (a `grammar.xml` rule id no word exercises). `rules:`
per parse is a verified dimension, not aspirational: self-check runs the oracle with tracing on and
FAILs if the traced rule applications diverge from the declared list. As of this writing, 19/19
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
   of the eight (see the future-expansion tier in `docs/conformance-language-suite-plan.md` §3).

## See also

- `PROTOCOL.md` — the adapter contract: CLI shape, TSV format, signature algorithm, capability
  profiles, per-engine grammar representation.
- `docs/conformance-migration-ledger.md` — the permanent provenance bridge from every v1 fixture to
  its v2 destination.
- `docs/conformance-language-suite-plan.md` — the full design rationale, language-selection research,
  and phased plan this suite was restructured from.
- `docs/pangloss-handoff.md` — the handoff record for a consuming implementation: exactly what is and
  is not delivered, current numbers verified against the checked-in files, and the claim scoped
  precisely.
