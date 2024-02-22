using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class ParallelTextCorpus : IParallelTextCorpus
    {
        public ParallelTextCorpus(
            ITextCorpus sourceCorpus,
            ITextCorpus targetCorpus,
            IAlignmentCorpus alignmentCorpus = null,
            IComparer<object> rowRefComparer = null
        )
        {
            SourceCorpus = sourceCorpus;
            TargetCorpus = targetCorpus;
            AlignmentCorpus = alignmentCorpus ?? new DictionaryAlignmentCorpus();
            RowRefComparer = rowRefComparer ?? new DefaultRowRefComparer();
        }

        public bool IsSourceTokenized => SourceCorpus.IsTokenized;
        public bool IsTargetTokenized => TargetCorpus.IsTokenized;

        public bool AllSourceRows { get; set; }
        public bool AllTargetRows { get; set; }

        public ITextCorpus SourceCorpus { get; }
        public ITextCorpus TargetCorpus { get; }
        public IAlignmentCorpus AlignmentCorpus { get; }
        public IComparer<object> RowRefComparer { get; }

        public int Count(bool includeEmpty = true)
        {
            return includeEmpty ? GetRows().Count() : GetRows().Count(r => !r.IsEmpty);
        }

        public IEnumerator<ParallelTextRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<ParallelTextRow> GetRows()
        {
            IEnumerable<string> sourceTextIds = SourceCorpus.Texts.Select(t => t.Id);
            IEnumerable<string> targetTextIds = TargetCorpus.Texts.Select(t => t.Id);

            IEnumerable<string> textIds;
            if (AllSourceRows && AllTargetRows)
                textIds = sourceTextIds.Union(targetTextIds);
            else if (!AllSourceRows && !AllTargetRows)
                textIds = sourceTextIds.Intersect(targetTextIds);
            else if (AllSourceRows)
                textIds = sourceTextIds;
            else
                textIds = targetTextIds;

            using (IEnumerator<TextRow> srcEnumerator = SourceCorpus.GetRows(textIds).GetEnumerator())
            using (var trgEnumerator = new TargetCorpusEnumerator(TargetCorpus.GetRows(textIds).GetEnumerator()))
            using (IEnumerator<AlignmentRow> alignmentEnumerator = AlignmentCorpus.GetRows(textIds).GetEnumerator())
            {
                var rangeInfo = new RangeInfo();
                rangeInfo.Versification =
                    TargetCorpus is ScriptureTextCorpus tc && SourceCorpus is ScriptureTextCorpus
                        ? tc.Versification
                        : null;
                var sourceSameRefRows = new List<TextRow>();
                var targetSameRefRows = new List<TextRow>();

                bool srcCompleted = !srcEnumerator.MoveNext();
                if (!srcCompleted && srcEnumerator.Current.Ref is VerseRef verseRef)
                    trgEnumerator.SourceVersification = verseRef.Versification;
                bool trgCompleted = !trgEnumerator.MoveNext();
                while (!srcCompleted && !trgCompleted)
                {
                    int compare1 = 0;
                    try
                    {
                        compare1 = RowRefComparer.Compare(srcEnumerator.Current.Ref, trgEnumerator.Current.Ref);
                    }
                    catch (ArgumentException)
                    {
                        throw new CorpusAlignmentException(
                            srcEnumerator.Current.Ref.ToString(),
                            trgEnumerator.Current.Ref.ToString()
                        );
                    }
                    if (compare1 < 0)
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
                    else if (compare1 > 0)
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
                    {
                        int compare2;
                        do
                        {
                            try
                            {
                                compare2 = alignmentEnumerator.MoveNext()
                                    ? RowRefComparer.Compare(srcEnumerator.Current.Ref, alignmentEnumerator.Current.Ref)
                                    : 1;
                            }
                            catch (ArgumentException)
                            {
                                throw new CorpusAlignmentException(
                                    srcEnumerator.Current.Ref.ToString(),
                                    trgEnumerator.Current.Ref.ToString()
                                );
                            }
                        } while (compare2 < 0);

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

            var sourceRefs = srcRow != null ? new object[] { srcRow.Ref } : Array.Empty<object>();
            var targetRefs = trgRow != null ? new object[] { trgRow.Ref } : Array.Empty<object>();
            if (targetRefs.Length == 0 && TargetCorpus is ScriptureTextCorpus stc)
            {
                targetRefs = sourceRefs
                    .Cast<VerseRef>()
                    .Select(r =>
                    {
                        var t = r.Clone();
                        t.ChangeVersification(stc.Versification);
                        return t;
                    })
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

        private class RangeInfo
        {
            public string TextId { get; set; } = "";
            public List<object> SourceRefs { get; } = new List<object>();
            public List<object> TargetRefs { get; } = new List<object>();
            public List<string> SourceSegment { get; } = new List<string>();
            public List<string> TargetSegment { get; } = new List<string>();
            public bool IsSourceSentenceStart { get; set; } = false;
            public bool IsTargetSentenceStart { get; set; } = false;
            public bool IsInRange => SourceRefs.Count > 0 || TargetRefs.Count > 0;
            public bool IsSourceEmpty => SourceSegment.Count == 0;
            public bool IsTargetEmpty => TargetSegment.Count == 0;

            public ScrVers Versification { get; set; } = null;

            public ParallelTextRow CreateRow()
            {
                object[] trgRefs = TargetRefs.ToArray();
                if (TargetRefs.Count == 0 && Versification != null)
                {
                    trgRefs = SourceRefs
                        .ToArray()
                        .Cast<VerseRef>()
                        .Select(r =>
                        {
                            VerseRef t = r.Clone();
                            t.ChangeVersification(Versification);
                            return t;
                        })
                        .Cast<object>()
                        .ToArray();
                }
                var row = new ParallelTextRow(TextId, SourceRefs.ToArray(), trgRefs)
                {
                    SourceSegment = SourceSegment.ToArray(),
                    TargetSegment = TargetSegment.ToArray(),
                    SourceFlags = IsSourceSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None,
                    TargetFlags = IsTargetSentenceStart ? TextRowFlags.SentenceStart : TextRowFlags.None
                };
                TextId = "";
                SourceRefs.Clear();
                TargetRefs.Clear();
                SourceSegment.Clear();
                TargetSegment.Clear();
                IsSourceSentenceStart = false;
                IsTargetSentenceStart = false;
                return row;
            }
        }

        private class DefaultRowRefComparer : IComparer<object>
        {
            private static readonly VerseRefComparer VerseRefComparer = new VerseRefComparer(compareSegments: false);

            public int Compare(object x, object y)
            {
                // Do not use the default comparer for VerseRef, since we want to compare all verses in a range or
                // sequence
                if (x is VerseRef vx && y is VerseRef vy)
                    return VerseRefComparer.Compare(vx, vy);

                return Comparer<object>.Default.Compare(x, y);
            }
        }

        private class TargetCorpusEnumerator : DisposableBase, IEnumerator<TextRow>
        {
            private readonly IEnumerator<TextRow> _enumerator;
            private bool _isScripture = false;
            private bool _isEnumerating = false;
            private readonly Queue<TextRow> _verseRows;
            private TextRow _current;

            public TargetCorpusEnumerator(IEnumerator<TextRow> enumerator)
            {
                _enumerator = enumerator;
                _verseRows = new Queue<TextRow>();
            }

            public ScrVers SourceVersification { get; set; }

            public TextRow Current => _current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                bool result;
                if (!_isEnumerating)
                {
                    _isEnumerating = true;
                    result = _enumerator.MoveNext();
                    if (
                        result
                        && _enumerator.Current.Ref is VerseRef verseRef
                        && SourceVersification != null
                        && SourceVersification != verseRef.Versification
                    )
                    {
                        _isScripture = true;
                    }
                    else
                    {
                        _current = _enumerator.Current;
                        return result;
                    }
                }

                if (_isScripture)
                {
                    if (_verseRows.Count == 0 && _enumerator.Current != null)
                        CollectVerses();
                    if (_verseRows.Count > 0)
                    {
                        _current = _verseRows.Dequeue();
                        return true;
                    }
                    _current = null;
                    return false;
                }

                result = _enumerator.MoveNext();
                _current = _enumerator.Current;
                return result;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _isEnumerating = false;
                _isScripture = false;
            }

            protected override void DisposeManagedResources()
            {
                _enumerator.Dispose();
            }

            private void CollectVerses()
            {
                var rowList = new List<(VerseRef Ref, TextRow Row)>();
                bool outOfOrder = false;
                var prevVerseRef = new VerseRef();
                int rangeStartOffset = -1;
                do
                {
                    TextRow row = _enumerator.Current;
                    var verseRef = (VerseRef)row.Ref;
                    if (!prevVerseRef.IsDefault && verseRef.BookNum != prevVerseRef.BookNum)
                        break;

                    verseRef.ChangeVersification(SourceVersification);
                    // convert one-to-many versification mapping to a verse range
                    if (verseRef.Equals(prevVerseRef))
                    {
                        var (rangeStartVerseRef, rangeStartRow) = rowList[rowList.Count + rangeStartOffset];
                        var flags = TextRowFlags.InRange;
                        if (rangeStartRow.IsSentenceStart)
                            flags |= TextRowFlags.SentenceStart;
                        if (rangeStartOffset == -1 && (!rangeStartRow.IsInRange || rangeStartRow.IsRangeStart))
                            flags |= TextRowFlags.RangeStart;
                        rowList[rowList.Count + rangeStartOffset] = (
                            rangeStartVerseRef,
                            new TextRow(rangeStartRow.TextId, rangeStartRow.Ref)
                            {
                                Segment = rangeStartRow.Segment.Concat(row.Segment).ToArray(),
                                Flags = flags
                            }
                        );
                        row = new TextRow(row.TextId, row.Ref) { Flags = TextRowFlags.InRange };
                        rangeStartOffset--;
                    }
                    else
                    {
                        rangeStartOffset = -1;
                    }
                    rowList.Add((verseRef, row));
                    if (!outOfOrder && verseRef.CompareTo(prevVerseRef) < 0)
                        outOfOrder = true;
                    prevVerseRef = verseRef;
                } while (_enumerator.MoveNext());

                if (outOfOrder)
                    rowList.Sort((x, y) => x.Ref.CompareTo(y.Ref));

                foreach (var (_, row) in rowList)
                    _verseRows.Enqueue(row);
            }
        }
    }
}
