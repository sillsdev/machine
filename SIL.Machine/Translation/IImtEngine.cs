using System;

namespace SIL.Machine.Translation
{
	public interface IImtEngine : IDisposable
	{
		IImtSession StartSession();
	}
}
