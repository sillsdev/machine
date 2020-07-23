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
		private readonly CommandArgument _engineArgument;
		private readonly CommandOption _corpusOption;
		private readonly CommandOption _wordTokenizerOption;
		private readonly CommandOption _includeOption;
		private readonly CommandOption _excludeOption;
		private readonly CommandOption _maxCorpusSizeOption;
		private readonly CommandOption _outputOption;
		private readonly CommandOption _quietOption;

		public TranslateCommand()
		{
			Name = "translate";
			Description = "Translates source segments using a trained engine.";

			_engineArgument = Argument("engine", "The translation engine directory or configuration file.");
			_corpusOption = Option("-c|--corpus <[type,]path>",
				"The corpus to translate.\nTypes: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			_wordTokenizerOption = Option("-t|--tokenizer <type>",
				"The word tokenizer type.\nTypes:  \"whitespace\" (default), \"latin\".",
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

			if (!_corpusOption.HasValue())
			{
				Out.WriteLine("The source corpus was not specified.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateTextCorpusOption(_corpusOption.Value(), out string corpusType,
				out string corpusPath))
			{
				Out.WriteLine("The specified source corpus is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_wordTokenizerOption.Value(), false))
			{
				Out.WriteLine("The specified source word tokenizer type is invalid.");
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


			string engineConfigFileName = TranslatorHelpers.GetEngineConfigFileName(_engineArgument.Value);
			string engineDirectory = Path.GetDirectoryName(engineConfigFileName);

			StringTokenizer wordTokenizer = TranslatorHelpers.CreateWordTokenizer(_wordTokenizerOption.Value());
			StringDetokenizer wordDetokenizer = TranslatorHelpers.CreateWordDetokenizer(_wordTokenizerOption.Value());

			ITextCorpus corpus = TranslatorHelpers.CreateTextCorpus(wordTokenizer, corpusType, corpusPath);

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

			int corpusCount = Math.Min(maxCorpusCount, corpus.GetSegments().Count(s => !s.IsEmpty));

			if (!_quietOption.HasValue())
				Out.Write("Translating... ");
			int segmentCount = 0;
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (IInteractiveSmtModel smtModel = new ThotSmtModel(engineConfigFileName))
			using (ISmtEngine engine = smtModel.CreateEngine())
			using (StreamWriter writer = isOutputFile ? new StreamWriter(_outputOption.Value()) : null)
			{
				progress?.Report(new ProgressStatus(segmentCount, corpusCount));
				foreach (IText text in corpus.Texts)
				{
					StreamWriter textWriter = isOutputFile ? writer
						: new StreamWriter(Path.Combine(_outputOption.Value(), text.Id.Trim('*') + ".txt"));
					try
					{
						foreach (TextSegment segment in text.Segments.Where(s => !s.IsEmpty))
						{
							TranslationResult translateResult = engine.Translate(
								segment.Segment.Preprocess(Preprocessors.Lowercase));
							string translation = wordDetokenizer.Detokenize(
								translateResult.RecaseTargetWords(segment.Segment));
							textWriter.WriteLine(translation);

							segmentCount++;
							progress?.Report(new ProgressStatus(segmentCount, corpusCount));
							if (segmentCount == corpusCount)
								break;
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
