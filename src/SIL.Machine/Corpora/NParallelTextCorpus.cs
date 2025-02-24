using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SIL.Extensions;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class NParallelTextCorpus : NParallelTextCorpusBase
    {
        public NParallelTextCorpus(IEnumerable<ITextCorpus> corpora, IComparer<object> rowRefComparer = null)
        {
            Corpora = corpora.ToImmutableArray();
            RowRefComparer = rowRefComparer ?? new DefaultRowRefComparer();
            AllRows = new bool[Corpora.Count]
                .Select(_ => false)
                .ToImmutableArray();
        }

        public override bool IsTokenized(int i) =>
            i < Corpora.Count ? Corpora[i].IsTokenized : throw new ArgumentOutOfRangeException(nameof(i));

        public override int N => Corpora.Count;
        public IReadOnlyList<bool> AllRows { get; set; }
        public override IReadOnlyList<ITextCorpus> Corpora { get; }
        public IComparer<object> RowRefComparer { get; }

        private HashSet<string> GetTextIdsFromCorpora()
        {
            HashSet<string> textIds = new HashSet<string>();
            HashSet<string> allRowsTextIds = new HashSet<string>();
            for (int i = 0; i < Corpora.Count; i++)
            {
                if (i == 0)
                    textIds.AddRange(Corpora[i].Texts.Select(t => t.Id));
                else
                    textIds.IntersectWith(Corpora[i].Texts.Select(t => t.Id));

                if (AllRows[i])
                    allRowsTextIds.AddRange(Corpora[i].Texts.Select(t => t.Id));
            }
            textIds.UnionWith(allRowsTextIds);
            return textIds;
        }

        public override IEnumerable<NParallelTextRow> GetRows(IEnumerable<string> textIds)
        {
            HashSet<string> filterTextIds = GetTextIdsFromCorpora();

            if (textIds != null)
                filterTextIds.IntersectWith(textIds);

            List<IEnumerator<TextRow>> enumeratedCorpora = new List<IEnumerator<TextRow>>();
            try
            {
                for (int i = 0; i < Corpora.Count; i++)
                {
                    IEnumerator<TextRow> enumerator = Corpora[i].GetRows(filterTextIds).GetEnumerator();
                    enumeratedCorpora.Add(
                        new TextCorpusEnumerator(enumerator, Corpora[0].Versification, Corpora[i].Versification)
                    );
                }
                foreach (NParallelTextRow row in GetRows(enumeratedCorpora))
                    yield return row;
            }
            finally
            {
                foreach (IEnumerator<TextRow> enumerator in enumeratedCorpora)
                    enumerator.Dispose();
            }
        }

        private static bool AllInRangeHaveSegments(IList<TextRow> rows)
        {
            return rows.All(r => (r.IsInRange && !r.IsEmpty) || (!r.IsInRange));
        }

        private IList<int> MinRefIndexes(IList<object> refs)
        {
            object minRef = refs[0];
            List<int> minRefIndexes = new List<int>() { 0 };
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
            var rangeInfo = new NRangeInfo(N)
            {
                Versifications = Corpora.Select(c => c.Versification).ToArray(),
                RowRefComparer = RowRefComparer
            };
            List<List<TextRow>> sameRefRows = new List<List<TextRow>>();
            for (int i = 0; i < N; i++)
            {
                sameRefRows.Add(new List<TextRow>());
            }

            bool[] completed = new bool[N];
            int numCompleted = 0;
            for (int i = 0; i < N; i++)
            {
                bool isCompleted = !enumerators[i].MoveNext();
                completed[i] = isCompleted;
                if (isCompleted)
                    numCompleted++;
            }
            int numberOfRemainingRows = N - numCompleted;

            while (numCompleted < N)
            {
                List<int> minRefIndexes;
                List<TextRow> currentRows = enumerators.Select(e => e.Current).ToList();
                try
                {
                    minRefIndexes = MinRefIndexes(
                            currentRows
                                .Select(
                                    (e, i) =>
                                    {
                                        if (!completed[i])
                                            return e.Ref;
                                        return null;
                                    }
                                )
                                .ToArray()
                        )
                        .ToList();
                }
                catch (ArgumentException)
                {
                    throw new CorpusAlignmentException(currentRows.Select(e => e.Ref.ToString()).ToArray());
                }
                List<int> nonMinRefIndexes = Enumerable.Range(0, N).Except(minRefIndexes).ToList();
                if (minRefIndexes.Count < numberOfRemainingRows || minRefIndexes.Count(i => !completed[i]) == 1)
                //then there are some non-min refs or only one incomplete enumerator
                {
                    if (
                        nonMinRefIndexes.Any(i => !AllRows[i]) //At least one of the non-min rows has not been marked as 'all rows'
                        && minRefIndexes.Any(i => !completed[i] && currentRows[i].IsInRange) //and at least one of the min rows is not completed and in a range
                    )
                    {
                        foreach (int i in minRefIndexes)
                            rangeInfo.AddTextRow(enumerators[i].Current, i);
                        foreach (int i in nonMinRefIndexes)
                            sameRefRows[i].Clear();
                    }
                    else
                    {
                        bool anyNonMinEnumeratorsMidRange = nonMinRefIndexes.Any(i =>
                            !completed[i] && !currentRows[i].IsRangeStart && currentRows[i].IsInRange
                        );
                        foreach (
                            NParallelTextRow row in CreateMinRefRows(
                                rangeInfo,
                                currentRows.ToArray(),
                                minRefIndexes.ToArray(),
                                nonMinRefIndexes.ToArray(),
                                sameRefRows,
                                forceInRange: minRefIndexes
                                    .Select(i =>
                                        anyNonMinEnumeratorsMidRange
                                        && nonMinRefIndexes.All(j =>
                                            !completed[j] && currentRows[j].TextId == currentRows[i].TextId
                                        ) //All non-min rows have the same textId as the given min row
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
                        if (completed[i])
                            continue;
                        sameRefRows[i].Add(enumerators[i].Current);
                        bool isCompleted = !enumerators[i].MoveNext();
                        completed[i] = isCompleted;
                        if (isCompleted)
                        {
                            numCompleted++;
                            numberOfRemainingRows--;
                        }
                    }
                }
                else if (minRefIndexes.Count == numberOfRemainingRows)
                // the refs are all the same
                {
                    if (
                        minRefIndexes.Any(i =>
                            currentRows[i].IsInRange && minRefIndexes.All(j => j == i || !AllRows[j])
                        ) //At least one row is in range while the other rows are all not marked as 'all rows'
                    )
                    {
                        if (
                            rangeInfo.IsInRange
                            && AllInRangeHaveSegments(currentRows.Where((r, i) => !completed[i]).ToArray())
                        )
                        {
                            yield return rangeInfo.CreateRow();
                        }

                        for (int i = 0; i < rangeInfo.Rows.Count; i++)
                        {
                            if (completed[i])
                                continue;
                            rangeInfo.AddTextRow(currentRows[i], i);
                            sameRefRows[i].Clear();
                        }
                    }
                    else
                    {
                        foreach (
                            NParallelTextRow row in CreateSameRefRows(rangeInfo, completed, currentRows, sameRefRows)
                        )
                        {
                            yield return row;
                        }

                        foreach (
                            NParallelTextRow row in CreateRows(
                                rangeInfo,
                                currentRows.Select((r, i) => completed[i] ? null : r).ToArray()
                            )
                        )
                        {
                            yield return row;
                        }
                    }

                    for (int i = 0; i < rangeInfo.Rows.Count; i++)
                    {
                        if (completed[i])
                            continue;
                        sameRefRows[i].Add(currentRows[i]);
                        bool isCompleted = !enumerators[i].MoveNext();
                        completed[i] = isCompleted;
                        if (isCompleted)
                        {
                            numCompleted++;
                            numberOfRemainingRows--;
                        }
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
            IReadOnlyList<bool> forceInRange = null
        )
        {
            if (rangeInfo.IsInRange)
                yield return rangeInfo.CreateRow();

            if (rows.All(r => r == null))
                throw new ArgumentNullException("A corpus row must be specified.");

            object[] defaultRefs = new object[] { rows.Where(r => r != null).Select(r => r.Ref).First() };
            string textId = null;
            object[][] refs = new object[N][];
            TextRowFlags[] flags = new TextRowFlags[N];
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    textId = textId ?? rows[i]?.TextId;
                    refs[i] = CorrectVersification(rows[i].Ref == null ? defaultRefs : new object[] { rows[i].Ref }, i);
                    flags[i] = rows[i].Flags;
                }
                else
                {
                    if (Corpora[i].IsScripture())
                        refs[i] = CorrectVersification(defaultRefs, i);
                    else
                        refs[i] = new object[] { };
                    flags[i] = forceInRange != null && forceInRange[i] ? TextRowFlags.InRange : TextRowFlags.None;
                }
            }
            refs = refs.Select(r => r ?? (new object[] { })).ToArray();

            yield return new NParallelTextRow(textId, refs)
            {
                NSegments = rows.Select(r => r?.Segment ?? Array.Empty<string>()).ToArray(),
                NFlags = flags.ToReadOnlyList()
            };
        }

        private IEnumerable<NParallelTextRow> CreateMinRefRows(
            NRangeInfo rangeInfo,
            IReadOnlyList<TextRow> currentRows,
            IReadOnlyList<int> minRefIndexes,
            IReadOnlyList<int> nonMinRefIndexes,
            IReadOnlyList<IList<TextRow>> sameRefRowsPerIndex,
            IReadOnlyList<bool> forceInRange = null
        )
        {
            HashSet<int> alreadyYielded = new HashSet<int>();
            TextRow[] textRows;
            foreach (int i in minRefIndexes)
            {
                TextRow textRow = currentRows[i];
                foreach (int j in nonMinRefIndexes)
                {
                    IList<TextRow> sameRefRows = sameRefRowsPerIndex[j];
                    if (CheckSameRefRows(sameRefRows, textRow))
                    {
                        alreadyYielded.Add(i);
                        foreach (TextRow sameRefRow in sameRefRows)
                        {
                            textRows = new TextRow[N];
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
            textRows = new TextRow[N];
            var forceCurrentInRange = new bool[N];
            bool rowsHaveContent = false;
            foreach (int i in minRefIndexes.Where(i => AllRows[i]).Except(alreadyYielded))
            {
                TextRow textRow = currentRows[i];
                textRows[i] = textRow;
                forceCurrentInRange[i] = forceCurrentInRange[i];
                rowsHaveContent = true;
            }
            if (rowsHaveContent)
            {
                foreach (NParallelTextRow row in CreateRows(rangeInfo, textRows, forceCurrentInRange))
                {
                    yield return row;
                }
            }
        }

        private bool CheckSameRefRows(IList<TextRow> sameRefRows, TextRow otherRow)
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

        private IEnumerable<NParallelTextRow> CreateSameRefRows(
            NRangeInfo rangeInfo,
            IList<bool> completed,
            IList<TextRow> currentRows,
            IReadOnlyList<IList<TextRow>> sameRefRows
        )
        {
            for (int i = 0; i < N; i++)
            {
                if (completed[i])
                    continue;

                for (int j = 0; j < N; j++)
                {
                    if (i == j || completed[j])
                        continue;

                    if (CheckSameRefRows(sameRefRows[i], currentRows[j]))
                    {
                        foreach (TextRow tr in sameRefRows[i])
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
        }

        private class RangeRow
        {
            public IList<object> Refs { get; } = new List<object>();
            public IList<string> Segment { get; } = new List<string>();
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
                List<object> referenceRefs = Rows.Where(r => r.Refs.Count > 0)
                    .Select(r => r.Refs)
                    .FirstOrDefault()
                    .ToList();
                foreach (int i in Enumerable.Range(0, Rows.Count))
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

        public class DefaultRowRefComparer : IComparer<object>
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
