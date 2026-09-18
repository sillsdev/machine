# machine shared domain context

The words this codebase overloads, and what each one means here. Read the code
for structure; read this to avoid using a term for the wrong thing. Operational
rules live in `AGENTS.md`.

Anchor any term you add to a current path under `src/`. If it has no anchor,
label it external, historical, or proposed.

## The ambiguous ones

| Term | Here it means | Anchor |
| --- | --- | --- |
| Corpus | A collection that produces rows - not a file, tokenizer, or model | `Corpora/ICorpus.cs` |
| Row | One corpus or alignment record, not a token | `Corpora/TextRow.cs` |
| Segment | A row's tokens, or the unit given to an engine; in morphology, phonological | `Corpora/TextRow.cs` |
| Token | Tokenizer output or a USFM token; not always a whitespace word | `Corpora/UsfmToken.cs` |
| Word | Say which: corpus word position, `WordAnalysis`, or HermitCrab `Word` | `Morphology/WordAnalysis.cs` |
| Reference | A Scripture or row location, never object identity | `Corpora/ScriptureRef.cs` |
| Versification | The numbering system a reference is read in | `Scripture/ScriptureRangeParser.cs` |
| Model | Learned or saved translation or alignment state | `Translation/ITranslationModel.cs` |
| Engine | The object that translates; name the backend when it matters | `Translation/ITranslationEngine.cs` |
| Trainer | The object that trains and saves model state | `Translation/ITrainer.cs` |
| Alignment | A relation between source and target positions, not a translation | `Translation/WordAlignmentMatrix.cs` |
| Analysis | Morphological decomposition, unless you name another domain | `Morphology/IMorphologicalAnalyzer.cs` |
| Synthesis | Generating surface forms from morphemes and features | `HermitCrab/Morpher.cs` |
| Grammar | The HermitCrab configuration as a whole; no `Grammar` type exists | `HermitCrab/Language.cs` |
| Stratum | One stage of the HermitCrab pipeline, not a data layer | `HermitCrab/Stratum.cs` |
| Shape | A HermitCrab phonological form, not geometry | `HermitCrab/Segments.cs` |
| Allomorph | A conditioned realization of a morpheme | `HermitCrab/Allomorph.cs` |
| SMT | Statistical machine translation, currently `ThotSmtModel` | `SIL.Machine.Translation.Thot/ThotSmtModel.cs` |

## Two traps

A corpus is not automatically a list of tokens. Its rows may be tokenized,
untokenized, empty, parallel, alignment-bearing, or Scripture-aware; say which
representation a method expects.

Tokenization and detokenization are not guaranteed inverses. Keep the tokenizer
and detokenizer pair an engine was configured with.
