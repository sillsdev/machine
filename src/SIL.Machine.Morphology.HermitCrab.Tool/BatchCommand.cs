using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManyConsole;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Phase 0 of parse-optimization.md: parses every word in a word list and records a per-word result
/// signature plus elapsed time, so two runs (before/after an engine change) can be diffed to confirm
/// parse results are unchanged. Flushed per word and crash-resumable via --start, since some corpus
/// words are expensive enough (100+ seconds, and one has been observed to crash a process outright) that
/// losing partial progress on a multi-hour corpus run is unacceptable.
/// </summary>
internal class BatchCommand : ConsoleCommand
{
    private readonly HCContext _context;
    private int _startIndex;
    private string _ruleStatsPath;
    private bool _parallel;
    private int _parallelDegree = -1;

    public BatchCommand(HCContext context)
    {
        _context = context;

        IsCommand(
            "batch",
            "Parses every word in a word list, recording a result signature and timing per word (see parse-optimization.md Phase 0)"
        );
        SkipsCommandSummaryBeforeRunning();
        HasAdditionalArguments(2, "<wordlist-file> <output-tsv>");
        HasOption(
            "start=",
            "0-based line index to resume at (for crash recovery; ignored with --parallel)",
            v => _startIndex = int.Parse(v)
        );
        HasOption(
            "rule-stats=",
            "accumulate per-rule firing stats (category/stem/allomorph/environment buckets, with example "
                + "words) across the whole run and write a report to {FILE} -- run with --sequential, the "
                + "counters are not thread-safe",
            v => _ruleStatsPath = v
        );
        HasOption(
            "parallel:",
            "parse words concurrently across a load-balanced, longest-word-first Parallel.ForEach "
                + "(parse-optimization.md Phase 8a) -- requires the Morpher itself to be --sequential for "
                + "the per-word memo tables to engage; degree defaults to Environment.ProcessorCount, or "
                + "{N} if given; trades --start crash-resume for speed (output is buffered and written "
                + "index-ordered at the end)",
            v =>
            {
                _parallel = true;
                if (!string.IsNullOrEmpty(v))
                    _parallelDegree = int.Parse(v);
            }
        );
    }

