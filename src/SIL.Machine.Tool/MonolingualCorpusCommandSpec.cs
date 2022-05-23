using System.Collections.Generic;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine
{
    public class MonolingualCorpusCommandSpec : CorpusCommandSpecBase
    {
        private CommandArgument _corpusArgument;
        private CommandOption _corpusFormatOption;
        private CommandOption _wordTokenizerOption;

        public ITextCorpus Corpus { get; private set; }

        public override void AddParameters(CommandBase command)
        {
            _corpusArgument = command.Argument("CORPUS_PATH", "The corpus.").IsRequired();
            _corpusFormatOption = command.Option(
                "-cf|--corpus-format <CORPUS_FORMAT>",
                "The corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
                CommandOptionType.SingleValue
            );
            _wordTokenizerOption = command.Option(
                "-t|--tokenizer <TOKENIZER>",
                "The word tokenizer.\nTypes: \"whitespace\" (default), \"latin\", \"zwsp\".",
                CommandOptionType.SingleValue
            );
            base.AddParameters(command);
        }

        public override bool Validate(TextWriter outWriter)
        {
            if (!base.Validate(outWriter))
                return false;

            if (!ToolHelpers.ValidateCorpusFormatOption(_corpusFormatOption.Value()))
            {
                outWriter.WriteLine("The specified corpus format is invalid.");
                return false;
            }

            if (!ToolHelpers.ValidateWordTokenizerOption(_wordTokenizerOption.Value(), false))
            {
                outWriter.WriteLine("The specified word tokenizer type is invalid.");
                return false;
            }

            Corpus = ToolHelpers.CreateTextCorpus(_corpusFormatOption.Value() ?? "text", _corpusArgument.Value);

            Corpus = FilterTextCorpus(Corpus);

            ITokenizer<string, int, string> wordTokenizer = ToolHelpers.CreateWordTokenizer(
                _wordTokenizerOption.Value() ?? "whitespace"
            );
            Corpus = Corpus.Tokenize(wordTokenizer).UnescapeSpaces();
            return true;
        }
    }
}
