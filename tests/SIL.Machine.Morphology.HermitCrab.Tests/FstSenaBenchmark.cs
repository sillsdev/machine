using System.Collections.Concurrent;
using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Manual end-to-end benchmark on a real grammar: census/closure, build, per-analyzer timing + set
/// parity vs the search engine, a negative-example soundness check, and a parallel-consistency check.
/// [Explicit] — set HC_GRAMMAR (an HC config XML) and HC_WORDS (one word per line); optionally
/// HC_MAX_WORDS. The reference oracle runs with unlimited unapplications (the only sound+complete
/// baseline). Run:
///   $env:HC_GRAMMAR=...; $env:HC_WORDS=...; dotnet test --filter "FullyQualifiedName~FstSenaBenchmark"
/// </summary>
[TestFixture]
[Explicit("Manual FST-vs-search benchmark on an external grammar; not part of CI.")]
public class FstSenaBenchmark
{
    [Test]
    public void Benchmark_FstVsSearch()
    {
        (Language language, List<string> words) = Load();
        var search = new Morpher(new TraceManager(), language) { MaxUnapplications = 0 };

        GrammarFstReport census = GrammarFstAdvisor.Analyze(language);
        ClosureReport closure = GrammarFstClosure.Analyze(language);
        TestContext.Out.WriteLine($"census  : {census.Tier} ({census.EscapeCount} escapes)");
        TestContext.Out.WriteLine($"closure : {(closure.FstClosed ? "FST-CLOSED" : "not closed")}");

        var verified = new VerifiedFstAnalyzer(
            new FstTemplateAnalyzer(language, search),
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );
        var caching = CachingMorphologicalAnalyzer.FromLanguage(new TraceManager(), language, words);

        long searchMs = TimeParse("search  ", words, w => search.AnalyzeWord(w).Count());
        TimeParse("verified", words, w => verified.AnalyzeWord(w).Count());
        // Default (guaranteed-complete) path: FST-only when the grammar is certified, else engine+cache.
        TimeParse("caching ", words, w => caching.AnalyzeWord(w).Count());

        AnalysisComparison parity = FstVerification.Compare(search, verified, words);
        TestContext.Out.WriteLine(
            $"verified vs search : {(parity.IsComplete ? "IDENTICAL" : parity.Divergences.Count + " divergent words")}  "
                + $"(grammar certified = {caching.GrammarCertified} → "
                + (caching.GrammarCertified ? "FST-only, no full search" : "engine/cache backstop")
                + ")"
        );
        TestContext.Out.WriteLine($"(search total {searchMs} ms)");
    }

