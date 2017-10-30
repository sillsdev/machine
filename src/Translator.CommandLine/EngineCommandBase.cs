using McMaster.Extensions.CommandLineUtils;
using System.IO;

namespace SIL.Machine.Translation
{
	public abstract class EngineCommandBase : ParallelTextCorpusCommandBase
	{
		private readonly CommandArgument _engineArgument;

		public EngineCommandBase(bool supportAlignmentsCorpus)
			: base(supportAlignmentsCorpus)
		{
			_engineArgument = Argument("engine", "The translation engine directory.");
		}

		protected string EngineDirectory => _engineArgument.Value;
		protected string EngineConfigFileName => Path.Combine(EngineDirectory, "smt.cfg");
	}
}
