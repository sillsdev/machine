using System;
using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine
{
	public class TranslationModelCommandSpec : ICommandSpec
	{
		private const string Hmm = "hmm";
		private const string Ibm1 = "ibm1";
		private const string Ibm2 = "ibm2";
		private const string FastAlign = "fast_align";

		private CommandArgument _modelArgument;
		private CommandOption _modelTypeOption;

		private string _modelDirectory;
		private string _modelConfigFileName;

		private string ModelType => _modelTypeOption.Value();

		public void AddParameters(CommandBase command)
		{
			_modelArgument = command.Argument("MODEL_PATH", "The translation model.").IsRequired();
			_modelTypeOption = command.Option("-mt|--model-type <MODEL_TYPE>",
				$"The word alignment model type.\nTypes: \"{Hmm}\" (default), \"{Ibm1}\", \"{Ibm2}\", \"{FastAlign}\".",
				CommandOptionType.SingleValue);
		}

		public bool Validate(TextWriter outWriter)
		{
			if (!ValidateModelTypeOption(_modelTypeOption.Value()))
			{
				outWriter.WriteLine("The specified model type is invalid.");
				return false;
			}

			_modelConfigFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
			_modelDirectory = Path.GetDirectoryName(_modelConfigFileName);

			return true;
		}

		public IInteractiveTranslationModel CreateModel()
		{
			switch (ModelType)
			{
				default:
				case Hmm:
					return CreateThotSmtModel<HmmWordAlignmentModel>();
				case Ibm1:
					return CreateThotSmtModel<Ibm1WordAlignmentModel>();
				case Ibm2:
					return CreateThotSmtModel<Ibm2WordAlignmentModel>();
				case FastAlign:
					return CreateThotSmtModel<FastAlignWordAlignmentModel>();
			}
		}

		public ITranslationModelTrainer CreateTrainer(ParallelTextCorpus corpus, int maxSize)
		{
			switch (ModelType)
			{
				default:
				case Hmm:
					return CreateThotSmtModelTrainer<HmmWordAlignmentModel>(corpus, maxSize);
				case Ibm1:
					return CreateThotSmtModelTrainer<Ibm1WordAlignmentModel>(corpus, maxSize);
				case Ibm2:
					return CreateThotSmtModelTrainer<Ibm2WordAlignmentModel>(corpus, maxSize);
				case FastAlign:
					return CreateThotSmtModelTrainer<FastAlignWordAlignmentModel>(corpus, maxSize);
			}
		}

		private void CreateConfigFile()
		{
			string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
				"default-smt.cfg");
			string text = File.ReadAllText(defaultConfigFileName);
			int emIters = 5;
			if (ModelType == FastAlign)
				emIters = 4;
			text = text.Replace("{em_iters}", $"{emIters}");
			File.WriteAllText(_modelConfigFileName, text);
		}

		private ITranslationModelTrainer CreateThotSmtModelTrainer<TAlignModel>(ParallelTextCorpus corpus, int maxSize)
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			if (!Directory.Exists(_modelDirectory))
				Directory.CreateDirectory(_modelDirectory);

			if (!File.Exists(_modelConfigFileName))
				CreateConfigFile();

			return new ThotSmtModelTrainer<TAlignModel>(_modelConfigFileName, TokenProcessors.Lowercase,
				TokenProcessors.Lowercase, corpus, maxSize);
		}

		private IInteractiveTranslationModel CreateThotSmtModel<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			return new ThotSmtModel<TAlignModel>(_modelConfigFileName);
		}

		private static bool ValidateModelTypeOption(string value)
		{
			var validTypes = new HashSet<string> { Hmm, Ibm1, Ibm2, FastAlign };
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);

		}
	}
}
