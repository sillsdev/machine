# Conformance suite v2 — typologically-selected synthetic languages

**Status:** approved direction (John, 2026-07-11): hybrid layout. Roster grew 6 → 8 in response to
John's coverage question ("are all language features covered? should we add 1–5 more?") — see §3
for the two additions and the rationale; the roster is in review. Supersedes the
fixture-per-mechanism layout of `docs/archive/conformance-framework-plan.md` (kept for history); the
adapter contract in `conformance/PROTOCOL.md` is unchanged. Nothing has shipped — PR #454 is a
draft — so this is a restructure of the same branch, not a migration users can observe.

## 1. Why restructure

The F0–F3 suite works (41 fixtures, self-check green, adapter mode green) but has three costs:

1. **File sprawl.** 252 files; each fixture carries up to 7 (`grammar.xml`, `words.txt`,
   `expected.tsv`, `script.txt`, `manifest.json`, `words.yaml`, `README.md`), with each word's
   story smeared across four of them.
2. **Isolation is the wrong default.** Every migrated fixture is a hand-minimized single-mechanism
   probe, yet `HISTORY-MATRIX.md` shows C#'s real bugs clustered in *interactions*. Cross-cutting
   coverage exists only as a special category (1 fixture).
3. **Coverage follows the engine, not languages.** Fixtures are named after HermitCrab class names.
   Six constructs sit at zero coverage because no bug ever happened to hit them; nothing about the
   taxonomy pulls toward closing them. The suite's stated ambition — an all-language parser oracle —
   is not reflected in its structure.

## 2. The new shape

A small set of **synthetic languages**, each simulating a named real language family chosen from
morphological typology, each dense enough that ordinary words exercise several constructs at once.
Plus a small **edge-cases** set for things no shared grammar can host.

```
conformance/
  README.md            # the one doc: project, families, coverage philosophy, growth policy
  PROTOCOL.md          # adapter contract (unchanged semantics, trimmed prose)
  constructs.txt       # coverage keys (unchanged role)
  coverage.csv         # GENERATED: word × language × constructs exercised
  languages/<name>/    # exactly two files each: grammar.xml + words.yaml
  edge-cases/<name>/   # micro-grammars: loader probes, expectCrash, budgeted stress, unembeddable pins
```

Two files per fixture, ~40 files total (from 252).

### 2.1 `words.yaml` is the ground truth

One YAML per grammar replaces `words.txt` + `expected.tsv` + `script.txt` + `manifest.json` +
`words.yaml` + `README.md`:

```yaml
language: Veyra                       # invented name; never a real language's name
inspired_by: ["Turkish (Turkic)", "Finnish (Uralic)"]
sources: ["WALS ch. 20/21", "..."]    # the technical sources the simulation is grounded in
requires: [phonology]                 # capability profile, validated against grammar.xml as today
# edge-cases only:
# budget_ms: 15000
# expect_crash: true
words:
  - word: evlerimde
    note: Backness harmony propagates across the whole suffix chain.
    parses:
      - signature: "EV+PL+1SG+LOC|evlerimde"
        gloss: house-PL-1SG-LOC
        rules: [mrPlural, mrPoss1sg, mrLocative, prBacknessHarmony]
        exercises:
          - "RewriteRule Iterative (epenthesis/deletion/feature/expansion/merge)"
          - "Affix template slots (obligatory/disjunctive/ordering)"
  - word: evlarimda            # non-parse: the grammar must reject it
    note: Harmony violation; must produce zero parses.
    expect_fail: true
    blocked_by: [prBacknessHarmony]
    exercises: ["RewriteRule Iterative (epenthesis/deletion/feature/expansion/merge)"]
    # optional on any word: provenance: "LT-XXXXX / PR link" (bug-driven growth policy)
  - word: mekterinde           # guess-stem parse: root deliberately absent from the lexicon
    note: Only a guesser (LexicalGuess) analysis exists; pins guessed-stem behavior.
    parses:
      - signature: "<guess-stem rendering, pinned in G1>+PL+LOC|mekterinde"
        guess: true
        rules: [mrPlural, mrLocative]
        exercises: ["Guesser/LexicalGuess"]
```

