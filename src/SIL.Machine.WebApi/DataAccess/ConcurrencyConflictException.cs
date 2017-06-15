using System;

namespace SIL.Machine.WebApi.DataAccess
{
	public class ConcurrencyConflictException : Exception
	{
		public ConcurrencyConflictException(string message)
			: base(message)
		{
		}
	}
}
