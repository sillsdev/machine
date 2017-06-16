using System;

namespace SIL.Machine.WebApi.DataAccess
{
	public class KeyAlreadyExistsException : Exception
	{
		public KeyAlreadyExistsException(string message)
			: base(message)
		{
		}

		public string IndexName { get; set; }
		public object Entity { get; set; }
	}
}
