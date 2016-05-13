using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtSession : IImtSession
	{
		void Train(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment);
	}
}
