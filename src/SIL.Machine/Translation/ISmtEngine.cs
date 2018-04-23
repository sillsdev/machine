using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : ITranslationEngine
	{
		TranslationResult GetBestPhraseAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment);

		WordGraph GetWordGraph(IReadOnlyList<string> segment);

		void TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment,
			WordAlignmentMatrix matrix = null);
	}
}
