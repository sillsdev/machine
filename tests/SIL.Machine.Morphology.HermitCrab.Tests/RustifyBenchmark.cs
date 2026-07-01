using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.FiniteState;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Fast, budget-bounded, SINGLE-PASS allocation probe for RUSTIFY work (see RUSTIFY.md).
/// Unlike MorpherBenchmark (5 passes, TestContext.Out buffered) this does ONE measured pass,
/// prints to Console (flushes immediately so partial results survive a kill), and stops after a
/// wall-clock budget — so it is usable on the real Sena grammar, whose per-word cost otherwise
/// makes A/B iteration impossible.
///
/// Run (Sena):
///   HC_GRAMMAR=...\sena-hc.xml HC_WORDS=...\sena-words.txt HC_MAX_UNAPP=5 HC_BUDGET_MS=20000 \
///   dotnet test ...HermitCrab.Tests --no-build --filter FullyQualifiedName~RustifyBenchmark.SenaQuick
///
/// Falls back to the in-repo English toy grammar + WEB-PT corpus when HC_GRAMMAR/HC_WORDS
/// are not set (suitable for verifying the breakdown harness, not for production numbers).
/// </summary>
[TestFixture]
[Explicit("RUSTIFY allocation probe; not part of CI.")]
public class RustifyBenchmark
{
    // Byte-identical correctness gate on a REAL grammar (en-hc's 2 clones/word under-exercises the COW
    // never-inflated clone path). Emits a deterministic per-word analysis signature to HC_SIG_OUT; diff
    // the file at HEAD vs the pre-Stage-3 baseline to prove the analysis set is unchanged where COW runs hot.
    [Test]
    public void Signature()
    {
        string repoRoot = FindRepoRoot();
        string grammar =
            System.Environment.GetEnvironmentVariable("HC_GRAMMAR")
            ?? System.IO.Path.Combine(repoRoot, "samples", "data", "en-hc.xml");
        string? wordsFile = System.Environment.GetEnvironmentVariable("HC_WORDS");
        string outFile =
            System.Environment.GetEnvironmentVariable("HC_SIG_OUT") ?? System.IO.Path.Combine(repoRoot, "sig.txt");

        var words = new List<string>();
        if (wordsFile != null)
        {
            foreach (string line in System.IO.File.ReadAllLines(wordsFile))
            {
                string w = line.Trim();
                if (w.Length > 0)
                    words.Add(w);
            }
        }
        words = new List<string>(
            new System.Collections.Generic.SortedSet<string>(words, System.StringComparer.Ordinal)
        );
        int maxWords = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;
        if (words.Count > maxWords)
            words = words.GetRange(0, maxWords);
        int maxUnapp = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 5;

        Language language = XmlLanguageLoader.Load(grammar);
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
        {
            MaxUnapplications = maxUnapp,
        };

        var lines = new List<string>();
        foreach (string w in words)
        {
            var sigs = new System.Collections.Generic.SortedSet<string>(System.StringComparer.Ordinal);
            foreach (SIL.Machine.Morphology.WordAnalysis a in morpher.AnalyzeWord(w))
            {
                string morphs = string.Join(" ", a.Morphemes.Select(m => m.Gloss ?? m.Id ?? "?"));
                sigs.Add($"{a.Category}|{a.RootMorphemeIndex}|{morphs}");
            }
            lines.Add($"{w}\t{string.Join(" || ", sigs)}");
        }
        System.IO.File.WriteAllLines(outFile, lines);
        System.Console.WriteLine($"SIGNATURE wrote {lines.Count} words to {outFile}");
    }

