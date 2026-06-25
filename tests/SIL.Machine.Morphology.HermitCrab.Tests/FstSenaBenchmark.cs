using System.Collections.Concurrent;
using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Manual end-to-end benchmark on a real grammar: grammar census, build, per-analyzer timing + a
/// gap-diagnostic comparison vs the search engine, a negative-example soundness check, and a
/// parallel-consistency check. [Explicit] — set HC_GRAMMAR (an HC config XML) and HC_WORDS (one word
/// per line); optionally HC_MAX_WORDS. The reference oracle runs with unlimited unapplications (the
/// only sound+complete baseline). Run:
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
        TestContext.Out.WriteLine($"census  : {census.Tier} ({census.EscapeCount} escapes)");

        var fst = new FstTemplateAnalyzer(language, search);
        TestContext.Out.WriteLine($"FST states: {fst.StateCount}");
        var verified = new VerifiedFstAnalyzer(fst, new MorpherPool(() => new Morpher(new TraceManager(), language)));

        long searchMs = TimeParse("search  ", words, w => search.AnalyzeWord(w).Count());
        TimeParse("verified", words, w => verified.AnalyzeWord(w).Count());

        AnalysisComparison gap = FstVerification.Compare(search, verified, words);
        TestContext.Out.WriteLine(
            $"verified vs search : {(gap.MatchesReferenceExactly ? "IDENTICAL" : gap.Divergences.Count + " divergent words")}"
        );
        TestContext.Out.WriteLine($"(search total {searchMs} ms)");
    }

    /// <summary>
    /// Composite (FST + reduplication + infix + phonology-composition generators) vs the bare FST,
    /// both verified, against the search oracle: how many words each fully covers (set parity), whether
    /// the composite is a sound subset of search (no false positives), and the extra coverage the
    /// generators buy on a real grammar.
    /// </summary>
    [Test]
    public void Benchmark_CompositeVsSearch()
    {
        (Language language, List<string> words) = Load();
        var search = new Morpher(new TraceManager(), language) { MaxUnapplications = 0 };
        var bare = new VerifiedFstAnalyzer(
            new FstTemplateAnalyzer(language, search),
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );
        CompositeProposer composite = CompositeProposer.ForLanguage(
            language,
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language))
        );
        var composed = new VerifiedFstAnalyzer(
            composite,
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );

        int bareFull = 0,
            compFull = 0,
            unsound = 0,
            wordsWithAnalysis = 0;
        foreach (string w in words)
        {
            var oracle = new HashSet<string>(search.AnalyzeWord(w).Select(Sig));
            var bareSet = new HashSet<string>(bare.AnalyzeWord(w).Select(Sig));
            var compSet = new HashSet<string>(composed.AnalyzeWord(w).Select(Sig));
            if (oracle.Count > 0)
                wordsWithAnalysis++;
            if (bareSet.SetEquals(oracle))
                bareFull++;
            if (compSet.SetEquals(oracle))
                compFull++;
            if (!compSet.IsSubsetOf(oracle))
                unsound++; // composite produced an analysis the engine did not — a soundness failure
        }
        TestContext.Out.WriteLine($"words: {words.Count} ({wordsWithAnalysis} with an analysis)");
        TestContext.Out.WriteLine($"fully covered — bare FST: {bareFull}, composite: {compFull}");
        TestContext.Out.WriteLine($"composite unsound words (⊄ search): {unsound}");
        Assert.That(unsound, Is.Zero, "soundness: composite must never produce a non-engine analysis");
        Assert.That(
            compFull,
            Is.GreaterThanOrEqualTo(bareFull),
            "composite must cover at least as much as the bare FST"
        );
    }

    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    /// <summary>Measure the forward-synthesis precompile: build cost, table size, how many words it lifts
    /// to full coverage over the bare composite, and that it stays a sound subset of the engine.</summary>
    [Test]
    public void Benchmark_ForwardSynthVsSearch()
    {
        (Language language, List<string> words) = Load();
        var search = new Morpher(new TraceManager(), language) { MaxUnapplications = 0 };
        var pool = new MorpherPool(() => new Morpher(new TraceManager(), language));

        var bareComposite = CompositeProposer.ForLanguage(
            language,
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language))
        );
        var bare = new VerifiedFstAnalyzer(bareComposite, pool);

        var sw = Stopwatch.StartNew();
        int maxAffixes = int.TryParse(Environment.GetEnvironmentVariable("HC_MAX_AFFIXES"), out int ma) ? ma : 2;
        var synth = new ForwardSynthesisProposer(language, new Morpher(new TraceManager(), language), maxAffixes);
        sw.Stop();
        var fullComposite = new CompositeProposer(
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language)),
            synth,
            new ReduplicationProposer(language, new FstTemplateAnalyzer(language)),
            new InfixProposer(language, new FstTemplateAnalyzer(language))
        );
        var full = new VerifiedFstAnalyzer(fullComposite, pool);
        TestContext.Out.WriteLine(
            $"forward-synth build: {sw.ElapsedMilliseconds} ms, {synth.EntryCount} entries, capped={synth.WasCapped}"
        );

        int bareFull = 0,
            fullFull = 0,
            unsound = 0,
            analyzable = 0;
        foreach (string w in words)
        {
            var oracle = search.AnalyzeWord(w).Select(Sig).ToHashSet();
            var b = bare.AnalyzeWord(w).Select(Sig).ToHashSet();
            var f = full.AnalyzeWord(w).Select(Sig).ToHashSet();
            if (oracle.Count > 0)
                analyzable++;
            if (b.SetEquals(oracle))
                bareFull++;
            if (f.SetEquals(oracle))
            {
                fullFull++;
            }
            else if (oracle.Count > 0)
            {
                TestContext.Out.WriteLine(
                    $"  still missed {w}: engine={oracle.Count} fst={f.Count} | {string.Join(" ; ", oracle.Except(f))}"
                );
            }
            if (!f.IsSubsetOf(oracle))
                unsound++;
        }
        TestContext.Out.WriteLine($"words: {words.Count} ({analyzable} analyzable)");
        TestContext.Out.WriteLine($"fully covered — bare composite: {bareFull}, +forward-synth: {fullFull}");
        TestContext.Out.WriteLine($"forward-synth unsound words (⊄ search): {unsound}");
        Assert.That(unsound, Is.Zero, "soundness: forward-synth must never produce a non-engine analysis");
        Assert.That(fullFull, Is.GreaterThanOrEqualTo(bareFull));
    }

    /// <summary>Diagnostic: list the words the composite under-generates on, with what the engine found
    /// that the FST missed, and dump the census escapes — to see WHICH constructs block coverage.</summary>
    [Test]
    public void Diagnose_Divergences()
    {
        (Language language, List<string> words) = Load();
        var search = new Morpher(new TraceManager(), language) { MaxUnapplications = 0 };
        CompositeProposer composite = CompositeProposer.ForLanguage(
            language,
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language))
        );
        var composed = new VerifiedFstAnalyzer(
            composite,
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );

        GrammarFstReport census = GrammarFstAdvisor.Analyze(language);
        TestContext.Out.WriteLine($"=== census escapes ({census.EscapeCount}) ===");
        foreach (GrammarAdvisory e in census.Escapes.Take(40))
        {
            TestContext.Out.WriteLine(
                $"  ESCAPE [{e.Kind}] rule={e.Rule} stratum={e.Stratum} regular={e.Regular}: {e.Issue}"
            );
        }

        TestContext.Out.WriteLine("=== divergent words (engine finds, FST misses) ===");
        foreach (string w in words)
        {
            var oracle = search.AnalyzeWord(w).Select(Sig).ToHashSet();
            var comp = composed.AnalyzeWord(w).Select(Sig).ToHashSet();
            if (!comp.SetEquals(oracle))
            {
                var missed = oracle.Except(comp).ToList();
                TestContext.Out.WriteLine(
                    $"  {w}: engine={oracle.Count} fst={comp.Count} | missed: {string.Join("  ;  ", missed)}"
                );
            }
        }
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

    /// <summary>Parallel wall-clock throughput at a controlled degree of parallelism (HC_THREADS, default
    /// 16): the pooled search engine (oracle, unbounded) vs the bare verified-FST path (the always-fast,
    /// not-necessarily-complete "first pass" — <see cref="VerifiedFstAnalyzer"/>, unconditional and
    /// always used, never gated on anything). Both sides rent a per-call <see cref="Morpher"/> from a
    /// <see cref="MorpherPool"/> — a single shared <see cref="Morpher"/> hammered by concurrent
    /// <c>AnalyzeWord</c> calls is not an established-safe pattern in this codebase (only the
    /// pool-per-call pattern is validated by <c>Concurrent_MatchesSequential</c>), so the oracle side must
    /// pool too for the comparison to be apples-to-apples. Reports whether the process actually got Server
    /// GC (<c>DOTNET_gcServer=1</c> env var, checked at process start).</summary>
    [Test]
    public void Benchmark_ParallelThroughput()
    {
        (Language language, List<string> words) = Load();
        int degree = int.TryParse(Environment.GetEnvironmentVariable("HC_THREADS"), out int t) ? t : 16;
        List<string> load = words.Distinct().ToList();

        var searchPool = new MorpherPool(() => new Morpher(new TraceManager(), language) { MaxUnapplications = 0 });
        var verified = new VerifiedFstAnalyzer(
            new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language)),
            new MorpherPool(() => new Morpher(new TraceManager(), language))
        );

        TestContext.Out.WriteLine($"Server GC: {System.Runtime.GCSettings.IsServerGC}");
        TestContext.Out.WriteLine($"threads={degree}  distinct words={load.Count}");

        var po = new ParallelOptions { MaxDegreeOfParallelism = degree };

        // warm up (JIT + FST build) outside the timed region
        Morpher warm = searchPool.Rent();
        warm.AnalyzeWord(load[0]).Count();
        searchPool.Return(warm);
        verified.AnalyzeWord(load[0]).Count();

        long searchAnalyses = 0;
        var swSearch = Stopwatch.StartNew();
        Parallel.ForEach(
            load,
            po,
            w =>
            {
                Morpher m = searchPool.Rent();
                try
                {
                    int n = m.AnalyzeWord(w).Count();
                    Interlocked.Add(ref searchAnalyses, n);
                }
                finally
                {
                    searchPool.Return(m);
                }
            }
        );
        swSearch.Stop();

        long fstAnalyses = 0;
        var swFst = Stopwatch.StartNew();
        Parallel.ForEach(
            load,
            po,
            w =>
            {
                int n = verified.AnalyzeWord(w).Count();
                Interlocked.Add(ref fstAnalyses, n);
            }
        );
        swFst.Stop();

        TestContext.Out.WriteLine(
            $"search   (parallel, pooled) : {swSearch.ElapsedMilliseconds, 7} ms  "
                + $"({(double)swSearch.ElapsedMilliseconds / load.Count:F3} ms/word, {searchAnalyses} analyses)"
        );
        TestContext.Out.WriteLine(
            $"verified (parallel)         : {swFst.ElapsedMilliseconds, 7} ms  "
                + $"({(double)swFst.ElapsedMilliseconds / load.Count:F3} ms/word, {fstAnalyses} analyses)"
        );
        TestContext.Out.WriteLine(
            $"speedup: {(double)swSearch.ElapsedMilliseconds / Math.Max(1, swFst.ElapsedMilliseconds):F1}x"
        );
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

    /// <summary>End-to-end FstCoverageProbe benchmark over the FULL wordlist (not the HC_MAX_WORDS-capped
    /// slice the other benchmarks use): coverage + p50/p95 per-word latency. Set HC_PROBE_MAX_WORDS to
    /// cap it (default: the whole file — Sena's is 7,121 words).</summary>
    [Test]
    public void Benchmark_FullCorpusProbe()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_GRAMMAR");
        string? wordsPath = Environment.GetEnvironmentVariable("HC_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
        {
            Assert.Ignore("set HC_GRAMMAR and HC_WORDS");
        }
        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_PROBE_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;
        Language language = XmlLanguageLoader.Load(grammarPath!);
        List<string> words = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();

        FstCoverageProbe probe = FstCoverageProbe.ForLanguage(language);
        var latenciesMs = new List<double>(words.Count);
        int parsed = 0;
        var sw = Stopwatch.StartNew();
        foreach (string word in words)
        {
            ProbeReport single = probe.Probe(new[] { word });
            latenciesMs.Add(single.Elapsed.TotalMilliseconds);
            if (single.ParsedWords > 0)
            {
                parsed++;
            }
        }
        sw.Stop();
        latenciesMs.Sort();
        double P(double pct) => latenciesMs[(int)System.Math.Min(latenciesMs.Count - 1, pct * latenciesMs.Count)];

        TestContext.Out.WriteLine($"words: {words.Count}, parsed: {parsed} ({(double)parsed / words.Count:P1})");
        TestContext.Out.WriteLine($"total wall time: {sw.ElapsedMilliseconds} ms");
        TestContext.Out.WriteLine($"p50: {P(0.50):F3} ms/word, p95: {P(0.95):F3} ms/word, p99: {P(0.99):F3} ms/word");
    }

    /// <summary>Genuinely parallel (HC_THREADS, default 16), FST-only (no oracle, so no open-ended
    /// runtime risk on a large corpus) throughput benchmark over the FULL wordlist. Distinct from
    /// Benchmark_FullCorpusProbe (deliberately single-threaded, for clean per-word latency percentiles)
    /// and Benchmark_ParallelThroughput (parallel, but pairs against the oracle, and HC_MAX_WORDS
    /// defaults to a 60-word cap) — this is the "how fast is the FST alone across all cores over
    /// everything" number.</summary>
    [Test]
    public void Benchmark_FullCorpusParallelThroughput()
    {
        string? grammarPath = Environment.GetEnvironmentVariable("HC_GRAMMAR");
        string? wordsPath = Environment.GetEnvironmentVariable("HC_WORDS");
        if (string.IsNullOrEmpty(grammarPath) || string.IsNullOrEmpty(wordsPath))
        {
            Assert.Ignore("set HC_GRAMMAR and HC_WORDS");
        }
        int maxWords = int.TryParse(Environment.GetEnvironmentVariable("HC_PROBE_MAX_WORDS"), out int mw)
            ? mw
            : int.MaxValue;
        int degree = int.TryParse(Environment.GetEnvironmentVariable("HC_THREADS"), out int t) ? t : 16;
        Language language = XmlLanguageLoader.Load(grammarPath!);
        List<string> words = File.ReadAllLines(wordsPath!)
            .Select(w => w.Trim())
            .Where(w => w.Length > 0)
            .Take(maxWords)
            .ToList();

        var fst = new FstTemplateAnalyzer(language, new Morpher(new TraceManager(), language));
        CompositeProposer composite = CompositeProposer.ForLanguage(language, fst);
        var pool = new MorpherPool(() => new Morpher(new TraceManager(), language));
        var verified = new VerifiedFstAnalyzer(composite, pool);

        TestContext.Out.WriteLine($"Server GC: {System.Runtime.GCSettings.IsServerGC}");
        TestContext.Out.WriteLine($"threads={degree}  words={words.Count}");

        var po = new ParallelOptions { MaxDegreeOfParallelism = degree };
        verified.AnalyzeWord(words[0]).Count(); // warm up outside the timed region

        long analyses = 0;
        int parsed = 0;
        var sw = Stopwatch.StartNew();
        Parallel.ForEach(
            words,
            po,
            w =>
            {
                int n = verified.AnalyzeWord(w).Count();
                Interlocked.Add(ref analyses, n);
                if (n > 0)
                {
                    Interlocked.Increment(ref parsed);
                }
            }
        );
        sw.Stop();

        TestContext.Out.WriteLine($"parsed: {parsed}/{words.Count} ({(double)parsed / words.Count:P1})");
        TestContext.Out.WriteLine(
            $"total wall time: {sw.ElapsedMilliseconds} ms "
                + $"({(double)sw.ElapsedMilliseconds / words.Count:F3} ms/word average, {analyses} analyses)"
        );
        TestContext.Out.WriteLine($"throughput: {words.Count / sw.Elapsed.TotalSeconds:F0} words/sec");
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
