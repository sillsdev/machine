using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

// Multi-PROCESS (not multi-thread) scaling probe: each OS process runs single-threaded with its own
// private Workstation GC heap, processing one slice of the word list. Proves/disproves whether process-
// level parallelism (independent heaps, independent collectors) recovers the scaling that a single
// Workstation-GC process can't reach via threads alone (see RustifyBenchmark.SenaParallel). A driver
// script launches HC_SLICE_COUNT copies of this test as separate `dotnet test` processes and aggregates
// the per-process lines this prints.
[TestFixture]
[Explicit("multi-process scaling probe; not CI")]
public class WorkerSlice
{
    [Test]
    public void Slice()
    {
        string grammar = System.Environment.GetEnvironmentVariable("HC_GRAMMAR")!;
        string wordsFile = System.Environment.GetEnvironmentVariable("HC_WORDS")!;
        int maxUnapp = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_UNAPP"), out int mu) ? mu : 5;
        int maxWords = int.TryParse(System.Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;
        int sliceIndex = int.Parse(System.Environment.GetEnvironmentVariable("HC_SLICE_INDEX") ?? "0");
        int sliceCount = int.Parse(System.Environment.GetEnvironmentVariable("HC_SLICE_COUNT") ?? "1");

        var allWords = new List<string>();
        foreach (string line in System.IO.File.ReadAllLines(wordsFile))
        {
            string w = line.Trim();
            if (w.Length > 0)
                allWords.Add(w);
        }
        allWords = new List<string>(new SortedSet<string>(allWords, System.StringComparer.Ordinal));
        if (allWords.Count > maxWords)
            allWords = allWords.GetRange(0, maxWords);

        // Round-robin slice assignment for balance across processes.
        var slice = new List<string>();
        for (int i = sliceIndex; i < allWords.Count; i += sliceCount)
            slice.Add(allWords[i]);

        Language lang = XmlLanguageLoader.Load(grammar);
        var morpher = new Morpher(new TraceManager(), lang, 1) { MaxUnapplications = maxUnapp };

        // Warm up on the same prefix of the FULL list in every process, so JIT/grammar-cache state is
        // equivalent across slices regardless of which words land in this process's slice.
        for (int i = 0; i < System.Math.Min(10, allWords.Count); i++)
        {
            foreach (var _ in morpher.AnalyzeWord(allWords[i])) { }
        }

        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        long b0 = System.GC.GetTotalAllocatedBytes(true);
        int g0 = System.GC.CollectionCount(0),
            g1 = System.GC.CollectionCount(1),
            g2 = System.GC.CollectionCount(2);
        var sw = Stopwatch.StartNew();
        long analyses = 0;
        foreach (string word in slice)
        {
            foreach (var _ in morpher.AnalyzeWord(word))
                analyses++;
        }
        sw.Stop();
        long mb = (System.GC.GetTotalAllocatedBytes(true) - b0) / 1048576;
        double wps = slice.Count / sw.Elapsed.TotalSeconds;
        System.Console.WriteLine(
            $"WORKERSLICE idx={sliceIndex,2} of={sliceCount,2} words={slice.Count,4} analyses={analyses} "
                + $"ms={sw.ElapsedMilliseconds,7} wps={wps,7:F1} MB={mb,5} "
                + $"gen0={System.GC.CollectionCount(0) - g0,3} gen1={System.GC.CollectionCount(1) - g1,3} "
                + $"gen2={System.GC.CollectionCount(2) - g2,3} serverGC={System.Runtime.GCSettings.IsServerGC}"
        );
    }
}