    [Test]
    public void SenaQuick()
    {
        string repoRoot = FindRepoRoot();
        string? grammar =
            System.Environment.GetEnvironmentVariable("HC_GRAMMAR")
            ?? System.IO.Path.Combine(repoRoot, "samples", "data", "en-hc.xml");
        string? wordsFile = System.Environment.GetEnvironmentVariable("HC_WORDS");
        Assert.That(grammar, Is.Not.Null.And.Not.Empty, "set HC_GRAMMAR");

        // Load words: explicit list, or fall back to WEB-PT corpus tokens.
        List<string> words;
        if (wordsFile != null)
        {
            words = new List<string>();
            foreach (string line in System.IO.File.ReadAllLines(wordsFile))
            {
                string w = line.Trim();
                if (w.Length > 0)
                    words.Add(w);
            }
        }
        else
        {
            // Fall back to the same WEB-PT tokens MorpherBenchmark uses.
            words = ReadWebPtTokens(System.IO.Path.Combine(repoRoot, "samples", "data", "WEB-PT"));
            System.Console.WriteLine($"(no HC_WORDS set — using WEB-PT corpus: {words.Count} tokens)");
        }

        FeatureStruct.FlatUnifyEnabled = System.Environment.GetEnvironmentVariable("HC_FLAT") != "0";

        int budgetMs = int.TryParse(System.Environment.GetEnvironmentVariable("HC_BUDGET_MS"), out int b) ? b : 20000;
        int maxUnapp = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 5;
        int maxWords = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;

        // Apply maxWords cap, and de-duplicate so all words are exercised once.
        var wordSet = new System.Collections.Generic.HashSet<string>(words);
        words = new List<string>(wordSet);
        if (words.Count > maxWords)
            words = words.GetRange(0, maxWords);

        Language language = XmlLanguageLoader.Load(grammar!);
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
        {
            MaxUnapplications = maxUnapp,
        };

        // Warm up JIT + grammar caches on a few words (excluded from the measurement).
        for (int i = 0; i < System.Math.Min(3, words.Count); i++)
            Consume(morpher.AnalyzeWord(words[i]));

        MorpherStatistics.Reset();
        FstStatistics.Reset();
        MorpherStatistics.Enabled = true;
        FstStatistics.Enabled = true;
        // Attribute allocation done inside Word.Clone (single-threaded probe).
        MorpherStatistics.AllocationProbe = System.GC.GetAllocatedBytesForCurrentThread;
        FstStatistics.AllocationProbe = System.GC.GetAllocatedBytesForCurrentThread;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        long bytes0 = System.GC.GetTotalAllocatedBytes(precise: true);
        int gen0 = System.GC.CollectionCount(0);

        int parsed = 0;
        var sw = Stopwatch.StartNew();
        foreach (string w in words)
        {
            Consume(morpher.AnalyzeWord(w));
            parsed++;
            if (sw.ElapsedMilliseconds > budgetMs)
                break;
        }
        sw.Stop();

        long bytes = System.GC.GetTotalAllocatedBytes(precise: true) - bytes0;
        int gen0Delta = System.GC.CollectionCount(0) - gen0;
        long clones = MorpherStatistics.WordClones;
        long cloneBytes = MorpherStatistics.CloneBytes;
        long wordCtorBytes = MorpherStatistics.WordCtorBytes;
        long wordCtorCount = MorpherStatistics.WordCtorCount;
        long markMorphBytes = MorpherStatistics.MarkMorphBytes;
        long segmentBytes = MorpherStatistics.SegmentBytes;
        long analysisCascadeBytes = MorpherStatistics.AnalysisCascadeBytes;
        long varBindingsBytes = FstStatistics.VarBindingsBytes;
        long varBindingsClones = FstStatistics.VarBindingsClones;
        long registerCloneBytes = FstStatistics.RegisterCloneBytes;
        long registerClones = FstStatistics.RegisterClones;
        long scaffoldBytes = FstStatistics.ScaffoldBytes;
        long transduceScaffolds = FstStatistics.TransduceScaffolds;
        long traversalMethodBytes = FstStatistics.TraversalMethodBytes;
        long traversalMethodCreates = FstStatistics.TraversalMethodCreates;
        MorpherStatistics.AllocationProbe = null;
        MorpherStatistics.Enabled = false;
        FstStatistics.AllocationProbe = null;
        FstStatistics.Enabled = false;

        double perWordKb = parsed == 0 ? 0 : (double)bytes / parsed / 1024;
        double clonesPerWord = parsed == 0 ? 0 : (double)clones / parsed;
        // Compute allocation fractions of total for each category.
        double ClonePct(long cat) => bytes == 0 ? 0 : 100.0 * cat / bytes;
        long accountedBytes =
            cloneBytes
            + wordCtorBytes
            + markMorphBytes
            + segmentBytes
            + varBindingsBytes
            + registerCloneBytes
            + scaffoldBytes
            + traversalMethodBytes;
        // AnalysisCascade overlaps other probes (it's a superset window); show it separately for context.
        double otherPct = bytes == 0 ? 0 : 100.0 * (bytes - accountedBytes) / bytes;
        System.Console.WriteLine(
            $"SENAQUICK parsed={parsed} clones/word={clonesPerWord:F0} KB/word={perWordKb:F1} "
                + $"totalMB={bytes / 1024 / 1024} gen0={gen0Delta} ms={sw.ElapsedMilliseconds}"
        );
        System.Console.WriteLine($"  BREAKDOWN (% of total alloc):");
        System.Console.WriteLine(
            $"    Segment (initial Shape)  {ClonePct(segmentBytes), 5:F1}%  ({segmentBytes / 1024}KB, {parsed} words)"
        );
        System.Console.WriteLine(
            $"    Word.ctor(new)           {ClonePct(wordCtorBytes), 5:F1}%  ({wordCtorBytes / 1024}KB, {parsed} words)"
        );
        System.Console.WriteLine(
            $"    Word.Clone               {ClonePct(cloneBytes), 5:F1}%  ({cloneBytes / 1024}KB, {clones} clones, {(parsed > 0 ? clones / parsed : 0)} /word)"
        );
        System.Console.WriteLine(
            $"    MarkMorph                {ClonePct(markMorphBytes), 5:F1}%  ({markMorphBytes / 1024}KB)"
        );
        System.Console.WriteLine(
            $"    VarBindings.Clone        {ClonePct(varBindingsBytes), 5:F1}%  ({varBindingsBytes / 1024}KB, {varBindingsClones} clones, {(parsed > 0 ? varBindingsClones / parsed : 0)} /word)"
        );
        System.Console.WriteLine(
            $"    Registers.Clone          {ClonePct(registerCloneBytes), 5:F1}%  ({registerCloneBytes / 1024}KB, {registerClones} clones, {(parsed > 0 ? registerClones / parsed : 0)} /word)"
        );
        System.Console.WriteLine(
            $"    TraversalMethod          {ClonePct(traversalMethodBytes), 5:F1}%  ({traversalMethodBytes / 1024}KB, {traversalMethodCreates} creates, {(parsed > 0 ? traversalMethodCreates / parsed : 0)} /word)"
        );
        System.Console.WriteLine(
            $"    Scaffold (initAnns/regs) {ClonePct(scaffoldBytes), 5:F1}%  ({scaffoldBytes / 1024}KB, {transduceScaffolds} calls)"
        );
        System.Console.WriteLine(
            $"    Other (LINQ, FstResult)  {otherPct, 5:F1}%  ({(bytes - accountedBytes) / 1024}KB)"
        );
        System.Console.WriteLine(
            $"  [analysis window superset: {ClonePct(analysisCascadeBytes), 5:F1}%  ({analysisCascadeBytes / 1024}KB) — includes Clone/Scaffold/etc]"
        );
    }

