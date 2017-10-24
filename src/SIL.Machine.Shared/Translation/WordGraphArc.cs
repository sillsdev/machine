using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class WordGraphArc
	{
		public WordGraphArc(int prevState, int nextState, double score, IEnumerable<string> words,
			WordAlignmentMatrix alignment, int sourceStartIndex, int sourceEndIndex, bool isUnknown,
			IEnumerable<double> wordConfidences = null)
		{
			PrevState = prevState;
			NextState = nextState;
			Score = score;
			Words = words.ToArray();
			Alignment = alignment;
			SourceStartIndex = sourceStartIndex;
			SourceEndIndex = sourceEndIndex;
			IsUnknown = isUnknown;
			if (wordConfidences == null)
				WordConfidences = Enumerable.Repeat(-1.0, Words.Count).ToList();
			else
				WordConfidences = wordConfidences.ToList();
		}

		public int PrevState { get; }
		public int NextState { get; }
		public double Score { get; }
		public IReadOnlyList<string> Words { get; }
		public WordAlignmentMatrix Alignment { get; }
		public IList<double> WordConfidences { get; }
		public int SourceStartIndex { get; }
		public int SourceEndIndex { get; }
		public bool IsUnknown { get; }
	}
}
