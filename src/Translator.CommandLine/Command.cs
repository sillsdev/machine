using McMaster.Extensions.CommandLineUtils;
using System;

namespace SIL.Machine.Translation
{
	public class Command : CommandLineApplication
	{
		public Command()
			: base(false)
		{
			OnExecute((Func<int>) ExecuteCommand);
		}

		protected virtual int ExecuteCommand()
		{
			return 0;
		}
	}
}
