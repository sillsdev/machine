using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace SIL.Machine
{
	public class TranslationModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;

		public string ModelDirectory { get; private set; }
		public string ModelConfigFileName { get; private set; }

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The translation model.").IsRequired();
		}

		public bool Validate(TextWriter outWriter)
		{
			ModelConfigFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
			ModelDirectory = Path.GetDirectoryName(ModelConfigFileName);

			return true;
		}
	}
}
