using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for the auto-compiled lockstep phonology (FST_FAST_PATH_PLAN.md Phase 3): does
/// <see cref="PhonologyRuleCompiler"/> build the SAME kind of Pinv the LEVER_2.md spike hand-built and
/// proved sound (<c>LeverTwo_LazyComposition_RecoversBoundaryDeletion_RealTypes</c> in
/// VerifiedFstAnalyzerTests.cs), and does <see cref="LockstepPhonologyProposer"/> wire it correctly.
/// </summary>
public class PhonologyRuleCompilerTests : HermitCrabTestBase
{
    private static string Sig(WordAnalysis a) =>
        string.Join("+", a.Morphemes.Select(m => (m as Morpheme)?.Gloss ?? "?")) + ":" + a.RootMorphemeIndex;

    [Test]
    public void Compile_AutoRecoversBoundaryDeletion()
    {
        // Same shape as the hand-built LEVER_2 spike: a "kd" suffix whose "k" deletes before "d" (a
        // plain right-context deletion rule), so sag+KD = "sagkd" -> "sagd". The compiler must find
        // this on its own — no hand-built Pinv.
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
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
            new RewriteSubrule // no Rhs => deletion of k before d
            {
                RightEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value,
            }
        );
        Surface.PhonologicalRules.Add(kDel);
        try
        {
            var search = new Morpher(TraceManager, Language);
            var engine = new HashSet<string>(search.AnalyzeWord("sagd").Select(Sig));
            Assert.That(engine.Any(s => s.Contains("KD")), Is.True, "precondition: 'sagd' = sag+KD (k->0/_d)");

            var morpher = new Morpher(new TraceManager(), Language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(Language, morpher);
            Assert.That(unsupported, Is.Zero, "this rule is entirely within the v1 supported shape");

            var lex = new FstTemplateAnalyzer(Language); // default ctor: underlying-only arcs
            var composed = new HashSet<string>(lex.AnalyzeComposed("sagd", pinv).Select(Sig));

            Assert.That(
                composed.Any(s => s.Contains("KD")),
                Is.True,
                "auto-compiled Pinv must recover the deletion form"
            );
            Assert.That(
                composed.IsSubsetOf(engine),
                Is.True,
                "soundness: composed candidates must be a subset of the engine's"
            );
            Assert.That(lex.AnalyzeComposed("saga", pinv), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(kDel);
            Morphophonemic.MorphologicalRules.Remove(kdSuffix);
        }
    }

    [Test]
    public void Compile_AutoRecoversUnconditionedSubstitution()
    {
        // An unconditional (no left/right environment) t->d rule: bare root "dat" (entry 8) surfaces
        // only as "dad". Exercises the zero-right-environment substitution branch.
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
            Assert.That(search.AnalyzeWord("dad").Any(), Is.True, "precondition: 'dad' analyzes");

            var morpher = new Morpher(new TraceManager(), Language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(Language, morpher);
            Assert.That(unsupported, Is.Zero);

            var lex = new FstTemplateAnalyzer(Language);
            Assert.That(lex.AnalyzeWord("dad"), Is.Empty, "baseline: the underlying-only walk alone must miss 'dad'");

            var composed = lex.AnalyzeComposed("dad", pinv);
            Assert.That(composed, Is.Not.Empty, "auto-compiled Pinv must recover the substituted bare root");
            Assert.That(lex.AnalyzeComposed("zzz", pinv), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
        }
    }

    [Test]
    public void Compile_SkipsUnconditionedDeletion_AsUnsupported()
    {
        // Deletion with NO right environment would over-restore everywhere (there's nothing to bound
        // it), so the compiler must decline this shape rather than build an unsound-in-practice arc.
        var kDel = new RewriteRule
        {
            Name = "unconditioned_k_deletion",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "k")).Value,
        };
        kDel.Subrules.Add(new RewriteSubrule()); // empty Rhs, empty environments => deletes k everywhere
        Surface.PhonologicalRules.Add(kDel);
        try
        {
            var morpher = new Morpher(new TraceManager(), Language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(Language, morpher);
            Assert.That(unsupported, Is.EqualTo(1), "unconditioned deletion must be marked unsupported, not compiled");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(kDel);
        }
    }

    [Test]
    public void Compile_AutoRecoversLeftContextDeletion()
    {
        // Mirror of Compile_AutoRecoversBoundaryDeletion but conditioned on the LEFT side: a "dk"
        // suffix whose "k" deletes after "d" (left-context-only deletion, no right environment at
        // all), so sag+DK = "sagdk" -> "sagd".
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var dkSuffix = new AffixProcessRule
        {
            Name = "dk_suffix",
            Gloss = "DK",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        dkSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table1, "dk") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(dkSuffix);
        var kDel = new RewriteRule
        {
            Name = "k_deletion_left",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "k")).Value,
        };
        kDel.Subrules.Add(
            new RewriteSubrule // no Rhs => deletion of k after d; no right environment at all
            {
                LeftEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value,
            }
        );
        Surface.PhonologicalRules.Add(kDel);
        try
        {
            var search = new Morpher(TraceManager, Language);
            var engine = new HashSet<string>(search.AnalyzeWord("sagd").Select(Sig));
            Assert.That(engine.Any(s => s.Contains("DK")), Is.True, "precondition: 'sagd' = sag+DK (k->0/d_)");

            var morpher = new Morpher(new TraceManager(), Language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(Language, morpher);
            Assert.That(unsupported, Is.Zero, "left-context-only deletion is within the supported shape");

            var lex = new FstTemplateAnalyzer(Language);
            var composed = new HashSet<string>(lex.AnalyzeComposed("sagd", pinv).Select(Sig));

            Assert.That(
                composed.Any(s => s.Contains("DK")),
                Is.True,
                "auto-compiled Pinv must recover the left-context deletion form"
            );
            Assert.That(
                composed.IsSubsetOf(engine),
                Is.True,
                "soundness: composed candidates must be a subset of the engine's"
            );
            Assert.That(lex.AnalyzeComposed("saga", pinv), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(kDel);
            Morphophonemic.MorphologicalRules.Remove(dkSuffix);
        }
    }

    [Test]
    public void Compile_AutoRecoversLeftContextSubstitution()
    {
        // A t->d rule triggered only when preceded by "g" (left environment, no right environment):
        // exercises the zero-right-environment branch of the left-context chain. Bare root "kagt"
        // (Surface stratum, so no morphological rule is needed) surfaces only as "kagd".
        LexEntry entry = AddEntry(
            "kagt_root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
            Surface,
            "kagt"
        );
        var tToD = new RewriteRule
        {
            Name = "t_to_d_left",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule
            {
                LeftEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "g")).Value,
                Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value,
            }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("kagd").Any(), Is.True, "precondition: 'kagd' = kagt (t->d/g_)");

