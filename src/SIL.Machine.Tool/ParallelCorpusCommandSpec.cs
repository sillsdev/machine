using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine;

public class ParallelCorpusCommandSpec : CorpusCommandSpecBase
{
    private CommandArgument _sourceArgument;
    private CommandArgument _targetArgument;
    private CommandOption _sourceFormatOption;
    private CommandOption _targetFormatOption;
    private CommandOption _alignmentsOption;
    private CommandOption _sourceWordTokenizerOption;
    private CommandOption _targetWordTokenizerOption;

    public bool SupportAlignmentsCorpus { get; set; } = true;
    public bool DefaultNullTokenizer { get; set; }
    public bool AllSourceRows { get; set; } = false;
    public bool AllTargetRows { get; set; } = false;

    public ITextCorpus SourceCorpus { get; private set; }
    public ITextCorpus TargetCorpus { get; private set; }
    public IAlignmentCorpus AlignmentCorpus { get; private set; }
    public IParallelTextCorpus ParallelCorpus { get; private set; }

    public override void AddParameters(CommandBase command)
    {
        _sourceArgument = command.Argument("SOURCE_PATH", "The source corpus.").IsRequired();
        _targetArgument = command.Argument("TARGET_PATH", "The target corpus.").IsRequired();

        _sourceFormatOption = command.Option(
            "-sf|--source-format <CORPUS_FORMAT>",
            "The source corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\", \"pt_m\".",
            CommandOptionType.SingleValue
        );
        _targetFormatOption = command.Option(
            "-tf|--target-format <CORPUS_FORMAT>",
            "The target corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\", \"pt_m\".",
            CommandOptionType.SingleValue
        );
        if (SupportAlignmentsCorpus)
        {
            _alignmentsOption = command.Option(
                "-a|--alignments <ALIGNMENTS_PATH>",
                "The partial alignments corpus.",
                CommandOptionType.SingleValue
            );
        }

        string typesStr = "Types: \"whitespace\" (default), \"latin\", \"zwsp\"";
        if (DefaultNullTokenizer)
            typesStr = "Types: \"none\" (default), \"whitespace\", \"latin\", \"zwsp\"";
        _sourceWordTokenizerOption = command.Option(
            "-st|--source-tokenizer <TOKENIZER>",
            $"The source word tokenizer.\n{typesStr}.",
            CommandOptionType.SingleValue
        );
        _targetWordTokenizerOption = command.Option(
            "-tt|--target-tokenizer <TOKENIZER>",
            $"The target word tokenizer.\n{typesStr}.",
            CommandOptionType.SingleValue
        );

        base.AddParameters(command);
    }

    public override bool Validate(TextWriter outWriter)
    {
        if (!base.Validate(outWriter))
            return false;

        if (!ToolHelpers.ValidateCorpusFormatOption(_sourceFormatOption.Value()))
        {
            outWriter.WriteLine("The specified source corpus format is invalid.");
            return false;
        }

        if (!ToolHelpers.ValidateCorpusFormatOption(_targetFormatOption.Value()))
        {
            outWriter.WriteLine("The specified target corpus format is invalid.");
            return false;
        }

        if (!ToolHelpers.ValidateWordTokenizerOption(_sourceWordTokenizerOption.Value(), DefaultNullTokenizer))
        {
            outWriter.WriteLine("The specified source word tokenizer is invalid.");
            return false;
        }

        if (!ToolHelpers.ValidateWordTokenizerOption(_targetWordTokenizerOption.Value(), DefaultNullTokenizer))
        {
            outWriter.WriteLine("The specified target word tokenizer is invalid.");
            return false;
        }

        SourceCorpus = ToolHelpers.CreateTextCorpus(_sourceFormatOption.Value() ?? "text", _sourceArgument.Value);
        TargetCorpus = ToolHelpers.CreateTextCorpus(_targetFormatOption.Value() ?? "text", _targetArgument.Value);
        AlignmentCorpus = null;
        if (_alignmentsOption != null && _alignmentsOption.HasValue())
            AlignmentCorpus = ToolHelpers.CreateAlignmentsCorpus("text", _alignmentsOption.Value());

        if (!AllSourceRows)
            SourceCorpus = FilterTextCorpus(SourceCorpus);
        if (!AllTargetRows)
            TargetCorpus = FilterTextCorpus(TargetCorpus);
        if (AlignmentCorpus != null)
            AlignmentCorpus = FilterAlignmentCorpus(AlignmentCorpus);

        string defaultTokenizerType = DefaultNullTokenizer ? "none" : "whitespace";
        IRangeTokenizer<string, int, string> sourceWordTokenizer = ToolHelpers.CreateWordTokenizer(
            _sourceWordTokenizerOption.Value() ?? defaultTokenizerType
        );
        SourceCorpus = SourceCorpus.Tokenize(sourceWordTokenizer).UnescapeSpaces();

        IRangeTokenizer<string, int, string> targetWordTokenizer = ToolHelpers.CreateWordTokenizer(
            _targetWordTokenizerOption.Value() ?? defaultTokenizerType
        );
        TargetCorpus = TargetCorpus.Tokenize(targetWordTokenizer).UnescapeSpaces();

        ParallelCorpus = SourceCorpus.AlignRows(TargetCorpus, AlignmentCorpus, AllSourceRows, AllTargetRows);
        return true;
    }
}
