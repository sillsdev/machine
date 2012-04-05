using System;
using System.Collections.Generic;
using ManyConsole;
using NDesk.Options;

namespace SIL.HermitCrab
{
	public class TracingCommand : ConsoleCommand
	{
		private readonly HCContext _context;
		private readonly List<string> _curRuleIDs;

		public TracingCommand(HCContext context)
		{
			_context = context;
			_curRuleIDs = new List<string>();

			IsCommand("tracing", "Turns tracing on and off");

			Options = new OptionSet
			          	{
			          		{"o|object=", "stratum|template|rule ID", value => _curRuleIDs.Add(value)}
			          	};


			AllowsAnyAdditionalArguments("[on|off]");
			SkipsCommandSummaryBeforeRunning();
		}

		public override int Run(string[] remainingArguments)
		{
			if (remainingArguments.Length > 0)
			{
				bool trace = remainingArguments[0] == "on";
				if (_curRuleIDs.Count > 0)
				{
					try
					{
						if (trace)
						{
							foreach (string ruleID in _curRuleIDs)
								_context.Morpher.TraceRules.Add(ruleID);
							_context.Morpher.TraceBlocking = true;
							_context.Morpher.TraceSuccess = true;
							_context.Morpher.TraceLexicalLookup = true;
						}
						else
						{
							foreach (string ruleID in _curRuleIDs)
								_context.Morpher.TraceRules.Remove(ruleID);
							if (_context.Morpher.TraceRules.Count == 0)
								_context.Morpher.TraceAll = false;
						}
					}
					catch (ArgumentException)
					{
						Console.WriteLine("One of the specified IDs is not valid.");
						_context.Morpher.TraceAll = false;
					}
					finally
					{
						_curRuleIDs.Clear();
					}
				}
				else
				{
					_context.Morpher.TraceAll = trace;
				}
			}
			else if (_curRuleIDs.Count > 0)
			{
				foreach (string id in _curRuleIDs)
					Console.WriteLine("Tracing is turned {0} for object {1}.", _context.Morpher.TraceRules.Contains(id) ? "on" : "off", id);
			}

			if (_context.Morpher.IsTracing)
			{
				if (_context.Morpher.TraceRules.IsTracingAllRules)
				{
					Console.WriteLine("Tracing is turned on for all objects.");
				}
				else
				{
					Console.WriteLine("Tracing is turned on for the following objects:");
					foreach (IHCRule rule in _context.Morpher.TraceRules)
					{
						Console.Write(rule.ID);
						if (rule.ID != rule.Description)
							Console.Write(" ({0})", rule.Description);
						Console.WriteLine();
					}
				}
			}
			else
			{
				Console.WriteLine("Tracing is turned off.");
			}

			return 0;
		}
	}
}
