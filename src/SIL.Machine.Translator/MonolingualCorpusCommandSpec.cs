using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
	public class MonolingualCorpusCommandSpec : CorpusCommandSpecBase
	{
		private CommandArgument _corpusArgument;
		private CommandOption _corpusFormatOption;
		private CommandOption _wordTokenizerOption;

		public ITextCorpus Corpus { get; set; }

		public override void AddParameters(CommandBase command)
		{
			_corpusArgument = command.Argument("CORPUS_PATH", "The corpus.").IsRequired();
			_corpusFormatOption = command.Option("-cf|--corpus-format <CORPUS_FORMAT>",
				"The corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			_wordTokenizerOption = command.Option("-t|--tokenizer <TOKENIZER>",
				"The word tokenizer.\nTypes: \"whitespace\" (default), \"latin\", \"zwsp\".",
				CommandOptionType.SingleValue);
			base.AddParameters(command);
		}

		public override bool Validate(TextWriter outWriter)
		{
			if (!base.Validate(outWriter))
				return false;

			if (!TranslatorHelpers.ValidateCorpusFormatOption(_corpusFormatOption.Value()))
			{
				outWriter.WriteLine("The specified corpus format is invalid.");
				return false;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_wordTokenizerOption.Value(), false))
			{
				outWriter.WriteLine("The specified word tokenizer type is invalid.");
				return false;
			}

			ITokenizer<string, int, string> wordTokenizer = TranslatorHelpers.CreateWordTokenizer(
				_wordTokenizerOption.Value() ?? "whitespace");

			Corpus = TranslatorHelpers.CreateTextCorpus(wordTokenizer,
				_corpusFormatOption.Value() ?? "text", _corpusArgument.Value);

			Corpus = FilterTextCorpus(Corpus);
			return true;
		}

		public int GetNonemptyCorpusCount()
		{
			return Math.Min(MaxCorpusCount, Corpus.GetSegments().Count(s => !s.IsEmpty));
		}

		public int GetCorpusCount()
		{
			return Math.Min(MaxCorpusCount, Corpus.GetSegments().Count());
		}
	}
}