            var morpher = new Morpher(new TraceManager(), Language);
            (InversePhonology pinv, int unsupported) = PhonologyRuleCompiler.Compile(Language, morpher);
            Assert.That(unsupported, Is.Zero);

            var lex = new FstTemplateAnalyzer(Language);
            Assert.That(lex.AnalyzeWord("kagd"), Is.Empty, "baseline: the underlying-only walk alone must miss 'kagd'");

            var composed = lex.AnalyzeComposed("kagd", pinv);
            Assert.That(composed, Is.Not.Empty, "auto-compiled Pinv must recover the left-conditioned substitution");
            Assert.That(lex.AnalyzeComposed("kagz", pinv), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
            Surface.Entries.Remove(entry);
            Entries.Remove("kagt_root");
        }
    }

    [Test]
    public void LockstepPhonologyProposer_CoversDeletion_WiredThroughComposite()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var kdSuffix = new AffixProcessRule
        {
            Name = "kd_suffix2",
            Gloss = "KD2",
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
            Name = "k_deletion2",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "k")).Value,
        };
        kDel.Subrules.Add(
            new RewriteSubrule { RightEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(kDel);
        try
        {
            var search = new Morpher(TraceManager, Language);
            var oracle = new HashSet<string>(search.AnalyzeWord("sagd").Select(Sig));

            CompositeProposer composite = CompositeProposer.ForLanguage(
                Language,
                new FstTemplateAnalyzer(Language, new Morpher(new TraceManager(), Language))
            );
            var fast = new VerifiedFstAnalyzer(
                composite,
                new MorpherPool(() => new Morpher(new TraceManager(), Language))
            );
            var got = new HashSet<string>(fast.AnalyzeWord("sagd").Select(Sig));

            Assert.That(
                got.SetEquals(oracle),
                Is.True,
                "composite (via the lockstep proposer) must match the engine for 'sagd'"
            );
            Assert.That(fast.AnalyzeWord("zzz"), Is.Empty, "soundness: a non-word must still yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(kDel);
            Morphophonemic.MorphologicalRules.Remove(kdSuffix);
        }
    }
}
