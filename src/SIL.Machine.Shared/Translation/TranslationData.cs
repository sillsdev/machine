using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class TranslationData : IComparable<TranslationData>
	{
		public IList<string> Target { get; } = new List<string>();
		public IList<Tuple<int, int>> SourceSegmentation { get; } = new List<Tuple<int, int>>();
		public IList<int> TargetSegmentCuts { get; } = new List<int>();
		public ISet<int> TargetUnknownWords { get; } = new HashSet<int>();
		public double Score { get; set; }
		public IList<double> ScoreComponents { get; } = new List<double>();

		public int CompareTo(TranslationData other)
		{
			return -Score.CompareTo(other.Score);
		}
	}
}
