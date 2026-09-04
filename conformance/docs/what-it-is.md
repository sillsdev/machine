# What this suite is

An engine-agnostic conformance suite for morphological/phonological parsers: a set of grammars and
words with pre-computed correct answers, plus the machinery that justifies calling that answer set
complete. `what-it-claims.md` covers the second half. This document covers the first: the fixture
format, what a case is, and why the corpus is split into `languages/` and `edge-cases/`.

## Shape

```
conformance/
  languages/<name>/    grammar.xml + words.yaml   -- typologically-selected synthetic languages
  edge-cases/<name>/   grammar.xml + words.yaml   -- micro-grammars for things no shared grammar hosts
  HermitCrabInput.dtd  the published grammar DTD (byte-identical to the library's embedded copy)
  schema/              JSON Schema for words.yaml and the manifest
  generated/           the versioned JSON manifest a consumer imports
  adapters/            wrapper scripts for engines whose CLI predates the adapter protocol
  evidence-cards/       one rendered Markdown card per obligation cell (see how-it-is-computed.md)
  skills/               the agent skills that author and review coverage claims
  PROTOCOL.md           the adapter contract: CLI shape, TSV format, signature algorithm
```

Per `conformance/generated/hc-conformance-manifest.v1.json`, today that is **33 fixtures — 8 under
`languages/`, 25 under `edge-cases/` — holding 446 words in total**, computed by summing the
manifest's own `caseCount` field per fixture rather than counted by hand. Regenerate the manifest
with `hc-conformance --generate-manifest`; `--check-manifest` fails the build if the checked-in file
has drifted from what a fresh scan produces.

## The fixture contract: exactly two authored files

Every fixture is exactly two files, and there is no third, generated restatement of either:

- **`grammar.xml`** — a `HermitCrabInput` XML document, validated against `HermitCrabInput.dtd`. It
  is the authoritative grammar: parts of speech, phonological feature system, character definitions,
  strata, morphological and phonological rules. This is also the reference *representation* — the
  one every fixture ships, and the one the C# founding oracle read to produce every case's ground
  truth. An engine that cannot consume XML directly names an alternate grammar representation instead
  (`PROTOCOL.md` section 6); `grammar.xml` remains the source of truth either way.
- **`words.yaml`** — the single canonical authored format for ground truth. Front matter
  (`language`, `inspired_by`, `sources`, `requires`, optionally `budget_ms` and `expect_crash`)
  followed by a `words` list, one entry per case. Validated against `schema/words.schema.json`
  (`hc-conformance-words/v1`).

An earlier product, `hc-conformance-corpus/v1`, duplicated `words.yaml`'s ground truth into a
generated 409KB JSON file — and that file carried *less* than the 383KB of authored source it was
built from, because the authored comments hold reasoning (why a fixture exists, what it pins) that
JSON cannot express. It has been deleted, along with its schema. What a manifest genuinely
contributes is provenance and integrity, not case data, so that is the only generated artifact that
survives: `generated/hc-conformance-manifest.v1.json` (`schema/conformance-manifest.schema.json`),
one entry per fixture carrying `fixtureId`, `category`, `displayLanguage`, both files' paths and
SHA-256 hashes, `caseCount`, and `expectedCrash`. A consumer reads the manifest to discover fixtures
and verify integrity, then reads `grammar.xml` and `words.yaml` directly for the grammar and the
cases themselves.

## What a case is

A case is one entry in `words.yaml`'s `words` list: a literal word string, a `note` explaining what
it pins, and exactly one of three outcomes. The simplest case is a bare-root control with a single
trivial parse:

```yaml
- word: ka
  note: Bare head root, number=sg, class=a.
  parses:
    - signature: "KA|ka"
      rules: []
```

A case exercising more machinery carries a longer `rules:` chain and its own `gloss`:

