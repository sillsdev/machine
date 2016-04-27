using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : IDisposable
	{
		ISmtSession StartSession();

		void SaveModels();

		ISegmentAligner SegmentAligner { get; }

		void Train(IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus);
	}
}
