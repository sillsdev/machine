using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// End-to-end tests/benchmark for the out-of-process, Server-GC HermitCrab parser
/// (HermitCrabServerClient + HermitCrabServerHost). Point HC_GRAMMAR/HC_WORDS at a real
/// FLEx-exported grammar + word list (e.g. the Sena project) for meaningful numbers.
/// </summary>
[TestFixture]
public class HermitCrabServerTests
{
    // The worker must be launched from a build output that carries its own runtimeconfig.json
    // (so Server GC + the right framework apply). Locate the Server project's Release output.
    private static string WorkerAssemblyPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Machine.sln")))
            dir = dir.Parent;
        Assert.That(dir, Is.Not.Null, "could not locate repo root");
        string path = Path.Combine(
            dir!.FullName,
            "src",
            "SIL.Machine.Morphology.HermitCrab.Server",
            "bin",
            "Release",
            "net10.0",
            "SIL.Machine.Morphology.HermitCrab.Server.dll"
        );
        Assert.That(File.Exists(path), Is.True, $"build the Server project in Release first: {path}");
        return path;
    }

    private static string GrammarPath()
    {
        string? grammar = Environment.GetEnvironmentVariable("HC_GRAMMAR");
        if (grammar == null)
            Assert.Ignore("set HC_GRAMMAR to a compiled HermitCrab config to run this test");
        return grammar!;
    }

    [Test]
    [Explicit("Requires HC_GRAMMAR (a compiled HermitCrab config).")]
    public void Server_MatchesInProcess_AndRunsServerGc()
    {
        string grammar = GrammarPath();
        string[] words = { "kuzala", "akele", "pamaso", "mafuta", "xyzzy" };
        string? wordsFile = Environment.GetEnvironmentVariable("HC_WORDS");
        if (wordsFile != null)
            words = File.ReadAllLines(wordsFile).Take(40).ToArray();

        // In-process baseline.
        Language language = XmlLanguageLoader.Load(grammar);
        var inProc = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);

        using var client = new HermitCrabServerClient(grammar, workerAssemblyPath: WorkerAssemblyPath());

        TestContext.Out.WriteLine($"host GC: {(System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")}");
        TestContext.Out.WriteLine($"worker GC: {client.WorkerGarbageCollectorMode}");
        Assert.That(client.WorkerGarbageCollectorMode, Is.EqualTo("Server"), "worker should run Server GC");
        Assert.That(System.Runtime.GCSettings.IsServerGC, Is.False, "host (test) should NOT be Server GC");

        IReadOnlyList<IReadOnlyList<WordAnalysis>> batch = client.AnalyzeWords(words);
        for (int i = 0; i < words.Length; i++)
        {
            string expected = Signature(inProc.AnalyzeWord(words[i]));
            string actual = Signature(batch[i]);
            Assert.That(actual, Is.EqualTo(expected), $"server disagrees with in-process for '{words[i]}'");
        }
    }

    [Test]
    [Explicit("Benchmark; requires HC_GRAMMAR + HC_WORDS.")]
    public void Server_Benchmark()
    {
        string grammar = GrammarPath();
        string? wordsFile = Environment.GetEnvironmentVariable("HC_WORDS");
        if (wordsFile == null)
            Assert.Ignore("set HC_WORDS to a word-per-line file");
        var words = new List<string>(new HashSet<string>(File.ReadAllLines(wordsFile!)));
        if (int.TryParse(Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int max) && words.Count > max)
            words = words.GetRange(0, max);

        using var client = new HermitCrabServerClient(grammar, workerAssemblyPath: WorkerAssemblyPath());
        // warm up
        client.AnalyzeWords(words.GetRange(0, System.Math.Min(20, words.Count)));

        var sw = Stopwatch.StartNew();
        IReadOnlyList<IReadOnlyList<WordAnalysis>> results = client.AnalyzeWords(words);
        sw.Stop();

        int total = 0;
        foreach (IReadOnlyList<WordAnalysis> r in results)
            total += r.Count;
        TestContext.Out.WriteLine($"host GC: {(System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation")}, worker GC: {client.WorkerGarbageCollectorMode}");
        TestContext.Out.WriteLine($"out-of-process batch: {words.Count} words, {total} analyses, {sw.ElapsedMilliseconds} ms");
    }

    private static string Signature(IEnumerable<WordAnalysis> analyses)
    {
        return string.Join(
            "|",
            analyses
                .Select(a => string.Join("+", a.Morphemes.Select(m => m.Id)) + ":" + a.RootMorphemeIndex)
                .OrderBy(s => s, System.StringComparer.Ordinal)
        );
    }
}