Rules:

- `parses[].signature` uses the existing signature algorithm — the comparison unit is unchanged.
- Signatures are **authored-and-verified, never blindly regenerated** (the LT-22613 lesson: the
  oracle can be wrong). Self-check FAILs on mismatch; a `--propose` mode prints the would-be YAML
  patch for a human to accept. No mode ever writes the file.
- `exercises` values are verbatim `constructs.txt` entries (unknown value = soft warning, as today).
- `rules` (per parse, **required**): every grammar rule — by its `grammar.xml` rule id — expected
  to fire in that parse. This is the fine-grained coverage dimension under `exercises`' coarse one,
  and it is *verified*, not aspirational: self-check runs the oracle with tracing enabled and FAILs
  if the traced rule applications don't match the declared list. (Tracing stays engine-internal and
  oracle-side only — the adapter contract and `expected.tsv` semantics are untouched.)
- Non-parses: `expect_fail: true` plus word-level `exercises`, and optionally `blocked_by` — the
  rule id(s) whose correct behavior forbids the parse — so rejection coverage shows up in the rule
  tables too.
- Skips: `expect_skip: true` (mutually exclusive with `expect_fail`) marks a word the oracle *skips*
  rather than returning a genuine zero-parse "ok" — it throws `InvalidShapeException` (e.g. an
  undeclared segment). The two differ in the adapter-contract status column ("SKIPPED" vs "ok"), so
  they are distinct keys: self-check verifies the `InvalidShapeException` actually fired and adapter
  mode materializes status "SKIPPED", keeping the two run modes consistent on the distinction.
- Guess-stem parses: `guess: true` marks an analysis produced via `LexicalGuess` (root deliberately
  absent from the lexicon). The exact signature rendering of a guessed stem is pinned in G1 from
  `BatchCommand`'s actual output and documented in PROTOCOL.md.
- `gloss` is **per parse, and optional**: a gloss describes one analysis, so an ambiguous word
  carries one gloss per parse and an `expect_fail` word carries none. (A parse may also lack a
  gloss where morpheme-by-morpheme glossing is meaningless, e.g. pathological stress words.)
- Every word needs a `note`; multi-parse words describe each parse.
- **Machine-parsable, human-readable — both are hard requirements** (tables are generated from
  these files). Plain YAML 1.2 subset only: no anchors, aliases, merge keys, or custom tags; fixed
  key vocabulary (schema-checked by the harness, unknown keys are errors); block style with one
  word per entry so diffs and reviews stay line-oriented.
- The old category system dissolves: negative = `expect_fail` word; cross-cutting = the default
  nature of a language grammar; pathological = an edge-case with `budget_ms`; single-feature = an
  edge-case pin.

### 2.2 The adapter contract does not change

`PROTOCOL.md`'s CLI contract (`<engine> batch <grammar> <words.txt> <out.tsv>`) stays byte-for-byte.
The harness materializes `words.txt` and `expected.tsv` into a temp directory from `words.yaml`
before invoking an adapter, so existing adapters (including `adapters/hc-dotnet-wrapper.sh`) work
unmodified. The YAML is a repository format, not a wire format.

## 3. Language selection (the research)

Candidate pool of 32, drawn from morphological-typology surveys (WALS fusion/exponence/
prefixing-suffixing/reduplication chapters; the metathesis, subtraction, and polysynthesis
literature), scored against the 20-row construct checklist. Full matrix in §3.1; the picks:

| Grammar | Simulates | Carries (uniquely bold) |
|---|---|---|
| `templatic-semitic` | Arabic, Amharic | **`ModifyFromInput`/`InsertSimpleContext`** (zero today), stem names, OCP root co-occurrence, `RewriteRule` Simultaneous, epenthesis |
| `agglutinative-turkic` | Turkish, Finnish | Iterative long-distance harmony, gradation allomorphy, obligatory/disjunctive slots, natural-class precision, boundary markers |
| `bantu-verbal` | Swahili, Ndebele, Sena | Realizational rules, MPR features/groups, **verbal reduplication** (zero today), extension-slot ordering, nasal-prefix mutation (Celtic-style mutation coverage rides here) |
| `austronesian-phase` | Tagalog, Indonesian, Rotuman, Leti | Infix, circumfix, CV- + full reduplication, **`MetathesisRule`** (Rotuman phase), truncation (Rotuman deletion-phase; O'odham cited), **isolated `CopyFromInput`/`InsertSegments`** (zero today) |
| `polysynthetic-inuit` | Yup'ik, Kalaallisut | **Stratum ordering** (zero today) via derivation-then-inflection recursion, incorporation-style compounding, **Guesser/`LexicalGuess`** (zero today), seam epenthesis/deletion |
| `suffixing-quechua` | Cuzco Quechua | `requires: []` — the XAmple-eligible grammar: evidential chains, suffix co-occurrence, environment-conditioned allomorphs, obligatory slots |
| `prefixal-athabaskan` | Navajo | Position-class **prefixal** template with fused/discontinuous slot dependencies — the hardest documented template morphology; everything else in the roster is suffix-dominant, so template stress from the left edge is otherwise untested |
| `fusional-latin` | Latin, Russian; German ablaut | Inflection classes + syncretism (one affix, many features; many affixes, one cell), stem alternants as principal parts, ablaut/apophony as **sole** exponent — realizational machinery under fusional load, which `bantu-verbal`'s agglutinative use of the same rules never produces |

The first six cover every in-scope checklist construct (infixation rides `austronesian-phase` via
Tagalog `-um-`); rows 7–8 exist because checklist coverage is not the same as *stress* coverage —
they re-exercise already-covered constructs under configurations (left-edge position classes,
fusional syncretism, affixless exponence) that no suffix-dominant or agglutinative grammar
produces. Five of the six zero-coverage constructs get natural homes; the sixth (`Tracing`) stays
explicitly out of scope (it never was in `expected.tsv`'s domain). Without `suffixing-quechua`,
XAmple-eligible coverage would drop from 19 fixtures to zero, killing plan-v1 goal 7 (XAmple-ready);
it is the designated morphotactics-pure grammar.

**Future expansion tier** (documented trigger, not built now): Nilotic stem-internal-only
inflection (Nuer/Dinka — vowel grade + voice quality with almost no affixes), Kayardild/Warlpiri
case stacking (recursive suffix concord), Chukchi (circumfix + incorporation + harmony combined in
one grammar). Each enters as a new `languages/` member the day an engine bug or capability question
touches its territory — same growth policy as everything else.

Grammars are synthetic: invented lexemes, invented (plausible) segment inventories, no orthographic
claims about any real language — `inspired_by` + `sources` say what phenomenon class each mechanism
simulates. A grammar may cite multiple family members (e.g. Rotuman for metathesis inside an
otherwise Philippine-style grammar) as long as every member is Austronesian-plausible.

### 3.1 Candidate matrix (32)

| # | Language (family) | Signature phenomena | Disposition |
|---|---|---|---|
| 1 | Turkish (Turkic) | vowel harmony, long suffix chains | **picked** → `agglutinative-turkic` |
| 2 | Finnish (Uralic) | harmony + consonant gradation | folded into 1 |
| 3 | Hungarian (Uralic) | harmony, definiteness conjugation | class covered by 1 |
| 4 | Kazakh (Turkic) | rounding harmony | class covered by 1 |
| 5 | Arabic (Semitic) | root-and-pattern, OCP root constraints | **picked** → `templatic-semitic` |
| 6 | Hebrew (Semitic) | templatic, weak-root deletions | folded into 5 |
| 7 | Amharic (Ethiosemitic) | templatic + affixal mix (HC reference corpus) | folded into 5 |
| 8 | Swahili (Bantu) | noun classes, verb template | **picked** → `bantu-verbal` |
| 9 | Ndebele (Bantu) | verbal reduplication | folded into 8 |
| 10 | Sena (Bantu) | HC reference corpus | folded into 8 |
| 11 | Tagalog (Austronesian) | infix `-um-`, CV-reduplication | **picked** → `austronesian-phase` |
| 12 | Indonesian (Austronesian) | circumfix `ke-…-an`, full reduplication | folded into 11 |
| 13 | Rotuman (Oceanic) | phase alternation: metathesis/umlaut/deletion | folded into 11 (metathesis + truncation carrier) |
| 14 | Leti (Austronesian) | phrase-conditioned metathesis | folded into 11 |
| 15 | Sierra Miwok (Utian) | templatic stem shapes, metathesis | skipped — covered by 13/14 |
| 16 | Navajo (Athabaskan) | position-class template, extreme fusion | **picked** → `prefixal-athabaskan` |
| 17 | C.A. Yup'ik (Eskimo-Aleut) | recursive derivational suffixation | **picked** → `polysynthetic-inuit` |
| 18 | Kalaallisut (Eskimo-Aleut) | extreme synthesis | folded into 17 |
| 19 | Mohawk (Iroquoian) | noun incorporation | folded into 17 (as compounding) |
| 20 | Chukchi (Chukotko-Kamchatkan) | incorporation + circumfix + harmony | skipped — overlaps 17/12/1 |
| 21 | Georgian (Kartvelian) | circumfixes, version vowels | skipped — circumfix via 12 |
| 22 | Welsh (Celtic) | initial consonant mutation | folded into 8 (nasal-prefix mutation) |
| 23 | Irish (Celtic) | mutation + eclipsis | skipped — same class as 22 |
| 24 | Tohono O'odham (Uto-Aztecan) | subtractive perfective truncation | folded into 11 (deletion-phase; cited in provenance) |
| 25 | Alabama (Muskogean) | subtraction, internal change | skipped — same class as 24 |
| 26 | Cuzco Quechua (Quechuan) | strictly-suffixing, regular, evidentials | **picked** → `suffixing-quechua` |
| 27 | Sanskrit (Indo-Aryan) | sandhi cascades, long-distance retroflexion | folded — long-distance rewrite via 1's harmony |
| 28 | Latin (Italic) | fusional classes, stem alternants, syncretism | **picked** → `fusional-latin` |
| 29 | German (Germanic) | circumfix `ge-…-t`, ablaut, compounding | folded — ablaut into 28, circumfix via 12, compounding via 17 |
| 30 | Warlpiri (Pama-Nyungan) | reduplication, case stacking | future tier — case stacking |
| 31 | Nuer (Nilotic) | stem-internal-only inflection (vowel grade, voice quality) | future tier |
| 32 | Kayardild (Tangkic) | extreme case stacking / suffix concord | future tier |

## 4. Harness changes

`SIL.Machine.Morphology.HermitCrab.Conformance` keeps its runner/self-check/adapter/coverage modes;
what changes is the fixture model:

- **Discovery:** a fixture is any directory under `languages/` or `edge-cases/` containing
  `grammar.xml` + `words.yaml`. `FixtureManifest` is replaced by the YAML front matter (parsed with
  YamlDotNet, a new test-side dependency).
- **Self-check:** per word, run the engine, build the signature set, compare against the set of
  `parses[].signature` (empty set for `expect_fail`). Same order-independent semantics as today.
- **Adapter mode:** materialize `words.txt`/`expected.tsv` per fixture into temp, then the existing
  subprocess/diff flow runs unchanged.
- **`requires` validation:** `RequiresDerivation` is unchanged — front-matter `requires` is still
  mechanically re-derived from `grammar.xml` every run, mismatch FAILs.
- **Coverage:** emits two generated tables, both committed and regenerable (CI can diff for
  freshness): `conformance/coverage.csv` (language, word, parse signature, construct) and
  `conformance/rules.csv` (language, rule id, exercising words — including `blocked_by`
  attributions). The harness enumerates every rule id in each `grammar.xml` and flags **dead
  rules** (rules no word exercises) — a mechanical detector for authoring gaps inside a grammar,
  parallel to what `constructs.txt` does across grammars. Console rollup stays.
- **Traced verification of `rules`:** self-check runs the oracle with tracing on and FAILs a parse
  whose actual rule applications diverge from its declared `rules` list.
- **`--propose`:** on signature mismatch, print the YAML patch that would reconcile; never write.
- **Edge-case semantics:** `expect_crash` and `budget_ms` move from manifest to front matter,
  behavior unchanged (crash contract, post-hoc + enforced timeout budgets, `--include-pathological`
  gating keyed on `budget_ms` presence).

## 5. Migration: the ledger

Every one of the 41 existing fixtures gets a row in `docs/conformance-migration-ledger.md`
(retained permanently — it is the provenance bridge) recording: old fixture id, its constructs, its
destination (language + word(s), or `edge-cases/<name>`), and how the bug-triggering configuration
was preserved. Hard rules:

1. **No construct's coverage may regress.** The old suite's construct→fixture map is the floor.
2. **A pin that can't embed faithfully stays a micro-grammar.** Expected residents of `edge-cases/`:
   the three `loader/` XML-semantics probes, `rewrite/simultaneous-epenthesis-cascade`
   (`expect_crash`), `pathological/deep-optional-affix-nesting` (`budget_ms`), and any allomorphy/
   rewrite pin whose exact rule configuration a naturalistic grammar won't contain (candidates:
   `disjunctive-recheck`, `strrep-identity`, `n2-default-symbol`-class cases — the ledger decides
   per fixture, embedding is not forced).
3. **Old `manifest.json.provenance` moves into per-word `provenance:` fields.** Nothing about a
   bug's history is lost, including `rewrite/expand`'s LT-22613 account.
4. The old fixture tree is deleted only when the ledger is complete and §7's gates pass.

## 6. Growth policy (restated for v2)

Unchanged in spirit: **every engine bug fix lands with a conformance addition.** The cheap path is
now even cheaper — one YAML entry (word + note + parses + `provenance`) in the language whose
grammar already covers the territory, verified by self-check. If no language's grammar can express
the trigger, either extend a language grammar (preferred when family-plausible) or add an
`edge-cases/` micro-grammar. New typological territory (the §3 future tier — Nilotic stem-internal
inflection, case stacking, Chukotkan) enters as a whole new `languages/` member with its own
`sources`.

## 7. Phases and acceptance gates

- **G1 — Pilot.** Harness rework (YAML fixture model, temp materialization, CSV, `--propose`) +
  convert one language end-to-end (`suffixing-quechua`, smallest and phonology-free) + two
  edge-case conversions (`loader/n1-isactive`, the `expect_crash` cascade). G1 also pins the
  guessed-stem signature rendering (from `BatchCommand`'s real output, documented in PROTOCOL.md)
  and implements traced `rules` verification. Gate: self-check and adapter mode green on the pilot;
  traced-rules verification demonstrably FAILs on a wrong `rules` list; `--propose` demonstrably
  never writes; broken-adapter and mistagged-`requires` negative tests still FAIL.
- **G2 — Author + migrate.** Write the seven remaining grammars (each: hand-derive expected parses
  for ≥5 words *before* consulting the oracle, then self-check the rest); complete the ledger;
  delete the old tree. Gate: ledger complete with zero coverage regression; all fixtures green in
  both modes.
- **G3 — Docs consolidation.** One `conformance/README.md` (project, the six families, coverage
  philosophy, growth policy — target ≤120 lines); PROTOCOL.md trimmed (contract untouched);
  `conformance-framework-plan.md` + `HISTORY-MATRIX.md` archived under `docs/archive/` (live
  content already moved into ledger/provenance fields); `constructs.txt` header updated.
- **G4 — Verify + PR.** Full run both modes; coverage.csv shows every construct except Tracing
  covered (including the five newly-closed); rules.csv shows zero dead rules in every grammar;
  every language has at least one non-parse word and the suite has at least one guess-stem word;
  file count ≤ ~45, HermitCrab test suite still green;
  update PR #454 in place (still draft) with the restructure story.

Authoring model per John's standing pattern: Sonnet subagents write (G1 harness, G2 grammars),
Fable reviews each gate.

## 8. Non-goals

- No change to the adapter protocol, signature algorithm, or comparison semantics.
- No XAmple execution (still deferred; `suffixing-quechua` preserves *readiness*).
- No claim of documentary accuracy about any real language — synthetic grammars simulate phenomenon
  classes, with sources cited per grammar.
- Not replacing corpus-level parity testing (Sena/Indonesian/Amharic goldens) wherever the Rust
  port lives.