```yaml
- word: menulik
  note: >-
    TULIK + mrNPfx: underlying "meN+tulik" -- the placeholder nasal assimilates to alveolar "n"
    ..., then the now-real "n" triggers prObstruentDeletion ... -- "mentulik" -> "menulik".
  parses:
    - signature: "NPFX+TULIK|menulik"
      rules: [mrNPfx, prNasalAssimAlveolar, prObstruentDeletion]
```

(`conformance/edge-cases/compounding-breadth/words.yaml`, `conformance/edge-cases/mpr-gated-exception/words.yaml`.)

A word carrying **more than one** distinct analysis lists more than one entry under `parses:` — the
signature algorithm (`PROTOCOL.md` section 3) treats a word's full analysis set as a multiset of
`<morpheme-chain>|<shape>` strings, so ambiguity is represented directly as multiple list items, not
as a separate field.

Per-parse fields: `signature` and `rules` are required (`rules` is the traced list of rule ids the
oracle actually applied, checked against a live trace — not merely declared); `gloss` is optional
prose; `exercises` optionally names which constructs (`constructs.txt` vocabulary) this specific
parse demonstrates, distinct from the word-level `exercises` naming what the word as a whole
demonstrates; `guess` marks a parse only reachable via root-guessing (see below).

Two more per-word fields worth knowing:

- **`neutralizes`** — names a construct (an id from the grammar) whose deactivation this word is
  designed to observe. A word can observe a deactivation either by failing (paired with
  `blocked_by`) or by parsing *differently* once the construct is gone — the schema admits either
  outcome, because the point is what the word demonstrates about removing something, not which
  polarity that removal produces.
- **`claimed_cells`** — a word's declaration that it witnesses one specific cell of
  `dataflow-obligations.tsv` (see `how-it-is-computed.md`). Checked mechanically against that ledger
  and never trusted at face value.

## `expect_fail`, `expect_skip`, and `expect_crash`

Three distinct ways a word's expected outcome can be "not a parse":

| | means | oracle status | schema constraint |
|---|---|---|---|
| `expect_fail: true` | well-formed input, the grammar legitimately produces **zero** analyses | `ok`, signature `-` | `parses` must be empty; if paired with `blocked_by`, names the rule(s) responsible |
| `expect_skip: true` | the word contains a character the grammar's character-definition table never declares | `SKIPPED` | `parses` must be empty; mutually exclusive with `expect_fail` |
| `expect_crash: true` (fixture-level) | the founding oracle itself did not complete — it crashed while loading or parsing | no result row at all, only a `STARTED` sentinel | fixture must declare exactly one case |

`expect_fail` and `expect_skip` are both legitimate, "ok" outcomes for the harness, and the schema
refuses a word marked as both — an engine returning `SKIPPED` where `ok`/`-` was expected (or vice
versa) is a genuine status mismatch, not a fuzzy match. A concrete `expect_skip` case:
`edge-cases/loader-pattern-shapes/words.yaml`'s word `bit` — `"i"` is never declared in that
grammar's character table, so the C# oracle throws `InvalidShapeException` rather than returning a
zero-analysis `ok`, and the fixture pins `SKIPPED` rather than `expect_fail` to keep that distinction
honest.