    public override int Run(string[] remainingArguments)
    {
        string wordListPath = remainingArguments[0];
        string outputPath = remainingArguments[1];

        string[] words = File.ReadAllLines(wordListPath).Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();

        if (_ruleStatsPath != null)
        {
            if (_parallel)
            {
                _context.Out.WriteLine(
                    "ERROR: --rule-stats and --parallel cannot be combined (counters are not thread-safe)."
                );
                return -1;
            }
            if (_context.Morpher.MaxDegreeOfParallelism != 1)
            {
                _context.Out.WriteLine(
                    "WARNING: --rule-stats requested without --sequential; per-rule counters are not "
                        + "thread-safe and will be unreliable under within-word parallelism."
                );
            }
            _context.Morpher.AccumulateRuleStats = true;
        }

        if (_parallel)
        {
            if (_context.Morpher.MaxDegreeOfParallelism != 1)
            {
                _context.Out.WriteLine(
                    "WARNING: --parallel requested without --sequential; the per-word memo tables "
                        + "(parse-optimization.md Phases 2/3/3b) only engage on the sequential cascade, so "
                        + "this run will not get their benefit."
                );
            }
            if (_startIndex > 0)
            {
                _context.Out.WriteLine("WARNING: --start is ignored under --parallel; running the full word list.");
            }
            return RunParallel(words, outputPath);
        }

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
            // can read the last line to find where to resume (see run_sena_shards.ps1 precedent).
            writer.WriteLine($"{i}\t{word}\tSTARTED");
            (string status, long elapsedMs, string signature) = ParseOneWord(word);
            writer.WriteLine($"{i}\t{word}\t{elapsedMs}\t{status}\t{signature}");
            if (status == "SKIPPED")
                skipped++;
            else
                parsed++;
            if (i % 100 == 0)
            {
                _context.Out.WriteLine("[{0}/{1}]", i, words.Length);
                // Rewritten (not appended) every checkpoint so a mid-run crash on a pathological word
                // still leaves a usable report reflecting everything parsed so far.
                if (_ruleStatsPath != null)
                    WriteRuleStatsReport();
            }
        }
        totalSw.Stop();
        if (_ruleStatsPath != null)
            WriteRuleStatsReport();
        _context.Out.WriteLine(
            "batch complete: {0} words parsed ({1} skipped), {2}ms total",
            parsed,
            skipped,
            totalSw.ElapsedMilliseconds
        );
        return 0;
    }

    // Phase 8a: the earlier per-word AutoFlush writer is not thread-safe and crash-resume has no meaning
    // once words are handed out out-of-order, so rows are buffered per index and written once at the end.
    // Ordering the work queue longest-word-first, combined with the load-balanced (chunked, not static
    // range) partitioner below, is what closes the 2.9x gap between wall clock and the perfect-packing
    // bound measured on 2026-07-03 -- heavy words no longer cluster onto a few threads.
    private int RunParallel(string[] words, string outputPath)
    {
        var rows = new string[words.Length];
        int[] order = Enumerable.Range(0, words.Length).OrderByDescending(i => words[i].Length).ToArray();

        var totalSw = Stopwatch.StartNew();
        long parsed = 0,
            skipped = 0;
        long completed = 0;

        var parallelOptions = new ParallelOptions();
        if (_parallelDegree > 0)
            parallelOptions.MaxDegreeOfParallelism = _parallelDegree;

        Parallel.ForEach(
            Partitioner.Create(order, loadBalance: true),
            parallelOptions,
            i =>
            {
                string word = words[i];
                (string status, long elapsedMs, string signature) = ParseOneWord(word);
                rows[i] = $"{i}\t{word}\t{elapsedMs}\t{status}\t{signature}";
                if (status == "SKIPPED")
                    Interlocked.Increment(ref skipped);
                else
                    Interlocked.Increment(ref parsed);
                long n = Interlocked.Increment(ref completed);
                if (n % 100 == 0)
                    _context.Out.WriteLine("[{0}/{1}]", n, words.Length);
            }
        );
        totalSw.Stop();

        using (var writer = new StreamWriter(outputPath, append: false))
        {
            foreach (string row in rows)
                writer.WriteLine(row);
        }

        _context.Out.WriteLine(
            "batch complete: {0} words parsed ({1} skipped), {2}ms total",
            parsed,
            skipped,
            totalSw.ElapsedMilliseconds
        );
        return 0;
    }

    private (string status, long elapsedMs, string signature) ParseOneWord(string word)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Word[] results = _context.Morpher.ParseWord(word, out _).ToArray();
            sw.Stop();
            return ("ok", sw.ElapsedMilliseconds, BuildSignature(results));
        }
        catch (InvalidShapeException)
        {
            return ("SKIPPED", 0, "-");
        }
    }

    private void WriteRuleStatsReport()
    {
        using var statsWriter = new StreamWriter(_ruleStatsPath, append: false);
        RuleStatsReport.Write(statsWriter, "Analysis", _context.Morpher.AnalysisRuleStats);
        RuleStatsReport.Write(statsWriter, "Synthesis", _context.Morpher.SynthesisRuleStats);
    }

    // Order-independent (sorted) so two runs that find the same parses in a different internal order
    // still compare equal; a change in this signature means parse RESULTS changed, which every phase in
    // parse-optimization.md is required not to do.
    private static string BuildSignature(IEnumerable<Word> results)
    {
        List<string> signatures = results
            .Select(w =>
                string.Join("+", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id))
                + "|"
                + w.Shape.ToRegexString(w.Stratum.CharacterDefinitionTable, true)
            )
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        return signatures.Count == 0 ? "-" : string.Join(";", signatures);
    }
}
