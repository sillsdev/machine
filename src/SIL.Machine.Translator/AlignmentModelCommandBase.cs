using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine.Translation
{
	public class AlignmentModelCommandBase : ParallelTextCorpusCommandBase
	{
		private readonly CommandArgument _modelArgument;

		public AlignmentModelCommandBase(bool supportAlignmentsCorpus)
			: base(supportAlignmentsCorpus, defaultNullTokenizer: false)
		{
			_modelArgument = Argument("<[type,]path>",
				"The word alignment model.\nTypes: \"hmm\" (default), \"ibm1\", \"ibm2\", \"pt\".");
		}

		protected override int ExecuteCommand()
		{
			int result = base.ExecuteCommand();
			if (result != 0)
				return result;

			if (!TranslatorHelpers.ValidateAlignmentModelOption(_modelArgument.Value, out string modelType,
				out string modelPath))
			{
				Out.WriteLine("The specified source corpus is invalid.");
				return 1;
			}

			ModelType = modelType;
			ModelPath = modelPath;

			return 0;
		}

		protected string ModelType { get; private set; }
		protected string ModelPath { get; private set; }
	}
}
