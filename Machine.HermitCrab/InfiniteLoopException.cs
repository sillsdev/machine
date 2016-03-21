using System;

namespace SIL.Machine.HermitCrab
{
	/// <summary>
	/// This exception is thrown when a rule is caught in an infinite loop.
	/// </summary>
	public class InfiniteLoopException : Exception
	{
		public InfiniteLoopException(string message)
			: base(message)
		{
		}
	}
}
