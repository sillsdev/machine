using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public class AlignmentModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;

		public string ModelType => _modelTypeOption.Value();
		public string ModelPath => _modelArgument.Value;

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				"The word alignment model type.\nTypes: \"hmm\" (default), \"ibm1\", \"ibm2\", \"pt\", \"smt\".",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (!ValidateAlignmentModelTypeOption(_modelTypeOption.Value()))
			{
				outWriter.WriteLine("The specified word alignment model type is invalid.");
				return false;
			}

			return true;
		}

		private static bool ValidateAlignmentModelTypeOption(string value)
		{
			return string.IsNullOrEmpty(value) || value.IsOneOf("hmm", "ibm1", "ibm2", "pt", "smt");

		}
	}
}
