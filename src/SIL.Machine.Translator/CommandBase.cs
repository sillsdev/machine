using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine.Translation
{
	public abstract class CommandBase : CommandLineApplication
	{
		protected CommandBase()
		{
			UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;
			OnExecute(ExecuteCommand);
		}

		protected virtual int ExecuteCommand()
		{
			return 0;
		}
	}
}
