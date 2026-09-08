using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Reusable A/B timing harness for the memoized sequential parse path (<c>maxDegreeOfParallelism: 1</c>),
/// so an optimization attempt can be measured against a baseline JSON captured before it. Modeled on
/// <see cref="MemoCorpusVerification"/>: [Explicit] and entirely env-driven because this repo never commits
/// real grammars, word lists, or anything derived from them (morpheme names, signatures included). The
/// harness embeds no grammar content and writes only to a path supplied by <c>HC_BENCH_OUT</c>; per-word
/// output to <see cref="TestContext"/> is index-keyed, never the word itself, for anything reachable from a
/// real (non-fixture) grammar.
/// <code>
///   $env:HC_BENCH_GRAMMAR = "...\sena-hc.xml"
///   $env:HC_BENCH_WORDS = "...\sena-words.txt"          # optional
///   $env:HC_BENCH_WORD_LIST = "wordA,wordB"              # optional; combined with HC_BENCH_WORDS, deduped
///   $env:HC_BENCH_MAX_WORDS = "40"                       # optional, default 40 (caps HC_BENCH_WORDS only)
///   $env:HC_BENCH_REPS = "3"                             # optional, default 3 (after one discarded warm-up)
///   $env:HC_BENCH_OUT = "...\baseline.json"              # required
///   $env:HC_BENCH_FIXTURES_ROOT = "...\conformance"      # optional; also runs every fixture grammar
///   $env:HC_BENCH_LABEL = "fallback-label"                # optional; used only if git HEAD can't be read
///   $env:HC_BENCH_TOGGLES = "prefilter=0,syntacticfs=0"    # optional retained-feature A/B switches
///   dotnet test --filter "FullyQualifiedName~PerfBench" -c Release
/// </code>
/// The harness intentionally parses synchronously. Callers must enforce hard timeouts by terminating the
/// entire test process externally; abandoning a Task.Run parse leaks CPU and mutable Morpher state into later samples.
/// </summary>
[TestFixture]
[Explicit("Manual A/B performance benchmark against a local, uncommitted real grammar; not part of CI.")]
public class PerfBench
{
    private static readonly Regex WordLineRegex = new(@"^\s*-\s*word:\s*(.+)$");

    [Test]
    public void RunBench()
    {
        string? outPath = Environment.GetEnvironmentVariable("HC_BENCH_OUT");
        if (string.IsNullOrEmpty(outPath))
            Assert.Ignore("set HC_BENCH_OUT");

        int maxWords = ParseIntOr("HC_BENCH_MAX_WORDS", 40);
        int reps = ParseIntOr("HC_BENCH_REPS", 3);
        string togglesEnv = Environment.GetEnvironmentVariable("HC_BENCH_TOGGLES") ?? "";
        IReadOnlyDictionary<string, string> toggles = ParseToggles(togglesEnv);

        var result = new BenchResult
        {
            GitSha = ResolveGitSha(),
            ProcessorCount = Environment.ProcessorCount,
            TesthostCountAtStart = Process.GetProcessesByName("testhost").Length,
            Reps = reps,
            Toggles = togglesEnv,
        };

        string? grammarPath = Environment.GetEnvironmentVariable("HC_BENCH_GRAMMAR");
        string? fixturesRoot = Environment.GetEnvironmentVariable("HC_BENCH_FIXTURES_ROOT");

        if (string.IsNullOrEmpty(grammarPath) && string.IsNullOrEmpty(fixturesRoot))
            Assert.Ignore("set HC_BENCH_GRAMMAR or HC_BENCH_FIXTURES_ROOT");

        if (!string.IsNullOrEmpty(grammarPath))
        {
            List<string> words = LoadWords(maxWords);
            TestContext.Out.WriteLine($"main grammar: {Path.GetFileName(grammarPath)}, words: {words.Count}");
            GrammarRun run = BenchGrammar(
                grammarPath,
                Path.GetFileName(grammarPath),
                words,
                reps,
                warmUp: true,
                printWords: false,
                toggles
            );
            result.Main = run;
        }

        if (!string.IsNullOrEmpty(fixturesRoot))
        {
            result.Fixtures = BenchFixtures(fixturesRoot, toggles);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(outPath, JsonSerializer.Serialize(result, options));
        TestContext.Out.WriteLine($"wrote {outPath}");
    }

    private static Dictionary<string, string> ParseToggles(string env)
    {
        var toggles = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(env))
            return toggles;
        foreach (string part in env.Split(','))
        {
            string[] pair = part.Split('=', 2);
            if (pair.Length == 2)
                toggles[pair[0].Trim()] = pair[1].Trim();
        }
        return toggles;
    }

