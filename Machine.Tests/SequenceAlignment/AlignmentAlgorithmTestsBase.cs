using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Tests.SequenceAlignment
{
	[TestFixture]
	public abstract class AlignmentAlgorithmTestsBase
	{
		protected static IEnumerable<char> GetChars(string sequence, out int index, out int count)
		{
			index = 0;
			count = sequence.Length;
			return sequence;
		}

		protected static Alignment<string, char> CreateAlignment(params string[] alignment)
		{
			var sequences = new Tuple<string, AlignmentCell<char>, IEnumerable<AlignmentCell<char>>, AlignmentCell<char>>[alignment.GetLength(0)];
			for (int i = 0; i < alignment.Length; i++)
			{
				var sb = new StringBuilder();
				string[] split = alignment[i].Split('|');
				string prefix = split[0].Trim();
				sb.Append(prefix);

				string[] cellStrs = split[1].Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				var cells = new AlignmentCell<char>[cellStrs.Length];
				for (int j = 0; j < cellStrs.Length; j++)
				{
					if (cellStrs[j] == "-")
					{
						cells[j] = new AlignmentCell<char>();
					}
					else
					{
						sb.Append(cellStrs[j]);
						cells[j] = new AlignmentCell<char>(cellStrs[j]);
					}
				}

				string suffix = split[2].Trim();
				sb.Append(suffix);

				sequences[i] = Tuple.Create(sb.ToString(), new AlignmentCell<char>(prefix), (IEnumerable<AlignmentCell<char>>) cells, new AlignmentCell<char>(suffix));
			}
			return new Alignment<string, char>(0, 0, sequences);
		}

		protected static void AssertAlignmentsEqual(Alignment<string, char> actual, Alignment<string, char> expected)
		{
			Assert.That(actual.Sequences, Is.EqualTo(expected.Sequences));
			Assert.That(actual.Prefixes, Is.EqualTo(expected.Prefixes));
			Assert.That(actual.Suffixes, Is.EqualTo(expected.Suffixes));
			Assert.That(actual.ColumnCount, Is.EqualTo(expected.ColumnCount));
			for (int i = 0; i < expected.SequenceCount; i++)
			{
				for (int j = 0; j < expected.ColumnCount; j++)
					Assert.That(actual[i, j], Is.EqualTo(expected[i, j]));
			}
		}

		protected class ZeroMaxScoreStringScorer : StringScorer
		{
			public override int GetMaxScore1(string sequence1, char p, string sequence2)
			{
				return 0;
			}

			public override int GetMaxScore2(string sequence1, string sequence2, char q)
			{
				return 0;
			}
		}
	}
}
