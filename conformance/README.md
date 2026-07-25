# The morphological parser conformance suite

An **all-language parser oracle**: a general-purpose, engine-agnostic conformance test suite for
what a morphological/phonological parser needs to get right, usable independently of this repo and
of any one engine. Passing this suite means an engine can correctly parse any language's morphology
— or will, as coverage grows. `PROTOCOL.md` specifies the adapter contract in engine-agnostic terms;
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
  edge-cases/<name>/   grammar.xml + words.yaml   -- 8 micro-grammars for things no shared grammar hosts
  coverage.csv         GENERATED: language x word x construct
  rules.csv            GENERATED: language x grammar rule id x exercising words
```

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
one-mechanism probe. `edge-cases/` holds the eight things no shared grammar can host faithfully:
loader/XML-semantics probes, an `expect_crash` pin, a `budget_ms` pathological stress case, and
allomorphy/rewrite pins whose exact rule shape a naturalistic grammar wouldn't contain.

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
