using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class WordAlignmentMatrix : IValueEquatable<WordAlignmentMatrix>, ICloneable<WordAlignmentMatrix>
    {
        private bool[,] _matrix;

        public WordAlignmentMatrix(int rowCount, int columnCount, IEnumerable<(int, int)> setValues = null)
        {
            _matrix = new bool[rowCount, columnCount];
            if (setValues != null)
                foreach ((int i, int j) in setValues)
                    _matrix[i, j] = true;
        }

        private WordAlignmentMatrix(WordAlignmentMatrix other)
        {
            _matrix = new bool[other.RowCount, other.ColumnCount];
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                    _matrix[i, j] = other._matrix[i, j];
            }
        }

        public int RowCount => _matrix.GetLength(0);

        public int ColumnCount => _matrix.GetLength(1);

        public void SetAll(bool value)
        {
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                    _matrix[i, j] = value;
            }
        }

        public bool this[int i, int j]
        {
            get => _matrix[i, j];
            set => _matrix[i, j] = value;
        }

        public bool IsRowAligned(int i)
        {
            for (int j = 0; j < ColumnCount; j++)
            {
                if (_matrix[i, j])
                    return true;
            }
            return false;
        }

        public bool IsColumnAligned(int j)
        {
            for (int i = 0; i < RowCount; i++)
            {
                if (_matrix[i, j])
                    return true;
            }
            return false;
        }

        public IEnumerable<int> GetRowAlignedIndices(int i)
        {
            for (int j = 0; j < ColumnCount; j++)
            {
                if (_matrix[i, j])
                    yield return j;
            }
        }

        public IEnumerable<int> GetColumnAlignedIndices(int j)
        {
            for (int i = 0; i < RowCount; i++)
            {
                if (_matrix[i, j])
                    yield return i;
            }
        }

        public bool IsDiagonalNeighborAligned(int i, int j)
        {
            foreach ((int di, int dj) in new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) })
                if (GetSafe(i + di, j + dj))
                    return true;
            return false;
        }

        public bool IsHorizontalNeighborAligned(int i, int j)
        {
            foreach ((int di, int dj) in new[] { (0, 1), (0, -1) })
                if (GetSafe(i + di, j + dj))
                    return true;
            return false;
        }

        public bool IsVerticalNeighborAligned(int i, int j)
        {
            foreach ((int di, int dj) in new[] { (1, 0), (-1, 0) })
                if (GetSafe(i + di, j + dj))
                    return true;
            return false;
        }

        private bool GetSafe(int i, int j)
        {
            if (i >= 0 && j >= 0 && i < RowCount && j < ColumnCount)
                return _matrix[i, j];
            return false;
        }

        public void UnionWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (_matrix[i, j] || other._matrix[i, j])
                        _matrix[i, j] = true;
                }
            }
        }

        public void IntersectWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (!(_matrix[i, j] && other._matrix[i, j]))
                        _matrix[i, j] = false;
                }
            }
        }

        public void SymmetrizeWith(
            WordAlignmentMatrix other,
            SymmetrizationHeuristic heuristic = SymmetrizationHeuristic.Och
        )
        {
            switch (heuristic)
            {
                case SymmetrizationHeuristic.Union:
                    UnionWith(other);
                    break;
                case SymmetrizationHeuristic.Intersection:
                    IntersectWith(other);
                    break;
                case SymmetrizationHeuristic.Och:
                    OchSymmetrizeWith(other);
                    break;
                case SymmetrizationHeuristic.Grow:
                    GrowSymmetrizeWith(other);
                    break;
                case SymmetrizationHeuristic.GrowDiag:
                    GrowDiagSymmetrizeWith(other);
                    break;
                case SymmetrizationHeuristic.GrowDiagFinal:
                    GrowDiagFinalSymmetrizeWith(other);
                    break;
                case SymmetrizationHeuristic.GrowDiagFinalAnd:
                    GrowDiagFinalAndSymmetrizeWith(other);
                    break;
            }
        }

        /// <summary>
        /// Implements the symmetrization method defined in "Improved Alignment Models for Statistical Machine
        /// Translation" (Och et al., 1999).
        /// </summary>
        public void OchSymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            WordAlignmentMatrix orig = Clone();
            IntersectWith(other);
            bool IsBlockNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j);
            OchGrow(IsBlockNeighborAligned, orig, other);
        }

        public void PrioritySymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            bool IsPriorityBlockNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) ^ IsVerticalNeighborAligned(i, j);
            OchGrow(IsPriorityBlockNeighborAligned, this, other);
        }

        private void OchGrow(Func<int, int, bool> growCondition, WordAlignmentMatrix orig, WordAlignmentMatrix other)
        {
            bool added;
            do
            {
                added = false;
                for (int i = 0; i < RowCount; i++)
                {
                    for (int j = 0; j < ColumnCount; j++)
                    {
                        if ((other[i, j] || orig[i, j]) && !_matrix[i, j])
                        {
                            if (!IsRowAligned(i) && !IsColumnAligned(j))
                            {
                                _matrix[i, j] = true;
                                added = true;
                            }
                            else if (growCondition(i, j))
                            {
                                _matrix[i, j] = true;
                                added = true;
                            }
                        }
                    }
                }
            } while (added);
        }

        /// <summary>
        /// Implements the "base" method defined in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without
        /// final step.
        /// </summary>
        public void GrowSymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            WordAlignmentMatrix orig = Clone();
            IntersectWith(other);

            bool IsBlockNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j);
            KoehnGrow(IsBlockNeighborAligned, orig, other);
        }

        /// <summary>
        /// Implements the "diag" method defined in "Statistical Phrase-Based Translation" (Koehn et al., 2003) without
        /// final step.
        /// </summary>
        public void GrowDiagSymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            WordAlignmentMatrix orig = Clone();
            IntersectWith(other);

            bool IsBlockOrDiagNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j) || IsDiagonalNeighborAligned(i, j);
            KoehnGrow(IsBlockOrDiagNeighborAligned, orig, other);
        }

        /// <summary>
        /// Implements the "diag" method defined in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
        /// </summary>
        public void GrowDiagFinalSymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            WordAlignmentMatrix orig = Clone();
            IntersectWith(other);

            bool IsBlockOrDiagNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j) || IsDiagonalNeighborAligned(i, j);
            KoehnGrow(IsBlockOrDiagNeighborAligned, orig, other);

            bool IsOneOrBothUnaligned(int i, int j) => !IsRowAligned(i) || !IsColumnAligned(j);
            Final(IsOneOrBothUnaligned, orig);
            Final(IsOneOrBothUnaligned, other);
        }

        /// <summary>
        /// Implements the "diag-and" method defined in "Statistical Phrase-Based Translation" (Koehn et al., 2003).
        /// </summary>
        public void GrowDiagFinalAndSymmetrizeWith(WordAlignmentMatrix other)
        {
            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                throw new ArgumentException("The matrices are not the same size.", nameof(other));

            WordAlignmentMatrix orig = Clone();
            IntersectWith(other);

            bool IsBlockOrDiagNeighborAligned(int i, int j) =>
                IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j) || IsDiagonalNeighborAligned(i, j);
            KoehnGrow(IsBlockOrDiagNeighborAligned, orig, other);

            bool IsBothUnaligned(int i, int j) => !IsRowAligned(i) && !IsColumnAligned(j);
            Final(IsBothUnaligned, orig);
            Final(IsBothUnaligned, other);
        }

        private void KoehnGrow(Func<int, int, bool> growCondition, WordAlignmentMatrix orig, WordAlignmentMatrix other)
        {
            var p = new SortedSet<(int, int)>();
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if ((orig[i, j] || other[i, j]) && !_matrix[i, j])
                        p.Add((i, j));
                }
            }

            bool keepGoing = p.Count > 0;
            while (keepGoing)
            {
                keepGoing = false;
                var added = new SortedSet<(int, int)>();
                foreach ((int i, int j) in p)
                {
                    if ((!IsRowAligned(i) || !IsColumnAligned(j)) && growCondition(i, j))
                    {
                        _matrix[i, j] = true;
                        added.Add((i, j));
                        keepGoing = true;
                    }
                }
                p.ExceptWith(added);
            }
        }

        private void Final(Func<int, int, bool> pred, WordAlignmentMatrix adds)
        {
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (adds[i, j] && !this[i, j] && pred(i, j))
                        _matrix[i, j] = true;
                }
            }
        }

        public void Transpose()
        {
            bool[,] newMatrix = new bool[ColumnCount, RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                    newMatrix[j, i] = _matrix[i, j];
            }
            _matrix = newMatrix;
        }

        public IReadOnlyCollection<AlignedWordPair> GetAsymmetricAlignments(
            out IReadOnlyList<int> sourceIndices,
            out IReadOnlyList<int> targetIndices
        )
        {
            int[] source = new int[ColumnCount];
            int[] target = Enumerable.Repeat(-2, RowCount).ToArray();
            var wordPairs = new List<AlignedWordPair>();
            int prev = -1;
            for (int j = 0; j < ColumnCount; j++)
            {
                bool found = false;
                for (int i = 0; i < RowCount; i++)
                {
                    if (this[i, j])
                    {
                        if (!found)
                            source[j] = i;
                        if (target[i] == -2)
                            target[i] = j;
                        wordPairs.Add(new AlignedWordPair(i, j));
                        prev = i;
                        found = true;
                    }
                }

                // unaligned indices
                if (!found)
                    source[j] = prev == -1 ? -1 : RowCount + prev;
            }

            // all remaining target indices are unaligned, so fill them in
            prev = -1;
            for (int i = 0; i < RowCount; i++)
            {
                if (target[i] == -2)
                    target[i] = prev == -1 ? -1 : ColumnCount + prev;
                else
                    prev = target[i];
            }

            sourceIndices = source;
            targetIndices = target;
            return wordPairs;
        }

        public IReadOnlyCollection<AlignedWordPair> ToAlignedWordPairs(bool includeNull = false)
        {
            var wordPairs = new List<AlignedWordPair>();
            var nullAlignedTargetIndices = new HashSet<int>(Enumerable.Range(0, ColumnCount));
            for (int i = 0; i < RowCount; i++)
            {
                bool found = false;
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (this[i, j])
                    {
                        wordPairs.Add(new AlignedWordPair(i, j));
                        found = true;
                        nullAlignedTargetIndices.Remove(j);
                    }
                }

                // unaligned indices
                if (includeNull && !found)
                    wordPairs.Add(new AlignedWordPair(i, -1));
            }

            // all remaining target indices are unaligned, so fill them in
            if (includeNull && nullAlignedTargetIndices.Count > 0)
            {
                IEnumerable<AlignedWordPair> nullAlignedTargetWordPairs = nullAlignedTargetIndices
                    .OrderBy(j => j)
                    .Select(j => new AlignedWordPair(-1, j));
                wordPairs = nullAlignedTargetWordPairs.Concat(wordPairs).ToList();
            }

            return wordPairs;
        }

        public string ToGizaFormat(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}\n", string.Join(" ", targetSegment));

            var sourceWords = new List<string> { "NULL" };
            sourceWords.AddRange(sourceSegment);

            int i = 0;
            foreach (string sourceWord in sourceWords)
            {
                if (i > 0)
                    sb.Append(" ");
                sb.Append(sourceWord);
                sb.Append(" ({ ");
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (i == 0)
                    {
                        if (!IsColumnAligned(j))
                        {
                            sb.Append(j + 1);
                            sb.Append(" ");
                        }
                    }
                    else if (_matrix[i - 1, j])
                    {
                        sb.Append(j + 1);
                        sb.Append(" ");
                    }
                }

                sb.Append("})");
                i++;
            }
            sb.Append("\n");
            return sb.ToString();
        }

        public string ToString(
            IWordAlignmentModel model,
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool includeScores = true
        )
        {
            if (!includeScores)
                return ToString();

            IReadOnlyCollection<AlignedWordPair> wordPairs = ToAlignedWordPairs();
            model.ComputeAlignedWordPairScores(sourceSegment, targetSegment, wordPairs);
            return string.Join(" ", wordPairs.Select(wp => wp.ToString()));
        }

        public bool ValueEquals(WordAlignmentMatrix other)
        {
            if (other == null)
                return false;

            if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
                return false;

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (_matrix[i, j] != other._matrix[i, j])
                        return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            return string.Join(" ", ToAlignedWordPairs().Select(wp => wp.ToString()));
        }

        public WordAlignmentMatrix Clone()
        {
            return new WordAlignmentMatrix(this);
        }

        public void Resize(int rowCount, int columnCount)
        {
            if (rowCount == RowCount && columnCount == ColumnCount)
                return;

            bool[,] newMatrix = new bool[rowCount, columnCount];
            int minI = Math.Min(RowCount, rowCount);
            int minJ = Math.Min(ColumnCount, columnCount);

            for (int i = 0; i < minI; ++i)
                Array.Copy(_matrix, i * ColumnCount, newMatrix, i * columnCount, minJ);

            _matrix = newMatrix;
        }
    }
}
