using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.SequenceAlignment
{
    public enum AlignmentMode
    {
        Global = 0,
        SemiGlobal,
        HalfLocal,
        Local
    }

    public delegate IEnumerable<TItem> ItemsSelector<in TSeq, out TItem>(
        TSeq sequence,
        out int startIndex,
        out int count
    );

    public class PairwiseAlignmentAlgorithm<TSeq, TItem>
    {
        private readonly int[,] _sim;
        private readonly IPairwiseAlignmentScorer<TSeq, TItem> _scorer;
        private readonly TSeq _sequence1;
        private readonly int _startIndex1;
        private readonly int _count1;
        private readonly TItem[] _items1;
        private readonly TSeq _sequence2;
        private readonly int _startIndex2;
        private readonly int _count2;
        private readonly TItem[] _items2;
        private int _bestRawScore = -1;
        private readonly int _gapPenalty;

        public PairwiseAlignmentAlgorithm(
            IPairwiseAlignmentScorer<TSeq, TItem> scorer,
            TSeq sequence1,
            TSeq sequence2,
            ItemsSelector<TSeq, TItem> itemsSelector
        )
        {
            _scorer = scorer;
            _sequence1 = sequence1;
            _items1 = itemsSelector(sequence1, out _startIndex1, out _count1).ToArray();
            _sequence2 = sequence2;
            _items2 = itemsSelector(sequence2, out _startIndex2, out _count2).ToArray();
            _sim = new int[_count1 + 1, _count2 + 1];
            _gapPenalty = scorer.GetGapPenalty(sequence1, sequence2);
        }

        public AlignmentMode Mode { get; set; }
        public bool ExpansionCompressionEnabled { get; set; }
        public bool TranspositionEnabled { get; set; }

        public int BestRawScore
        {
            get { return _bestRawScore; }
        }

        public void Compute()
        {
            int maxScore = int.MinValue;

            if (Mode == AlignmentMode.Global || Mode == AlignmentMode.HalfLocal)
            {
                for (int i = 1; i < _sim.GetLength(0); i++)
                {
                    _sim[i, 0] =
                        _sim[i - 1, 0]
                        + _gapPenalty
                        + _scorer.GetDeletionScore(_sequence1, Get1(i), _sequence2, default);
                }

                for (int j = 1; j < _sim.GetLength(1); j++)
                {
                    _sim[0, j] =
                        _sim[0, j - 1]
                        + _gapPenalty
                        + _scorer.GetInsertionScore(_sequence1, default, _sequence2, Get2(j));
                }
            }

            for (int i = 1; i < _sim.GetLength(0); i++)
            {
                for (int j = 1; j < _sim.GetLength(1); j++)
                {
                    int m1 =
                        _sim[i - 1, j]
                        + _gapPenalty
                        + _scorer.GetDeletionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    int m2 =
                        _sim[i, j - 1]
                        + _gapPenalty
                        + _scorer.GetInsertionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    int m3 =
                        _sim[i - 1, j - 1] + _scorer.GetSubstitutionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    int m4 =
                        !ExpansionCompressionEnabled || j - 2 < 0
                            ? int.MinValue
                            : _sim[i - 1, j - 2]
                                + _scorer.GetExpansionScore(_sequence1, Get1(i), _sequence2, Get2(j - 1), Get2(j));
                    int m5 =
                        !ExpansionCompressionEnabled || i - 2 < 0
                            ? int.MinValue
                            : _sim[i - 2, j - 1]
                                + _scorer.GetCompressionScore(_sequence1, Get1(i - 1), Get1(i), _sequence2, Get2(j));
                    int m6 =
                        !TranspositionEnabled || i - 2 < 0 || j - 2 < 0
                            ? int.MinValue
                            : _sim[i - 2, j - 2]
                                + _scorer.GetTranspositionScore(
                                    _sequence1,
                                    Get1(i - 1),
                                    Get1(i),
                                    _sequence2,
                                    Get2(j - 1),
                                    Get2(j)
                                );

                    if (Mode == AlignmentMode.Local)
                        _sim[i, j] = new[] { m1, m2, m3, m4, m5, m6, 0 }.Max();
                    else
                        _sim[i, j] = new[] { m1, m2, m3, m4, m5, m6 }.Max();

                    if (_sim[i, j] > maxScore)
                    {
                        if (Mode == AlignmentMode.SemiGlobal)
                        {
                            if (i == _sim.GetLength(0) - 1 || j == _sim.GetLength(1) - 1)
                                maxScore = _sim[i, j];
                        }
                        else
                            maxScore = _sim[i, j];
                    }
                }
            }
            _bestRawScore =
                Mode == AlignmentMode.Global || maxScore == int.MinValue
                    ? _sim[_sim.GetLength(0) - 1, _sim.GetLength(1) - 1]
                    : maxScore;
        }

        private TItem Get1(int i)
        {
            if (i == 0)
                return default;
            return _items1[_startIndex1 + i - 1];
        }

        private TItem Get2(int j)
        {
            if (j == 0)
                return default;
            return _items2[_startIndex2 + j - 1];
        }

        public IEnumerable<Alignment<TSeq, TItem>> GetAlignments()
        {
            return GetAlignments(_bestRawScore);
        }

        public IEnumerable<Alignment<TSeq, TItem>> GetAlignments(double scoreMargin)
        {
            return GetAlignments((int)(scoreMargin * _bestRawScore));
        }

        private IEnumerable<Alignment<TSeq, TItem>> GetAlignments(int threshold)
        {
            switch (Mode)
            {
                case AlignmentMode.Global:

                    {
                        foreach (
                            Alignment<TSeq, TItem> alignment in GetAlignments(
                                _sim.GetLength(0) - 1,
                                _sim.GetLength(1) - 1,
                                threshold
                            )
                        )
                            yield return alignment;
                    }
                    break;

                case AlignmentMode.SemiGlobal:

                    {
                        if (_sim.GetLength(0) == 1 && _sim.GetLength(1) == 1)
                            foreach (Alignment<TSeq, TItem> alignment in GetAlignments(0, 0, threshold))
                                yield return alignment;
                        else
                        {
                            for (int i = 1; i < _sim.GetLength(0); i++)
                            {
                                foreach (
                                    Alignment<TSeq, TItem> alignment in GetAlignments(
                                        i,
                                        _sim.GetLength(1) - 1,
                                        threshold
                                    )
                                )
                                    yield return alignment;
                            }

                            for (int j = 1; j < _sim.GetLength(1) - 1; j++)
                            {
                                foreach (
                                    Alignment<TSeq, TItem> alignment in GetAlignments(
                                        _sim.GetLength(0) - 1,
                                        j,
                                        threshold
                                    )
                                )
                                    yield return alignment;
                            }
                        }
                    }
                    break;

                case AlignmentMode.Local:
                case AlignmentMode.HalfLocal:

                    {
                        if (_sim.GetLength(0) == 1 && _sim.GetLength(1) == 1)
                            foreach (Alignment<TSeq, TItem> alignment in GetAlignments(0, 0, threshold))
                                yield return alignment;
                        else
                        {
                            for (int i = 1; i < _sim.GetLength(0); i++)
                            {
                                for (int j = 1; j < _sim.GetLength(1); j++)
                                {
                                    foreach (Alignment<TSeq, TItem> alignment in GetAlignments(i, j, threshold))
                                        yield return alignment;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private IEnumerable<Alignment<TSeq, TItem>> GetAlignments(int i, int j, int threshold)
        {
            if (_sim[i, j] < threshold)
                yield break;

            foreach (
                Tuple<List<AlignmentCell<TItem>>, List<AlignmentCell<TItem>>, int, int, int> alignment in Retrieve(
                    i,
                    j,
                    0,
                    threshold
                )
            )
            {
                int startIndex1 = alignment.Item3;
                int endIndex1 = _startIndex1 + i;
                int startIndex2 = alignment.Item4;
                int endIndex2 = _startIndex2 + j;

                yield return new Alignment<TSeq, TItem>(
                    alignment.Item5,
                    CalcNormalizedScore(startIndex1, endIndex1, startIndex2, endIndex2, alignment.Item5),
                    Tuple.Create(
                        _sequence1,
                        new AlignmentCell<TItem>(_items1.Take(startIndex1)),
                        (IEnumerable<AlignmentCell<TItem>>)alignment.Item1,
                        new AlignmentCell<TItem>(_items1.Skip(endIndex1))
                    ),
                    Tuple.Create(
                        _sequence2,
                        new AlignmentCell<TItem>(_items2.Take(startIndex2)),
                        (IEnumerable<AlignmentCell<TItem>>)alignment.Item2,
                        new AlignmentCell<TItem>(_items2.Skip(endIndex2))
                    )
                );
            }
        }

        private double CalcNormalizedScore(int startIndex1, int endIndex1, int startIndex2, int endIndex2, int score)
        {
            int maxScore = Math.Max(CalcMaxScore1(startIndex1, endIndex1), CalcMaxScore2(startIndex2, endIndex2));
            if (maxScore == 0)
                return 0;
            return Math.Max(0.0, Math.Min(1.0, (double)score / maxScore));
        }

        private int CalcMaxScore1(int startIndex, int endIndex)
        {
            int sum = 0;
            for (int i = _startIndex1; i < _count1; i++)
            {
                int score = _scorer.GetMaxScore1(_sequence1, _items1[i], _sequence2);
                sum += (i < startIndex || i >= endIndex) ? score / 2 : score;
            }
            return sum;
        }

        private int CalcMaxScore2(int startIndex, int endIndex)
        {
            int sum = 0;
            for (int j = _startIndex2; j < _count2; j++)
            {
                int score = _scorer.GetMaxScore2(_sequence1, _sequence2, _items2[j]);
                sum += (j < startIndex || j >= endIndex) ? score / 2 : score;
            }
            return sum;
        }

        private IEnumerable<Tuple<List<AlignmentCell<TItem>>, List<AlignmentCell<TItem>>, int, int, int>> Retrieve(
            int i,
            int j,
            int score,
            int threshold
        )
        {
            if ((Mode == AlignmentMode.Local || Mode == AlignmentMode.SemiGlobal) && (i == 0 || j == 0))
                yield return CreateAlignment(i, j, score);
            else if (i == 0 && j == 0)
                yield return CreateAlignment(i, j, score);
            else
            {
                int opScore;
                if (i != 0 && j != 0)
                {
                    opScore = _scorer.GetSubstitutionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    if (_sim[i - 1, j - 1] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i - 1, j - 1, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i)));
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j)));
                            yield return alignment;
                        }
                    }
                }

                if (j != 0)
                {
                    opScore = _gapPenalty + _scorer.GetInsertionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    if (i == 0 || _sim[i, j - 1] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i, j - 1, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>());
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j)));
                            yield return alignment;
                        }
                    }
                }

                if (ExpansionCompressionEnabled && i != 0 && j - 2 >= 0)
                {
                    opScore = _scorer.GetExpansionScore(_sequence1, Get1(i), _sequence2, Get2(j - 1), Get2(j));
                    if (_sim[i - 1, j - 2] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i - 1, j - 2, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i)));
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j - 1), Get2(j)));
                            yield return alignment;
                        }
                    }
                }

                if (i != 0)
                {
                    opScore = _gapPenalty + _scorer.GetDeletionScore(_sequence1, Get1(i), _sequence2, Get2(j));
                    if (j == 0 || _sim[i - 1, j] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i - 1, j, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i)));
                            alignment.Item2.Add(new AlignmentCell<TItem>());
                            yield return alignment;
                        }
                    }
                }

                if (ExpansionCompressionEnabled && i - 2 >= 0 && j != 0)
                {
                    opScore = _scorer.GetCompressionScore(_sequence1, Get1(i - 1), Get1(i), _sequence2, Get2(j));
                    if (_sim[i - 2, j - 1] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i - 2, j - 1, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i - 1), Get1(i)));
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j)));
                            yield return alignment;
                        }
                    }
                }

                if (TranspositionEnabled && i - 2 >= 0 && j - 2 >= 0)
                {
                    opScore = _scorer.GetTranspositionScore(
                        _sequence1,
                        Get1(i - 1),
                        Get1(i),
                        _sequence2,
                        Get2(j - 1),
                        Get2(j)
                    );
                    if (_sim[i - 2, j - 2] + opScore + score >= threshold)
                    {
                        foreach (
                            Tuple<
                                List<AlignmentCell<TItem>>,
                                List<AlignmentCell<TItem>>,
                                int,
                                int,
                                int
                            > alignment in Retrieve(i - 2, j - 2, score + opScore, threshold)
                        )
                        {
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i - 1)));
                            alignment.Item1.Add(new AlignmentCell<TItem>(Get1(i)));
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j)));
                            alignment.Item2.Add(new AlignmentCell<TItem>(Get2(j - 1)));
                            yield return alignment;
                        }
                    }
                }

                if (Mode == AlignmentMode.Local && _sim[i, j] == 0)
                    yield return CreateAlignment(i, j, score);
            }
        }

        private Tuple<List<AlignmentCell<TItem>>, List<AlignmentCell<TItem>>, int, int, int> CreateAlignment(
            int i,
            int j,
            int score
        )
        {
            return Tuple.Create(
                new List<AlignmentCell<TItem>>(),
                new List<AlignmentCell<TItem>>(),
                _startIndex1 + i,
                _startIndex2 + j,
                score
            );
        }
    }
}
