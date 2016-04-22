using System;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : IDisposable
	{
		ISmtSession StartSession();

		void SaveModels();

		ISegmentAligner SegmentAligner { get; }
	}
}
