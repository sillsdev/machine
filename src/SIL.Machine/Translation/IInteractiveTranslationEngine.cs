using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationEngine : ITranslationEngine
	{
		WordGraph GetWordGraph(IReadOnlyList<string> segment);

		void TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
	}
}
