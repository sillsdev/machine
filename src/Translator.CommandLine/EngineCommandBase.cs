using McMaster.Extensions.CommandLineUtils;
using System.IO;

namespace SIL.Machine.Translation
{
	public abstract class EngineCommandBase : ParallelTextCorpusCommandBase
	{
		private readonly CommandArgument _engineArgument;

		public EngineCommandBase(bool supportAlignmentsCorpus)
			: base(supportAlignmentsCorpus, supportsNullTokenizer: false)
		{
			_engineArgument = Argument("engine", "The translation engine directory or configuration file.");
		}

		protected override int ExecuteCommand()
		{
			int result = base.ExecuteCommand();
			if (result != 0)
				return result;

			EngineConfigFileName = TranslatorHelpers.GetEngineConfigFileName(_engineArgument.Value);
			EngineDirectory = Path.GetDirectoryName(EngineConfigFileName);

			return 0;
		}

		protected string EngineDirectory { get; private set; }
		protected string EngineConfigFileName { get; private set; }
	}
}
