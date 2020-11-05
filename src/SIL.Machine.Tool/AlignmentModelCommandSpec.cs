using System;
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
	public class AlignmentModelCommandSpec : ICommandSpec
	{
		private const string Hmm = "hmm";
		private const string Ibm1 = "ibm1";
		private const string Ibm2 = "ibm2";
		private const string FastAlign = "fast_align";
		private const string Smt = "smt";

		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;
		private CommandOption _pluginOption;

		private IWordAlignmentModelFactory _modelFactory;

		private string ModelType => _modelTypeOption.Value();
		private string ModelPath => _modelArgument.Value;

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{Hmm}\" (default), \"{Ibm1}\", \"{Ibm2}\", \"{Smt}\", \"{FastAlign}\".",
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
			var factories = pluginLoader.Create<IWordAlignmentModelFactory>().ToDictionary(f => f.ModelType);

			if (!ValidateAlignmentModelTypeOption(_modelTypeOption.Value(), factories.Keys))
			{
				outWriter.WriteLine("The specified word alignment model type is invalid.");
				return false;
			}

			if (factories.TryGetValue(ModelType, out IWordAlignmentModelFactory factory))
				_modelFactory = factory;

			return true;
		}

		public IWordAlignmentModel CreateAlignmentModel()
		{
			if (_modelFactory != null)
				return _modelFactory.CreateModel(ModelPath);

			switch (ModelType)
			{
				default:
				case Hmm:
					return CreateThotAlignmentModel<HmmWordAlignmentModel>();
				case Ibm1:
					return CreateThotAlignmentModel<Ibm1WordAlignmentModel>();
				case Ibm2:
					return CreateThotAlignmentModel<Ibm2WordAlignmentModel>();
				case FastAlign:
					return CreateThotAlignmentModel<FastAlignWordAlignmentModel>();
				case Smt:
					string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(ModelPath);
					return new ThotSmtWordAlignmentModel(modelCfgFileName);
			}
		}

		public bool IsSegmentInvalid(ParallelTextSegment segment)
		{
			return segment.IsEmpty || (ModelType == Smt
				&& segment.SourceSegment.Count > TranslationConstants.MaxSegmentLength);
		}

		public ITrainer CreateAlignmentModelTrainer(ParallelTextCorpus corpus, int maxSize)
		{
			if (_modelFactory != null)
			{
				return _modelFactory.CreateTrainer(ModelPath, TokenProcessors.Lowercase, TokenProcessors.Lowercase,
					corpus, maxSize);
			}

			switch (ModelType)
			{
				default:
				case Hmm:
					return CreateThotAlignmentModelTrainer<HmmWordAlignmentModel>(corpus, maxSize);
				case Ibm1:
					return CreateThotAlignmentModelTrainer<Ibm1WordAlignmentModel>(corpus, maxSize);
				case Ibm2:
					return CreateThotAlignmentModelTrainer<Ibm2WordAlignmentModel>(corpus, maxSize);
				case FastAlign:
					return CreateThotAlignmentModelTrainer<FastAlignWordAlignmentModel>(corpus, maxSize);
				case Smt:
					string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(ModelPath);
					string modelDir = Path.GetDirectoryName(modelCfgFileName);
					if (!Directory.Exists(modelDir))
						Directory.CreateDirectory(modelDir);
					if (!File.Exists(modelCfgFileName))
					{
						string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
							"default-smt.cfg");
						File.Copy(defaultConfigFileName, modelCfgFileName);
					}
					return new ThotSmtModelTrainer(modelCfgFileName, TokenProcessors.Lowercase,
						TokenProcessors.Lowercase, corpus, maxSize);
			}
		}

		private ITrainer CreateThotAlignmentModelTrainer<TAlignModel>(ParallelTextCorpus corpus, int maxSize)
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = ModelPath;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");
			string modelDir = Path.GetDirectoryName(modelPath);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);
			var directTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_invswm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, corpus, maxSize);
			var inverseTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_swm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, corpus.Invert(), maxSize);
			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}

		private IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = ModelPath;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");

			var directModel = new TAlignModel();
			directModel.Load(modelPath + "_invswm");

			var inverseModel = new TAlignModel();
			inverseModel.Load(modelPath + "_swm");

			return new SymmetrizedWordAlignmentModel(directModel, inverseModel);
		}


		private static bool ValidateAlignmentModelTypeOption(string value, IEnumerable<string> pluginTypes)
		{
			var validTypes = new HashSet<string> { Hmm, Ibm1, Ibm2, Smt, FastAlign };
			validTypes.UnionWith(pluginTypes);
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);

		}
	}
}
