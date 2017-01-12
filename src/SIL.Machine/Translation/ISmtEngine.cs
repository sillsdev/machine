using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : ITranslationEngine
	{
		TranslationResult GetBestPhraseAlignment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment);

		WordGraph GetWordGraph(IEnumerable<string> segment);

		void TrainSegment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, WordAlignmentMatrix matrix = null);
	}
}
