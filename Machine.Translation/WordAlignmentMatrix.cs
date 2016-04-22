using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public class WordAlignmentMatrix
	{
		private readonly byte[,] _matrix;

		public WordAlignmentMatrix(int i, int j)
		{
			_matrix = new byte[i, j];
		}

		public int I
		{
			get { return _matrix.GetLength(0); }
		}

		public int J
		{
			get { return _matrix.GetLength(1); }
		}

		public bool this[int i, int j]
		{
			get { return _matrix[i, j] == 1; }
			set { _matrix[i, j] = (byte) (value ? 1 : 0); }
		}

		public bool IsJAligned(int j)
		{
			for (int i = 0; i < I; i++)
			{
				if (_matrix[i, j] != 0)
					return true;
			}
			return false;
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
						if (!IsJAligned(j))
						{
							sb.Append(j + 1);
							sb.Append(" ");
						}
					}
					else
					{
						if (_matrix[i - 1, j] != 0)
						{
							for (int n = 0; n < _matrix[i - 1, j]; n++)
							{
								sb.Append(j + 1);
								sb.Append(" ");
							}
						}
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
