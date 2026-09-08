using System.Diagnostics;
using System.IO;
using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Parses every word in a word list and records a per-word result signature plus elapsed time, so two
/// runs (e.g. before/after an engine change on this branch) can be diffed to confirm parse results are
/// unchanged. Flushed per word and crash-resumable via --start, since some corpus words are expensive
/// enough (this branch is plain master, with none of the hc-rustify or parse-optimization speedups, so a
/// handful of words can take minutes) that losing partial progress on a long corpus run is unacceptable.
///
/// Ported from the parse-opt worktree's BatchCommand.cs, with the --rule-stats and --parallel options
/// removed: those depend on Morpher.AccumulateRuleStats / AnalysisRuleStats / SynthesisRuleStats and
/// Morpher.MaxDegreeOfParallelism, which are parse-optimization-branch-only additions (#446/#451) that do
/// not exist on this branch's plain-master Morpher.
/// </summary>
internal class BatchCommand : ConsoleCommand
{
    private readonly HCContext _context;
    private int _startIndex;

    public BatchCommand(HCContext context)
    {
        _context = context;

        IsCommand("batch", "Parses every word in a word list, recording a result signature and timing per word");
        SkipsCommandSummaryBeforeRunning();
        HasAdditionalArguments(2, "<wordlist-file> <output-tsv>");
        HasOption("start=", "0-based line index to resume at (for crash recovery)", v => _startIndex = int.Parse(v));
    }

    public override int Run(string[] remainingArguments)
    {
        string wordListPath = remainingArguments[0];
        string outputPath = remainingArguments[1];

        string[] words = SignatureFormat.LoadWords(wordListPath);

        return RunSequential(words, outputPath);
    }

    private int RunSequential(string[] words, string outputPath)
    {
        using var writer = new StreamWriter(outputPath, append: _startIndex > 0) { AutoFlush = true };
        var totalSw = Stopwatch.StartNew();
        long parsed = 0,
            skipped = 0;
        for (int i = _startIndex; i < words.Length; i++)
        {
            string word = words[i];
            // Sentinel written before the attempt: if this word crashes the process, a wrapper script
            // can read the last line to find where to resume.
            writer.WriteLine($"{i}\t{word}\tSTARTED");
            (string status, long elapsedMs, string signature) = SignatureFormat.ParseOneWord(_context.Morpher, word);
            writer.WriteLine($"{i}\t{word}\t{elapsedMs}\t{status}\t{signature}");
            if (status == "SKIPPED")
                skipped++;
            else
                parsed++;
            if (i % 100 == 0)
                _context.Out.WriteLine("[{0}/{1}]", i, words.Length);
        }
        totalSw.Stop();
        _context.Out.WriteLine(
            "batch complete: {0} words parsed ({1} skipped), {2}ms total",
            parsed,
            skipped,
            totalSw.ElapsedMilliseconds
        );
        return 0;
    }
}
