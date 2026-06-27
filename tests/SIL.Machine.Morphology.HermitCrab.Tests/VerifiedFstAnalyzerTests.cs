using System.Collections.Concurrent;
using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for the propose-and-verify spine (HERMITCRAB_FST_PLAN.md §11.8/§12): the FST proposes,
/// HC's own engine confirms each candidate by restricted re-analysis (<see cref="FstReplay"/>), and
/// the confirmed engine analysis is emitted. Exercises soundness (no false positives), the M2 fix
/// (yields genuine HC analyses with their category), the per-word opt-out, and thread-safety.
/// </summary>
public class VerifiedFstAnalyzerTests : HermitCrabTestBase
{
    private AffixProcessRule AddSuffix()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "NMLZ",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(sSuffix);
        return sSuffix;
    }

    [Test]
    public void Verified_MatchesSearch_OnConcatenativeCorpus()
    {
        AffixProcessRule suffix = AddSuffix();
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        string[] corpus = { "sag", "sags", "dat", "sagg" }; // inflected, bare, homograph, non-word
        AnalysisComparison comparison = FstVerification.Compare(search, verified, corpus);
        Assert.That(comparison.IsComplete, Is.True, comparison.Format());
        Morphophonemic.MorphologicalRules.Remove(suffix);
    }

    [Test]
    public void Verified_RejectsNonWord_NoFalsePositive()
    {
        IMorphologicalAnalyzer search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        Assert.That(search.AnalyzeWord("sagg"), Is.Empty, "precondition: sagg is a non-word");
        Assert.That(verified.AnalyzeWord("sagg"), Is.Empty, "verify must not analyze a non-word");
    }

    [Test]
    public void Verified_YieldsGenuineEngineAnalyses_WithCategory()
    {
        // M2: VerifiedFstAnalyzer must yield the matched HC analysis (real category), not the
        // category-less FST candidate. WordAnalysis.Equals includes Category, so set-equality vs the
        // engine fails if the category is dropped.
        var search = new Morpher(TraceManager, Language);
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        foreach (string word in new[] { "sag", "dat" })
        {
            var fromSearch = new HashSet<WordAnalysis>(search.AnalyzeWord(word));
            List<WordAnalysis> fromVerified = verified.AnalyzeWord(word).ToList();
            Assert.That(fromVerified, Is.Not.Empty, $"expected analyses for {word}");
            foreach (WordAnalysis a in fromVerified)
            {
                Assert.That(a.Category, Is.Not.Null, $"verified analysis of {word} lost its category");
                Assert.That(
                    fromSearch,
                    Does.Contain(a),
                    $"verified analysis of {word} is not a genuine engine analysis"
                );
            }
        }
    }

    [Test]
    public void CompleteHybrid_PerWordOptOut_EngineMatchesSearch()
    {
        string[] corpus = { "sag", "dat" };
        var search = new Morpher(TraceManager, Language);
        var complete = CompleteHybridMorpher.FromLanguage(TraceManager, Language, corpus);
        foreach (string word in corpus)
        {
            var engine = new HashSet<string>(complete.AnalyzeWord(word, useFst: false).Select(Sig));
            var fst = new HashSet<string>(complete.AnalyzeWord(word, useFst: true).Select(Sig));
            var oracle = new HashSet<string>(search.AnalyzeWord(word).Select(Sig));
            Assert.That(engine.SetEquals(oracle), Is.True, $"engine opt-out path wrong for {word}");
            Assert.That(fst.SetEquals(oracle), Is.True, $"fst path wrong for {word}");
        }
    }

    [Test]
    public void Verified_ParallelMatchesSequential()
    {
        AddSuffix();
        IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
        var corpus = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            corpus.AddRange(new[] { "sag", "sags", "dat", "sat", "saz", "sas", "sagg" });
        }
        Dictionary<string, string> sequential = corpus.Distinct().ToDictionary(w => w, w => SigSet(verified, w));
        var parallel = new ConcurrentDictionary<string, string>();
        Parallel.ForEach(corpus, w => parallel[w] = SigSet(verified, w));
        Assert.That(
            corpus.Distinct().All(w => parallel[w] == sequential[w]),
            Is.True,
            "concurrent analyses diverged from sequential"
        );
    }

    [Test]
    public void Verified_CoversPhonologicallyAlteredBareRoot()
    {
        // Surface-allomorph precompile (§C): an unconditional t→d rule means the underlying bare root
        // "dat" (entry 8) can ONLY surface as "dad". The old proposer (underlying arcs) misses it — its
        // "t" arc can't match surface "d", and BareRootValid rejected it (it doesn't surface as itself).
        // The surface-precompile builds an arc from the actual generated surface ("dad"), so the altered
        // bare root is now matched. Confirmed via probe: gen dat(8)→dad, and "dad" analyzes while "dat"
        // no longer does.
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(
                search.AnalyzeWord("dad").Any(),
                Is.True,
                "precondition: 'dad' analyzes (bare root 'dat' surfaces as 'dad')"
            );

            // Baseline: the underlying-only proposer (no-morpher ctor builds arcs from underlying shapes)
            // misses the altered surface — both "dad" readings are underlying "dat", so it has no "dad" arc.
            Assert.That(
                new FstTemplateAnalyzer(Language).AnalyzeWord("dad"),
                Is.Empty,
                "baseline: the underlying-only proposer must miss the phonologically-altered surface"
            );

            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "dad" });
            Assert.That(cmp.IsComplete, Is.True, "altered bare root not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
        }
    }

    [Test]
    public void ComposedPhonology_CoversCrossBoundaryAlternation_WherePrecompileMisses()
    {
        // Point 4 (C-exact, composition with phonology inverse): a CROSS-BOUNDARY rule the per-morpheme
        // precompile cannot see. A suffix inserts "t"; the root-final "g" devoices to "k" before that
        // suffixal "t" — so sag+SUF = "sagt" -> "sakt". The precompile sees the bare root ("sag", no
        // following t -> no devoicing) and the affix ("t") only in isolation, so it builds a "sagt" path
        // and MISSES "sakt". Composition un-applies the rule on the assembled surface and recovers it.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var tSuffix = new AffixProcessRule
        {
            Name = "t_suffix",
            Gloss = "TSF",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        tSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);
        var gDevoice = new RewriteRule
        {
            Name = "g_devoice",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "g")).Value,
        };
        gDevoice.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "k")).Value,
                RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
            }
        );
        Surface.PhonologicalRules.Add(gDevoice);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sakt").Any(), Is.True, "precondition: 'sakt' = sag+TSF (g->k / _t)");

            // Even the surface-precompile proposer misses the cross-boundary form.
            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(fst.AnalyzeWord("sakt"), Is.Empty, "baseline: per-morpheme precompile misses cross-boundary 'sakt'");

            var composed = new ComposedPhonologyProposer(Language, new Morpher(TraceManager, Language), fst);
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(new CompositeProposer(fst, composed), pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sakt" });
            Assert.That(cmp.IsComplete, Is.True, "cross-boundary alternation not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(gDevoice);
            Morphophonemic.MorphologicalRules.Remove(tSuffix);
        }
    }

    [Test]
    public void Composite_CoversFullReduplication_WhereFstAloneMisses()
    {
        // Point 3: full reduplication (copy the whole stem) is non-regular — the FST cannot represent
        // it, but the ReduplicationProposer strips one copy, recurses the residual through the FST, and
        // wraps it with the reduplication morpheme; verify confirms it as a genuine HC analysis.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "redup",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") }, // copy the stem twice
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sagsag").Any(), Is.True, "precondition: 'sagsag' = RED('sag')");

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(fst.AnalyzeWord("sagsag"), Is.Empty, "baseline: the FST alone cannot represent reduplication");
            Assert.That(fst.CoversAllConstructs, Is.False, "reduplication marks the FST not-fully-covered");

            var composite = new CompositeProposer(fst, new ReduplicationProposer(Language, fst));
            Assert.That(composite.CoversAllConstructs, Is.True, "the reduplication generator covers the skipped op");

            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sagsag" });
            Assert.That(cmp.IsComplete, Is.True, "reduplication not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    [Test]
    public void SurfacePhonology_AppliesRulesForwardToASegmentString()
    {
        // The forward helper applies synthesis phonology to a segment string in isolation: an
        // unconditional t->d rule means "t" surfaces as "d" (and the underlying form is always kept).
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            var sp = new SurfacePhonology(Language, new Morpher(TraceManager, Language));
            Assert.That(sp.Variants("t"), Does.Contain("d"), "'t' must surface as 'd'");
            Assert.That(sp.Variants("t"), Does.Contain("t"), "the underlying form is always included");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
        }
    }

    [Test]
    public void SurfacePhonology_BoundaryTier_RecoversAffixSurfaceFromNeighborContext()
    {
        // Point 1b (C-boundary): a suffixal "t" voices to "d" only AFTER "g". In isolation "t" stays
        // "t" (1a misses the alternation); with the left neighbor "g" the boundary tier recovers "d".
        var tVoice = new RewriteRule
        {
            Name = "t_voice",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        tVoice.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "g")).Value,
            }
        );
        Surface.PhonologicalRules.Add(tVoice);
        try
        {
            var sp = new SurfacePhonology(Language, new Morpher(TraceManager, Language));
            IReadOnlyCollection<string> variants = sp.Variants("t");
            Assert.That(variants, Does.Contain("t"), "underlying form is always included");
            Assert.That(
                variants,
                Does.Contain("d"),
                "boundary tier must recover the post-'g' surface 'd' (isolation alone would miss it)"
            );
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tVoice);
        }
    }

    [Test]
    public void Proposer_CoversPhonologicallyAlteredAffix()
    {
        // Point 1 (affix surface-precompile): a suffix inserts "t", but an unconditional t->d rule means
        // it can only surface as "d" — so "sag"+SUF = "sagt" -> "sagd". The underlying-only proposer
        // builds a "t" affix arc and misses "sagd"; the surface-precompile proposer builds the "d" arc.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var tSuffix = new AffixProcessRule
        {
            Name = "t_suffix",
            Gloss = "TSF",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        tSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sagd").Any(), Is.True, "precondition: 'sagd' = sag+TSF (t->d)");

            Assert.That(
                new FstTemplateAnalyzer(Language).AnalyzeWord("sagd"),
                Is.Empty,
                "baseline: the underlying-only proposer builds a 't' affix arc and misses the 'd' surface"
            );

            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(TraceManager, Language);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sagd" });
            Assert.That(cmp.IsComplete, Is.True, "altered affix not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
            Morphophonemic.MorphologicalRules.Remove(tSuffix);
        }
    }

    [Test]
    public void Composite_CoversInfixation_WhereFstAloneMisses()
    {
        // Point 2: infixation (affix inserted inside the stem). The FST recognizes but does not build
        // infix slots; the InfixProposer removes the infix's segments at each interior position, recurses
        // the residual through the FST, and appends the infix morpheme. Here an "a" is infixed after the
        // first segment: "sag" -> "s·a·ag" = "saag".
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var infix = new AffixProcessRule
        {
            Name = "a_infix",
            Gloss = "INF",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        infix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).Value, // first segment
                    Pattern<Word, ShapeNode>.New("2").Annotation(any).OneOrMore.Value, // rest of stem
                },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "a"), new CopyFromInput("2") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(infix);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("saag").Any(), Is.True, "precondition: 'saag' = INF('sag')");

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(fst.AnalyzeWord("saag"), Is.Empty, "baseline: the FST alone does not build infix slots");
            Assert.That(fst.CoversAllConstructs, Is.False, "infixation marks the FST not-fully-covered");

            var composite = new CompositeProposer(fst, new InfixProposer(Language, fst));
            Assert.That(composite.CoversAllConstructs, Is.True, "the infix generator covers the skipped op");

            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "saag" });
            Assert.That(cmp.IsComplete, Is.True, "infixation not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(infix);
        }
    }

    [Test]
    public void CompleteHybrid_WiresGenerators_ReduplicatingGrammarCertifiesAndMatchesEngine()
    {
        // Integration: the production factory must build the CompositeProposer (FST + generators), so a
        // reduplicating grammar certifies (the generator covers the construct the FST skips) and the
        // fast path matches the engine — not just the hand-built composite in the unit tests.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "redup",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        try
        {
            string[] corpus = { "sag", "sagsag", "dat" }; // bare, reduplicated, homograph
            var search = new Morpher(TraceManager, Language);
            var complete = CompleteHybridMorpher.FromLanguage(TraceManager, Language, corpus);
            Assert.That(complete.Certified, Is.True, "the reduplicating grammar must certify once generators are wired");
            foreach (string word in corpus.Append("zzz"))
            {
                var fast = new HashSet<string>(complete.AnalyzeWord(word).Select(Sig));
                var oracle = new HashSet<string>(search.AnalyzeWord(word).Select(Sig));
                Assert.That(fast.SetEquals(oracle), Is.True, $"fast path disagrees with the engine for {word}");
            }
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    private static string SigSet(IMorphologicalAnalyzer analyzer, string word) =>
        string.Join("|", analyzer.AnalyzeWord(word).Select(Sig).OrderBy(s => s, System.StringComparer.Ordinal));
}
