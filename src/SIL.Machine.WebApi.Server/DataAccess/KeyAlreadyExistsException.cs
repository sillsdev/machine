using System;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class KeyAlreadyExistsException : Exception
	{
		public KeyAlreadyExistsException(string message)
			: base(message)
		{
		}
	}
}
