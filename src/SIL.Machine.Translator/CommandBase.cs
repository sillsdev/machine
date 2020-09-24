using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine.Translation
{
	public abstract class CommandBase : CommandLineApplication
	{
		private readonly HashSet<ICommandSpec> _specs;

		protected CommandBase()
		{
			_specs = new HashSet<ICommandSpec>();
			UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;
			OnExecute(ExecuteCommand);
		}

		protected virtual int ExecuteCommand()
		{
			foreach (ICommandSpec spec in _specs)
			{
				if (!spec.Validate(Out))
					return 1;
			}

			return 0;
		}

		protected void AddCommand(CommandBase command)
		{
			command.Parent = this;
			Commands.Add(command);
		}

		protected TSpec AddSpec<TSpec>(TSpec spec) where TSpec : ICommandSpec
		{
			spec.AddParameters(this);
			_specs.Add(spec);
			return spec;
		}
	}
}
