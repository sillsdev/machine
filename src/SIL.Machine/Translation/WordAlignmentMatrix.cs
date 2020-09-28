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

		public bool IsNeighborAligned(int i, int j)
		{
			return IsHorizontalNeighborAligned(i, j) || IsVerticalNeighborAligned(i, j);
		}

		public bool IsHorizontalNeighborAligned(int i, int j)
		{
			if (j > 0 && _matrix[i, j - 1])
				return true;
			if (j < ColumnCount - 1 && _matrix[i, j + 1])
				return true;
			return false;
		}

		public bool IsVerticalNeighborAligned(int i, int j)
		{
			if (i > 0 && _matrix[i - 1, j])
				return true;
			if (i < RowCount - 1 && _matrix[i + 1, j])
				return true;
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
					if (!(_matrix[i, j] || other._matrix[i, j]))
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

		public void SymmetrizeWith(WordAlignmentMatrix other)
		{
			if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
				throw new ArgumentException("The matrices are not the same size.", nameof(other));

			WordAlignmentMatrix orig = Clone();
			IntersectWith(other);
			Func<int, int, bool> growCondition = (i, j) => IsNeighborAligned(i, j);
			Symmetrize(growCondition, orig, other);
		}

		public void PrioritySymmetrizeWith(WordAlignmentMatrix other)
		{
			if (RowCount != other.RowCount || ColumnCount != other.ColumnCount)
				throw new ArgumentException("The matrices are not the same size.", nameof(other));

			Func<int, int, bool> growCondition = (i, j) => IsNeighborAligned(i, j)
				&& !(IsHorizontalNeighborAligned(i, j) && IsVerticalNeighborAligned(i, j));
			Symmetrize(growCondition, this, other);
		}

		private void Symmetrize(Func<int, int, bool> growCondition, WordAlignmentMatrix orig, WordAlignmentMatrix other)
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
							if (!IsColumnAligned(j) && !IsRowAligned(i))
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