    private static void ApplyToggles(Morpher morpher, IReadOnlyDictionary<string, string> toggles)
    {
        if (toggles.TryGetValue("prefilter", out string? prefilter) && prefilter == "0")
            morpher.EdgePrefilterEnabled = false;
        if (toggles.TryGetValue("syntacticfs", out string? syntacticFs) && syntacticFs == "0")
            morpher.ShareSyntacticFeatureStructs = false;
    }

    private static List<FixtureRun> BenchFixtures(string fixturesRoot, IReadOnlyDictionary<string, string> toggles)
    {
        var runs = new List<FixtureRun>();
        foreach (string sub in new[] { "languages", "edge-cases" })
        {
            string dir = Path.Combine(fixturesRoot, sub);
            if (!Directory.Exists(dir))
                continue;
            foreach (string grammarPath in Directory.EnumerateFiles(dir, "grammar.xml", SearchOption.AllDirectories))
            {
                string fixtureDir = Path.GetDirectoryName(grammarPath)!;
                string fixtureId = $"{sub}/{Path.GetFileName(fixtureDir)}";
                string wordsYaml = Path.Combine(fixtureDir, "words.yaml");
                var words = new List<string>();
                if (File.Exists(wordsYaml))
                {
                    foreach (string line in File.ReadAllLines(wordsYaml))
                    {
                        Match m = WordLineRegex.Match(line);
                        if (!m.Success)
                            continue;
                        string w = m.Groups[1].Value.Trim();
                        if (w.Length >= 2 && ((w[0] == '"' && w[^1] == '"') || (w[0] == '\'' && w[^1] == '\'')))
                            w = w[1..^1];
                        if (w.Length > 0)
                            words.Add(w);
                    }
                }
                TestContext.Out.WriteLine($"fixture: {fixtureId}, words: {words.Count}");
                GrammarRun run = BenchGrammar(
                    grammarPath,
                    fixtureId,
                    words,
                    reps: 1,
                    warmUp: false,
                    printWords: true,
                    toggles
                );
                run.Reliable = false;
                runs.Add(new FixtureRun { FixtureId = fixtureId, Run = run });
            }
        }
        return runs;
    }

    private static GrammarRun BenchGrammar(
        string grammarPath,
        string grammarLabel,
        List<string> words,
        int reps,
        bool warmUp,
        bool printWords,
        IReadOnlyDictionary<string, string> toggles
    )
    {
        var run = new GrammarRun
        {
            GrammarFile = grammarLabel,
            WordCount = words.Count,
            Reliable = true,
        };
        Morpher morpher;
        try
        {
            Language language = XmlLanguageLoader.Load(grammarPath);
            morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);
            ApplyToggles(morpher, toggles);
        }
        catch (Exception e)
        {
            run.LoadError = e.GetType().Name;
            TestContext.Out.WriteLine($"  grammar {grammarLabel} failed to load: {run.LoadError}");
            return run;
        }

