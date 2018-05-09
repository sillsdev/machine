using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public partial class WordAlignmentMatrix : IValueEquatable<WordAlignmentMatrix>, ICloneable<WordAlignmentMatrix>
	{
		public IEnumerable<AlignedWordPair> GetAlignedWordPairs(IWordAlignmentModel model,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
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

				yield return wordPair;
			}
		}

		public string ToString(IWordAlignmentModel model, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			return string.Join(" ", GetAlignedWordPairs(model, sourceSegment, targetSegment)
				.Select(wp => wp.ToString()));
		}
	}
}
