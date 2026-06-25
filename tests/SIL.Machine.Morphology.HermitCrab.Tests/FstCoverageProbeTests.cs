using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for the bounded, opt-in grammar-tuning probe (<see cref="FstCoverageProbe"/>): it reports
/// coverage over a wordlist and diffs coverage between two grammar versions, without ever running the
/// full search engine.
/// </summary>
public class FstCoverageProbeTests : HermitCrabTestBase
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
    public void Probe_ReportsCoverageAndUnparsedWords()
    {
        string[] corpus = { "sag", "dat", "zzz" }; // two bare roots, one non-word
        ProbeReport report = FstCoverageProbe.ForLanguage(Language).Probe(corpus);

        Assert.That(report.TotalWords, Is.EqualTo(3));
        Assert.That(report.ParsedWords, Is.EqualTo(2));
        Assert.That(report.UnparsedWords, Is.EquivalentTo(new[] { "zzz" }));
        Assert.That(report.CoverageRate, Is.EqualTo(2.0 / 3).Within(0.0001));
    }

    [Test]
    public void Probe_NeverReportsANonWordAsParsed()
    {
        // Soundness contract: "sagg" does not parse in the base grammar (shared negative-control word
        // used across the FST test suite); the probe must agree, never over-generating a false positive.
        ProbeReport report = FstCoverageProbe.ForLanguage(Language).Probe(new[] { "sagg" });

        Assert.That(report.ParsedWords, Is.Zero);
        Assert.That(report.UnparsedWords, Is.EquivalentTo(new[] { "sagg" }));
    }

    [Test]
    public void CompareGrammars_SameGrammarTwice_NoGainedOrLost()
    {
        string[] corpus = { "sag", "dat", "zzz" };
        CoverageDiff diff = FstCoverageProbe.CompareGrammars(Language, Language, corpus);

        Assert.That(diff.Gained, Is.Empty);
        Assert.That(diff.Lost, Is.Empty);
        Assert.That(diff.Before.ParsedWords, Is.EqualTo(diff.After.ParsedWords));
    }

    [Test]
    public void Probe_DetectsGainedCoverage_AfterAddingSuffixRule()
    {
        // The direct "did this grammar edit make parsing better or worse" workflow: probe before the
        // edit, apply the edit, probe again, and confirm the newly-coverable word is picked up. This is
        // the affix-rule edit class of FST_FAST_PATH_PLAN.md's Phase 5.4 edit-loop promise.
        string[] corpus = { "sag", "sags", "dat" };
        ProbeReport before = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
        Assert.That(before.UnparsedWords, Does.Contain("sags"), "precondition: sags not yet coverable");

        AffixProcessRule suffix = AddSuffix();
        try
        {
            ProbeReport after = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
            Assert.That(after.UnparsedWords, Does.Not.Contain("sags"));
            Assert.That(after.ParsedWords, Is.EqualTo(before.ParsedWords + 1));
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(suffix);
        }
    }

    [Test]
    public void Probe_DetectsGainedCoverage_AfterAddingPhonologicalRule()
    {
        // The phonological-rule edit class of the Phase 5.4 edit-loop promise: an unconditional t->d
        // rule means bare root "dat" (entry 8) now surfaces only as "dad" — invisible to the probe
        // until the rule exists. ("dat" itself is deliberately excluded from the corpus: the same
        // unconditional rule also makes the literal string "dat" stop being a valid surface form of
        // anything once every underlying "t" surfaces as "d" — a real "gained dad, lost dat" situation,
        // not tested here to keep this assertion to a single, unconfounded gain.)
        string[] corpus = { "sag", "dad" };
        ProbeReport before = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
        Assert.That(before.UnparsedWords, Does.Contain("dad"), "precondition: dad not yet coverable");

        var tToD = new RewriteRule
        {
            Name = "t_to_d_probe",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "t")).Value,
        };
        tToD.Subrules.Add(
            new RewriteSubrule { Rhs = Pattern<Word, int>.New().Annotation(Character(Table1, "d")).Value }
        );
        Surface.PhonologicalRules.Add(tToD);
        try
        {
            ProbeReport after = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
            Assert.That(after.UnparsedWords, Does.Not.Contain("dad"));
            Assert.That(after.ParsedWords, Is.EqualTo(before.ParsedWords + 1));
        }
        finally
        {
            Surface.PhonologicalRules.Remove(tToD);
        }
    }

    [Test]
    public void Probe_DetectsGainedCoverage_AfterAddingReduplicationRule()
    {
        // The reduplication-rule edit class of the Phase 5.4 edit-loop promise: a full-copy rule means
        // "sagsag" (RED('sag')) is only coverable once the rule exists.
        string[] corpus = { "sag", "sagsag", "dat" };
        ProbeReport before = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
        Assert.That(before.UnparsedWords, Does.Contain("sagsag"), "precondition: sagsag not yet coverable");

        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule
        {
            Name = "redup_probe",
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
            ProbeReport after = FstCoverageProbe.ForLanguage(Language).Probe(corpus);
            Assert.That(after.UnparsedWords, Does.Not.Contain("sagsag"));
            Assert.That(after.ParsedWords, Is.EqualTo(before.ParsedWords + 1));
        }
        finally
        {
            Morphophonemic.MorphologicalRules.Remove(redup);
        }
    }
}
