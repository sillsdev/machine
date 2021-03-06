﻿using System.Collections.Generic;
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
		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;
		private CommandOption _smtModelTypeOption;
		private CommandOption _pluginOption;

		private IWordAlignmentModelFactory _modelFactory;

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The word alignment model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\", \"{ToolHelpers.Smt}\".",
				CommandOptionType.SingleValue);
			_smtModelTypeOption = command.Option("-smt|--smt-model-type <SMT_MODEL_TYPE>",
				$"The SMT model type.\nTypes: \"{ToolHelpers.Hmm}\" (default), \"{ToolHelpers.Ibm1}\", \"{ToolHelpers.Ibm2}\", \"{ToolHelpers.FastAlign}\".",
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

			if (!ToolHelpers.ValidateTranslationModelTypeOption(_smtModelTypeOption.Value()))
			{
				outWriter.WriteLine("The specified SMT model type is invalid.");
				return false;
			}

			if (factories.TryGetValue(_modelTypeOption.Value(), out IWordAlignmentModelFactory factory))
				_modelFactory = factory;

			return true;
		}

		public IWordAlignmentModel CreateAlignmentModel(
			WordAlignmentDirection direction = WordAlignmentDirection.Symmetric,
			SymmetrizationHeuristic symHeuristic = SymmetrizationHeuristic.Och)
		{
			if (_modelFactory != null)
				return _modelFactory.CreateModel(_modelArgument.Value, direction, symHeuristic);

			switch (_modelTypeOption.Value())
			{
				default:
				case ToolHelpers.Hmm:
					return CreateThotAlignmentModel<HmmWordAlignmentModel>(direction, symHeuristic);
				case ToolHelpers.Ibm1:
					return CreateThotAlignmentModel<Ibm1WordAlignmentModel>(direction, symHeuristic);
				case ToolHelpers.Ibm2:
					return CreateThotAlignmentModel<Ibm2WordAlignmentModel>(direction, symHeuristic);
				case ToolHelpers.FastAlign:
					return CreateThotAlignmentModel<FastAlignWordAlignmentModel>(direction, symHeuristic);
				case ToolHelpers.Smt:
					switch (_smtModelTypeOption.Value())
					{
						default:
						case ToolHelpers.Hmm:
							return CreateThotSmtAlignmentModel<HmmWordAlignmentModel>(direction);
						case ToolHelpers.Ibm1:
							return CreateThotSmtAlignmentModel<Ibm1WordAlignmentModel>(direction);
						case ToolHelpers.Ibm2:
							return CreateThotSmtAlignmentModel<Ibm2WordAlignmentModel>(direction);
						case ToolHelpers.FastAlign:
							return CreateThotSmtAlignmentModel<FastAlignWordAlignmentModel>(direction);
					}
			}
		}

		public bool IsSegmentInvalid(ParallelTextSegment segment)
		{
			return segment.IsEmpty || (_modelTypeOption.Value() == ToolHelpers.Smt
				&& segment.SourceSegment.Count > TranslationConstants.MaxSegmentLength);
		}

		public ITrainer CreateAlignmentModelTrainer(ParallelTextCorpus corpus, int maxSize, ITokenProcessor processor,
			Dictionary<string, string> parameters)
		{
			if (_modelFactory != null)
			{
				return _modelFactory.CreateTrainer(_modelArgument.Value, processor, processor, corpus, maxSize,
					parameters);
			}

			switch (_modelTypeOption.Value())
			{
				default:
				case ToolHelpers.Hmm:
					return CreateThotAlignmentModelTrainer<HmmWordAlignmentModel>(corpus, maxSize, processor,
						parameters);
				case ToolHelpers.Ibm1:
					return CreateThotAlignmentModelTrainer<Ibm1WordAlignmentModel>(corpus, maxSize, processor,
						parameters);
				case ToolHelpers.Ibm2:
					return CreateThotAlignmentModelTrainer<Ibm2WordAlignmentModel>(corpus, maxSize, processor,
						parameters);
				case ToolHelpers.FastAlign:
					return CreateThotAlignmentModelTrainer<FastAlignWordAlignmentModel>(corpus, maxSize, processor,
						parameters);
				case ToolHelpers.Smt:
					string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
					return ToolHelpers.CreateTranslationModelTrainer(_smtModelTypeOption.Value(), modelCfgFileName,
						corpus, maxSize, processor);
			}
		}

		private ITrainer CreateThotAlignmentModelTrainer<TAlignModel>(ParallelTextCorpus corpus, int maxSize,
			ITokenProcessor processor, Dictionary<string, string> parameters)
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = _modelArgument.Value;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");
			string modelDir = Path.GetDirectoryName(modelPath);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);

			int iters = -1;
			if (parameters.TryGetValue("iters", out string itersStr))
				iters = int.Parse(itersStr);

			var directTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_invswm", processor,
				processor, corpus, maxSize);
			if (iters != -1)
				directTrainer.TrainingIterationCount = iters;
			var inverseTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_swm", processor,
				processor, corpus.Invert(), maxSize);
			if (iters != -1)
				inverseTrainer.TrainingIterationCount = iters;
			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}

		private IWordAlignmentModel CreateThotAlignmentModel<TAlignModel>(WordAlignmentDirection direction,
			SymmetrizationHeuristic symHeuristic) where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = _modelArgument.Value;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");

			if (direction == WordAlignmentDirection.Direct)
			{
				var directModel = new TAlignModel();
				directModel.Load(modelPath + "_invswm");
				return directModel;
			}
			else if (direction == WordAlignmentDirection.Inverse)
			{
				var inverseModel = new TAlignModel();
				inverseModel.Load(modelPath + "_swm");
				return inverseModel;
			}
			else
			{
				var directModel = new TAlignModel();
				directModel.Load(modelPath + "_invswm");

				var inverseModel = new TAlignModel();
				inverseModel.Load(modelPath + "_swm");

				return new SymmetrizedWordAlignmentModel(directModel, inverseModel) { Heuristic = symHeuristic };
			}
		}

		private IWordAlignmentModel CreateThotSmtAlignmentModel<TAlignModel>(WordAlignmentDirection direction)
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
			string modelDir = Path.GetDirectoryName(modelCfgFileName);
			string alignModelPath = Path.Combine(modelDir, "src_trg");

			if (direction == WordAlignmentDirection.Direct)
			{
				var directModel = new TAlignModel();
				directModel.Load(alignModelPath + "_invswm");
				return directModel;
			}
			else if (direction == WordAlignmentDirection.Inverse)
			{
				var inverseModel = new TAlignModel();
				inverseModel.Load(alignModelPath + "_swm");
				return inverseModel;
			}
			else
			{
				return new ThotSmtWordAlignmentModel<TAlignModel>(modelCfgFileName);
			}
		}

		private static bool ValidateAlignmentModelTypeOption(string value, IEnumerable<string> pluginTypes)
		{
			var validTypes = new HashSet<string>
			{
				ToolHelpers.Hmm,
				ToolHelpers.Ibm1,
				ToolHelpers.Ibm2,
				ToolHelpers.FastAlign,
				ToolHelpers.Smt
			};
			validTypes.UnionWith(pluginTypes);
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);
		}
	}
}