`expect_crash` is the strongest and strangest of the three: for a crash fixture, the engine under
test is expected to **also** crash (not necessarily with the same exception) to count as a **pass**
— reproducing the founding oracle's crash is what conforming means here. If the engine crashes
without `expect_crash` set, that is always a fail; if `expect_crash` is set and the engine returns a
normal result instead of crashing, that is *also* a fail, even though the engine's behavior is
arguably better than the oracle's — `PROTOCOL.md` section 2 is explicit that this pins an
implementation limitation of the founding oracle, not a linguistic fact. Two fixtures do this today
(`edge-cases/metathesis-comparison-crash`, `edge-cases/simultaneous-epenthesis-cascade`, per the
manifest's `expectedCrash` field), and each keeps its `words.txt` to a single word, since nothing
after a crash is ever reached.

One more fixture-level field: `budget_ms`, present only on
`edge-cases/deep-optional-affix-nesting/words.yaml` (`budget_ms: 15000`) — a wall-clock ceiling for a
pathological word whose correct answer is a large but fully tractable analysis set (924 distinct
analyses for one word). A fixture carrying `budget_ms` is excluded from a default self-check run and
only exercised with `--include-pathological`, because it is deliberately expensive, not because its
answer is in doubt.

## The `guess` parse, and its adapter-mode gap

A parse marked `guess: true` is only reachable through `Morpher`'s root-guessing path
(`LexicalGuess`), which the CLI's own `batch` command has no flag to enable — so a `guess: true`
parse can only ever be checked by the harness's in-process self-check mode, never by an external
adapter driven through the documented `batch` contract. Adapter-mode materialization omits any word
carrying a `guess: true` parse from the generated `words.txt`/`expected.tsv` entirely, so such words
are asserted by self-check only and an adapter run neither sees them nor can be falsely failed by
them (`languages/polysynthetic-stratal-derivation-chain/words.yaml` has a worked example: a guessed
shape reachable at two different strata renders as the same signature listed twice, since a
signature multiset does not dedupe).

## `languages/` versus `edge-cases/`

The corpus is split into two directories that answer different questions, and the split is
deliberate rather than a filing convenience.

**`languages/`** holds 8 dense, synthetic grammars, each simulating a distinct typological profile —
invented lexemes and segment inventories, no orthographic claim about any real language — where
ordinary words exercise several constructs at once rather than probing one mechanism in isolation:

| Directory | Simulates |
|---|---|
| `suffixing-vowel-harmony` | Long-distance backness harmony, long agglutinative suffix chains, consonant-gradation allomorphy |
| `templatic-root-modification` | Templatic root-and-pattern morphology, stem names, OCP co-occurrence, simultaneous rewrite |
| `suffixing-extension-slot-ordering` | Realizational rules, MPR groups, verbal reduplication, post-nasal mutation, CARP-style extension ordering |
| `metathesis-phase-isolation` | Infix, circumfix, reduplication, metathesis, subtractive truncation |
| `polysynthetic-stratal-derivation-chain` | Derivation-then-inflection recursion across strata, incorporation-style compounding, guessed stems |
| `suffixing-evidential-adjacency-chain` | Evidential suffix chain, morpheme-adjacency co-occurrence, phonology-free |
| `prefixal-discontinuous-slot-dependency` | Left-edge position-class template, fused/discontinuous slot dependencies |
| `fusional-realizational-morphology` | Inflection classes, syncretism, ablaut as sole exponent, realizational blocking/suppletion |

(Table per `conformance/README.md`.) `how-it-is-computed.md` shows why this half of the split matters
beyond typological breadth: of the interfaces the whole corpus actually exercises, the large majority
are reached by one of these eight, not by a fabricated fixture — realistic grammars carry most of the
suite's real evidentiary weight, and the fabricated half exists for what realistic grammars cannot
host.

**`edge-cases/`** holds 25 micro-grammars for things no shared, realistic grammar can host
faithfully: loader/XML-semantics probes (`loader-isactive`, `loader-pattern-shapes`,
`loader-default-symbol`), an `expect_crash` pin, a `budget_ms` pathological stress case, and
allomorphy/rewrite pins whose exact rule shape a naturalistic grammar wouldn't contain (e.g.
`mpr-gated-exception`'s deliberately-isolated MPR-feature exclusion, or
`metathesis-comparison-crash`, reduced down from a larger fixture specifically to isolate one
engine-internal comparison defect). Each is deliberately narrow — most declare `requires: []` or a
short, purpose-built grammar — because its entire job is to pin one specific hazard cleanly, not to
read as a plausible grammar.

Neither directory subsumes the other. A construct that can occur naturally belongs in a
`languages/` grammar, extended rather than duplicated, because a realistic grammar that grows a
realistic feature buys coverage *and* keeps that feature interacting with everything else the
grammar already does; an edge-case fixture is the fallback for what cannot be made to occur
naturally at all.
