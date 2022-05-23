using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
    public static class CorporaExtensions
    {
        #region ICorpus operations

        public static (IEnumerable<(T, bool)>, int, int) InterleavedSplit<T>(
            this ICorpus<T> corpus,
            double? percent = null,
            int? size = null,
            bool includeEmpty = true,
            int? seed = null
        ) where T : IRow
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

        public static ITextCorpus Tokenize(this ITextCorpus corpus, ITokenizer<string, int, string> tokenizer)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = tokenizer.Tokenize(row.Text).ToArray();
                    return row;
                }
            );
        }

        public static ITextCorpus Tokenize<T>(this ITextCorpus corpus) where T : ITokenizer<string, int, string>, new()
        {
            var tokenizer = new T();
            return corpus.Tokenize(tokenizer);
        }

        public static ITextCorpus Detokenize(this ITextCorpus corpus, IDetokenizer<string, string> detokenizer)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = new[] { detokenizer.Detokenize(row.Segment) };
                    return row;
                }
            );
        }

        public static ITextCorpus Detokenize<T>(this ITextCorpus corpus) where T : IDetokenizer<string, string>, new()
        {
            var detokenizer = new T();
            return corpus.Detokenize(detokenizer);
        }

        public static ITextCorpus Normalize(
            this ITextCorpus corpus,
            NormalizationForm normalizationForm = NormalizationForm.FormC
        )
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = row.Segment.Normalize(normalizationForm);
                    return row;
                }
            );
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
            return corpus.Transform(
                row =>
                {
                    row.Segment = row.Segment.EscapeSpaces();
                    return row;
                }
            );
        }

        public static ITextCorpus UnescapeSpaces(this ITextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = row.Segment.UnescapeSpaces();
                    return row;
                }
            );
        }

        public static ITextCorpus Lowercase(this ITextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = row.Segment.Lowercase();
                    return row;
                }
            );
        }

        public static ITextCorpus Uppercase(this ITextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = row.Segment.Uppercase();
                    return row;
                }
            );
        }

        public static ITextCorpus Truecase(this ITextCorpus corpus, ITruecaser truecaser)
        {
            return corpus.Transform(
                row =>
                {
                    row.Segment = truecaser.Truecase(row.Segment);
                    return row;
                }
            );
        }

        public static ITextCorpus Transform(this ITextCorpus corpus, IRowProcessor<TextRow> processor)
        {
            return corpus.Transform(processor.Process);
        }

        public static ITextCorpus Transform(this ITextCorpus corpus, Func<TextRow, TextRow> transform)
        {
            return new TransformTextCorpus(corpus, transform);
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

        private class TransformTextCorpus : TextCorpusBase
        {
            private readonly ITextCorpus _corpus;
            private readonly Func<TextRow, TextRow> _transform;

            public TransformTextCorpus(ITextCorpus corpus, Func<TextRow, TextRow> transform)
            {
                _corpus = corpus;
                _transform = transform;
            }

            public override IEnumerable<IText> Texts => _corpus.Texts;

            public override bool MissingRowsAllowed => _corpus.MissingRowsAllowed;

            public override int Count(bool includeEmpty = true)
            {
                return _corpus.Count(includeEmpty);
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

            public override bool MissingRowsAllowed => _corpora.Any(corpus => corpus.MissingRowsAllowed);

            public override int Count(bool includeEmpty = true)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty));
            }

            public override IEnumerable<TextRow> GetRows(IEnumerable<string> textIds)
            {
                // TODO: it is possible that rows will be returned out-of-order. This could cause issues when aligning
                // rows to create a parallel corpus.
                return _corpora.SelectMany(corpus => corpus.GetRows(textIds));
            }
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

            public override bool MissingRowsAllowed => _corpus.MissingRowsAllowed;

            public override int Count(bool includeEmpty = true)
            {
                return _corpus.Count(includeEmpty);
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

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds)
            {
                return _corpus.GetRows(alignmentCollectionIds).Where(_predicate);
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

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds)
            {
                return GetRows(alignmentCollectionIds).Take(_count);
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

            public override bool MissingRowsAllowed => _corpora.Any(corpus => corpus.MissingRowsAllowed);

            public override int Count(bool includeEmpty = true)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty));
            }

            public override IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds)
            {
                return _corpora.SelectMany(corpus => corpus.GetRows(alignmentCollectionIds));
            }
        }

        #endregion

        #region IParallelTextCorpus operations

        public static IParallelTextCorpus Tokenize(
            this IParallelTextCorpus corpus,
            ITokenizer<string, int, string> tokenizer
        )
        {
            return corpus.Tokenize(tokenizer, tokenizer);
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
            ITokenizer<string, int, string> targetTokenizer
        )
        {
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = sourceTokenizer.Tokenize(row.SourceText).ToArray();
                    row.TargetSegment = targetTokenizer.Tokenize(row.TargetText).ToArray();
                    return row;
                }
            );
        }

        public static IParallelTextCorpus Tokenize<TSource, TTarget>(this IParallelTextCorpus corpus)
            where TSource : ITokenizer<string, int, string>, new()
            where TTarget : ITokenizer<string, int, string>, new()
        {
            var sourceTokenizer = new TSource();
            var targetTokenizer = new TTarget();
            return corpus.Tokenize(sourceTokenizer, targetTokenizer);
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
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = row.SourceSegment.Normalize(normalizationForm);
                    row.TargetSegment = row.TargetSegment.Normalize(normalizationForm);
                    return row;
                }
            );
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
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = row.SourceSegment.EscapeSpaces();
                    row.TargetSegment = row.TargetSegment.EscapeSpaces();
                    return row;
                }
            );
        }

        public static IParallelTextCorpus UnescapeSpaces(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = row.SourceSegment.UnescapeSpaces();
                    row.TargetSegment = row.TargetSegment.UnescapeSpaces();
                    return row;
                }
            );
        }

        public static IParallelTextCorpus Lowercase(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = row.SourceSegment.Lowercase();
                    row.TargetSegment = row.TargetSegment.Lowercase();
                    return row;
                }
            );
        }

        public static IParallelTextCorpus Uppercase(this IParallelTextCorpus corpus)
        {
            return corpus.Transform(
                row =>
                {
                    row.SourceSegment = row.SourceSegment.Uppercase();
                    row.TargetSegment = row.TargetSegment.Uppercase();
                    return row;
                }
            );
        }

        public static IParallelTextCorpus Transform(
            this IParallelTextCorpus corpus,
            IRowProcessor<ParallelTextRow> processor
        )
        {
            return corpus.Transform(processor.Process);
        }

        public static IParallelTextCorpus Transform(
            this IParallelTextCorpus corpus,
            Func<ParallelTextRow, ParallelTextRow> transform
        )
        {
            return new TransformParallelTextCorpus(corpus, transform);
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

        private class TransformParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly Func<ParallelTextRow, ParallelTextRow> _transform;

            public TransformParallelTextCorpus(
                IParallelTextCorpus corpus,
                Func<ParallelTextRow, ParallelTextRow> transform
            )
            {
                _corpus = corpus;
                _transform = transform;
            }

            public override bool MissingRowsAllowed => _corpus.MissingRowsAllowed;

            public override int Count(bool includeEmpty = true)
            {
                return _corpus.Count(includeEmpty);
            }

            public override IEnumerable<ParallelTextRow> GetRows()
            {
                return _corpus.GetRows().Select(_transform);
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

            public override IEnumerable<ParallelTextRow> GetRows()
            {
                return _corpus.GetRows().Where(_predicate);
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

            public override IEnumerable<ParallelTextRow> GetRows()
            {
                return _corpus.GetRows().Take(_count);
            }
        }

        private class FlattenParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus[] _corpora;

            public FlattenParallelTextCorpus(IParallelTextCorpus[] corpora)
            {
                _corpora = corpora;
            }

            public override bool MissingRowsAllowed => _corpora.Any(corpus => corpus.MissingRowsAllowed);

            public override int Count(bool includeEmpty = true)
            {
                return _corpora.Sum(corpus => corpus.Count(includeEmpty));
            }

            public override IEnumerable<ParallelTextRow> GetRows()
            {
                return _corpora.SelectMany(corpus => corpus.GetRows());
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
