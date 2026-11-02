using System.Diagnostics;
using System.Globalization;
using System.IO;
using ManyConsole;
using Mono.Options;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Parses every word in a word list and records a per-word result signature plus elapsed time, so two
/// runs (for example, before and after an engine change) can be diffed to confirm parse results are
/// unchanged. Output is flushed per word, and --start resumes a run after a crash, because a single word
/// can take minutes.
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
        HasOption("start=", "0-based line index to resume at (for crash recovery)", v => _startIndex = ParseStart(v));
    }

    /// <summary>NumberStyles.None rejects a sign, so a negative index fails here rather than at words[i].</summary>
    private static int ParseStart(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int start))
            throw new OptionException($"--start must be a non-negative integer, got '{value}'", "--start");
        return start;
    }

    public override int Run(string[] remainingArguments)
    {
        string wordListPath = remainingArguments[0];
        string outputPath = remainingArguments[1];

        try
        {
            string[] words = SignatureFormat.LoadWords(wordListPath);
            return RunSequential(words, outputPath);
        }
        finally
        {
            // One instance serves every line of a script, and --start is parsed only when given.
            _startIndex = 0;
        }
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
