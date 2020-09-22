using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class TranslateCommand : CommandBase
	{
		private readonly CommandArgument _modelArgument;
		private readonly CommandOption _sourceOption;
		private readonly CommandOption _sourceWordTokenizerOption;
		private readonly CommandOption _targetWordTokenizerOption;
		private readonly CommandOption _includeOption;
		private readonly CommandOption _excludeOption;
		private readonly CommandOption _maxCorpusSizeOption;
		private readonly CommandOption _outputOption;
		private readonly CommandOption _quietOption;

		public TranslateCommand()
		{
			Name = "translate";
			Description = "Translates source segments using a trained engine.";

			_modelArgument = Argument("model", "The translation model directory or configuration file.");
			_sourceOption = Option("-s|--source <[type,]path>",
				"The source corpus to translate.\nTypes: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			const string typesStr = "Types: \"whitespace\" (default), \"latin\", \"zwsp\"";
			_sourceWordTokenizerOption = Option("-st|--source-tokenizer <type>",
				$"The source word tokenizer type.\n{typesStr}.",
				CommandOptionType.SingleValue);
			_targetWordTokenizerOption = Option("-tt|--target-tokenizer <type>",
				$"The target word tokenizer type.\n{typesStr}.",
				CommandOptionType.SingleValue);
			_includeOption = Option("-i|--include <texts>",
				"The texts to include.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_excludeOption = Option("-e|--exclude <texts>",
				"The texts to exclude.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_maxCorpusSizeOption = Option("-m|--max-size <size>", "The maximum corpus size.",
				CommandOptionType.SingleValue);
			_outputOption = Option("-o|--output <path>", "The output translations file/directory.",
				CommandOptionType.SingleValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int result = base.ExecuteCommand();
			if (result != 0)
				return result;

			if (!_sourceOption.HasValue())
			{
				Out.WriteLine("The source corpus was not specified.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateTextCorpusOption(_sourceOption.Value(), out string sourceCorpusType,
				out string sourceCorpusPath))
			{
				Out.WriteLine("The specified source corpus is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_sourceWordTokenizerOption.Value(),
				supportsNullTokenizer: false))
			{
				Out.WriteLine("The specified source word tokenizer type is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_targetWordTokenizerOption.Value(),
				supportsNullTokenizer: false))
			{
				Out.WriteLine("The specified target word tokenizer type is invalid.");
				return 1;
			}

			int maxCorpusCount = int.MaxValue;
			if (_maxCorpusSizeOption.HasValue())
			{
				if (!int.TryParse(_maxCorpusSizeOption.Value(), out int maxCorpusSize) || maxCorpusSize <= 0)
				{
					Out.WriteLine("The specified maximum corpus size is invalid.");
					return 1;
				}
				maxCorpusCount = maxCorpusSize;
			}

			if (!_outputOption.HasValue())
			{
				Out.WriteLine("The output translations file/directory was not specified");
				return 1;
			}

			bool isOutputFile;
			if (TranslatorHelpers.IsDirectoryPath(_outputOption.Value()))
			{
				Directory.CreateDirectory(_outputOption.Value());
				isOutputFile = false;
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_outputOption.Value()));
				isOutputFile = true;
			}


			string modelConfigFileName = TranslatorHelpers.GetTranslationModelConfigFileName(_modelArgument.Value);
			string modelDirectory = Path.GetDirectoryName(modelConfigFileName);

			ITokenizer<string, int, string> sourceWordTokenizer = TranslatorHelpers.CreateWordTokenizer(
				_sourceWordTokenizerOption.Value() ?? "whitespace");
			IDetokenizer<string, string> targetWordDetokenizer = TranslatorHelpers.CreateWordDetokenizer(
				_targetWordTokenizerOption.Value() ?? "whitespace");

			ITextCorpus corpus = TranslatorHelpers.CreateTextCorpus(sourceWordTokenizer, sourceCorpusType ?? "text",
				sourceCorpusPath);

			ISet<string> includeTexts = null;
			if (_includeOption.HasValue())
				includeTexts = TranslatorHelpers.GetTexts(_includeOption.Values);

			ISet<string> excludeTexts = null;
			if (_excludeOption.HasValue())
				excludeTexts = TranslatorHelpers.GetTexts(_excludeOption.Values);

			if (includeTexts != null || excludeTexts != null)
			{
				bool Filter(string id)
				{
					if (excludeTexts != null && excludeTexts.Contains(id))
						return false;

					if (includeTexts != null && includeTexts.Contains(id))
						return true;

					return includeTexts == null;
				}

				corpus = new FilteredTextCorpus(corpus, text => Filter(text.Id));
			}

			int corpusCount = Math.Min(maxCorpusCount, corpus.GetSegments()
				.Count(s => !s.IsEmpty && s.Segment.Count <= TranslationConstants.MaxSegmentLength));

			var truecaser = new TransferTruecaser();
			if (!_quietOption.HasValue())
				Out.Write("Translating... ");
			int segmentCount = 0;
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (IInteractiveTranslationModel model = new ThotSmtModel(modelConfigFileName))
			using (ITranslationEngine engine = model.CreateEngine())
			using (StreamWriter writer = isOutputFile ? new StreamWriter(_outputOption.Value()) : null)
			{
				progress?.Report(new ProgressStatus(segmentCount, corpusCount));
				foreach (IText text in corpus.Texts)
				{
					StreamWriter textWriter = isOutputFile ? writer
						: new StreamWriter(Path.Combine(_outputOption.Value(), text.Id.Trim('*') + ".txt"));
					try
					{
						foreach (TextSegment segment in text.Segments)
						{
							if (segment.IsEmpty || segment.Segment.Count > TranslationConstants.MaxSegmentLength)
							{
								textWriter.WriteLine();
							}
							else
							{
								TranslationResult translateResult = engine.Translate(
									TokenProcessors.Lowercase.Process(segment.Segment));
								translateResult = truecaser.Truecase(segment.Segment, translateResult);
								string translation = targetWordDetokenizer.Detokenize(translateResult.TargetSegment);
								textWriter.WriteLine(translation);

								segmentCount++;
								progress?.Report(new ProgressStatus(segmentCount, corpusCount));
								if (segmentCount == corpusCount)
									break;
							}
						}
						if (segmentCount == corpusCount)
							break;
					}
					finally
					{
						if (!isOutputFile)
							textWriter.Close();
					}
				}
			}
			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			return 0;
		}
	}
}
