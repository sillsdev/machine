using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SIL.Extensions;
using SIL.Linq;
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

        private bool AnyInRangeWithSegments(IList<TextRow> rows)
        {
            return rows.Any(r => r.IsInRange) && rows.All(r => !(r.IsInRange && r.Segment.Count == 0));
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

                while (!completed.All(c => c))
                {
                    IList<int> minRefIndexes;
                    IList<TextRow> currentRows = listOfEnumerators
                        .Where((e, i) => !completed[i])
                        .Select(e => e.Current)
                        .ToArray();
                    try
                    {
                        minRefIndexes = MinRefIndexes(currentRows.Select(e => e.Ref).ToArray());
                    }
                    catch (ArgumentException)
                    {
                        throw new CorpusAlignmentException(currentRows.Select(e => e.Ref.ToString()).ToArray());
                    }

                    if (minRefIndexes.Count < (N - completed.Count(c => c))) //then there are some non-min refs
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
                                    nonMinRefIndexes,
                                    forceInRange: minEnumerators
                                        .Select(e => e.Current.TextId)
                                        .Union(nonMinEnumerators.Select(e => e.Current.TextId))
                                        .Distinct()
                                        .Count() == 1
                                        && nonMinEnumerators
                                            .Select(e => !e.Current.IsRangeStart && e.Current.IsInRange)
                                            .Any()
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
                    }
                    else if (minRefIndexes.Count == (N - completed.Count(c => c)))
                    // the refs are all the same
                    {
                        if (
                            !currentRows.Select((r, i) => AllRowsList[i]).Any()
                            && currentRows.Select(r => r.IsInRange).Any()
                        )
                        {
                            if (rangeInfo.IsInRange && AnyInRangeWithSegments(currentRows))
                            {
                                yield return rangeInfo.CreateRow();
                            }

                            for (int i = 0; i < currentRows.Count; i++)
                            {
                                rangeInfo.AddTextRow(currentRows[i], i);
                                rangeInfo.Rows[i].SameRefRows.Clear();
                            }
                        }
                        else
                        {
                            foreach (var row in currentRows) //TODO walk through together
                            {
                                if (rangeInfo.CheckSameRefRows(row))
                                {
                                    foreach (TextRow tr in rangeInfo.Rows.SelectMany(r => r.SameRefRows))
                                    {
                                        foreach (
                                            NParallelTextRow r in CreateRows(rangeInfo, new List<TextRow> { tr, row })
                                        )
                                        {
                                            yield return r;
                                        }
                                    }
                                }
                            }
                            foreach (NParallelTextRow row in CreateRows(rangeInfo, currentRows))
                            {
                                yield return row;
                            }
                        }

                        for (int i = 0; i < currentRows.Count; i++)
                        {
                            rangeInfo.Rows[i].SameRefRows.Add(currentRows[i]);
                        }
                    }
                    else
                    {
                        throw new CorpusAlignmentException(
                            minRefIndexes.Select(i => currentRows[i].Ref.ToString()).ToArray()
                        );
                    }
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
                    flags.Add(forceInRange == null || !forceInRange[i] ? TextRowFlags.None : TextRowFlags.InRange);
                }
            }

            yield return new NParallelTextRow(textId, refs)
            {
                NSegments = rows.Select(r => r?.Segment ?? Array.Empty<string>()).ToArray(),
                NFlags = flags.ToReadOnlyList()
            };
        }

        private IEnumerable<NParallelTextRow> CreateMinRefRows(
            NRangeInfo rangeInfo,
            IList<TextRow> minRefRows,
            IList<int> nonMinRefIndexes,
            bool forceInRange = false
        )
        {
            List<TextRow> sameRefRows = rangeInfo
                .Rows.Where((r, i) => nonMinRefIndexes.Contains(i))
                .SelectMany(r => r.SameRefRows)
                .ToList();

            foreach (TextRow textRow in minRefRows)
            {
                if (rangeInfo.CheckSameRefRows(sameRefRows, textRow))
                {
                    foreach (TextRow sameRefRow in sameRefRows)
                    {
                        foreach (
                            NParallelTextRow row in CreateRows(
                                rangeInfo,
                                new List<TextRow>() { textRow, sameRefRow },
                                forceInRange: new List<bool>() { false, forceInRange }
                            )
                        )
                        {
                            yield return row;
                        }
                    }
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

            public bool CheckSameRefRows(IList<TextRow> sameRefRows, TextRow otherRow)
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

            public bool CheckSameRefRows(TextRow row)
            {
                var sameRefRows = Rows.SelectMany(r => r.SameRefRows).ToList();
                try
                {
                    if (sameRefRows.Count > 0 && RowRefComparer.Compare(sameRefRows[0].Ref, row.Ref) != 0)
                        sameRefRows.Clear();
                }
                catch (ArgumentException)
                {
                    throw new CorpusAlignmentException(sameRefRows[0].Ref.ToString(), row.Ref.ToString());
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
                var nParRow = new NParallelTextRow(TextId, Rows.Select(r => r.Refs.ToList()).ToArray())
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
