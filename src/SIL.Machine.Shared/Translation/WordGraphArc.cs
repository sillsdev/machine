using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class WordGraphArc
	{
		public WordGraphArc(int predStateIndex, int succStateIndex, double score, IEnumerable<string> words, int srcStartIndex, int srcEndIndex, bool isUnknown)
		{
			PredStateIndex = predStateIndex;
			SuccStateIndex = succStateIndex;
			Score = score;
			Words = words.ToArray();
			SrcStartIndex = srcStartIndex;
			SrcEndIndex = srcEndIndex;
			IsUnknown = isUnknown;
		}

		public int PredStateIndex { get; }
		public int SuccStateIndex { get; }
		public double Score { get; }
		public IReadOnlyList<string> Words { get; }
		public int SrcStartIndex { get; }
		public int SrcEndIndex { get; }
		public bool IsUnknown { get; }
	}
}