        double totalMinMs = 0;
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            var wr = new WordResult { Index = i };
            Progress($"{grammarLabel} word[{i}] start");
            try
            {
                if (warmUp)
                    Signatures(morpher, word);

                var samples = new List<double>();
                List<string> lastSignatures = new();
                for (int r = 0; r < reps; r++)
                {
                    var sw = Stopwatch.StartNew();
                    lastSignatures = Signatures(morpher, word);
                    sw.Stop();
                    samples.Add(sw.Elapsed.TotalMilliseconds);
                }
                wr.SamplesMs = samples;
                wr.MinMs = samples.Min();
                double max = samples.Max();
                wr.Spread = wr.MinMs > 0 ? (max - wr.MinMs) / wr.MinMs : 0;
                wr.Signatures = lastSignatures;
                totalMinMs += wr.MinMs;

                if (printWords)
                {
                    TestContext.Out.WriteLine(
                        $"  [{i}] min={wr.MinMs:F2}ms spread={wr.Spread:F2} parses={lastSignatures.Count}"
                    );
                }
                else
                {
                    TestContext.Out.WriteLine(
                        $"  word[{i}] min={wr.MinMs:F2}ms spread={wr.Spread:F2} parses={lastSignatures.Count}"
                    );
                }
            }
            catch (Exception e)
            {
                // A word that throws (one conformance fixture does, identically on every build) is recorded
                // and compared like any other outcome; it must not abort the run and lose every other timing.
                Exception inner = e is AggregateException ae && ae.InnerException != null ? ae.InnerException : e;
                wr.Error = inner.GetType().Name;
                TestContext.Out.WriteLine($"  word[{i}] THREW {wr.Error}");
            }
            // Heavy words can leave tens of GB of garbage behind; collect between words so one word's
            // garbage cannot push the next word's peak past physical memory, and record what survived.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            wr.HeapAfterGcMB = GC.GetTotalMemory(false) / (1024 * 1024);
            wr.PeakWorkingSetMB = Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024);
            Progress(
                $"{grammarLabel} word[{i}] done min={wr.MinMs:F0}ms heapAfterGc={wr.HeapAfterGcMB}MB peakWS={wr.PeakWorkingSetMB}MB"
            );
            run.Words.Add(wr);
        }
        run.TotalMinMs = totalMinMs;
        run.MemoHits = morpher.MemoHits;
        run.NogoodHits = morpher.NogoodHits;
        run.TemplateMemoHits = morpher.TemplateMemoHits;
        run.TemplateNogoodHits = morpher.TemplateNogoodHits;
        return run;
    }

    private static List<string> Signatures(Morpher morpher, string word)
    {
        try
        {
            return morpher
                .ParseWord(word)
                .Select(MorpherTests.WordAnalysisSignature)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
        }
        catch (InvalidShapeException)
        {
            return new List<string>();
        }
    }

    private static List<string> LoadWords(int maxWords)
    {
        var words = new List<string>();
        string? explicitList = Environment.GetEnvironmentVariable("HC_BENCH_WORD_LIST");
        if (!string.IsNullOrEmpty(explicitList))
        {
            words.AddRange(explicitList.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0));
        }
        string? wordsPath = Environment.GetEnvironmentVariable("HC_BENCH_WORDS");
        if (!string.IsNullOrEmpty(wordsPath) && File.Exists(wordsPath))
        {
            IEnumerable<string> fromFile = File.ReadAllLines(wordsPath)
                .Select(w => w.Trim())
                .Where(w => w.Length > 0)
                .Take(maxWords);
            words.AddRange(fromFile);
        }
        return words.Distinct().ToList();
    }

    /// <summary>
    /// Unbuffered progress line to the file named by HC_BENCH_LOG (TestContext output is only flushed
    /// when the test ends, so it says nothing about where a crashed run got to).
    /// </summary>
    private static void Progress(string line)
    {
        string? log = Environment.GetEnvironmentVariable("HC_BENCH_LOG");
        if (string.IsNullOrEmpty(log))
            return;
        File.AppendAllText(log, $"{DateTime.Now:HH:mm:ss} {line}{Environment.NewLine}");
    }

    private static int ParseIntOr(string envVar, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(envVar), out int v) ? v : fallback;
    }

    private static string ResolveGitSha()
    {
        try
        {
            // Let git resolve the repository itself: in a worktree `.git` is a file, not a directory, and
            // walking up for a `.git` directory lands in the main checkout and reports its sha instead.
            {
                var psi = new ProcessStartInfo("git", "rev-parse HEAD")
                {
                    WorkingDirectory = TestContext.CurrentContext.TestDirectory,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                using Process? proc = Process.Start(psi);
                if (proc != null)
                {
                    string sha = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit();
                    if (proc.ExitCode == 0 && sha.Length > 0)
                        return sha;
                }
            }
        }
        catch
        {
            // fall through to env-var fallback
        }
        return Environment.GetEnvironmentVariable("HC_BENCH_LABEL") ?? "unknown";
    }

    private class BenchResult
    {
        public string GitSha { get; set; } = "";
        public int ProcessorCount { get; set; }
        public int TesthostCountAtStart { get; set; }
        public int Reps { get; set; }
        public string Toggles { get; set; } = "";
        public GrammarRun? Main { get; set; }
        public List<FixtureRun> Fixtures { get; set; } = new();
    }

    private class FixtureRun
    {
        public string FixtureId { get; set; } = "";
        public GrammarRun Run { get; set; } = new();
    }

    private class GrammarRun
    {
        public string GrammarFile { get; set; } = "";
        public int WordCount { get; set; }
        public bool Reliable { get; set; }
        public string? LoadError { get; set; }
        public double TotalMinMs { get; set; }
        public long MemoHits { get; set; }
        public long NogoodHits { get; set; }
        public long TemplateMemoHits { get; set; }
        public long TemplateNogoodHits { get; set; }
        public List<WordResult> Words { get; set; } = new();
    }

    private class WordResult
    {
        public int Index { get; set; }
        public double MinMs { get; set; }
        public List<double> SamplesMs { get; set; } = new();
        public double Spread { get; set; }
        public string? Error { get; set; }
        public long HeapAfterGcMB { get; set; }
        public long PeakWorkingSetMB { get; set; }
        public List<string> Signatures { get; set; } = new();
    }
}
