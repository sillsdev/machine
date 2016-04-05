using System;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : IDisposable
	{
		ThotSmtSession StartSession();

		void SaveModels();
	}
}
