# machine shared domain context

## Scope and non-goals

This file defines the shared language for machine's corpora, tokenization,
alignment, translation, Scripture references, and morphology code. It is a
terminology and relationship layer, not an architecture manual, an API reference,
or a release checklist. Operational rules belong in `AGENTS.md`.

Prefer terms that a type under `src/` actually represents. If a term has no
current source or test anchor, label it external, historical, or proposed rather
than presenting it as current implementation.

## Product scope

machine is a natural-language-processing library, aimed in part at resource-poor
languages. The repository provides corpus abstractions, tokenizers and
detokenizers, Scripture-aware text corpora, word alignment and translation
abstractions, Thot SMT, TensorFlow SavedModel translation, and HermitCrab
morphology. `README.md` also documents the statistical methods, NuGet packages,
command-line tools, and tutorial notebooks.

## Corpora and rows

`ICorpus<T>` is an enumerable corpus of rows where `T` implements `IRow`
(`src/SIL.Machine/Corpora/ICorpus.cs`). `Count` can include or exclude empty rows.

`IText` is a corpus-backed text with an `Id` and a `SortKey`
(`src/SIL.Machine/Corpora/IText.cs`). `ITextCorpus` adds a collection of texts, an
`IsTokenized` state, Scripture versification, and row access by text id
(`src/SIL.Machine/Corpora/ITextCorpus.cs`).

`IParallelTextCorpus` pairs a source and target side with separate tokenization
states (`src/SIL.Machine/Corpora/IParallelTextCorpus.cs`). `IAlignmentCorpus`
provides alignment rows (`src/SIL.Machine/Corpora/IAlignmentCorpus.cs`).

`TextRow` carries a text id, a Scripture reference, a content type, flags, and a
segment held as `IReadOnlyList<string>` (`src/SIL.Machine/Corpora/TextRow.cs`).
Its `Text` property joins the segment tokens with spaces. A row is empty when its
segment has no tokens.

`ParallelTextRow` holds source and target segments, references, flags, and
aligned word pairs (`src/SIL.Machine/Corpora/ParallelTextRow.cs`). It is empty if
either side is empty; `Invert` swaps the sides and the alignment.
`NParallelTextRow` generalizes this to several parallel segments
(`src/SIL.Machine/Corpora/NParallelTextRow.cs`). `AlignmentRow` stores aligned
word pairs (`src/SIL.Machine/Corpora/AlignmentRow.cs`).

A corpus is not automatically a list of tokens. Its rows may be tokenized,
untokenized, empty, parallel, alignment-bearing, or Scripture-aware. Always say
which representation a method expects.

## Tokenization and detokenization

`ITokenizer<TData,TToken>` tokenizes whole data or a range
(`src/SIL.Machine/Tokenization/ITokenizer.cs`); `IDetokenizer<TToken>` rebuilds
data from tokens (`src/SIL.Machine/Tokenization/IDetokenizer.cs`).

`StringTokenizer` is the base for string tokenizers. `WhitespaceTokenizer`
handles whitespace, zero-width space, and byte-order-mark boundaries.
`LatinWordTokenizer` adds URL, punctuation, inner-punctuation, abbreviation, and
apostrophe rules. `StringDetokenizer` defines no-op and merge-left, merge-right,
and merge-both behaviors with a configurable separator.

Tokenization and detokenization are not guaranteed inverses for every rule set.
Preserve the tokenizer and detokenizer pair that an engine was configured with.

USFM is the Scripture text format the corpus code handles. `UsfmToken` has token
types for book, chapter, verse, text, paragraph, character, note, end, milestone,
attribute, and unknown, plus marker, text, data, and source position
(`src/SIL.Machine/Corpora/UsfmToken.cs`). `UsfmTag` carries text type, style, and
property information. `UsfmTokenizer` uses a stylesheet and right-to-left order
and can preserve whitespace. `UsfmFileTextCorpus` reads `.SFM` files and
`UsxFileTextCorpus` reads `.usx` files.

## Scripture references and versification

`ScriptureTextCorpus` and `ScriptureText` carry a `ScrVers` versification and
build rows from Scripture references and ranges.

`ScriptureRef` is a verse reference with a nested path
(`src/SIL.Machine/Corpora/ScriptureRef.cs`). `ScriptureRangeParser`
(`src/SIL.Machine/Scripture/ScriptureRangeParser.cs`) parses
chapters, verses, and ranges, defaulting to `ScrVers.Original` unless another
versification is supplied.

"Reference" in corpus code means a Scripture location or row location, not object
identity. "Versification" is the numbering system used to interpret references.

## Translation engines, models, and training

`ITranslationEngine` translates strings or token lists, synchronously and
asynchronously, supports batches, and may return n-best results
(`src/SIL.Machine/Translation/ITranslationEngine.cs`). A segment here is the unit
submitted to an engine: a string or an ordered token list, depending on the
overload.

`ITranslationModel` extends the engine abstraction and creates a trainer from an
`IParallelTextCorpus`. `ITrainer` trains, saves, and reports statistics.

