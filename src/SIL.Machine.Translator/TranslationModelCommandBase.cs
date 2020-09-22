using McMaster.Extensions.CommandLineUtils;
using System.IO;

namespace SIL.Machine.Translation
{
	public abstract class TranslationModelCommandBase : ParallelTextCorpusCommandBase
	{
		private readonly CommandArgument _modelArgument;

		public TranslationModelCommandBase(bool supportAlignmentsCorpus)
			: base(supportAlignmentsCorpus, defaultNullTokenizer: false)
		{
			_modelArgument = Argument("<path>", "The translation model directory or configuration file.");
		}

		protected override int ExecuteCommand()
		{
			int result = base.ExecuteCommand();
			if (result != 0)
				return result;

			ModelConfigFileName = TranslatorHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
			ModelDirectory = Path.GetDirectoryName(ModelConfigFileName);

			return 0;
		}

		protected string ModelDirectory { get; private set; }
		protected string ModelConfigFileName { get; private set; }
	}
}