    /// <summary>
    /// Soundness on NEGATIVE examples: plausible-looking non-words (real words over-prefixed,
    /// over-suffixed, prefix-swapped, fake-reduplicated, fake-compounded) must analyze to NOTHING. We
    /// keep only true negatives (search = ∅), preferring those the raw FST proposes for (so the verify
    /// is exercised), then require the verified FST to also return ∅. A non-empty result is a false
    /// positive — the soundness failure this hunts for.
    /// </summary>
    [Test]
    public void Soundness_NegativeExamples()
    {
        (Language language, List<string> real0) = Load();
        int targetCount = int.TryParse(Environment.GetEnvironmentVariable("HC_NEG_COUNT"), out int nc) ? nc : 50;
        var search = new Morpher(new TraceManager(), language) { MaxUnapplications = 0 };
        var raw = new FstTemplateAnalyzer(language, search);
        var verified = new VerifiedFstAnalyzer(
            new FstTemplateAnalyzer(language, search),
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );

        List<string> real = real0.Take(80).ToList();
        string[] pre = { "ku", "a", "ci", "ka", "mu", "ma", "ni", "wa", "ti", "pa" };
        string[] suf = { "a", "e", "ira", "isa", "ka", "ni", "wa", "esa" };
        var candidates = new List<string>();
        for (int i = 0; i < real.Count; i++)
        {
            string w = real[i].ToLowerInvariant();
            foreach (string p in pre)
            {
                candidates.Add(p + w);
                if (w.Length > p.Length + 1 && w.StartsWith(p, StringComparison.Ordinal))
                {
                    foreach (string p2 in pre)
                    {
                        if (p2 != p)
                        {
                            candidates.Add(string.Concat(p2.AsSpan(), w.AsSpan(p.Length)));
                        }
                    }
                }
            }
            foreach (string s in suf)
            {
                candidates.Add(w + s);
            }
            candidates.Add(string.Concat(w.AsSpan(0, 2), w));
            if (i + 1 < real.Count)
            {
                candidates.Add(w + real[i + 1].ToLowerInvariant());
            }
        }

        int chosen = 0;
        int fstProposed = 0;
        int falsePositives = 0;
        var fp = new List<string>();
        var seen = new HashSet<string>();
        foreach (string c in candidates)
        {
            if (chosen >= targetCount || !seen.Add(c))
            {
                continue;
            }
            try
            {
                if (search.AnalyzeWord(c).Any())
                {
                    continue; // actually parses — not a negative
                }
                int rawCount = raw.AnalyzeWord(c).Count();
                int verifiedCount = verified.AnalyzeWord(c).Count();
                chosen++;
                if (rawCount > 0)
                {
                    fstProposed++;
                }
                if (verifiedCount != 0)
                {
                    falsePositives++;
                    if (fp.Count < 20)
                    {
                        fp.Add(c);
                    }
                }
            }
            catch (Exception) { }
        }

        TestContext.Out.WriteLine(
            $"negatives: {chosen}; raw FST proposed {fstProposed}; false positives {falsePositives}"
        );
        foreach (string e in fp)
        {
            TestContext.Out.WriteLine($"  FALSE POSITIVE: {e}");
        }
        Assert.That(chosen, Is.GreaterThanOrEqualTo(targetCount), "could not assemble enough true negatives");
        Assert.That(falsePositives, Is.Zero, "soundness FAILURE: verified FST analyzed a non-word");
    }

    /// <summary>Parallel-consistency: parsing the corpus concurrently must give the same analyses as
    /// sequentially (validates the pooled-Morpher thread-safety fix).</summary>
    [Test]
    public void Concurrent_MatchesSequential()
    {
        (Language language, List<string> words) = Load();
        var verified = new VerifiedFstAnalyzer(
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language)),
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );

        Dictionary<string, string> sequential = words.Distinct().ToDictionary(w => w, w => SigSet(verified, w));
        var parallel = new ConcurrentDictionary<string, string>();
        Parallel.ForEach(words.Distinct(), w => parallel[w] = SigSet(verified, w));

        int mismatches = sequential.Count(kv => parallel[kv.Key] != kv.Value);
        TestContext.Out.WriteLine($"parallel vs sequential: {mismatches} mismatches of {sequential.Count} words");
        Assert.That(mismatches, Is.Zero, "thread-safety FAILURE: concurrent analyses differ from sequential");
    }

    private static string SigSet(IMorphologicalAnalyzer analyzer, string word)
    {
        return string.Join(
            "|",
            analyzer
                .AnalyzeWord(word)
                .Select(a =>
                    string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex
                )
                .OrderBy(s => s, StringComparer.Ordinal)
        );
    }

    private static (Language, List<string>) Load()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_GRAMMAR");
        string? wordsPath = Environment.GetEnvironmentVariable("HC_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
        {
            Assert.Ignore("set HC_GRAMMAR and HC_WORDS");
        }
        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_MAX_WORDS"), out int mw) ? mw : 60;
        Language language = XmlLanguageLoader.Load(grammarPath!);
        List<string> words = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();
        return (language, words);
    }

    private static long TimeParse(string label, List<string> words, Func<string, int> parse)
    {
        try
        {
            parse(words[0]); // warm up
        }
        catch (Exception) { }
        var sw = Stopwatch.StartNew();
        long total = 0;
        foreach (string w in words)
        {
            try
            {
                total += parse(w);
            }
            catch (Exception) { }
        }
        sw.Stop();
        TestContext.Out.WriteLine(
            $"{label} : {sw.ElapsedMilliseconds, 7} ms  ({(double)sw.ElapsedMilliseconds / words.Count:F1} ms/word, {total} analyses)"
        );
        return sw.ElapsedMilliseconds;
    }
}
