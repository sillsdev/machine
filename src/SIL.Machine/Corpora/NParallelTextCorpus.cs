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
        public IAlignmentCorpus AlignmentCorpus { get; set; }
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

            IEnumerator<AlignmentRow> alignmentEnumerator = null;
            IList<IEnumerator<TextRow>> enumeratedCorpora = new List<IEnumerator<TextRow>>();
            IEnumerable<NParallelTextRow> rows = new List<NParallelTextRow>() { };
            try
            {
                for (int i = 0; i < Corpora.Count; i++)
                {
                    var enumerator = Corpora[i].GetRows(filterTextIds).GetEnumerator();
                    enumeratedCorpora.Add(
                        new TextCorpusEnumerator(enumerator, Corpora[0].Versification, Corpora[i].Versification)
                    );
                }

                if (AlignmentCorpus != null)
                    alignmentEnumerator = AlignmentCorpus.GetRows(filterTextIds).GetEnumerator();
                rows = GetRows(enumeratedCorpora, alignmentEnumerator).ToList();
            }
            finally
            {
                foreach (IEnumerator<TextRow> enumerator in enumeratedCorpora)
                {
                    enumerator.Dispose();
                }
                alignmentEnumerator?.Dispose();
            }
            return rows;
        }

        private bool AllInRangeHaveSegments(IList<TextRow> rows)
        {
            return rows.All(r => (r.IsInRange && r.Segment.Count > 0) || (!r.IsInRange));
        }

        private IList<int> MinRefIndexes(IList<object> refs)
        {
            object minRef = refs[0];
            IList<int> minRefIndexes = new List<int>() { 0 };
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

        private IEnumerable<NParallelTextRow> GetRows(
            IList<IEnumerator<TextRow>> listOfEnumerators,
            IEnumerator<AlignmentRow> alignmentEnumerator
        )
        {
            {
                var rangeInfo = new NRangeInfo(N)
                {
                    Versifications = Corpora.Select(c => c.Versification).ToArray(),
                    RowRefComparer = RowRefComparer
                };

                bool[] completed = listOfEnumerators.Select(e => !e.MoveNext()).ToArray();

                while (!completed.All(c => c))
                {
                    IList<int> minRefIndexes;
                    IList<TextRow> currentRows = listOfEnumerators.Select(e => e.Current).ToArray();
                    try
                    {
                        minRefIndexes = MinRefIndexes(
                            currentRows
                                .Select(e =>
                                {
                                    if (e != null)
                                        return e.Ref;
                                    return null;
                                })
                                .ToArray()
                        );
                    }
                    catch (ArgumentException)
                    {
                        throw new CorpusAlignmentException(currentRows.Select(e => e.Ref.ToString()).ToArray());
                    }
                    var currentIncompleteRows = currentRows.Where((r, i) => !completed[i]).ToArray();
                    IList<int> nonMinRefIndexes = System.Linq.Enumerable.Range(0, N).Except(minRefIndexes).ToList();

                    if (minRefIndexes.Count < (N - completed.Count(c => c)) || completed.Where(c => !c).Count() == 1) //then there are some non-min refs or only one incomplete enumerator
                    {
                        IList<IEnumerator<TextRow>> minEnumerators = minRefIndexes
                            .Select(i => listOfEnumerators[i])
                            .ToList();
                        IList<IEnumerator<TextRow>> nonMinEnumerators = nonMinRefIndexes
                            .Select(i => listOfEnumerators[i])
                            .ToList();

                        if (
                            nonMinRefIndexes.Any(i => !AllRowsList[i])
                            && minRefIndexes.Where(i => !completed[i] && listOfEnumerators[i].Current.IsInRange).Any()
                        )
                        {
                            if (
                                rangeInfo.IsInRange
                                && nonMinEnumerators
                                    .Where(e => e.Current != null && e.Current.IsInRange && e.Current.Segment.Count > 0)
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
                                    currentRows.ToArray(),
                                    minRefIndexes.ToArray(),
                                    nonMinRefIndexes.ToArray(),
                                    forceInRange: minRefIndexes
                                        .Select(i =>
                                            nonMinEnumerators.All(e =>
                                                e.Current != null && e.Current.TextId == currentRows[i].TextId
                                            )
                                            && nonMinEnumerators
                                                .Where(e => e.Current != null)
                                                .Select(e => !e.Current.IsRangeStart && e.Current.IsInRange)
                                                .Any(b => b)
                                        )
                                        .ToList()
                                )
                            )
                            {
                                yield return row;
                            }
                        }
                        foreach (int i in minRefIndexes)
                        {
                            rangeInfo.Rows[i].SameRefRows.Add(listOfEnumerators[i].Current);
                            completed[i] = !listOfEnumerators[i].MoveNext();
                        }
                    }
                    else if (minRefIndexes.Count == (N - completed.Count(c => c)))
                    // the refs are all the same
                    {
                        int compareAlignmentCorpus = -1;
                        if (AlignmentCorpus != null)
                        {
                            do
                            {
                                try
                                {
                                    compareAlignmentCorpus = alignmentEnumerator.MoveNext()
                                        ? RowRefComparer.Compare(
                                            currentIncompleteRows[0].Ref,
                                            alignmentEnumerator.Current.Ref
                                        )
                                        : 1;
                                }
                                catch (ArgumentException)
                                {
                                    throw new CorpusAlignmentException(
                                        currentRows.Select(e => e.Ref.ToString()).ToArray()
                                    );
                                }
                            } while (compareAlignmentCorpus < 0);
                        }

                        if (
                            minRefIndexes
                                .Select(i =>
                                    listOfEnumerators[i].Current.IsInRange
                                    && minRefIndexes.All(j => j == i || !AllRowsList[j])
                                )
                                .Any(b => b)
                        )
                        {
                            if (rangeInfo.IsInRange && AllInRangeHaveSegments(currentIncompleteRows))
                            {
                                yield return rangeInfo.CreateRow();
                            }

                            for (int i = 0; i < rangeInfo.Rows.Count; i++)
                            {
                                rangeInfo.AddTextRow(currentRows[i], i);
                                rangeInfo.Rows[i].SameRefRows.Clear();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < rangeInfo.Rows.Count; i++)
                            {
                                for (int j = 0; j < rangeInfo.Rows.Count; j++)
                                {
                                    if (i == j || completed[i] || completed[j])
                                        continue;

                                    if (rangeInfo.CheckSameRefRows(rangeInfo.Rows[i].SameRefRows, currentRows[j]))
                                    {
                                        foreach (TextRow tr in rangeInfo.Rows[i].SameRefRows)
                                        {
                                            var textRows = new TextRow[N];
                                            textRows[i] = tr;
                                            textRows[j] = currentRows[j];
                                            foreach (NParallelTextRow r in CreateRows(rangeInfo, textRows))
                                            {
                                                yield return r;
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (
                                NParallelTextRow row in CreateRows(
                                    rangeInfo,
                                    currentIncompleteRows,
                                    alignedWordPairs: AlignmentCorpus != null && compareAlignmentCorpus == 0
                                        ? alignmentEnumerator.Current.AlignedWordPairs.ToArray()
                                        : null
                                )
                            )
                            {
                                yield return row;
                            }
                        }

                        for (int i = 0; i < rangeInfo.Rows.Count; i++)
                        {
                            rangeInfo.Rows[i].SameRefRows.Add(currentRows[i]);
                            completed[i] = !listOfEnumerators[i].MoveNext();
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

        private object[] CorrectVersification(object[] refs, int i)
        {
            if (Corpora.Any(c => c.Versification == null) || refs.Length == 0)
                return refs;
            return refs.Cast<ScriptureRef>()
                .Select(r => r.ChangeVersification(Corpora[i].Versification))
                .Cast<object>()
                .ToArray();
        }

        private IEnumerable<NParallelTextRow> CreateRows(
            NRangeInfo rangeInfo,
            IReadOnlyList<TextRow> rows,
            IReadOnlyList<bool> forceInRange = null,
            IReadOnlyList<AlignedWordPair> alignedWordPairs = null
        )
        {
            if (rangeInfo.IsInRange)
                yield return rangeInfo.CreateRow();

            if (rows.All(r => r == null))
                throw new ArgumentNullException("A corpus row must be specified.");

            object[] refRefs = new object[] { rows.Select(r => r?.Ref).First() };
            string textId = null;
            IList<object[]> refs = new List<object[]>();
            IList<TextRowFlags> flags = new List<TextRowFlags>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    textId = textId ?? rows[i]?.TextId;
                    refs.Add(
                        CorrectVersification(rows[i].Ref == null ? new object[] { } : new object[] { rows[i].Ref }, i)
                    );
                    flags.Add(rows[i].Flags);
                }
                else
                {
                    refs.Add(CorrectVersification(refRefs, i));
                    flags.Add(forceInRange != null && forceInRange[i] ? TextRowFlags.InRange : TextRowFlags.None);
                }
            }

            yield return new NParallelTextRow(textId, refs)
            {
                NSegments = rows.Select(r => r?.Segment ?? Array.Empty<string>()).ToArray(),
                NFlags = flags.ToReadOnlyList(),
                AlignedWordPairs = alignedWordPairs
            };
        }

        private IEnumerable<NParallelTextRow> CreateMinRefRows(
            NRangeInfo rangeInfo,
            IReadOnlyList<TextRow> currentRows,
            IReadOnlyList<int> minRefIndexes,
            IReadOnlyList<int> nonMinRefIndexes,
            IReadOnlyList<bool> forceInRange = null
        )
        {
            List<(IList<TextRow> Rows, int Index)> sameRefRowsPerIndex = nonMinRefIndexes
                .Select(i => (rangeInfo.Rows[i], i))
                .Select(pair => (pair.Item1.SameRefRows, pair.Item2))
                .ToList();

            List<int> alreadyYielded = new List<int>();

            foreach (int i in minRefIndexes)
            {
                TextRow textRow = currentRows[i];
                foreach ((IList<TextRow> sameRefRows, int j) in sameRefRowsPerIndex)
                {
                    if (i == j)
                        continue;
                    if (rangeInfo.CheckSameRefRows(sameRefRows, textRow))
                    {
                        alreadyYielded.Add(i);
                        foreach (TextRow sameRefRow in sameRefRows)
                        {
                            var textRows = new TextRow[N];
                            textRows[i] = textRow;
                            textRows[j] = sameRefRow;
                            foreach (
                                NParallelTextRow row in CreateRows(rangeInfo, textRows, forceInRange: forceInRange)
                            )
                            {
                                yield return row;
                            }
                        }
                    }
                }
            }
            foreach (int i in minRefIndexes.Where(i => AllRowsList[i]).Except(alreadyYielded))
            {
                TextRow textRow = currentRows[i];
                var textRows = new TextRow[N];
                textRows[i] = textRow;
                var forceCurrentInRange = new bool[N];
                forceCurrentInRange[i] = forceCurrentInRange[i];
                foreach (NParallelTextRow row in CreateRows(rangeInfo, textRows, forceCurrentInRange))
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
            public int N;
            public string TextId { get; set; } = "";
            public ScrVers[] Versifications { get; set; } = null;
            public IComparer<object> RowRefComparer { get; set; } = null;
            public List<RangeRow> Rows { get; }
            public bool IsInRange => Rows.Any(r => r.IsInRange);

            public NRangeInfo(int n)
            {
                N = n;
                Rows = new List<RangeRow>();
                for (int i = 0; i < N; i++)
                {
                    Rows.Add(new RangeRow());
                }
            }

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

            public void AddTextRow(TextRow row, int index)
            {
                if (N <= index)
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
                object[][] refs = new object[N][];
                IList<object> referenceRefs = Rows.Where(r => r.Refs.Count > 0).Select(r => r.Refs).FirstOrDefault();
                foreach (int i in System.Linq.Enumerable.Range(0, Rows.Count))
                {
                    var row = Rows[i];

                    if (Versifications.All(v => v != null) && row.Refs.Count() == 0)
                    {
                        refs[i] = referenceRefs
                            .ToArray()
                            .Cast<ScriptureRef>()
                            .Select(r => r.ChangeVersification(Versifications[i]))
                            .Cast<object>()
                            .ToArray();
                    }
                    else
                    {
                        refs[i] = row.Refs.ToArray();
                    }
                }
                var nParRow = new NParallelTextRow(TextId, refs)
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
                if (x == null && y != null)
                    return 1;
                if (x != null && y == null)
                    return -1;

                return Comparer<object>.Default.Compare(x, y);
            }
        }
    }
}
