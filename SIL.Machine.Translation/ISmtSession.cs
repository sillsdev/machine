using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtSession : IInteractiveTranslator, IDisposable
	{
		void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment);
	}
}
