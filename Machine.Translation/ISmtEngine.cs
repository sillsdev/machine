using System;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : ISingleWordAlignmentModel, IDisposable
	{
		ISmtSession StartSession();

		void SaveModels();
	}
}
