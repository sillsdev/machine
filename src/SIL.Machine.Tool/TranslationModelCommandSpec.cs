using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Plugin;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine
{
	public class TranslationModelCommandSpec : ICommandSpec
	{
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;
		private CommandOption _pluginOption;

		private string _modelConfigFileName;
		private ITranslationModelFactory _modelFactory;

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The translation model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\".",
				CommandOptionType.SingleValue);
			_pluginOption = command.Option("-mp|--model-plugin <PLUGIN_FILE>", "The model plugin file.",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (_pluginOption.HasValue() && _pluginOption.Values.Any(p => !File.Exists(p)))
			{
				outWriter.WriteLine("A specified plugin file does not exist.");
				return false;
			}

			var pluginLoader = new PluginManager(_pluginOption.Values);
			var factories = pluginLoader.Create<ITranslationModelFactory>().ToDictionary(f => f.ModelType);

			if (!ToolHelpers.ValidateTranslationModelTypeOption(_modelTypeOption.Value()))
			{
				outWriter.WriteLine("The specified model type is invalid.");
				return false;
			}

			_modelConfigFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);

			if (factories.TryGetValue(_modelTypeOption.Value(), out ITranslationModelFactory factory))
				_modelFactory = factory;

			return true;
		}

		public ITranslationModel CreateModel()
		{
			if (_modelFactory != null)
				return _modelFactory.CreateModel(_modelArgument.Value);

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

		public ITranslationModelTrainer CreateTrainer(ParallelTextCorpus corpus, int maxSize, bool lowercase)
		{
			var processors = new List<ITokenProcessor> { TokenProcessors.Normalize };
			if (lowercase)
				processors.Add(TokenProcessors.Lowercase);
			ITokenProcessor processor = TokenProcessors.Pipeline(processors);

			if (_modelFactory != null)
				return _modelFactory.CreateTrainer(_modelArgument.Value, processor, processor, corpus, maxSize);

			return ToolHelpers.CreateTranslationModelTrainer(_modelTypeOption.Value(), _modelConfigFileName, corpus,
				maxSize, processor);
		}

		private IInteractiveTranslationModel CreateThotSmtModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			return new ThotSmtModel<TAlignModel>(_modelConfigFileName);
		}
	}
}
