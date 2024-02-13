using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;

namespace SIL.Machine;

public class BuildCorpusCommand : CommandBase
{
    private readonly ParallelCorpusCommandSpec _corpusSpec;
    private readonly CommandOption _sourceOutputOption;
    private readonly CommandOption _targetOutputOption;
    private readonly CommandOption _refOutputOption;
    private readonly CommandOption _allSourceOption;
    private readonly CommandOption _allTargetOption;
    private readonly CommandOption _includeEmptyOption;
    private readonly CommandOption _mergeDuplicatesOption;
    private readonly PreprocessCommandSpec _preprocessSpec;

    public BuildCorpusCommand()
    {
        Name = "build-corpus";
        Description = "Builds a parallel corpus from source and target monolingual corpora.";

        _corpusSpec = AddSpec(
            new ParallelCorpusCommandSpec { SupportAlignmentsCorpus = false, DefaultNullTokenizer = true }
        );
        _sourceOutputOption = Option(
            "-so|--source-output <SOURCE_OUTPUT_FILE>",
            "The source output text file.",
            CommandOptionType.SingleValue
        );
        _targetOutputOption = Option(
            "-to|--target-output <TARGET_OUTPUT_FILE>",
            "The target output text file.",
            CommandOptionType.SingleValue
        );
        _refOutputOption = Option(
            "-ro|--ref-output <REF_OUTPUT_FILE>",
            "The segment reference output text file.",
            CommandOptionType.SingleValue
        );
        _allSourceOption = Option(
            "-as|--all-source",
            "Include all source segments. Overrides include/exclude options.",
            CommandOptionType.NoValue
        );
        _allTargetOption = Option(
            "-at|--all-target",
            "Include all target segments. Overrides include/exclude options.",
            CommandOptionType.NoValue
        );
        _includeEmptyOption = Option("-ie|--include-empty", "Include empty segments.", CommandOptionType.NoValue);
        _mergeDuplicatesOption = Option(
            "-md|--merge-duplicates",
            "Merge segments with the same reference.",
            CommandOptionType.NoValue
        );
        _preprocessSpec = AddSpec(new PreprocessCommandSpec { EscapeSpaces = true });
    }

    protected override async Task<int> ExecuteCommandAsync(CancellationToken cancellationToken)
    {
        _corpusSpec.AllSourceRows = _allSourceOption.HasValue();
        _corpusSpec.AllTargetRows = _allTargetOption.HasValue();

        int code = await base.ExecuteCommandAsync(cancellationToken);
        if (code != 0)
            return code;

        if (!_sourceOutputOption.HasValue() && !_targetOutputOption.HasValue() && !_refOutputOption.HasValue())
        {
            Out.WriteLine("An output file was not specified.");
            return 1;
        }

        int segmentCount = 0;
        using (
            StreamWriter sourceOutputWriter = _sourceOutputOption.HasValue()
                ? ToolHelpers.CreateStreamWriter(_sourceOutputOption.Value())
                : null
        )
        using (
            StreamWriter targetOutputWriter = _targetOutputOption.HasValue()
                ? ToolHelpers.CreateStreamWriter(_targetOutputOption.Value())
                : null
        )
        using (
            StreamWriter refOutputWriter = _refOutputOption.HasValue()
                ? ToolHelpers.CreateStreamWriter(_refOutputOption.Value())
                : null
        )
        {
            IParallelTextCorpus corpus = _corpusSpec.ParallelCorpus.Where(row =>
            {
                if (!_includeEmptyOption.HasValue())
                {
                    if (_allSourceOption.HasValue() && _allTargetOption.HasValue())
                    {
                        if (row.SourceSegment.Count == 0 && row.TargetSegment.Count == 0)
                            return false;
                    }
                    else if (_allSourceOption.HasValue())
                    {
                        if (row.SourceSegment.Count == 0)
                            return false;
                    }
                    else if (_allTargetOption.HasValue())
                    {
                        if (row.TargetSegment.Count == 0)
                            return false;
                    }
                    else if (row.IsEmpty)
                        return false;
                }
                return true;
            });
            corpus = _preprocessSpec.Preprocess(corpus);

            object curRef = null;
            var curSourceLine = new StringBuilder();
            bool curSourceLineRange = true;
            var curTargetLine = new StringBuilder();
            bool curTargetLineRange = true;
            foreach (ParallelTextRow row in corpus)
            {
                if (curRef != null && (!_mergeDuplicatesOption.HasValue() || !row.Ref.Equals(curRef)))
                {
                    sourceOutputWriter?.WriteLine(curSourceLineRange ? "<range>" : curSourceLine.ToString());
                    targetOutputWriter?.WriteLine(curTargetLineRange ? "<range>" : curTargetLine.ToString());
                    refOutputWriter?.WriteLine(curRef);
                    curSourceLine.Clear();
                    curSourceLineRange = true;
                    curTargetLine.Clear();
                    curTargetLineRange = true;
                    segmentCount++;
                    if (segmentCount == _corpusSpec.MaxCorpusCount)
                        break;
                }

                curRef = row.Ref;

                if (
                    sourceOutputWriter != null
                    && (!row.IsSourceInRange || row.IsSourceRangeStart || row.SourceSegment.Count > 0)
                )
                {
                    if (curSourceLine.Length > 0)
                        curSourceLine.Append(' ');
                    curSourceLine.Append(row.SourceText);
                    curSourceLineRange = false;
                }
                if (
                    targetOutputWriter != null
                    && (!row.IsTargetInRange || row.IsTargetRangeStart || row.TargetSegment.Count > 0)
                )
                {
                    if (curTargetLine.Length > 0)
                        curTargetLine.Append(' ');
                    curTargetLine.Append(row.TargetText);
                    curTargetLineRange = false;
                }
            }

            if (segmentCount < _corpusSpec.MaxCorpusCount)
            {
                sourceOutputWriter?.WriteLine(curSourceLineRange ? "<range>" : curSourceLine.ToString());
                targetOutputWriter?.WriteLine(curTargetLineRange ? "<range>" : curTargetLine.ToString());
                refOutputWriter?.WriteLine(curRef);
                segmentCount++;
            }
        }

        Out.WriteLine($"# of Segments written: {segmentCount}");

        return 0;
    }
}
