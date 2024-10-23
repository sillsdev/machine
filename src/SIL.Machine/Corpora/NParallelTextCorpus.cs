using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SIL.Linq;
using SIL.ObjectModel;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class NParallelTextCorpus : NParallelTextCorpusBase
    {
        public NParallelTextCorpus(IEnumerable<ITextCorpus> corpora, IComparer<object> rowRefComparer = null)
        {
            Corpora = corpora.ToImmutableArray();
            if (Corpora.Count < 1)
                throw new ArgumentException("There must be at least one corpora.", nameof(corpora));
            RowRefComparer = rowRefComparer ?? new DefaultRowRefComparer();
            AllRowsList = new bool[Corpora.Count]
                .Select(_ => false)
                .ToImmutableArray();
        }

        public bool GetIsTokenized(int i) =>
            i < Corpora.Count ? Corpora[i].IsTokenized : throw new ArgumentOutOfRangeException(nameof(i));

        public int N => Corpora.Count;

        public IReadOnlyList<bool> AllRowsList { get; set; }
        public IReadOnlyList<ITextCorpus> Corpora { get; }
        public IComparer<object> RowRefComparer { get; }

        private static HashSet<string> GetTextIdsFromCorpora(
            IEnumerable<ITextCorpus> corpora,
            IEnumerable<bool> allRowsEnumerate
        )
        {
            IReadOnlyList<IEnumerable<string>> textIdListOfLists = corpora
                .Select(c => c.Texts.Select(t => t.Id))
                .ToImmutableArray();

            HashSet<string> textIds = textIdListOfLists
                .Skip(1)
                .Aggregate(
                    new HashSet<string>(textIdListOfLists.First()),
                    (h, e) =>
                    {
                        h.IntersectWith(e);
                        return h;
                    }
                );
            allRowsEnumerate
                .Select((allRows, i) => (allRows, i))
                .Where(t => t.allRows)
                .ForEach(t => textIds.UnionWith(textIdListOfLists[t.i]));
            return textIds;
        }

        public override IEnumerable<NParallelTextRow> GetRows(IEnumerable<string> textIds)
        {
            HashSet<string> filterTextIds = GetTextIdsFromCorpora(Corpora, AllRowsList);

            if (textIds != null)
                filterTextIds.IntersectWith(textIds);

            IList<IEnumerator<TextRow>> enumeratedCorpora = new List<IEnumerator<TextRow>>();
            try
            {
                for (int i = 0; i < Corpora.Count; i++)
                {
                    if (i == 0)
                    {
                        enumeratedCorpora.Add(Corpora[0].GetRows(filterTextIds).GetEnumerator());
                    }
                    else
                    {
                        enumeratedCorpora.Add(
                            new ParallelCorpusEnumerator(
                                Corpora[i].GetRows(filterTextIds).GetEnumerator(),
                                Corpora[0].Versification,
                                Corpora[i].Versification
                            )
                        );
                    }
                }
                return GetRows(enumeratedCorpora);
            }
            finally
            {
                foreach (IEnumerator<TextRow> enumerator in enumeratedCorpora)
                {
                    enumerator.Dispose();
                }
            }
        }

        private IList<int> MinRefIndexes(IList<object> refs)
        {
            object minRef = refs[0];
            IList<int> minRefIndexes = new List<int>(0);
            for (int i = 1; i < refs.Count; i++)
            {
                if (RowRefComparer.Compare(refs[i], minRef) < 0)
                {
                    minRef = refs[i];
                    minRefIndexes.Clear();
                    minRefIndexes.Add(i);
                }
                else if (RowRefComparer.Compare(refs[i], minRef) == 0)
                {
                    minRefIndexes.Add(i);
                }
            }
            return minRefIndexes;
        }

        private IEnumerable<NParallelTextRow> GetRows(IList<IEnumerator<TextRow>> enumerators)
        {
            {
                var rangeInfo = new NRangeInfo { Versification = Corpora[0].Versification };

                List<TextRow>[] sameRefRows = new List<TextRow>[Corpora.Count];
                bool[] completed = enumerators.Select(e => !e.MoveNext()).ToArray();

                while (!completed.Any())
                {
                    IList<int> minRefIndexes;
                    IList<object> currentRefs = enumerators.Select(e => e.Current.Ref).ToArray();
                    try
                    {
                        minRefIndexes = MinRefIndexes(currentRefs);
                    }
                    catch (ArgumentException)
                    {
                        throw new CorpusAlignmentException(currentRefs.Select(r => r.ToString()).ToArray());
                    }
                    if (minRefIndexes.Count == N)
                    {
                        if (
                            (!AllTargetRows && srcEnumerator.Current.IsInRange)
                            || (!AllSourceRows && trgEnumerator.Current.IsInRange)
                        )
                        {
                            if (
                                rangeInfo.IsInRange
                                && (
                                    (
                                        srcEnumerator.Current.IsInRange
                                        && !trgEnumerator.Current.IsInRange
                                        && srcEnumerator.Current.Segment.Count > 0
                                    )
                                    || (
                                        !srcEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.Segment.Count > 0
                                    )
                                    || (
                                        srcEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.IsInRange
                                        && srcEnumerator.Current.Segment.Count > 0
                                        && trgEnumerator.Current.Segment.Count > 0
                                    )
                                )
                            )
                            {
                                yield return rangeInfo.CreateRow();
                            }

                            rangeInfo.TextId = srcEnumerator.Current.TextId;
                            rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
                            rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
                            sourceSameRefRows.Clear();
                            targetSameRefRows.Clear();
                            if (rangeInfo.IsSourceEmpty)
                                rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
                            if (rangeInfo.IsTargetEmpty)
                                rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
                            rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
                            rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
                        }
                        else
                        {
                            if (CheckSameRefRows(sourceSameRefRows, trgEnumerator.Current))
                            {
                                foreach (TextRow prevSourceRow in sourceSameRefRows)
                                {
                                    foreach (
                                        ParallelTextRow row in CreateRows(
                                            rangeInfo,
                                            prevSourceRow,
                                            trgEnumerator.Current
                                        )
                                    )
                                    {
                                        yield return row;
                                    }
                                }
                            }

                            if (CheckSameRefRows(targetSameRefRows, srcEnumerator.Current))
                            {
                                foreach (TextRow prevTargetRow in targetSameRefRows)
                                {
                                    foreach (
                                        ParallelTextRow row in CreateRows(
                                            rangeInfo,
                                            srcEnumerator.Current,
                                            prevTargetRow
                                        )
                                    )
                                    {
                                        yield return row;
                                    }
                                }
                            }

                            foreach (
                                ParallelTextRow row in CreateRows(
                                    rangeInfo,
                                    srcEnumerator.Current,
                                    trgEnumerator.Current,
                                    compare2 == 0 ? alignmentEnumerator.Current.AlignedWordPairs : null
                                )
                            )
                            {
                                yield return row;
                            }
                        }

                        sourceSameRefRows.Add(srcEnumerator.Current);
                        srcCompleted = !srcEnumerator.MoveNext();

                        targetSameRefRows.Add(trgEnumerator.Current);
                        trgCompleted = !trgEnumerator.MoveNext();
                    }
                    if (compare < 0)
                    {
                        if (!AllTargetRows && srcEnumerator.Current.IsInRange)
                        {
                            if (
                                rangeInfo.IsInRange
                                && trgEnumerator.Current.IsInRange
                                && trgEnumerator.Current.Segment.Count > 0
                            )
                            {
                                yield return rangeInfo.CreateRow();
                            }
                            rangeInfo.TextId = srcEnumerator.Current.TextId;
                            rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
                            targetSameRefRows.Clear();
                            if (rangeInfo.IsSourceEmpty)
                                rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
                            rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
                        }
                        else
                        {
                            foreach (
                                ParallelTextRow row in CreateSourceRows(
                                    rangeInfo,
                                    srcEnumerator.Current,
                                    targetSameRefRows,
                                    forceTargetInRange: srcEnumerator.Current.TextId == trgEnumerator.Current.TextId
                                        && !trgEnumerator.Current.IsRangeStart
                                        && trgEnumerator.Current.IsInRange
                                )
                            )
                            {
                                yield return row;
                            }
                        }

                        sourceSameRefRows.Add(srcEnumerator.Current);
                        srcCompleted = !srcEnumerator.MoveNext();
                    }
                    else if (compare > 0)
                    {
                        if (!AllSourceRows && trgEnumerator.Current.IsInRange)
                        {
                            if (
                                rangeInfo.IsInRange
                                && srcEnumerator.Current.IsInRange
                                && srcEnumerator.Current.Segment.Count > 0
                            )
                            {
                                yield return rangeInfo.CreateRow();
                            }
                            rangeInfo.TextId = trgEnumerator.Current.TextId;
                            rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
                            sourceSameRefRows.Clear();
                            if (rangeInfo.IsTargetEmpty)
                                rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
                            rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
                        }
                        else
                        {
                            foreach (
                                ParallelTextRow row in CreateTargetRows(
                                    rangeInfo,
                                    trgEnumerator.Current,
                                    sourceSameRefRows,
                                    forceSourceInRange: trgEnumerator.Current.TextId == srcEnumerator.Current.TextId
                                        && !srcEnumerator.Current.IsRangeStart
                                        && srcEnumerator.Current.IsInRange
                                )
                            )
                            {
                                yield return row;
                            }
                        }

                        targetSameRefRows.Add(trgEnumerator.Current);
                        trgCompleted = !trgEnumerator.MoveNext();
                    }
                    else
                    // compare == 0 - the refs are the same
                    {
                        if (
                            (!AllTargetRows && srcEnumerator.Current.IsInRange)
                            || (!AllSourceRows && trgEnumerator.Current.IsInRange)
                        )
                        {
                            if (
                                rangeInfo.IsInRange
                                && (
                                    (
                                        srcEnumerator.Current.IsInRange
                                        && !trgEnumerator.Current.IsInRange
                                        && srcEnumerator.Current.Segment.Count > 0
                                    )
                                    || (
                                        !srcEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.Segment.Count > 0
                                    )
                                    || (
                                        srcEnumerator.Current.IsInRange
                                        && trgEnumerator.Current.IsInRange
                                        && srcEnumerator.Current.Segment.Count > 0
                                        && trgEnumerator.Current.Segment.Count > 0
                                    )
                                )
                            )
                            {
                                yield return rangeInfo.CreateRow();
                            }

                            rangeInfo.TextId = srcEnumerator.Current.TextId;
                            rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
                            rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
                            sourceSameRefRows.Clear();
                            targetSameRefRows.Clear();
                            if (rangeInfo.IsSourceEmpty)
                                rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
                            if (rangeInfo.IsTargetEmpty)
                                rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
                            rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
                            rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
                        }
                        else
                        {
                            if (CheckSameRefRows(sourceSameRefRows, trgEnumerator.Current))
                            {
                                foreach (TextRow prevSourceRow in sourceSameRefRows)
                                {
                                    foreach (
                                        ParallelTextRow row in CreateRows(
                                            rangeInfo,
                                            prevSourceRow,
                                            trgEnumerator.Current
                                        )
                                    )
                                    {
                                        yield return row;
                                    }
                                }
                            }

                            if (CheckSameRefRows(targetSameRefRows, srcEnumerator.Current))
                            {
                                foreach (TextRow prevTargetRow in targetSameRefRows)
                                {
                                    foreach (
                                        ParallelTextRow row in CreateRows(
                                            rangeInfo,
                                            srcEnumerator.Current,
                                            prevTargetRow
                                        )
                                    )
                                    {
                                        yield return row;
                                    }
                                }
                            }

                            foreach (
                                ParallelTextRow row in CreateRows(
                                    rangeInfo,
                                    srcEnumerator.Current,
                                    trgEnumerator.Current,
                                    compare2 == 0 ? alignmentEnumerator.Current.AlignedWordPairs : null
                                )
                            )
                            {
                                yield return row;
                            }
                        }

                        sourceSameRefRows.Add(srcEnumerator.Current);
                        srcCompleted = !srcEnumerator.MoveNext();

                        targetSameRefRows.Add(trgEnumerator.Current);
                        trgCompleted = !trgEnumerator.MoveNext();
                    }
                }

                while (!srcCompleted)
                {
                    if (!AllTargetRows && srcEnumerator.Current.IsInRange)
                    {
                        rangeInfo.TextId = srcEnumerator.Current.TextId;
                        rangeInfo.SourceRefs.Add(srcEnumerator.Current.Ref);
                        targetSameRefRows.Clear();
                        if (rangeInfo.IsSourceEmpty)
                            rangeInfo.IsSourceSentenceStart = srcEnumerator.Current.IsSentenceStart;
                        rangeInfo.SourceSegment.AddRange(srcEnumerator.Current.Segment);
                    }
                    else
                    {
                        foreach (
                            ParallelTextRow row in CreateSourceRows(rangeInfo, srcEnumerator.Current, targetSameRefRows)
                        )
                        {
                            yield return row;
                        }
                    }
                    srcCompleted = !srcEnumerator.MoveNext();
                }

                while (!trgCompleted)
                {
                    if (!AllSourceRows && trgEnumerator.Current.IsInRange)
                    {
                        rangeInfo.TextId = trgEnumerator.Current.TextId;
                        rangeInfo.TargetRefs.Add(trgEnumerator.Current.Ref);
                        sourceSameRefRows.Clear();
                        if (rangeInfo.IsTargetEmpty)
                            rangeInfo.IsTargetSentenceStart = trgEnumerator.Current.IsSentenceStart;
                        rangeInfo.TargetSegment.AddRange(trgEnumerator.Current.Segment);
                    }
                    else
                    {
                        foreach (
                            ParallelTextRow row in CreateTargetRows(rangeInfo, trgEnumerator.Current, sourceSameRefRows)
                        )
                        {
                            yield return row;
                        }
                    }
                    trgCompleted = !trgEnumerator.MoveNext();
                }

                if (rangeInfo.IsInRange)
                    yield return rangeInfo.CreateRow();
            }
        }

        private IEnumerable<ParallelTextRow> CreateRows(
            RangeInfo rangeInfo,
            TextRow srcRow,
            TextRow trgRow,
            IReadOnlyCollection<AlignedWordPair> alignedWordPairs = null,
            bool forceSourceInRange = false,
            bool forceTargetInRange = false
        )
        {
            if (rangeInfo.IsInRange)
                yield return rangeInfo.CreateRow();

            string textId;
            if (srcRow != null)
                textId = srcRow.TextId;
            else if (trgRow != null)
                textId = trgRow.TextId;
            else
                throw new ArgumentNullException("Either a source or target must be specified.");

            object[] sourceRefs = srcRow != null ? new object[] { srcRow.Ref } : Array.Empty<object>();
            object[] targetRefs = trgRow != null ? new object[] { trgRow.Ref } : Array.Empty<object>();
            if (targetRefs.Length == 0 && TargetCorpus.IsScripture())
            {
                targetRefs = sourceRefs
                    .Cast<ScriptureRef>()
                    .Select(r => r.ChangeVersification(TargetCorpus.Versification))
                    .Cast<object>()
                    .ToArray();
            }

            TextRowFlags sourceFlags;
            if (srcRow == null)
                sourceFlags = forceSourceInRange ? TextRowFlags.InRange : TextRowFlags.None;
            else
                sourceFlags = srcRow.Flags;

            TextRowFlags targetFlags;
            if (trgRow == null)
                targetFlags = forceTargetInRange ? TextRowFlags.InRange : TextRowFlags.None;
            else
                targetFlags = trgRow.Flags;

            yield return new ParallelTextRow(textId, sourceRefs, targetRefs)
            {
                SourceSegment = srcRow != null ? srcRow.Segment : Array.Empty<string>(),
                TargetSegment = trgRow != null ? trgRow.Segment : Array.Empty<string>(),
                AlignedWordPairs = alignedWordPairs,
                SourceFlags = sourceFlags,
                TargetFlags = targetFlags
            };
        }

        private bool CheckSameRefRows(List<TextRow> sameRefRows, TextRow otherRow)
        {
            try
            {
                if (sameRefRows.Count > 0 && RowRefComparer.Compare(sameRefRows[0].Ref, otherRow.Ref) != 0)
                    sameRefRows.Clear();
            }
            catch (ArgumentException)
            {
                throw new CorpusAlignmentException(sameRefRows[0].Ref.ToString(), otherRow.Ref.ToString());
            }
            return sameRefRows.Count > 0;
        }

        private IEnumerable<ParallelTextRow> CreateSourceRows(
            RangeInfo rangeInfo,
            TextRow sourceRow,
            List<TextRow> targetSameRefRows,
            bool forceTargetInRange = false
        )
        {
            if (CheckSameRefRows(targetSameRefRows, sourceRow))
            {
                foreach (TextRow targetSameRefRow in targetSameRefRows)
                {
                    foreach (ParallelTextRow row in CreateRows(rangeInfo, sourceRow, targetSameRefRow))
                        yield return row;
                }
            }
            else if (AllSourceRows)
            {
                foreach (
                    ParallelTextRow row in CreateRows(
                        rangeInfo,
                        sourceRow,
                        null,
                        forceTargetInRange: forceTargetInRange
                    )
                )
                {
                    yield return row;
                }
            }
        }

        private IEnumerable<ParallelTextRow> CreateTargetRows(
            RangeInfo rangeInfo,
            TextRow targetRow,
            List<TextRow> sourceSameRefRows,
            bool forceSourceInRange = false
        )
        {
            if (CheckSameRefRows(sourceSameRefRows, targetRow))
            {
                foreach (TextRow sourceSameRefRow in sourceSameRefRows)
                {
                    foreach (ParallelTextRow row in CreateRows(rangeInfo, sourceSameRefRow, targetRow))
                        yield return row;
                }
            }
            else if (AllTargetRows)
            {
                foreach (
                    ParallelTextRow row in CreateRows(
                        rangeInfo,
                        null,
                        targetRow,
                        forceSourceInRange: forceSourceInRange
                    )
                )
                {
                    yield return row;
                }
            }
        }

        private class RangeRow
        {
            public List<object> Refs { get; } = new List<object>();
            public List<string> Segment { get; } = new List<string>();
            public bool IsSentenceStart { get; set; } = false;
            public bool IsInRange => Refs.Count > 0;
            public bool IsEmpty => Segment.Count == 0;
        }

        private class NRangeInfo
        {
            public int N = -1;
            public string TextId { get; set; } = "";
            public ScrVers Versification { get; set; } = null;
            public List<RangeRow> Rows { get; } = new List<RangeRow>();
            public bool IsInRange => Rows.Any(r => r.IsInRange);

            public NParallelTextRow CreateRow()
            {
                object[] refs = new object[0];
                foreach (RangeRow cRow in Rows)
                {
                    if (refs.Count() == 0 && Versification != null)
                    {
                        refs = cRow
                            .Refs.ToArray()
                            .Cast<ScriptureRef>()
                            .Select(r => r.ChangeVersification(Versification))
                            .Cast<object>()
                            .ToArray();
                    }
                }
                var nParRow = new NParallelTextRow(TextId, Rows.Select(r => r.Refs).ToArray())
                {
                    Segments = Rows.Select(r => r.Segment.ToArray()).ToArray(),
                    Flags = Rows.Select(r => r.IsSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None)
                        .ToArray()
                };
                TextId = "";
                foreach (RangeRow r in Rows)
                {
                    r.Refs.Clear();
                    r.Segment.Clear();
                    r.IsSentenceStart = false;
                }
                return nParRow;
            }
        }

        private class DefaultRowRefComparer : IComparer<object>
        {
            public int Compare(object x, object y)
            {
                // Do not use the default comparer for ScriptureRef, since we want to ignore segments
                if (x is ScriptureRef sx && y is ScriptureRef sy)
                    return sx.CompareTo(sy, compareSegments: false);

                return Comparer<object>.Default.Compare(x, y);
            }
        }
    }
}
