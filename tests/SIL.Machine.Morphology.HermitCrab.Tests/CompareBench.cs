using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

// Self-contained master-vs-branch comparison harness: uses ONLY the public Morpher/XmlLanguageLoader API
// + GC.GetTotalAllocatedBytes + Stopwatch, so it compiles + runs identically on master and the rustify
// branch. The 2-arg Morpher ctor (historical default parallelism on both) keeps threading constant, so the
// only difference is the branch's internals. Grammar/words come from HC_GRAMMAR/HC_WORDS (absolute paths).
[TestFixture]
[Explicit("master-vs-branch comparison; not CI")]
public class CompareBench
{
    [Test]
    public void Run()
    {
        string grammar = System.Environment.GetEnvironmentVariable("HC_GRAMMAR")!;
        string wordsFile = System.Environment.GetEnvironmentVariable("HC_WORDS")!;
        int maxUnapp = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 5;
        int maxWords = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;
        int passes = int.TryParse(System.Environment.GetEnvironmentVariable("HC_PASSES"), out int p) ? p : 1;

        var words = new List<string>();
        foreach (string line in System.IO.File.ReadAllLines(wordsFile))
        {
            string w = line.Trim();
            if (w.Length > 0)
                words.Add(w);
        }
        words = new List<string>(new SortedSet<string>(words, System.StringComparer.Ordinal));
        if (words.Count > maxWords)
            words = words.GetRange(0, maxWords);

        Language lang = XmlLanguageLoader.Load(grammar);
        // Single-threaded on BOTH branches for a deterministic, parallel-overhead-free comparison: on the
        // rustify branch use the 3-arg ctor (maxDegreeOfParallelism=1) via reflection; on master that ctor
        // doesn't exist, so fall back to the 2-arg ctor (the project there is compiled with SINGLE_THREADED).
        var tm = new TraceManager();
        System.Reflection.ConstructorInfo? ctor3 = typeof(Morpher).GetConstructor(
            new[] { typeof(ITraceManager), typeof(Language), typeof(int) }
        );
        Morpher morpher = ctor3 != null ? (Morpher)ctor3.Invoke(new object[] { tm, lang, 1 }) : new Morpher(tm, lang);
        morpher.MaxUnapplications = maxUnapp;

        // Warm up JIT + grammar caches (excluded from the measurement).
        for (int i = 0; i < System.Math.Min(10, words.Count); i++)
        {
            foreach (var _ in morpher.AnalyzeWord(words[i])) { }
        }

        for (int pass = 0; pass < passes; pass++)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            long b0 = System.GC.GetTotalAllocatedBytes(true);
            int g0 = System.GC.CollectionCount(0),
                g1 = System.GC.CollectionCount(1),
                g2 = System.GC.CollectionCount(2);
            var sw = Stopwatch.StartNew();
            int parsed = 0;
            long analyses = 0;
            foreach (string word in words)
            {
                foreach (var _ in morpher.AnalyzeWord(word))
                    analyses++;
                parsed++;
            }
            sw.Stop();
            long bytes = System.GC.GetTotalAllocatedBytes(true) - b0;
            System.Console.WriteLine(
                $"COMPAREBENCH pass={pass} parsed={parsed} analyses={analyses} ms={sw.ElapsedMilliseconds} "
                    + $"ms/word={(double)sw.ElapsedMilliseconds / parsed:F2} totalMB={bytes / 1048576} "
                    + $"KB/word={bytes / 1024.0 / parsed:F0} gen0={System.GC.CollectionCount(0) - g0} "
                    + $"gen1={System.GC.CollectionCount(1) - g1} gen2={System.GC.CollectionCount(2) - g2}"
            );
        }
    }
}
