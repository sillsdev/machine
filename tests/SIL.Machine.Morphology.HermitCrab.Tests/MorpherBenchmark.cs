using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Manual throughput / allocation benchmark for the corpus-parsing work
/// (see RUSTIFY.md). Marked [Explicit] so it never runs in normal CI;
/// run it with:  dotnet test --filter "FullyQualifiedName~MorpherBenchmark"
///
/// It loads the in-repo sample English grammar (samples/data/en-hc.xml) and the in-repo
/// World English Bible portion (samples/data/WEB-PT/*.SFM), then:
///   1. verifies the single-threaded morpher (MaxDegreeOfParallelism=1) produces the same
///      analyses as the default (within-word parallel) morpher, both sequentially and
///      when parallelized ACROSS words (the architecture FieldWorks will adopt);
///   2. prints MorpherStatistics instrumentation (Word.Clone count, analysis vs. synthesis
///      time split, analyses produced) to find small, targeted allocation speedups.
///
/// en-hc.xml is a TOY grammar (~microsecond parses), so wall-clock is noisy; the clone
/// counts and phase split are the grammar-independent signal. For real numbers, point
/// GrammarPath/CorpusDir at a FLEx-exported grammar and a full book.
/// </summary>
[TestFixture]
[Explicit("Manual performance benchmark; not part of CI.")]
public class MorpherBenchmark
{
    [Test]
    public void Benchmark_CorpusParsing()
    {
        // Point HC_GRAMMAR at a FLEx-exported HC config and HC_WORDS at a word-per-line file
        // (e.g. the Sena project) for real numbers; otherwise falls back to the in-repo toy data.
        string repoRoot = FindRepoRoot();
        string grammarPath =
            Environment.GetEnvironmentVariable("HC_GRAMMAR") ?? Path.Combine(repoRoot, "samples", "data", "en-hc.xml");
        string? wordsFile = Environment.GetEnvironmentVariable("HC_WORDS");
        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw) ? mw : int.MaxValue;

        Assert.That(File.Exists(grammarPath), Is.True, $"grammar not found: {grammarPath}");

        List<string> tokens =
            wordsFile != null
                ? new List<string>(File.ReadAllLines(wordsFile))
                : ReadTokens(Path.Combine(repoRoot, "samples", "data", "WEB-PT"));

