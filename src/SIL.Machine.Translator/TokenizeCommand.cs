using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
	public class TokenizeCommand : CommandBase
	{
		private readonly CommandOption _corpusOption;
		private readonly CommandOption _wordTokenizerOption;
		private readonly CommandOption _includeOption;
		private readonly CommandOption _excludeOption;
		private readonly CommandOption _maxCorpusSizeOption;
		private readonly CommandOption _outputOption;
		private readonly CommandOption _lowercaseOption;

		public TokenizeCommand()
		{
			Name = "tokenize";
			Description = "Tokenizes a text corpus.";

			_corpusOption = Option("-c|--corpus <[type,]path>",
				"The source corpus.\nTypes: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			_wordTokenizerOption = Option("-t|--tokenizer <type>",
				"The word tokenizer type.\nTypes: \"whitespace\" (default), \"latin\", \"zwsp\".",
				CommandOptionType.SingleValue);
			_includeOption = Option("-i|--include <texts>",
				"The texts to include.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_excludeOption = Option("-e|--exclude <texts>",
				"The texts to exclude.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_maxCorpusSizeOption = Option("-m|--max-size <size>", "The maximum corpus size.",
				CommandOptionType.SingleValue);
			_outputOption = Option("-o|--output <path>", "The output file.", CommandOptionType.SingleValue);
			_lowercaseOption = Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			if (!_corpusOption.HasValue())
			{
				Out.WriteLine("The corpus was not specified.");
				return 1;
			}

			if (!_outputOption.HasValue())
			{
				Out.WriteLine("The output file was not specified.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateTextCorpusOption(_corpusOption.Value(), out string corpusType,
				out string corpusPath))
			{
				Out.WriteLine("The specified corpus is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_wordTokenizerOption.Value(), false))
			{
				Out.WriteLine("The specified word tokenizer type is invalid.");
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

			ITokenizer<string, int, string> wordTokenizer = TranslatorHelpers.CreateWordTokenizer(
				_wordTokenizerOption.Value() ?? "whitespace");

			ITextCorpus corpus = TranslatorHelpers.CreateTextCorpus(wordTokenizer, corpusType ?? "text", corpusPath);

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

			int segmentCount = 0;
			var utf8Encoding = new UTF8Encoding(false);
			using (var outputWriter = new StreamWriter(_outputOption.Value(), false, utf8Encoding))
			{
				foreach (TextSegment segment in corpus.GetSegments())
				{
					ITokenProcessor preprocessor = TokenProcessors.Null;
					if (_lowercaseOption.HasValue())
						preprocessor = TokenProcessors.Lowercase;

					outputWriter.WriteLine(string.Join(" ", TokenProcessors.Pipeline(preprocessor,
						TokenProcessors.EscapeSpaces).Process(segment.Segment)));

					segmentCount++;
					if (segmentCount == maxCorpusCount)
						break;
				}
			}

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
