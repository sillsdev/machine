using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveSmtSession : IInteractiveTranslationSession
	{
		void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment);
	}
}