    private static void Consume(System.Collections.Generic.IEnumerable<WordAnalysis> analyses)
    {
        foreach (WordAnalysis _ in analyses) { }
    }

    private static string FindRepoRoot()
    {
        var dir = new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (
            dir != null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "samples", "data", "en-hc.xml"))
        )
        {
            dir = dir.Parent;
        }
        Assert.That(dir, Is.Not.Null, "could not locate repo root containing samples/data/en-hc.xml");
        return dir!.FullName;
    }

    private static List<string> ReadWebPtTokens(string corpusDir)
    {
        var tokens = new List<string>();
        if (!System.IO.Directory.Exists(corpusDir))
            return tokens;
        foreach (string file in System.IO.Directory.GetFiles(corpusDir, "*.SFM"))
        {
            foreach (string line in System.IO.File.ReadAllLines(file))
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

    /// <summary>
    /// Across-word parallel throughput: one shared morpher (serial WITHIN a word,
    /// MaxDegreeOfParallelism=1 — the FieldWorks model), the same fixed word set parsed at
    /// dop = 1/4/8/16. Reports wall-clock words/sec and the scaling factor vs single-thread, so we
    /// can see whether the allocation work lifts the ~2.8x parallel ceiling.
    /// </summary>
    [Test]
    public void SenaParallel()
    {
        string repoRoot2 = FindRepoRoot();
        string? grammar =
            System.Environment.GetEnvironmentVariable("HC_GRAMMAR")
            ?? System.IO.Path.Combine(repoRoot2, "samples", "data", "en-hc.xml");
        string? wordsFile = System.Environment.GetEnvironmentVariable("HC_WORDS");
        Assert.That(grammar, Is.Not.Null.And.Not.Empty, "set HC_GRAMMAR");

        FeatureStruct.FlatUnifyEnabled = System.Environment.GetEnvironmentVariable("HC_FLAT") != "0";

        int maxUnapp = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 5;
        int maxWords = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw) ? mw : 800;

        List<string> words;
        if (wordsFile != null)
        {
            words = new List<string>();
            foreach (string line in System.IO.File.ReadAllLines(wordsFile))
            {
                string w = line.Trim();
                if (w.Length > 0)
                    words.Add(w);
                if (words.Count >= maxWords)
                    break;
            }
        }
        else
        {
            words = ReadWebPtTokens(System.IO.Path.Combine(repoRoot2, "samples", "data", "WEB-PT"));
            var wordSet2 = new System.Collections.Generic.HashSet<string>(words);
            words = new List<string>(wordSet2);
            System.Console.WriteLine($"(no HC_WORDS set — using WEB-PT corpus: {words.Count} tokens)");
        }
        if (words.Count > maxWords)
            words = words.GetRange(0, maxWords);

        Language language = XmlLanguageLoader.Load(grammar!);
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
        {
            MaxUnapplications = maxUnapp,
        };

        // Warm up JIT + grammar caches.
        for (int i = 0; i < System.Math.Min(10, words.Count); i++)
            Consume(morpher.AnalyzeWord(words[i]));

        System.Console.WriteLine(
            $"SENAPAR grammar words={words.Count} cores={System.Environment.ProcessorCount} maxUnapp={maxUnapp} "
                + $"serverGC={System.Runtime.GCSettings.IsServerGC}"
        );
        double baseMs = 0;
        foreach (int dop in new[] { 1, 2, 4, 8, 16 })
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            long bytes0 = System.GC.GetTotalAllocatedBytes(precise: true);
            int gen0 = System.GC.CollectionCount(0);
            int gen1 = System.GC.CollectionCount(1);
            int gen2 = System.GC.CollectionCount(2);
            var sw = Stopwatch.StartNew();
            if (dop == 1)
            {
                foreach (string w in words)
                    Consume(morpher.AnalyzeWord(w));
            }
            else
            {
                System.Threading.Tasks.Parallel.ForEach(
                    words,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = dop },
                    w => Consume(morpher.AnalyzeWord(w))
                );
            }
            sw.Stop();
            long mb = (System.GC.GetTotalAllocatedBytes(precise: true) - bytes0) / 1024 / 1024;
            int gen0Delta = System.GC.CollectionCount(0) - gen0;
            int gen1Delta = System.GC.CollectionCount(1) - gen1;
            int gen2Delta = System.GC.CollectionCount(2) - gen2;
            double ms = sw.Elapsed.TotalMilliseconds;
            if (dop == 1)
                baseMs = ms;
            double wps = words.Count / sw.Elapsed.TotalSeconds;
            System.Console.WriteLine(
                $"SENAPAR dop={dop, 2} ms={ms, 8:F0} words/sec={wps, 8:F0} scaling={baseMs / ms, 5:F2}x MB={mb, 5} "
                    + $"gen0={gen0Delta, 4} gen1={gen1Delta, 3} gen2={gen2Delta, 3}"
            );
        }
    }
}
