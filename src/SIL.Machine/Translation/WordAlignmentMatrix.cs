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

		public WordAlignmentMatrix(int i, int j)
		{
			_matrix = new bool[i, j];
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
			get { return _matrix[i, j]; }
			set { _matrix[i, j] = value; }
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

		public bool IsDiagNeighborAligned(int i, int j)
		{
			foreach ((int di, int dj) in new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) })
			{
				if (GetSafe(i + di, j + dj))
					return true;
			}
			return false;
		}

		public bool IsHorizontalNeighborAligned(int i, int j)
		{
			foreach ((int di, int dj) in new[] { (0, 1), (0, -1) })
			{
				if (GetSafe(i + di, j + dj))
					return true;
			}
			return false;
		}

		public bool IsVerticalNeighborAligned(int i, int j)
		{
			foreach ((int di, int dj) in new[] { (1, 0), (-1, 0) })
			{
				if (GetSafe(i + di, j + dj))
					return true;
			}
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

		/// <summary>
		/// Implements the symmetrization method defined in "Improved Alignment Models for Statistical Machine
		/// Translation" (Och et al., 1999).
		/// </summary>
		public void SymmetrizeWith(WordAlignmentMatrix other)
		{
			if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
				throw new ArgumentException("The matrices are not the same size.", nameof(other));

			WordAlignmentMatrix orig = Clone();
			IntersectWith(other);
			bool IsBlockNeighorAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				|| IsVerticalNeighborAligned(i, j);
			OchGrow(IsBlockNeighorAligned, orig, other);
		}

		public void PrioritySymmetrizeWith(WordAlignmentMatrix other)
		{
			if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
				throw new ArgumentException("The matrices are not the same size.", nameof(other));

			bool IsPriorityBlockNeighborAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				^ IsVerticalNeighborAligned(i, j);
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
						if ((other._matrix[i, j] || orig._matrix[i, j]) && !_matrix[i, j])
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
			}
			while (added);
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

			bool IsBlockOrDiagNeighborAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				|| IsVerticalNeighborAligned(i, j);
			KoehnGrow(IsBlockOrDiagNeighborAligned, orig, other);
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

			bool IsBlockOrDiagNeighborAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				|| IsVerticalNeighborAligned(i, j) || IsDiagNeighborAligned(i, j);
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

			bool IsBlockOrDiagNeighborAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				|| IsVerticalNeighborAligned(i, j) || IsDiagNeighborAligned(i, j);
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

			bool IsBlockOrDiagNeighborAligned(int i, int j) => IsHorizontalNeighborAligned(i, j)
				|| IsVerticalNeighborAligned(i, j) || IsDiagNeighborAligned(i, j);
			KoehnGrow(IsBlockOrDiagNeighborAligned, orig, other);

			bool IsBothUnaligned(int i, int j) => !IsRowAligned(i) && !IsColumnAligned(j);
			Final(IsBothUnaligned, orig);
			Final(IsBothUnaligned, other);
		}

		private void KoehnGrow(Func<int, int, bool> growCondition, WordAlignmentMatrix orig,
			WordAlignmentMatrix other)
		{
			var p = new HashSet<(int, int)>();
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
				var added = new HashSet<(int, int)>();
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
			var newMatrix = new bool[ColumnCount, RowCount];
			for (int i = 0; i < RowCount; i++)
			{
				for (int j = 0; j < ColumnCount; j++)
					newMatrix[j, i] = _matrix[i, j];
			}
			_matrix = newMatrix;
		}

		private IReadOnlyCollection<AlignedWordPair> GetAlignedWordPairs(out IReadOnlyList<int> sourceIndices,
			out IReadOnlyList<int> targetIndices)
		{
			var source = new int[ColumnCount];
			int[] target = Enumerable.Repeat(-2, RowCount).ToArray();
			var wordPairs = new HashSet<AlignedWordPair>();
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

		public IReadOnlyCollection<AlignedWordPair> GetAlignedWordPairs()
		{
			return GetAlignedWordPairs(out _, out _);
		}

		public string ToGizaFormat(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}\n", string.Join(" ", targetSegment));

			var sourceWords = new List<string> {"NULL"};
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
		
		public IReadOnlyCollection<AlignedWordPair> GetAlignedWordPairs(IWordAlignmentModel model,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			var wordPairs = new HashSet<AlignedWordPair>();
			foreach (AlignedWordPair wordPair in GetAlignedWordPairs(out IReadOnlyList<int> sourceIndices,
				out IReadOnlyList<int> targetIndices))
			{
				string sourceWord = sourceSegment[wordPair.SourceIndex];
				string targetWord = targetSegment[wordPair.TargetIndex];
				wordPair.TranslationProbability = model.GetTranslationProbability(sourceWord, targetWord);

				int prevSourceIndex = wordPair.TargetIndex == 0 ? -1 : sourceIndices[wordPair.TargetIndex - 1];
				int prevTargetIndex = wordPair.SourceIndex == 0 ? -1 : targetIndices[wordPair.SourceIndex - 1];
				wordPair.AlignmentProbability = model.GetAlignmentProbability(sourceSegment.Count, prevSourceIndex,
					wordPair.SourceIndex, targetSegment.Count, prevTargetIndex, wordPair.TargetIndex);

				wordPairs.Add(wordPair);
			}
			return wordPairs;
		}

		public string ToString(IWordAlignmentModel model, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, bool includeProbs = true)
		{
			if (!includeProbs)
				return ToString();
			return string.Join(" ", GetAlignedWordPairs(model, sourceSegment, targetSegment)
				.Select(wp => wp.ToString()));
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
			return string.Join(" ", GetAlignedWordPairs().Select(wp => wp.ToString()));
		}

		public WordAlignmentMatrix Clone()
		{
			return new WordAlignmentMatrix(this);
		}
	}
}
