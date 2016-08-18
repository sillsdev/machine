using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab
{
	internal class StatsCommand : ConsoleCommand
	{
		private readonly HCContext _context;
		private bool _reset;
		private bool _parse;
		private bool _test;

		public StatsCommand(HCContext context)
		{
			_context = context;

			IsCommand("stats", "Displays statistics on word parses and tests.");
			SkipsCommandSummaryBeforeRunning();
			HasOption("p|parse", "displays word parse statistics", o => _parse = true);
			HasOption("t|test", "displays test statistics", o => _test = true);
			HasOption("r|reset", "resets the gathered statistics", o => _reset = true);
		}

		public override int Run(string[] remainingArguments)
		{
			if (!_parse && !_test)
			{
				_parse = true;
				_test = true;
			}

			if (_reset)
			{
				if (_parse)
					_context.ResetParseStats();
				if (_test)
					_context.ResetTestStats();
			}
			else
			{
				if (_parse)
				{
					_context.Out.WriteLine("# of parses: {0}, successful: {1}, failed: {2}, error: {3}",
						_context.ParseCount, _context.SuccessfulParseCount, _context.FailedParseCount, _context.ErrorParseCount);
				}
				if (_test)
				{
					_context.Out.WriteLine("# of tests: {0}, passed: {1}, failed: {2}, error: {3}",
						_context.TestCount, _context.PassedTestCount, _context.FailedTestCount, _context.ErrorTestCount);
				}
			}

			_context.Out.WriteLine();

			ResetOptions();

			return 0;
		}

		private void ResetOptions()
		{
			_reset = false;
			_parse = false;
			_test = false;
		}
	}
}
