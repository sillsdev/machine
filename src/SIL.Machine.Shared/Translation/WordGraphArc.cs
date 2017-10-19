using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class WordGraphArc
	{
		public WordGraphArc(int prevState, int nextState, double score, IEnumerable<string> words,
			WordAlignmentMatrix alignment, IEnumerable<double> wordConfidences, int sourceStartIndex,
			int sourceEndIndex, bool isUnknown)
		{
			PrevState = prevState;
			NextState = nextState;
			Score = score;
			Words = words.ToArray();
			Alignment = alignment;
			WordConfidences = wordConfidences.ToArray();
			SourceStartIndex = sourceStartIndex;
			SourceEndIndex = sourceEndIndex;
			IsUnknown = isUnknown;
		}

		public int PrevState { get; }
		public int NextState { get; }
		public double Score { get; }
		public IReadOnlyList<string> Words { get; }
		public WordAlignmentMatrix Alignment { get; }
		public IReadOnlyList<double> WordConfidences { get; }
		public int SourceStartIndex { get; }
		public int SourceEndIndex { get; }
		public bool IsUnknown { get; }
	}
}
