using ManyConsole;

namespace SIL.HermitCrab
{
	internal class TracingCommand : ConsoleCommand
	{
		private readonly HCContext _context;

		public TracingCommand(HCContext context)
		{
			_context = context;

			IsCommand("tracing", "Turns tracing on and off");

			AllowsAnyAdditionalArguments("[on|off]");
			SkipsCommandSummaryBeforeRunning();
		}

		public override int Run(string[] remainingArguments)
		{
			if (remainingArguments.Length > 0)
				_context.Morpher.TraceManager.IsTracing = remainingArguments[0] == "on";

			_context.Out.WriteLine(_context.Morpher.TraceManager.IsTracing ? "Tracing is turned on." : "Tracing is turned off.");

			return 0;
		}
	}
}
