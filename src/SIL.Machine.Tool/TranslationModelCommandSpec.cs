using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine
{
	public class TranslationModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;

		private string _modelConfigFileName;

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The translation model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\".",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (!ToolHelpers.ValidateTranslationModelTypeOption(_modelTypeOption.Value()))
			{
				outWriter.WriteLine("The specified model type is invalid.");
				return false;
			}

			_modelConfigFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);

			return true;
		}

		public IInteractiveTranslationModel CreateModel()
		{
			switch (_modelTypeOption.Value())
			{
				default:
				case ToolHelpers.Hmm:
					return CreateThotSmtModel<HmmWordAlignmentModel>();
				case ToolHelpers.Ibm1:
					return CreateThotSmtModel<Ibm1WordAlignmentModel>();
				case ToolHelpers.Ibm2:
					return CreateThotSmtModel<Ibm2WordAlignmentModel>();
				case ToolHelpers.FastAlign:
					return CreateThotSmtModel<FastAlignWordAlignmentModel>();
			}
		}

		public ITranslationModelTrainer CreateTrainer(ParallelTextCorpus corpus, int maxSize)
		{
			return ToolHelpers.CreateTranslationModelTrainer(_modelTypeOption.Value(), _modelConfigFileName, corpus,
				maxSize);
		}

		private IInteractiveTranslationModel CreateThotSmtModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			return new ThotSmtModel<TAlignModel>(_modelConfigFileName);
		}
	}
}