A model is learned or saved state. An engine is the object that performs
translation. A trainer creates or updates model state. A model may expose an
engine-like interface, but model and engine are not synonyms when discussing
lifecycle or persistence.

`ThotSmtModel` is the Thot-backed statistical model. It owns direct, inverse, and
symmetrized word alignment models and exposes tokenizer and detokenizer
configuration. Thot's documented alignment methods are IBM 1-4, HMM, and
FastAlign. `SavedModelNmtEngine` loads a TensorFlow SavedModel using its
configured signature keys and defaults to whitespace tokenization.

HuggingFace is not a current in-repo engine or adapter; treat it as an external
or future term only.

## Word alignment

`IWordAligner` aligns one token pair or a batch and returns a
`WordAlignmentMatrix`. `IWordAlignmentMethod` supplies the score-selection
policy. `IWordAlignmentModel` exposes vocabularies, training, scores, and best
aligned pairs.

`ITransductiveWordAlignmentModel` exposes the training alignment count and
retrieval of a training alignment by index. Transductive here means access to the
alignments of the training examples, not a general claim about a learning
technique.

`WordAlignmentMatrix` is a boolean matrix whose rows are source word positions
and columns are target word positions
(`src/SIL.Machine/Translation/WordAlignmentMatrix.cs`). It supports union,
intersection, priority symmetrization, and conversion to aligned word pairs. An
aligned word pair is a relation between positions, not a dictionary entry.

## Morphology

The neutral API is `IMorpheme`, `WordAnalysis`, `IMorphologicalAnalyzer`, and
`IMorphologicalGenerator` under `src/SIL.Machine/Morphology/`. `IMorpheme`
describes a stem or affix. `WordAnalysis` is an ordered morpheme analysis with a
root index and category; it is a public value, not HermitCrab's internal `Word`.

HermitCrab's integration object is `Language`, which owns strata, feature
systems, lexicon and rule configuration, and analysis and synthesis compilation
(`src/SIL.Machine.Morphology.HermitCrab/Language.cs`). "Grammar" is acceptable as
an umbrella term for the configured morphology system; there is no `Grammar.cs`
type in this tree.

`Stratum` holds a character definition table, morphological rules, and a lexicon
for one stage of the pipeline. A stratum is a pipeline stage, not a data layer.

`Allomorph` is a conditioned realization of a morpheme; `RootAllomorph` is the
lexical-root specialization. `Segments` stores a representation through a
`CharacterDefinitionTable` and can expose a frozen `Shape`. Shape here is a
phonological form, not geometry. HermitCrab `Word` is internal morphology state
holding allomorphs, root, shape, rules, features, range, and stratum.
`Morpher.AnalyzeWord` produces `WordAnalysis`; `Morpher.GenerateWords` produces
surface forms.

Analysis means decomposing a word into morphemes and features. Synthesis means
generating surface forms from morphemes and features. Do not say "analysis" for a
word alignment without naming the domain.

## Architecture language

A source project is a `.csproj` under `src/`, not every namespace or directory. A
test project is a current `.csproj` under `tests/` with a solution or CI entry; a
`bin` or `obj` directory is not evidence of a project.

The `netstandard2.0` libraries are the reusable package boundary. `net10.0`
projects host tools, plugin behavior, and tests.

SentencePiece4c is a native build and runtime boundary beneath the managed
SentencePiece project. It is the only such boundary; most of the repository is
platform-independent managed code.

machine.py is a sibling repository coordinated through post-merge porting issues.
Serval is an external consumer. Neither is a verified in-repo dependency.

## Disambiguation rules

- **Corpus**: a row-producing collection; not a file, a tokenizer, or a model.
- **Row**: one corpus or alignment record; not a token.
- **Segment**: an ordered token sequence for a row, or the unit submitted to a
  translation engine. In morphology, say phonological segment.
- **Token**: tokenizer output or a structured USFM token; not necessarily a
  whitespace-delimited word.
- **Word**: qualify as corpus word position, `WordAnalysis`, or HermitCrab
  `Word`. These are three different abstractions.
- **Model**: learned or saved translation or alignment state.
- **Engine**: the object that performs translation; name the backend when it
  matters.
- **Trainer**: the object that trains and saves model state.
- **Alignment**: a relation between source and target positions; not a
  translation.
- **Analysis**: morphological decomposition, unless another domain is named.
- **Grammar**: the HermitCrab configuration as a whole. Prefer `Language`,
  `Stratum`, rules, lexicon, and features in code discussion.
- **Shape**: a HermitCrab phonological form.
- **Stratum**: one HermitCrab pipeline stage.
- **Reference**: a `ScriptureRef` or row location in corpus context.
- **Versification**: the Scripture numbering system.
- **SMT**: statistical machine translation, currently `ThotSmtModel`.

## Maintenance

When you add a term, anchor it to a current source or test path. When a type or
workflow changes, update the smallest relevant section. Do not copy command lists
from `AGENTS.md` into this file. Record uncertainty rather than promoting an
unverified relationship into architecture.
