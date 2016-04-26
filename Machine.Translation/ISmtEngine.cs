using System;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : IDisposable
	{
		ISmtSession StartSession();

		void SaveModels();

		ISegmentAligner SegmentAligner { get; }

		void Train(IReadOnlyList<IEnumerable<string>> sourceCorpus, IReadOnlyList<IEnumerable<string>> targetCorpus);
	}
}
