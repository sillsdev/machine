using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for the two-path caching analyzer (HERMITCRAB_FST_PLAN.md §13): the default path is
/// guaranteed-complete (engine, cached); the fast path is provisional until warmed; warming fills the
/// cache; and the cache persists across sessions with a grammar-version guard.
/// </summary>
public class CachingMorphologicalAnalyzerTests : HermitCrabTestBase
{
    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    private static HashSet<string> SigSet(IEnumerable<WordAnalysis> analyses) => new(analyses.Select(Sig));

    [Test]
    public void Default_IsGuaranteedComplete_AndMatchesEngine()
    {
        var engine = new Morpher(TraceManager, Language);
        var caching = CachingMorphologicalAnalyzer.FromLanguage(TraceManager, Language);
        foreach (string word in new[] { "sag", "dat", "sagg" })
        {
            Assert.That(SigSet(caching.AnalyzeWord(word)).SetEquals(SigSet(engine.AnalyzeWord(word))), Is.True, word);
        }
        // the second call is served from the cache (same result)
        Assert.That(SigSet(caching.AnalyzeWord("dat")).SetEquals(SigSet(engine.AnalyzeWord("dat"))), Is.True);
        Assert.That(caching.Cache.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Fast_IsProvisionalUntilWarmed_ThenComplete()
    {
        var caching = CachingMorphologicalAnalyzer.FromLanguage(TraceManager, Language);

        FastAnalysisResult before = caching.AnalyzeWordFast("dat");
        Assert.That(before.IsComplete, Is.False, "uncached fast result must be flagged provisional");

        caching.Warm(new[] { "dat" });

        FastAnalysisResult after = caching.AnalyzeWordFast("dat");
        Assert.That(after.IsComplete, Is.True, "after warming the fast result is the cached complete set");
        var engine = new Morpher(TraceManager, Language);
        Assert.That(SigSet(after.Analyses).SetEquals(SigSet(engine.AnalyzeWord("dat"))), Is.True);
    }

    [Test]
    public void Warm_FillsCacheForCorpus()
    {
        var caching = CachingMorphologicalAnalyzer.FromLanguage(TraceManager, Language);
        string[] corpus = { "sag", "dat", "sat", "saz" };
        caching.Warm(corpus);
        foreach (string w in corpus)
        {
            Assert.That(caching.AnalyzeWordFast(w).IsComplete, Is.True, $"{w} should be cached after warm");
        }
    }

    [Test]
    public void Certified_Grammar_SkipsEngine_FastIsProvenComplete()
    {
        // A certified grammar (FST-closed + set-parity) treats the FST as proven complete: no full
        // search, no cache, and the fast result is flagged complete.
        var fast = new VerifiedFstAnalyzer(TraceManager, Language);
        var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
        var certified = new CachingMorphologicalAnalyzer(fast, pool, new AnalysisCache(), grammarCertified: true);

        Assert.That(certified.GrammarCertified, Is.True);
        FastAnalysisResult r = certified.AnalyzeWordFast("dat");
        Assert.That(r.IsComplete, Is.True, "certified grammar: fast result is proven complete without warming");

        certified.AnalyzeWord("dat").ToList(); // default path
        Assert.That(certified.Cache.Count, Is.Zero, "certified grammar must never run the engine / populate the cache");
    }

    [Test]
    public void Persistence_RoundTrips_AndVersionGuardRejectsStale()
    {
        var caching = CachingMorphologicalAnalyzer.FromLanguage(TraceManager, Language);
        string[] corpus = { "sag", "dat", "sat", "sagg" }; // includes a non-word (empty analysis)
        caching.Warm(corpus);

        var registry = new MorphemeRegistry(Language);
        var buffer = new StringWriter();
        AnalysisCacheSerializer.Save(caching.Cache, registry, "v1", buffer);
        string serialized = buffer.ToString();

        // Reload into a fresh cache against the same grammar + version.
        var reloaded = new AnalysisCache();
        bool ok = AnalysisCacheSerializer.Load(reloaded, registry, "v1", new StringReader(serialized));
        Assert.That(ok, Is.True);
        Assert.That(reloaded.Count, Is.EqualTo(caching.Cache.Count));
        foreach (string w in corpus)
        {
            Assert.That(reloaded.TryGet(w, out IReadOnlyList<WordAnalysis> a), Is.True, w);
            Assert.That(caching.Cache.TryGet(w, out IReadOnlyList<WordAnalysis> orig), Is.True);
            Assert.That(SigSet(a).SetEquals(SigSet(orig)), Is.True, $"round-trip mismatch for {w}");
        }

        // A different grammar version must be rejected (stale cache → re-warm).
        var rejected = new AnalysisCache();
        bool loadedStale = AnalysisCacheSerializer.Load(rejected, registry, "v2", new StringReader(serialized));
        Assert.That(loadedStale, Is.False);
        Assert.That(rejected.Count, Is.Zero);
    }
}
