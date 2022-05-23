using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine
{
    public abstract class CommandBase : CommandLineApplication
    {
        private readonly HashSet<ICommandSpec> _specs;

        protected CommandBase()
        {
            _specs = new HashSet<ICommandSpec>();
            UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue;
            OnExecuteAsync(ExecuteCommandAsync);
        }

        protected virtual Task<int> ExecuteCommandAsync(CancellationToken ct)
        {
            foreach (ICommandSpec spec in _specs)
            {
                if (!spec.Validate(Out))
                    return Task.FromResult(1);
            }

            return Task.FromResult(0);
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
