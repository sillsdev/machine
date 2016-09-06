using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public enum AlignmentType
	{
		Unknown = -1,
		NotAligned = 0,
		Aligned = 1
	}

	public class WordAlignmentMatrix
	{
		private readonly AlignmentType[,] _matrix;

		public WordAlignmentMatrix(int i, int j, AlignmentType defaultValue = AlignmentType.NotAligned)
		{
			_matrix = new AlignmentType[i, j];
			if (defaultValue != AlignmentType.NotAligned)
				SetAll(defaultValue);
		}

		public int I => _matrix.GetLength(0);

		public int J => _matrix.GetLength(1);

		public void SetAll(AlignmentType value)
		{
			for (int i = 0; i < I; i++)
			{
				for (int j = 0; j < J; j++)
					_matrix[i, j] = value;
			}
		}

		public AlignmentType this[int i, int j]
		{
			get { return _matrix[i, j]; }
			set { _matrix[i, j] = value; }
		}

		public AlignmentType IsIAligned(int i)
		{
			for (int j = 0; j < J; j++)
			{
				if (_matrix[i, j] == AlignmentType.Aligned)
					return AlignmentType.Aligned;
				if (_matrix[i, j] == AlignmentType.Unknown)
					return AlignmentType.Unknown;
			}
			return AlignmentType.NotAligned;
		}

		public AlignmentType IsJAligned(int j)
		{
			for (int i = 0; i < I; i++)
			{
				if (_matrix[i, j] == AlignmentType.Aligned)
					return AlignmentType.Aligned;
				if (_matrix[i, j] == AlignmentType.Unknown)
					return AlignmentType.Unknown;
			}
			return AlignmentType.NotAligned;
		}

		public string ToGizaFormat(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("{0}\n", string.Join(" ", targetSegment));

			int i = 0;
			foreach (string sourceWord in "NULL".ToEnumerable().Concat(sourceSegment))
			{
				if (i > 0)
					sb.Append(" ");
				sb.Append(sourceWord);
				sb.Append(" ({ ");
				for (int j = 0; j < J; j++)
				{
					if (i == 0)
					{
						if (IsJAligned(j) == AlignmentType.NotAligned)
						{
							sb.Append(j + 1);
							sb.Append(" ");
						}
					}
					else if (_matrix[i - 1, j] == AlignmentType.Aligned)
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

		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int i = I - 1; i >= 0; i--)
			{
				for (int j = 0; j < J; j++)
				{
					sb.Append(_matrix[i, j]);
					sb.Append(" ");
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}