        Language language = XmlLanguageLoader.Load(grammarPath);
        var defaultMorpher = new Morpher(new TraceManager(), language);
        var singleThreaded = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);

        // Bound the unapplication search so one pathological word can't dominate the A/B
        // (applied identically to both morphers, so correctness comparison is unaffected).
        int maxUnapp = int.TryParse(Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 0;
        if (maxUnapp > 0)
        {
            defaultMorpher.MaxUnapplications = maxUnapp;
            singleThreaded.MaxUnapplications = maxUnapp;
        }

        var distinct = new List<string>(new HashSet<string>(tokens));
        if (distinct.Count > maxWords)
            distinct = distinct.GetRange(0, maxWords);
        TestContext.Out.WriteLine(
            $"Grammar: {Path.GetFileName(grammarPath)} ({new FileInfo(grammarPath).Length / 1024} KB)"
        );
        TestContext.Out.WriteLine($"Corpus: {tokens.Count} tokens, parsing {distinct.Count} distinct forms.");

        // Warm up the JIT + grammar caches so the timed/instrumented runs are representative.
        foreach (string t in distinct.GetRange(0, Math.Min(50, distinct.Count)))
        {
            defaultMorpher.AnalyzeWord(t);
            singleThreaded.AnalyzeWord(t);
        }

        // Across-word concurrency cap; matches FieldWorks' default (Min(cores-1,4)) and bounds
        // peak memory (pathological words have large analysis sets — 20-way blows up RAM).
        int acrossDop = Math.Max(
            1,
            int.TryParse(Environment.GetEnvironmentVariable("HC_ACROSS_DOP"), out int ad)
                ? ad
                : Math.Min(Environment.ProcessorCount - 1, 4)
        );

        // --- "parallel inside" = today's production default: within-word parallel, serial corpus ---
        var swDefault = Stopwatch.StartNew();
        foreach (string t in distinct)
        {
            defaultMorpher.AnalyzeWord(t);
        }
        swDefault.Stop();

        // --- "series words" = single-threaded morpher, serial corpus (bounded memory) ---
        var swSingle = Stopwatch.StartNew();
        foreach (string t in distinct)
        {
            singleThreaded.AnalyzeWord(t);
        }
        swSingle.Stop();

        // --- FieldWorks-after model: single-threaded per word, parallel ACROSS words (capped) ---
        // Measure GC under the PARALLEL load — this is where alloc/GC contention actually bites
        // (concurrent allocation from N threads), unlike the single-threaded run below.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long parBytesBefore = GC.GetTotalAllocatedBytes(precise: true);
        int parGen0Before = GC.CollectionCount(0);
        int parGen2Before = GC.CollectionCount(2);
        var swParAcross = Stopwatch.StartNew();
        Parallel.ForEach(
            distinct,
            new ParallelOptions { MaxDegreeOfParallelism = acrossDop },
            t => singleThreaded.AnalyzeWord(t)
        );
        swParAcross.Stop();
        long parBytes = GC.GetTotalAllocatedBytes(precise: true) - parBytesBefore;
        int parGen0 = GC.CollectionCount(0) - parGen0Before;
        int parGen2 = GC.CollectionCount(2) - parGen2Before;

        // Correctness on a subset (parsing every word 3x over the full set is wasteful).
        foreach (string t in distinct.GetRange(0, Math.Min(200, distinct.Count)))
        {
            Assert.That(
                Signature(singleThreaded.AnalyzeWord(t)),
                Is.EqualTo(Signature(defaultMorpher.AnalyzeWord(t))),
                $"single-threaded disagrees on '{t}'"
            );
        }

        // --- Instrumentation: clean numbers from a single-threaded sequential run, with GC ---
        MorpherStatistics.Reset();
        MorpherStatistics.Enabled = true;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long bytesBefore = GC.GetTotalAllocatedBytes(precise: true);
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        foreach (string t in distinct)
        {
            singleThreaded.AnalyzeWord(t);
        }
        long bytesAllocated = GC.GetTotalAllocatedBytes(precise: true) - bytesBefore;
        int gen0 = GC.CollectionCount(0) - gen0Before;
        int gen1 = GC.CollectionCount(1) - gen1Before;
        int gen2 = GC.CollectionCount(2) - gen2Before;
        MorpherStatistics.Enabled = false;

        long parsed = MorpherStatistics.WordsParsed;
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine(
            $"GC mode: {(System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")}, {Environment.ProcessorCount} cores"
        );
        TestContext.Out.WriteLine(
            $"parallel-inside,  serial corpus (prod today)   : {swDefault.ElapsedMilliseconds, 6} ms"
        );
        TestContext.Out.WriteLine(
            $"series words,     serial corpus (single-thread): {swSingle.ElapsedMilliseconds, 6} ms"
        );
        TestContext.Out.WriteLine(
            $"single-thread, parallel ACROSS words           : {swParAcross.ElapsedMilliseconds, 6} ms  (cap {acrossDop})"
        );
        TestContext.Out.WriteLine($"  ^ parallel pass GC: {parBytes / 1024 / 1024} MB, gen0={parGen0} gen2={parGen2}");
        TestContext.Out.WriteLine("");
        TestContext.Out.WriteLine("--- Instrumentation (single-threaded, allocation/phase profile) ---");
        TestContext.Out.WriteLine($"words parsed         : {parsed}");
        TestContext.Out.WriteLine(
            $"Word.Clone calls     : {MorpherStatistics.WordClones}  ({(double)MorpherStatistics.WordClones / Math.Max(1, parsed):F1} per word)"
        );
        TestContext.Out.WriteLine(
            $"analyses produced    : {MorpherStatistics.AnalysesProduced}  ({(double)MorpherStatistics.AnalysesProduced / Math.Max(1, parsed):F1} per word)"
        );
        TestContext.Out.WriteLine(
            $"managed allocated    : {bytesAllocated / 1024 / 1024} MB  ({(double)bytesAllocated / Math.Max(1, parsed) / 1024:F1} KB per word)"
        );
        TestContext.Out.WriteLine($"GC collections       : gen0={gen0} gen1={gen1} gen2={gen2}");
        TestContext.Out.WriteLine(
            $"analysis phase time  : {MorpherStatistics.AnalysisTime.TotalMilliseconds, 8:F1} ms"
        );
        TestContext.Out.WriteLine(
            $"synthesis phase time : {MorpherStatistics.SynthesisTime.TotalMilliseconds, 8:F1} ms"
        );
        TestContext.Out.WriteLine(
            "(Toy grammar => treat ms as noise; clone count + phase split guide where to trim allocations.)"
        );
    }

    /// <summary>A stable, order-independent signature of a form's analyses.</summary>
    private static string Signature(IEnumerable<WordAnalysis> analyses)
    {
        return string.Join(
            "|",
            analyses
                .Select(a => string.Join("+", a.Morphemes.Select(m => m.Id)) + ":" + a.RootMorphemeIndex)
                .OrderBy(s => s, System.StringComparer.Ordinal)
        );
    }

    private static List<string> ReadTokens(string corpusDir)
    {
        var tokens = new List<string>();
        if (!Directory.Exists(corpusDir))
        {
            return tokens;
        }
        foreach (string file in Directory.GetFiles(corpusDir, "*.SFM"))
        {
            foreach (string line in File.ReadAllLines(file))
            {
                if (!line.StartsWith("\\v "))
                {
                    continue;
                }
                var sb = new StringBuilder();
                foreach (char c in line)
                {
                    if (char.IsLetter(c))
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    else if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                }
            }
        }
        return tokens;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "samples", "data", "en-hc.xml")))
        {
            dir = dir.Parent;
        }
        Assert.That(dir, Is.Not.Null, "could not locate repo root containing samples/data/en-hc.xml");
        return dir!.FullName;
    }
}
