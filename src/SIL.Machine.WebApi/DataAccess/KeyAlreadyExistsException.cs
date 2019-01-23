using System;

namespace SIL.Machine.WebApi.DataAccess
{
	public class KeyAlreadyExistsException : Exception
	{
		public KeyAlreadyExistsException(string message)
			: base(message)
		{
		}
	}
}
