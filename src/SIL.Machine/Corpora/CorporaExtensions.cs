using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public static class CorporaExtensions
    {
        #region IEnumerable operations

        public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
        {
            var batch = new List<T>();
            foreach (T item in items)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        #endregion

        #region ICorpus operations

        public static (IEnumerable<(T, bool)>, int, int) InterleavedSplit<T>(
            this ICorpus<T> corpus,
            double? percent = null,
            int? size = null,
            bool includeEmpty = true,
            int? seed = null
        )
            where T : IRow
        {
            int corpusSize = corpus.Count(includeEmpty);
            ISet<int> splitIndices = CorporaUtils.GetSplitIndices(corpusSize, percent, size, seed);

            IEnumerable<T> rows = corpus;
            if (!includeEmpty)
                rows = rows.Where(row => !row.IsEmpty);

            var splitCorpus = corpus.Select((row, i) => (row, splitIndices.Contains(i)));
            return (splitCorpus, corpusSize - splitIndices.Count, splitIndices.Count);
        }

        #endregion

        #region ITextCorpus operations

        public static ITextCorpus Tokenize(
            this ITextCorpus corpus,
            ITokenizer<string, int, string> tokenizer,
            bool force = false
        )
        {
            if (!force && corpus.IsTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    row.Segment = tokenizer.Tokenize(row.Text).ToArray();
                    return row;
                },
                isTokenized: true
            );
        }

        public static ITextCorpus Tokenize<T>(this ITextCorpus corpus, bool force = false)
            where T : ITokenizer<string, int, string>, new()
        {
            var tokenizer = new T();
            return corpus.Tokenize(tokenizer, force);
        }

        public static ITextCorpus Detokenize(
            this ITextCorpus corpus,
            IDetokenizer<string, string> detokenizer,
            bool force = false
        )
        {
            if (!force && !corpus.IsTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if (row.Segment.Count > 1)
                        row.Segment = new[] { detokenizer.Detokenize(row.Segment) };
                    return row;
                },
                isTokenized: false
            );
        }

        public static ITextCorpus Detokenize<T>(this ITextCorpus corpus, bool force = false)
            where T : IDetokenizer<string, string>, new()
        {
            var detokenizer = new T();
            return corpus.Detokenize(detokenizer, force);
        }

        public static ITextCorpus Normalize(
            this ITextCorpus corpus,
            NormalizationForm normalizationForm = NormalizationForm.FormC
        )
        {
            return corpus.Transform(row =>
            {
                row.Segment = row.Segment.Normalize(normalizationForm);
                return row;
            });
        }

        public static ITextCorpus NfcNormalize(this ITextCorpus corpus)
        {
            return corpus.Normalize();
        }

        public static ITextCorpus NfdNormalize(this ITextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormD);
        }

        public static ITextCorpus NfkcNormalize(this ITextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormKC);
        }

        public static ITextCorpus NfkdNormalize(this ITextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormKD);
        }

        public static ITextCorpus EscapeSpaces(this ITextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.Segment = row.Segment.EscapeSpaces();
                return row;
            });
        }

        public static ITextCorpus UnescapeSpaces(this ITextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.Segment = row.Segment.UnescapeSpaces();
                return row;
            });
        }

        public static ITextCorpus Lowercase(this ITextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.Segment = row.Segment.Lowercase();
                return row;
            });
        }

        public static ITextCorpus Uppercase(this ITextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.Segment = row.Segment.Uppercase();
                return row;
            });
        }

        public static ITextCorpus Truecase(this ITextCorpus corpus, ITruecaser truecaser)
        {
            return corpus.Transform(row =>
            {
                row.Segment = truecaser.Truecase(row.Segment);
                return row;
            });
        }

        public static ITextCorpus Transform(
            this ITextCorpus corpus,
            IRowProcessor<TextRow> processor,
            bool? isTokenized = null
        )
        {
            return corpus.Transform(processor.Process, isTokenized);
        }

        public static ITextCorpus Transform(
            this ITextCorpus corpus,
            Func<TextRow, TextRow> transform,
            bool? isTokenized = null
        )
        {
            return new TransformTextCorpus(corpus, transform, isTokenized);
        }

        public static ITextCorpus FilterTexts(this ITextCorpus corpus, Func<IText, bool> predicate)
        {
            return new TextFilterTextCorpus(corpus, predicate);
        }

        public static ITextCorpus WhereNonempty(this ITextCorpus corpus)
        {
            return corpus.Where(r => !r.IsEmpty);
        }

        public static ITextCorpus Where(this ITextCorpus corpus, Func<TextRow, bool> predicate)
        {
            return corpus.Where((row, _) => predicate(row));
        }

        public static ITextCorpus Where(this ITextCorpus corpus, Func<TextRow, int, bool> predicate)
        {
            return new WhereTextCorpus(corpus, predicate);
        }

        public static ITextCorpus Take(this ITextCorpus corpus, int count)
        {
            return new TakeTextCorpus(corpus, count);
        }

        public static IParallelTextCorpus AlignRows(
            this ITextCorpus sourceCorpus,
            ITextCorpus targetCorpus,
            IAlignmentCorpus alignmentCorpus = null,
            bool allSourceRows = false,
            bool allTargetRows = false,
            IComparer<object> rowRefComparer = null
        )
        {
            return new ParallelTextCorpus(sourceCorpus, targetCorpus, alignmentCorpus, rowRefComparer)
            {
                AllSourceRows = allSourceRows,
                AllTargetRows = allTargetRows
            };
        }

        public static (ITextCorpus, ITextCorpus, int, int) Split(
            this ITextCorpus corpus,
            double? percent = null,
            int? size = null,
            bool includeEmpty = true,
            int? seed = null
        )
        {
            int corpusSize = corpus.Count(includeEmpty);
            ISet<int> splitIndices = CorporaUtils.GetSplitIndices(corpusSize, percent, size, seed);

            var mainCorpus = corpus.Where((row, i) => !splitIndices.Contains(i) && (includeEmpty || !row.IsEmpty));
            var splitCorpus = corpus.Where((row, i) => splitIndices.Contains(i) && (includeEmpty || !row.IsEmpty));
            return (mainCorpus, splitCorpus, corpusSize - splitIndices.Count, splitIndices.Count);
        }

        public static ITextCorpus Flatten(this IEnumerable<ITextCorpus> corpora)
        {
            ITextCorpus[] corpusArray = corpora.ToArray();
            if (corpusArray.Length == 1)
                return corpusArray[0];
            return new FlattenTextCorpus(corpusArray);
        }

        public static IEnumerable<(string Text, VerseRef RefCorpusVerseRef, VerseRef CorpusVerseRef)> ExtractScripture(
            this ITextCorpus corpus,
            ITextCorpus refCorpus = null
        )
        {
            if (refCorpus == null)
                refCorpus = ScriptureTextCorpus.CreateVersificationRefCorpus();

            IParallelTextCorpus parallelCorpus = refCorpus.AlignRows(corpus, allSourceRows: true);
            VerseRef? curRef = null;
            VerseRef? curTrgRef = null;
            var curTrgLine = new StringBuilder();
            bool curTrgLineRange = true;
            foreach (ParallelTextRow row in parallelCorpus)
            {
                var scriptureRef = (ScriptureRef)row.Ref;
                if (!scriptureRef.IsVerse)
                    continue;

                VerseRef vref = scriptureRef.VerseRef;
                if (
                    curRef.HasValue
                    && vref.CompareTo(curRef.Value, null, compareAllVerses: true, compareSegments: false) != 0
                )
                {
                    yield return (curTrgLineRange ? "<range>" : curTrgLine.ToString(), curRef.Value, curTrgRef.Value);
                    curTrgLineRange = curTrgLineRange || curTrgLine.Length > 0;
                    curTrgLine = new StringBuilder();
                    curTrgRef = null;
                }

                curRef = vref;
                if (!curTrgRef.HasValue && row.TargetRefs.Count > 0)
                {
                    curTrgRef = ((ScriptureRef)row.TargetRefs[0]).VerseRef;
                }
                else if (curTrgRef.HasValue && row.TargetRefs.Count > 0 && !curTrgRef.Value.Equals(row.TargetRefs[0]))
                {
                    curTrgRef.Value.Simplify();
                    VerseRef trgRef = ((ScriptureRef)row.TargetRefs[0]).VerseRef;
                    VerseRef startRef;
                    VerseRef endRef;
                    if (curTrgRef.Value < trgRef)
                    {
                        startRef = curTrgRef.Value;
                        endRef = trgRef;
                    }
                    else
                    {
                        startRef = trgRef;
                        endRef = curTrgRef.Value;
                    }
                    if (startRef.Chapter == endRef.Chapter)
                    {
                        if (startRef.VerseNum != endRef.VerseNum)
                        {
                            curTrgRef = new VerseRef(
                                startRef.Book,
                                startRef.Chapter,
                                $"{startRef.VerseNum}-{endRef.VerseNum}",
                                startRef.Versification
                            );
                        }
                    }
                    else
                    {
                        curTrgRef = endRef;
                    }
                }

                if (!row.IsTargetInRange || row.IsTargetRangeStart || row.TargetText.Length > 0)
                {
                    if (row.TargetText.Length > 0)
                    {
                        if (curTrgLine.Length > 0)
                            curTrgLine.Append(" ");
                        curTrgLine.Append(row.TargetText);
                    }
                    curTrgLineRange = false;
                }
            }

            if (curRef.HasValue)
                yield return (curTrgLineRange ? "<range>" : curTrgLine.ToString(), curRef.Value, curTrgRef.Value);
        }

        public static bool IsScripture(this ITextCorpus textCorpus)
        {
            return textCorpus.Versification != null;
        }

        public static ITextCorpus FilterTexts(this ITextCorpus corpus, IEnumerable<string> textIds)
        {
            if (textIds == null)
                return corpus;
            return new FilterTextsTextCorpus(corpus, textIds);
        }

        private class TransformTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly Func<TextRow, TextRow> _transform;

            public TransformTextCorpus(ITextCorpus corpus, Func<TextRow, TextRow> transform, bool? isTokenized)
            {
                _corpus = corpus;
                _transform = transform;
                IsTokenized = isTokenized ?? corpus.IsTokenized;
            }

            public override IEnumerable<IText> Texts => _corpus.Texts;

            public override bool IsTokenized { get; }

            public override ScrVers Versification => _corpus.Versification;

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds);
            }

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Select(_transform);
            }
        }

        private class WhereTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly Func<TextRow, int, bool> _predicate;

            public WhereTextCorpus(ITextCorpus corpus, Func<TextRow, int, bool> predicate)
            {
                _corpus = corpus;
                _predicate = predicate;
            }

            public override IEnumerable<IText> Texts => _corpus.Texts;

            public override bool IsTokenized => _corpus.IsTokenized;

            public override ScrVers Versification => _corpus.Versification;

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Where(_predicate);
            }
        }

        private class TextFilterTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly Func<IText, bool> _predicate;

            public TextFilterTextCorpus(ITextCorpus corpus, Func<IText, bool> predicate)
            {
                _corpus = corpus;
                _predicate = predicate;
            }

            public override IEnumerable<IText> Texts => _corpus.Texts.Where(_predicate);

            public override bool IsTokenized => _corpus.IsTokenized;

            public override ScrVers Versification => _corpus.Versification;

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds ?? Texts.Select(t => t.Id));
            }
        }

        private class TakeTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly int _count;

            public TakeTextCorpus(ITextCorpus corpus, int count)
            {
                _corpus = corpus;
                _count = count;
            }

            public override IEnumerable<IText> Texts => _corpus.Texts;

            public override bool IsTokenized => _corpus.IsTokenized;

            public override ScrVers Versification => _corpus.Versification;

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Take(_count);
            }
        }

        private class FlattenTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus[] _corpora;

            public FlattenTextCorpus(ITextCorpus[] corpora)
            {
                _corpora = corpora;
            }

            public override IEnumerable<IText> Texts => _corpora.SelectMany(corpus => corpus.Texts);

            public override bool IsTokenized => _corpora.All(corpus => corpus.IsTokenized);

            public override ScrVers Versification => _corpora.Length > 0 ? _corpora[0].Versification : null;

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty, textIds));
            }

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                // TODO: it is possible that rows will be returned out-of-order. This could cause issues when aligning
                // rows to create a parallel corpus.
                return _corpora.SelectMany(corpus => corpus.GetRows(textIds));
            }
        }

        private class FilterTextsTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly HashSet<string> _textIds;

            public FilterTextsTextCorpus(ITextCorpus corpus, IEnumerable<string> textIds)
            {
                _corpus = corpus;
                _textIds = new HashSet<string>(textIds);
            }

            public override IEnumerable<IText> Texts => _corpus.Texts.Where(t => _textIds.Contains(t.Id));

            public override bool IsTokenized => _corpus.IsTokenized;

            public override ScrVers Versification => _corpus.Versification;

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds == null ? _textIds : _textIds.Intersect(textIds));
            }

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds == null ? _textIds : _textIds.Intersect(textIds));
            }
        }

        #endregion

        #region INParallelTextCorpus operations

        public static INParallelTextCorpus AlignMany(
            this IEnumerable<ITextCorpus> corpora,
            IEnumerable<bool> allRowsPerCorpus = null
        )
        {
            NParallelTextCorpus nParallelTextCorpus = new NParallelTextCorpus(corpora);
            if (allRowsPerCorpus != null)
            {
                nParallelTextCorpus.AllRows = allRowsPerCorpus.ToArray();
            }
            return nParallelTextCorpus;
        }

        public static ITextCorpus ChooseRandom(this IEnumerable<ITextCorpus> corpora, int seed)
        {
            return new MergedTextCorpus(corpora, MergeRule.Random, seed);
        }

        public static ITextCorpus ChooseFirst(this IEnumerable<ITextCorpus> corpora)
        {
            return new MergedTextCorpus(corpora, MergeRule.First);
        }

        #endregion

        #region IAlignmentCorpus operations

        public static IAlignmentCorpus Transform(
            this IAlignmentCorpus corpus,
            Func<AlignmentRow, AlignmentRow> transform
        )
        {
            return new TransformAlignmentCorpus(corpus, transform);
        }

        public static IAlignmentCorpus FilterAlignmentCollections(
            this IAlignmentCorpus corpus,
            Func<IAlignmentCollection, bool> predicate
        )
        {
            return new AlignmentCollectionFilterAlignmentCorpus(corpus, predicate);
        }

        public static IAlignmentCorpus WhereNonempty(this IAlignmentCorpus corpus)
        {
            return corpus.Where(r => !r.IsEmpty);
        }

        public static IAlignmentCorpus Where(this IAlignmentCorpus corpus, Func<AlignmentRow, bool> predicate)
        {
            return corpus.Where((row, _) => predicate(row));
        }

        public static IAlignmentCorpus Where(this IAlignmentCorpus corpus, Func<AlignmentRow, int, bool> predicate)
        {
            return new WhereAlignmentCorpus(corpus, predicate);
        }

        public static IAlignmentCorpus Take(this IAlignmentCorpus corpus, int count)
        {
            return new TakeAlignmentCorpus(corpus, count);
        }

        public static IAlignmentCorpus Flatten(this IEnumerable<IAlignmentCorpus> corpora)
        {
            IAlignmentCorpus[] corpusArray = corpora.ToArray();
            if (corpusArray.Length == 1)
                return corpusArray[0];
            return new FlattenAlignmentCorpus(corpusArray);
        }

        public static IAlignmentCorpus FilterTexts(this IAlignmentCorpus corpus, IEnumerable<string> textIds)
        {
            if (textIds == null)
                return corpus;
            return new FilterTextsAlignmentCorpus(corpus, textIds);
        }

        private class TransformAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus _corpus;
            private readonly Func<AlignmentRow, AlignmentRow> _transform;

            public TransformAlignmentCorpus(IAlignmentCorpus corpus, Func<AlignmentRow, AlignmentRow> transform)
            {
                _corpus = corpus;
                _transform = transform;
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections => _corpus.AlignmentCollections;

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds);
            }

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Select(_transform);
            }
        }

        private class WhereAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus _corpus;
            private readonly Func<AlignmentRow, int, bool> _predicate;

            public WhereAlignmentCorpus(IAlignmentCorpus corpus, Func<AlignmentRow, int, bool> predicate)
            {
                _corpus = corpus;
                _predicate = predicate;
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections => _corpus.AlignmentCollections;

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Where(_predicate);
            }
        }

        private class AlignmentCollectionFilterAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus _corpus;
            private readonly Func<IAlignmentCollection, bool> _predicate;

            public AlignmentCollectionFilterAlignmentCorpus(
                IAlignmentCorpus corpus,
                Func<IAlignmentCollection, bool> predicate
            )
            {
                _corpus = corpus;
                _predicate = predicate;
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections =>
                _corpus.AlignmentCollections.Where(_predicate);

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds ?? AlignmentCollections.Select(t => t.Id));
            }
        }

        private class TakeAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus _corpus;
            private readonly int _count;

            public TakeAlignmentCorpus(IAlignmentCorpus corpus, int count)
            {
                _corpus = corpus;
                _count = count;
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections => _corpus.AlignmentCollections;

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return GetRows(textIds).Take(_count);
            }
        }

        private class FlattenAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus[] _corpora;

            public FlattenAlignmentCorpus(IAlignmentCorpus[] corpora)
            {
                _corpora = corpora;
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections =>
                _corpora.SelectMany(corpus => corpus.AlignmentCollections);

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty, textIds));
            }

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpora.SelectMany(corpus => corpus.GetRows(textIds));
            }
        }

        private class FilterTextsAlignmentCorpus : AlignmentCorpusBase
        {
            private readonly IAlignmentCorpus _corpus;
            private readonly HashSet<string> _textIds;

            public FilterTextsAlignmentCorpus(IAlignmentCorpus corpus, IEnumerable<string> textIds)
            {
                _corpus = corpus;
                _textIds = new HashSet<string>(textIds);
            }

            public override IEnumerable<IAlignmentCollection> AlignmentCollections =>
                _corpus.AlignmentCollections.Where(t => _textIds.Contains(t.Id));

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds == null ? _textIds : _textIds.Intersect(textIds));
            }

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds == null ? _textIds : _textIds.Intersect(textIds));
            }
        }

        #endregion

        #region IParallelTextCorpus operations

        public static IParallelTextCorpus Tokenize(
            this IParallelTextCorpus corpus,
            ITokenizer<string, int, string> tokenizer,
            bool force = false
        )
        {
            return corpus.Tokenize(tokenizer, tokenizer, force);
        }

        public static IParallelTextCorpus Tokenize<T>(this IParallelTextCorpus corpus)
            where T : ITokenizer<string, int, string>, new()
        {
            var tokenizer = new T();
            return corpus.Tokenize(tokenizer);
        }

        public static IParallelTextCorpus Tokenize(
            this IParallelTextCorpus corpus,
            ITokenizer<string, int, string> sourceTokenizer,
            ITokenizer<string, int, string> targetTokenizer,
            bool force = false
        )
        {
            if (!force && corpus.IsSourceTokenized && corpus.IsTargetTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if ((force || !corpus.IsSourceTokenized) && row.SourceSegment.Count > 0)
                        row.SourceSegment = sourceTokenizer.Tokenize(row.SourceText).ToArray();
                    if ((force || !corpus.IsTargetTokenized) && row.TargetSegment.Count > 0)
                        row.TargetSegment = targetTokenizer.Tokenize(row.TargetText).ToArray();
                    return row;
                },
                isSourceTokenized: true,
                isTargetTokenized: true
            );
        }

        public static IParallelTextCorpus Tokenize<TSource, TTarget>(
            this IParallelTextCorpus corpus,
            bool force = false
        )
            where TSource : ITokenizer<string, int, string>, new()
            where TTarget : ITokenizer<string, int, string>, new()
        {
            var sourceTokenizer = new TSource();
            var targetTokenizer = new TTarget();
            return corpus.Tokenize(sourceTokenizer, targetTokenizer, force);
        }

        public static IParallelTextCorpus TokenizeSource(
            this IParallelTextCorpus corpus,
            ITokenizer<string, int, string> tokenizer,
            bool force = false
        )
        {
            if (!force && corpus.IsSourceTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if (row.SourceSegment.Count > 0)
                        row.SourceSegment = tokenizer.Tokenize(row.SourceText).ToArray();
                    return row;
                },
                isSourceTokenized: true
            );
        }

        public static IParallelTextCorpus TokenizeSource<T>(this IParallelTextCorpus corpus, bool force = false)
            where T : ITokenizer<string, int, string>, new()
        {
            var tokenizer = new T();
            return corpus.TokenizeSource(tokenizer, force);
        }

        public static IParallelTextCorpus TokenizeTarget(
            this IParallelTextCorpus corpus,
            ITokenizer<string, int, string> tokenizer,
            bool force = false
        )
        {
            if (!force && corpus.IsTargetTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if (row.TargetSegment.Count > 0)
                        row.TargetSegment = tokenizer.Tokenize(row.TargetText).ToArray();
                    return row;
                },
                isTargetTokenized: true
            );
        }

        public static IParallelTextCorpus TokenizeTarget<T>(this IParallelTextCorpus corpus, bool force = false)
            where T : ITokenizer<string, int, string>, new()
        {
            var tokenizer = new T();
            return corpus.TokenizeTarget(tokenizer, force);
        }

        public static IParallelTextCorpus Detokenize(
            this IParallelTextCorpus corpus,
            IDetokenizer<string, string> detokenizer,
            bool force = false
        )
        {
            return corpus.Detokenize(detokenizer, detokenizer, force);
        }

        public static IParallelTextCorpus Detokenize<T>(this IParallelTextCorpus corpus, bool force = false)
            where T : IDetokenizer<string, string>, new()
        {
            var detokenizer = new T();
            return corpus.Detokenize(detokenizer, force);
        }

        public static IParallelTextCorpus Detokenize(
            this IParallelTextCorpus corpus,
            IDetokenizer<string, string> sourceDetokenizer,
            IDetokenizer<string, string> targetDetokenizer,
            bool force = false
        )
        {
            if (!force && !corpus.IsSourceTokenized && !corpus.IsTargetTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if ((force || corpus.IsSourceTokenized) && row.SourceSegment.Count > 1)
                        row.SourceSegment = new[] { sourceDetokenizer.Detokenize(row.SourceSegment) };
                    if ((force || corpus.IsTargetTokenized) && row.TargetSegment.Count > 1)
                        row.TargetSegment = new[] { targetDetokenizer.Detokenize(row.TargetSegment) };
                    return row;
                },
                isSourceTokenized: false,
                isTargetTokenized: false
            );
        }

        public static IParallelTextCorpus Detokenize<TSource, TTarget>(
            this IParallelTextCorpus corpus,
            bool force = false
        )
            where TSource : IDetokenizer<string, string>, new()
            where TTarget : IDetokenizer<string, string>, new()
        {
            var sourceDetokenizer = new TSource();
            var targetDetokenizer = new TTarget();
            return corpus.Detokenize(sourceDetokenizer, targetDetokenizer, force);
        }

        public static IParallelTextCorpus DetokenizeSource(
            this IParallelTextCorpus corpus,
            IDetokenizer<string, string> detokenizer,
            bool force = false
        )
        {
            if (!force && !corpus.IsSourceTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if (row.SourceSegment.Count > 1)
                        row.SourceSegment = new[] { detokenizer.Detokenize(row.SourceSegment) };
                    return row;
                },
                isSourceTokenized: false
            );
        }

        public static IParallelTextCorpus DetokenizeSource<T>(this IParallelTextCorpus corpus, bool force = false)
            where T : IDetokenizer<string, string>, new()
        {
            var detokenizer = new T();
            return corpus.DetokenizeSource(detokenizer, force);
        }

        public static IParallelTextCorpus DetokenizeTarget(
            this IParallelTextCorpus corpus,
            IDetokenizer<string, string> detokenizer,
            bool force = false
        )
        {
            if (!force && !corpus.IsTargetTokenized)
                return corpus;

            return corpus.Transform(
                row =>
                {
                    if (row.TargetSegment.Count > 1)
                        row.TargetSegment = new[] { detokenizer.Detokenize(row.TargetSegment) };
                    return row;
                },
                isTargetTokenized: false
            );
        }

        public static IParallelTextCorpus DetokenizeTarget<T>(this IParallelTextCorpus corpus, bool force = false)
            where T : IDetokenizer<string, string>, new()
        {
            var detokenizer = new T();
            return corpus.DetokenizeTarget(detokenizer, force);
        }

        public static IParallelTextCorpus Invert(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row => row.Invert());
        }

        public static IParallelTextCorpus Normalize(
            this IParallelTextCorpus corpus,
            NormalizationForm normalizationForm = NormalizationForm.FormC
        )
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.Normalize(normalizationForm);
                row.TargetSegment = row.TargetSegment.Normalize(normalizationForm);
                return row;
            });
        }

        public static IParallelTextCorpus NfcNormalize(this IParallelTextCorpus corpus)
        {
            return corpus.Normalize();
        }

        public static IParallelTextCorpus NfdNormalize(this IParallelTextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormD);
        }

        public static IParallelTextCorpus NfkcNormalize(this IParallelTextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormKC);
        }

        public static IParallelTextCorpus NfkdNormalize(this IParallelTextCorpus corpus)
        {
            return corpus.Normalize(NormalizationForm.FormKD);
        }

        public static IParallelTextCorpus EscapeSpaces(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.EscapeSpaces();
                row.TargetSegment = row.TargetSegment.EscapeSpaces();
                return row;
            });
        }

        public static IParallelTextCorpus UnescapeSpaces(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.UnescapeSpaces();
                row.TargetSegment = row.TargetSegment.UnescapeSpaces();
                return row;
            });
        }

        public static IParallelTextCorpus Lowercase(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.Lowercase();
                row.TargetSegment = row.TargetSegment.Lowercase();
                return row;
            });
        }

        public static IParallelTextCorpus LowercaseSource(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.Lowercase();
                return row;
            });
        }

        public static IParallelTextCorpus LowercaseTarget(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.TargetSegment = row.TargetSegment.Lowercase();
                return row;
            });
        }

        public static IParallelTextCorpus Uppercase(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(row =>
            {
                row.SourceSegment = row.SourceSegment.Uppercase();
                row.TargetSegment = row.TargetSegment.Uppercase();
                return row;
            });
        }

        public static IParallelTextCorpus Transform(
            this IParallelTextCorpus corpus,
            IRowProcessor<ParallelTextRow> processor,
            bool? isSourceTokenized = null,
            bool? isTargetTokenized = null
        )
        {
            return corpus.Transform(processor.Process, isSourceTokenized, isTargetTokenized);
        }

        public static IParallelTextCorpus Transform(
            this IParallelTextCorpus corpus,
            Func<ParallelTextRow, ParallelTextRow> transform,
            bool? isSourceTokenized = null,
            bool? isTargetTokenized = null
        )
        {
            return new TransformParallelTextCorpus(corpus, transform, isSourceTokenized, isTargetTokenized);
        }

        public static IParallelTextCorpus WhereNonempty(this IParallelTextCorpus corpus)
        {
            return corpus.Where(r => !r.IsEmpty);
        }

        public static IParallelTextCorpus Where(this IParallelTextCorpus corpus, Func<ParallelTextRow, bool> predicate)
        {
            return corpus.Where((row, _) => predicate(row));
        }

        public static IParallelTextCorpus Where(
            this IParallelTextCorpus corpus,
            Func<ParallelTextRow, int, bool> predicate
        )
        {
            return new WhereParallelTextCorpus(corpus, predicate);
        }

        public static IParallelTextCorpus Take(this IParallelTextCorpus corpus, int count)
        {
            return new TakeParallelTextCorpus(corpus, count);
        }

        public static (IParallelTextCorpus, IParallelTextCorpus, int, int) Split(
            this IParallelTextCorpus corpus,
            double? percent = null,
            int? size = null,
            bool includeEmpty = true,
            int? seed = null
        )
        {
            int corpusSize = corpus.Count(includeEmpty);
            ISet<int> splitIndices = CorporaUtils.GetSplitIndices(corpusSize, percent, size, seed);

            var mainCorpus = corpus.Where((row, i) => !splitIndices.Contains(i) && (includeEmpty || !row.IsEmpty));
            var splitCorpus = corpus.Where((row, i) => splitIndices.Contains(i) && (includeEmpty || !row.IsEmpty));
            return (mainCorpus, splitCorpus, corpusSize - splitIndices.Count, splitIndices.Count);
        }

        public static IParallelTextCorpus Flatten(this IEnumerable<IParallelTextCorpus> corpora)
        {
            IParallelTextCorpus[] corpusArray = corpora.ToArray();
            if (corpusArray.Length == 1)
                return corpusArray[0];
            return new FlattenParallelTextCorpus(corpusArray);
        }

        public static IParallelTextCorpus Translate(
            this IParallelTextCorpus corpus,
            ITranslationEngine translationEngine,
            int batchSize = 1024
        )
        {
            return new TranslateParallelTextCorpus(corpus, translationEngine, batchSize);
        }

        public static IParallelTextCorpus WordAlign(
            this IParallelTextCorpus corpus,
            IWordAligner aligner,
            int batchSize = 1024
        )
        {
            return new WordAlignParallelTextCorpus(corpus, aligner, batchSize);
        }

        public static IParallelTextCorpus FilterTexts(this IParallelTextCorpus corpus, IEnumerable<string> textIds)
        {
            if (textIds == null)
                return corpus;
            return new FilterTextsParallelTextCorpus(corpus, textIds);
        }

        private class TransformParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly Func<ParallelTextRow, ParallelTextRow> _transform;

            public TransformParallelTextCorpus(
                IParallelTextCorpus corpus,
                Func<ParallelTextRow, ParallelTextRow> transform,
                bool? isSourceTokenized,
                bool? isTargetTokenized
            )
            {
                _corpus = corpus;
                _transform = transform;
                IsSourceTokenized = isSourceTokenized ?? corpus.IsSourceTokenized;
                IsTargetTokenized = isTargetTokenized ?? corpus.IsTargetTokenized;
            }

            public override bool IsSourceTokenized { get; }

            public override bool IsTargetTokenized { get; }

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds);
            }

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Select(_transform);
            }
        }

        private class WhereParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly Func<ParallelTextRow, int, bool> _predicate;

            public WhereParallelTextCorpus(IParallelTextCorpus corpus, Func<ParallelTextRow, int, bool> predicate)
            {
                _corpus = corpus;
                _predicate = predicate;
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;
            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Where(_predicate);
            }
        }

        private class TakeParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly int _count;

            public TakeParallelTextCorpus(IParallelTextCorpus corpus, int count)
            {
                _corpus = corpus;
                _count = count;
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;
            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds).Take(_count);
            }
        }

        private class FlattenParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus[] _corpora;

            public FlattenParallelTextCorpus(IParallelTextCorpus[] corpora)
            {
                _corpora = corpora;
            }

            public override bool IsSourceTokenized => _corpora.All(corpus => corpus.IsSourceTokenized);
            public override bool IsTargetTokenized => _corpora.All(corpus => corpus.IsTargetTokenized);

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty, textIds));
            }

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpora.SelectMany(corpus => corpus.GetRows(textIds));
            }
        }

        private class TranslateParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly ITranslationEngine _translationEngine;
            private readonly int _batchSize;

            public TranslateParallelTextCorpus(
                IParallelTextCorpus corpus,
                ITranslationEngine translationEngine,
                int batchSize
            )
            {
                _corpus = corpus;
                _translationEngine = translationEngine;
                _batchSize = batchSize;
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;
            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                foreach (IReadOnlyList<ParallelTextRow> batch in _corpus.GetRows(textIds).Batch(_batchSize))
                {
                    IReadOnlyList<TranslationResult> translations;
                    if (IsSourceTokenized)
                        translations = _translationEngine.TranslateBatch(batch.Select(r => r.SourceSegment).ToArray());
                    else
                        translations = _translationEngine.TranslateBatch(batch.Select(r => r.SourceText).ToArray());
                    foreach (var (row, translation) in batch.Zip(translations, (r, t) => (r, t)))
                    {
                        row.TargetSegment = translation.TargetTokens;
                        yield return row;
                    }
                }
            }
        }

        private class WordAlignParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly IWordAligner _aligner;
            private readonly int _batchSize;

            public WordAlignParallelTextCorpus(IParallelTextCorpus corpus, IWordAligner aligner, int batchSize)
            {
                _corpus = corpus;
                _aligner = aligner;
                _batchSize = batchSize;
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;
            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                foreach (IReadOnlyList<ParallelTextRow> batch in _corpus.GetRows(textIds).Batch(_batchSize))
                {
                    IReadOnlyList<WordAlignmentMatrix> alignments = _aligner.AlignBatch(
                        batch.Select(r => (r.SourceSegment, r.TargetSegment)).ToArray()
                    );
                    foreach (var (row, alignment) in batch.Zip(alignments, (r, a) => (r, a)))
                    {
                        WordAlignmentMatrix knownAlignment = row.CreateAlignmentMatrix();
                        IReadOnlyCollection<AlignedWordPair> wordPairs;
                        if (knownAlignment != null)
                        {
                            knownAlignment.PrioritySymmetrizeWith(alignment);
                            wordPairs = knownAlignment.ToAlignedWordPairs();
                        }
                        else
                        {
                            wordPairs = alignment.ToAlignedWordPairs();
                        }
                        if (_aligner is IWordAlignmentModel model)
                            model.ComputeAlignedWordPairScores(row.SourceSegment, row.TargetSegment, wordPairs);
                        row.AlignedWordPairs = wordPairs;
                        yield return row;
                    }
                }
            }
        }

        private class FilterTextsParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly HashSet<string> _textIds;

            public FilterTextsParallelTextCorpus(IParallelTextCorpus corpus, IEnumerable<string> textIds)
            {
                _corpus = corpus;
                _textIds = new HashSet<string>(textIds);
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;
            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
            {
                return _corpus.Count(includeEmpty, textIds == null ? _textIds : _textIds.Intersect(textIds));
            }

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                return _corpus.GetRows(textIds == null ? _textIds : _textIds.Intersect(textIds));
            }
        }

        #endregion

        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb.Length == 0)
                return sb;

            int i = sb.Length - 1;
            for (; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(sb[i]))
                    break;
            }

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }
    }
}
