using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class InteractiveTranslator
	{
		public InteractiveTranslator(IInteractiveTranslationEngine engine)
		{
			Engine = engine;
			ErrorCorrectionModel = new ErrorCorrectionModel();
		}

		public IInteractiveTranslationEngine Engine { get; }
		public ErrorCorrectionModel ErrorCorrectionModel { get; }

		public InteractiveTranslationSession StartSession(int n, IReadOnlyList<string> segment)
		{
			return new InteractiveTranslationSession(this, n, segment, Engine.GetWordGraph(segment));
		}
	}
}
