using System.Collections.Concurrent;
using NUnit.Framework;
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
        Assert.That(comparison.MatchesReferenceExactly, Is.True, comparison.Format());
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
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
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
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "altered bare root not covered: " + cmp.Format());

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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);
        var gDevoice = new RewriteRule
        {
            Name = "g_devoice",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "g")).Value,
        };
        gDevoice.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "k")).Value,
                RightEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
            }
        );
        Surface.PhonologicalRules.Add(gDevoice);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sakt").Any(), Is.True, "precondition: 'sakt' = sag+TSF (g->k / _t)");

            // Even the surface-precompile proposer misses the cross-boundary form.
            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(
                fst.AnalyzeWord("sakt"),
                Is.Empty,
                "baseline: per-morpheme precompile misses cross-boundary 'sakt'"
            );

            var composed = new ComposedPhonologyProposer(Language, fst);
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(new CompositeProposer(fst, composed), pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sakt" });
            Assert.That(
                cmp.MatchesReferenceExactly,
                Is.True,
                "cross-boundary alternation not covered: " + cmp.Format()
            );

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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
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
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "reduplication not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");

            // Soundness of the generalized (any-length, not just half-word) copy scan added for partial
            // (CV-style) reduplication support: "sasag" has an incidental short prefix repeat ("sa"+"sag"
            // starts with "sa" again) that is NOT a real application of this full-copy-only rule. The
            // raw proposer may well propose it (that is the point of scanning every length), but verify
            // must reject it — this grammar's redup rule only produces base+base, never CV+base.
            Assert.That(
                verified.AnalyzeWord("sasag"),
                Is.Empty,
                "soundness: a coincidental short prefix repeat must not be confirmed by a full-copy-only rule"
            );
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    [Test]
    public void Composite_CoversSeparatorReduplication_WhereFstAloneMisses()
    {
        // Phase D (FST_FULL_GRAMMAR_PLAN.md): a copy separated by a literal character rather than
        // sitting immediately adjacent to the base — the shape Indonesian's "-Cont" produces
        // (menulis-nulis). This toy rule uses a full copy (base+sep+base) rather than a genuine partial
        // tail copy (base+sep+TAIL — the real Indonesian shape, which needs a multi-group Lhs pattern;
        // no existing test in this repo builds one, so it's unvalidated territory not attempted here,
        // same call Phase 4 made for CV-template partial reduplication). The full-121-word Indonesian
        // corpus benchmark is the positive evidence for the TAIL case specifically (93/121 -> 120/121
        // this session); this test covers the separator-scan mechanism itself and its soundness.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "sep_redup",
            Gloss = "CONT",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "z"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("sagzsag").Any(), Is.True, "precondition: 'sagzsag' = CONT('sag')");

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(
                fst.AnalyzeWord("sagzsag"),
                Is.Empty,
                "baseline: the FST alone cannot represent separator reduplication"
            );

            var composite = new CompositeProposer(fst, new ReduplicationProposer(Language, fst));
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sagzsag" });
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "separator reduplication not covered: " + cmp.Format());

            // "sagzag" passes the surface shape check the scan looks for ("ag" is a genuine tail of
            // "sag", so the separator scan DOES propose residual "sag") but this toy rule only produces
            // a FULL copy (base+sep+base = "sagzsag"), never a tail copy — verify must reject it.
            Assert.That(
                verified.AnalyzeWord("sagzag"),
                Is.Empty,
                "soundness: a tail-copy candidate must not be confirmed by a full-copy-only rule"
            );
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    [Test]
    public void Composite_CoversSuffixStackedOutsideReduplication_WhereSeparatorScanAloneMisses()
    {
        // Phase G1 (FST_FULL_GRAMMAR_PLAN.md): Indonesian's mengamat-amati is meng+amat -> -Cont ->
        // mengamat-amat -> -i(LOC) -> mengamat-amati - a plain suffix rule applied AFTER reduplication,
        // which (since it just appends at the very end) lands on the tail of the copy. The separator
        // scan alone sees copy="sags" against base="sagzsag" and finds no tail match (the trailing "s"
        // isn't part of any copy); it must additionally try peeling a known suffix surface off the
        // copy's end before re-testing the remainder as a tail.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "sep_redup2",
            Gloss = "CONT",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "z"), new CopyFromInput("1") },
            }
        );
        var trailingSuffix = new AffixProcessRule
        {
            Name = "trailing_suffix",
            Gloss = "TRL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        trailingSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "s") }, // suffix order
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        Morphophonemic.MorphologicalRules.Add(trailingSuffix);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(
                search.AnalyzeWord("sagzsags").Any(),
                Is.True,
                "precondition: 'sagzsags' = TRL(CONT('sag')) — the engine must stack the plain suffix on top of the reduplicated form"
            );

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            var composite = new CompositeProposer(fst, new ReduplicationProposer(Language, fst));
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);

            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "sagzsags" });
            Assert.That(
                cmp.MatchesReferenceExactly,
                Is.True,
                "suffix stacked outside reduplication not covered: " + cmp.Format()
            );

            Assert.That(
                verified.AnalyzeWord("sagzdats"),
                Is.Empty,
                "soundness: a suffix-peeled candidate whose stripped copy isn't a real tail must be rejected"
            );
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
            Morphophonemic.MorphologicalRules.Remove(trailingSuffix);
        }
    }

    [Test]
    public void Fst_CoversCompound_ViaTheCompoundLoop()
    {
        // Phase G2 (FST_FULL_GRAMMAR_PLAN.md): the FST couldn't represent a compound at all until the
        // compound loop landed directly in FstTemplateAnalyzer (a shared "join" state every root's
        // chain end feeds into and every root's chain entry feeds out of — unlike reduplication/infix,
        // this needed no sibling IConstructProposer) plus the FstReplay.Confirm fix (it used to reject
        // any candidate with a second LexEntry morpheme outright). The originally-documented
        // "cross-cutting WordAnalysis/MorphToken data-model lift" premise for this was wrong — both
        // types already represent compounds (MorphOp.Compound); the only real blocker was FstReplay.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        LexEntry head = AddEntry(
            "compound_head",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Surface,
            "pat"
        );
        LexEntry nonHead = AddEntry(
            "compound_nonhead",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Surface,
            "tak"
        );
        var compound = new CompoundingRule { Name = "compound_rule" };
        Surface.MorphologicalRules.Add(compound);
        compound.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, int>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, int>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("pattak").Any(), Is.True, "precondition: 'pattak' = pat+tak compound");

            var fst = new FstTemplateAnalyzer(Language, new Morpher(TraceManager, Language));
            Assert.That(
                fst.AnalyzeWord("pattak"),
                Is.Not.Empty,
                "the compound loop must let the bare FST propose the compound directly (no sibling generator needed)"
            );

            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(fst, pool);
            AnalysisComparison cmp = FstVerification.Compare(search, verified, new[] { "pattak" });
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "compound not covered: " + cmp.Format());

            // Soundness / boundedness: the compound loop is bounded to exactly ONE extra root (no arc
            // back into the join), matching CompoundingRule's own default MaxApplicationCount of 1 — a
            // three-root chain must be rejected by both the real engine and the verified FST alike.
            Assert.That(
                search.AnalyzeWord("pattakpat"),
                Is.Empty,
                "precondition: a 3-root chain exceeds MaxApplicationCount=1, so the engine itself rejects it"
            );
            Assert.That(
                verified.AnalyzeWord("pattakpat"),
                Is.Empty,
                "soundness: the compound loop must not chain a third root"
            );
        }
        finally
        {
            Surface.MorphologicalRules.Remove(compound);
            Surface.Entries.Remove(head);
            Surface.Entries.Remove(nonHead);
            Entries.Remove("compound_head");
            Entries.Remove("compound_nonhead");
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
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
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
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tVoice.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value,
                LeftEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "g")).Value,
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "t") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(tSuffix);
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
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
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "altered affix not covered: " + cmp.Format());

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
                    Pattern<Word, int>.New("1").Annotation(any).Value, // first segment
                    Pattern<Word, int>.New("2").Annotation(any).OneOrMore.Value, // rest of stem
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
            Assert.That(cmp.MatchesReferenceExactly, Is.True, "infixation not covered: " + cmp.Format());

            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(infix);
        }
    }

    [Test]
    public void Composite_WiresGenerators_ReduplicatingGrammarMatchesEngine()
    {
        // Integration: CompositeProposer.ForLanguage wires the FST + generators, so a reduplicating
        // grammar's fast path matches the engine — not just the hand-built composite in the unit tests.
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        try
        {
            string[] corpus = { "sag", "sagsag", "dat" }; // bare, reduplicated, homograph
            var search = new Morpher(TraceManager, Language);
            CompositeProposer composite = CompositeProposer.ForLanguage(
                Language,
                new FstTemplateAnalyzer(Language, new Morpher(new TraceManager(), Language))
            );
            var fast = new VerifiedFstAnalyzer(
                composite,
                new MorpherPool(() => new Morpher(new TraceManager(), Language))
            );
            foreach (string word in corpus.Append("zzz"))
            {
                var fastSet = new HashSet<string>(fast.AnalyzeWord(word).Select(Sig));
                var oracle = new HashSet<string>(search.AnalyzeWord(word).Select(Sig));
                Assert.That(fastSet.SetEquals(oracle), Is.True, $"fast path disagrees with the engine for {word}");
            }
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    [Test]
    public void Composite_WithPhonologyAndReduplication_ParallelMatchesSequential()
    {
        // Thread-safety on the concurrent path: the composite now runs HC's phonology inverse
        // (ComposedPhonologyProposer) and the reduplication generator at analyze time. Drive both
        // in parallel and assert no divergence / no exceptions.
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
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(redup);
        var tToD = new RewriteRule
        {
            Name = "t_to_d",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            CompositeProposer composite = CompositeProposer.ForLanguage(
                Language,
                new FstTemplateAnalyzer(Language, new Morpher(new TraceManager(), Language))
            );
            var fast = new VerifiedFstAnalyzer(
                composite,
                new MorpherPool(() => new Morpher(new TraceManager(), Language))
            );
            var corpus = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                corpus.AddRange(new[] { "sag", "sagsag", "dad", "daddad", "sad", "zzz" });
            }
            Dictionary<string, string> sequential = corpus.Distinct().ToDictionary(w => w, w => SigSet(fast, w));
            var parallel = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(corpus, w => parallel[w] = SigSet(fast, w));
            Assert.That(
                corpus.Distinct().All(w => parallel[w] == sequential[w]),
                Is.True,
                "concurrent analyses diverged from sequential (composite phonology/redup not thread-safe)"
            );
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }

    [Test]
    public void LeverTwo_LazyComposition_RecoversBoundaryDeletion_RealTypes()
    {
        // Lever 2 with REAL HC types (LEVER_2.md): lazy-compose an inverse-phonology transducer (Pinv)
        // with the underlying morphotactic FST (FstTemplateAnalyzer.AnalyzeComposed). A "-d" suffix plus
        // a deletion rule t→∅ / _d means sat+DSF = "satd" → "sad" (the root-final t deletes). The
        // underlying-only proposer misses "sad"; lazy composition restores the deleted t — constrained by
        // the lexicon — and recovers [sat, DSF].
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        // Suffix whose underlying form is "kd" but whose "k" deletes before "d" → it surfaces as "d".
        // All segments are Table1 (the earlier affix-phonology test confirmed Table1 rules fire on
        // Table1-inserted affix segments), so this avoids the root-table friction.
        var kdSuffix = new AffixProcessRule
        {
            Name = "kd_suffix",
            Gloss = "KD",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        kdSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "kd") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(kdSuffix);
        var kDel = new RewriteRule
        {
            Name = "k_deletion",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "k")).Value,
        };
        kDel.Subrules.Add(
            new RewriteSubrule // no Rhs ⇒ deletion of k before d
            {
                RightEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value,
            }
        );
        Surface.PhonologicalRules.Add(kDel);
        try
        {
            var search = new Morpher(TraceManager, Language);
            var engine = new HashSet<string>(search.AnalyzeWord("sagd").Select(Sig));
            Assert.That(engine.Any(s => s.Contains("KD")), Is.True, "precondition: 'sagd' = sag+KD (k→∅/_d)");

            // Baseline: the underlying-only proposer has a "k" arc the surface "sagd" cannot match.
            Assert.That(
                new FstTemplateAnalyzer(Language).AnalyzeWord("sagd").Select(Sig).Any(s => s.Contains("KD")),
                Is.False,
                "baseline: underlying-only proposer misses the deletion form"
            );

            // Pinv: identity on s/a/g/d, plus an ε-input arc restoring a deleted k immediately before a d.
            var pinv = new InversePhonology { StartState = 0 };
            pinv.SetAccepting(0);
            foreach (string c in new[] { "s", "a", "g", "d" })
                pinv.AddArc(0, Character(Table1, c), Character(Table1, c), 0);
            pinv.AddArc(0, null, Character(Table1, "k"), 1); // ε: restore underlying k
            pinv.AddArc(1, Character(Table1, "d"), Character(Table1, "d"), 0); // ...immediately before a d

            var lex = new FstTemplateAnalyzer(Language); // default ctor: underlying-only arcs
            var composed = new HashSet<string>(lex.AnalyzeComposed("sagd", pinv).Select(Sig));

            Assert.That(
                composed.Any(s => s.Contains("KD")),
                Is.True,
                "lazy composition must recover the deletion form"
            );
            Assert.That(composed.IsSubsetOf(engine), Is.True, "soundness: composed candidates ⊆ engine analyses");
            Assert.That(lex.AnalyzeComposed("saga", pinv), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(kDel);
            Morphophonemic.MorphologicalRules.Remove(kdSuffix);
        }
    }

    [Test]
    public void ForwardSynthesis_CoversAffixedForms_AndIsSound()
    {
        // Forward-synthesis precompile: enumerate root × affix combos, synthesize each surface (phonology
        // applied WITH the morpheme boundary present — boundary-correct, unlike the inverse), and
        // tabulate surface→analysis. Here the s-suffix form "sags" is tabulated and confirmed by verify.
        AffixProcessRule suffix = AddSuffix();
        try
        {
            var search = new Morpher(TraceManager, Language);
            var synth = new ForwardSynthesisProposer(Language, new Morpher(TraceManager, Language));
            var pool = new MorpherPool(() => new Morpher(new TraceManager(), Language));
            var composite = new CompositeProposer(new FstTemplateAnalyzer(Language), synth);
            IMorphologicalAnalyzer verified = new VerifiedFstAnalyzer(composite, pool);

            foreach (string w in new[] { "sag", "sags", "dat" })
            {
                var oracle = new HashSet<string>(search.AnalyzeWord(w).Select(Sig));
                var got = new HashSet<string>(verified.AnalyzeWord(w).Select(Sig));
                Assert.That(
                    got.IsSubsetOf(oracle),
                    Is.True,
                    $"soundness: forward-synth proposed a non-engine analysis for {w}"
                );
                Assert.That(got.SetEquals(oracle), Is.True, $"forward-synth + composite should fully cover {w}");
            }
            Assert.That(verified.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must yield nothing");
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(suffix);
        }
    }

    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    private static string SigSet(IMorphologicalAnalyzer analyzer, string word) =>
        string.Join("|", analyzer.AnalyzeWord(word).Select(Sig).OrderBy(s => s, System.StringComparer.Ordinal));
}
