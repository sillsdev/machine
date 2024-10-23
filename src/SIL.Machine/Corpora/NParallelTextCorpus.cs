using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SIL.Extensions;
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
                    enumeratedCorpora.Add(
                        new TextCorpusEnumerator(
                            Corpora[i].GetRows(filterTextIds).GetEnumerator(),
                            Corpora[0].Versification,
                            Corpora[i].Versification
                        )
                    );
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

        private bool AnyInRangeWithSegments(IList<IEnumerator<TextRow>> listOfEnumerators)
        {
            return listOfEnumerators.Any(e => e.Current.IsInRange)
                && listOfEnumerators.All(e => !(e.Current.IsInRange && e.Current.Segment.Count == 0));
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

        private IEnumerable<NParallelTextRow> GetRows(IList<IEnumerator<TextRow>> listOfEnumerators)
        {
            {
                var rangeInfo = new NRangeInfo { Versification = Corpora[0].Versification };

                List<TextRow>[] sameRefRows = new List<TextRow>[Corpora.Count];
                bool[] completed = listOfEnumerators.Select(e => !e.MoveNext()).ToArray();

                while (!completed.Any())
                {
                    IList<int> minRefIndexes;
                    IList<object> currentRefs = listOfEnumerators.Select(e => e.Current.Ref).ToArray();
                    try
                    {
                        minRefIndexes = MinRefIndexes(currentRefs);
                    }
                    catch (ArgumentException)
                    {
                        throw new CorpusAlignmentException(currentRefs.Select(r => r.ToString()).ToArray());
                    }
                    if (minRefIndexes.Count < N)
                    {
                        IList<int> nonMinRefIndexes = System.Linq.Enumerable.Range(0, N).Except(minRefIndexes).ToList();
                        IReadOnlyList<bool> allNonMinRows = nonMinRefIndexes
                            .Select(i => AllRowsList[i])
                            .ToImmutableArray();

                        IList<IEnumerator<TextRow>> minEnumerators = minRefIndexes
                            .Select(i => listOfEnumerators[i])
                            .ToList();
                        IList<IEnumerator<TextRow>> nonMinEnumerators = nonMinRefIndexes
                            .Select(i => listOfEnumerators[i])
                            .ToList();

                        if (!allNonMinRows.Any() && minEnumerators.Select(e => e.Current.IsInRange).Any())
                        {
                            if (
                                rangeInfo.IsInRange
                                && nonMinEnumerators
                                    .Select(e => e.Current.IsInRange && e.Current.Segment.Count > 0)
                                    .Any()
                            )
                            {
                                yield return rangeInfo.CreateRow();
                            }
                            minRefIndexes.ForEach(i => rangeInfo.AddTextRow(listOfEnumerators[i].Current, i));
                            nonMinRefIndexes.ForEach(i => rangeInfo.Rows[i].SameRefRows.Clear());
                        }
                        else
                        {
                            foreach (
                                NParallelTextRow row in CreateMinRefRows(
                                    rangeInfo,
                                    minEnumerators.Select(e => e.Current).ToList(),
                                    nonMinEnumerators.Select(e => e.Current).ToList(),
                                    allNonMinRows
                                )
                            )
                            {
                                yield return row;
                            }
                            foreach (int i in nonMinRefIndexes)
                            {
                                rangeInfo.Rows[i].SameRefRows.Add(listOfEnumerators[i].Current);
                                listOfEnumerators[i].MoveNext();
                            }
                        }
                        // source is less than target
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

                        if (
                            (!AllTargetRows && srcEnumerator.Current.IsInRange)
                            || (!AllSourceRows && trgEnumerator.Current.IsInRange)
                        )
                        {
                            if (rangeInfo.IsInRange && AnyInRangeWithSegments(listOfEnumerators))
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
                                        NParallelTextRow row in CreateRows(
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
                                        NParallelTextRow row in CreateRows(
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
                                NParallelTextRow row in CreateRows(
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
                                NParallelTextRow row in CreateSourceRows(
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
                                NParallelTextRow row in CreateTargetRows(
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
                                        NParallelTextRow row in CreateRows(
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
                                        NParallelTextRow row in CreateRows(
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
                                NParallelTextRow row in CreateRows(
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
                            NParallelTextRow row in CreateSourceRows(
                                rangeInfo,
                                srcEnumerator.Current,
                                targetSameRefRows
                            )
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
                            NParallelTextRow row in CreateTargetRows(
                                rangeInfo,
                                trgEnumerator.Current,
                                sourceSameRefRows
                            )
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

        private object[] UnifyVersification(object[] refs)
        {
            if (Corpora[0].Versification == null || refs.Length == 0)
                return refs;
            return refs.Cast<ScriptureRef>()
                .Select(r => r.ChangeVersification(Corpora[0].Versification))
                .Cast<object>()
                .ToArray();
        }

        private IEnumerable<NParallelTextRow> CreateRows(
            NRangeInfo rangeInfo,
            IList<TextRow> rows,
            IList<bool> forceInRange = null
        )
        {
            if (rangeInfo.IsInRange)
                yield return rangeInfo.CreateRow();

            if (!rows.Any(r => r != null))
                throw new ArgumentNullException("A corpus row must be specified.");

            object[] refRefs = new object[] { rows.Select(r => r?.Ref).First() };
            string textId = null;
            IList<object[]> refs = new List<object[]>();
            IList<TextRowFlags> flags = new List<TextRowFlags>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    textId = textId ?? rows[i].TextId;
                    refs.Add(UnifyVersification(new object[] { rows[i].Ref }));
                    flags.Add(rows[i].Flags);
                }
                else
                {
                    refs.Add(refRefs);
                    flags.Add(forceInRange[i] ? TextRowFlags.InRange : TextRowFlags.None);
                }
            }

            yield return new NParallelTextRow(textId, refs)
            {
                NSegments = rows.Select(r => r?.Segment ?? Array.Empty<string>()).ToArray(),
                NFlags = flags.ToReadOnlyList()
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

        private IEnumerable<NParallelTextRow> CreateMinRefRows(
            NRangeInfo rangeInfo,
            IList<TextRow> currentRows,
            IList<int> minRefIndexes,
            IList<int> nonMinRefIndexes,
            bool forceInRange = false
        )
        {
            IList<TextRow> minRows = minRefIndexes.Select(i => currentRows[i]).ToList();
            IList<TextRow> nonMinRows = nonMinRefIndexes.Select(i => currentRows[i]).ToList();

            if (CheckSameRefRows(targetSameRefRows, sourceRow))
            {
                foreach (TextRow targetSameRefRow in targetSameRefRows)
                {
                    foreach (NParallelTextRow row in CreateRows(rangeInfo, sourceRow, targetSameRefRow))
                        yield return row;
                }
            }
            else if (AllSourceRows)
            {
                foreach (
                    NParallelTextRow row in CreateRows(
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

        private IEnumerable<NParallelTextRow> CreateTargetRows(
            NRangeInfo rangeInfo,
            TextRow targetRow,
            List<TextRow> sourceSameRefRows,
            bool forceSourceInRange = false
        )
        {
            if (CheckSameRefRows(sourceSameRefRows, targetRow))
            {
                foreach (TextRow sourceSameRefRow in sourceSameRefRows)
                {
                    foreach (NParallelTextRow row in CreateRows(rangeInfo, sourceSameRefRow, targetRow))
                        yield return row;
                }
            }
            else if (AllTargetRows)
            {
                foreach (
                    NParallelTextRow row in CreateRows(
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
            public IList<object> Refs { get; } = new List<object>();
            public IList<string> Segment { get; } = new List<string>();
            public IList<TextRow> SameRefRows { get; } = new List<TextRow>();
            public bool IsSentenceStart { get; set; } = false;
            public bool IsInRange => Refs.Count > 0;
            public bool IsEmpty => Segment.Count == 0;
        }

        private class NRangeInfo
        {
            public int N = -1;
            public string TextId { get; set; } = "";
            public ScrVers Versification { get; set; } = null;
            public IComparer<object> RowRefComparer { get; set; } = null;
            public List<RangeRow> Rows { get; } = new List<RangeRow>();
            public bool IsInRange => Rows.Any(r => r.IsInRange);

            private bool CheckSameRefRows(List<int> sameRefRows, TextRow otherRow)
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

            public void AddTextRow(TextRow row, int index)
            {
                if (N <= row.Segment.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        $"There are only {N} parallel texts, but text {index} was chosen."
                    );
                }
                TextId = row.TextId;
                Rows[index].Refs.Add(row.Ref);
                if (Rows[index].IsEmpty)
                    Rows[index].IsSentenceStart = row.IsSentenceStart;
                Rows[index].Segment.AddRange(row.Segment);
            }

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
                    NSegments = Rows.Select(r => r.Segment.ToArray()).ToArray(),
                    NFlags = Rows.Select(r => r.IsSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None)
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
