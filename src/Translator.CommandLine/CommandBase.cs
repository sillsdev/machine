using McMaster.Extensions.CommandLineUtils;
using System;

namespace SIL.Machine.Translation
{
	public abstract class CommandBase : CommandLineApplication
	{
		protected CommandBase()
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
