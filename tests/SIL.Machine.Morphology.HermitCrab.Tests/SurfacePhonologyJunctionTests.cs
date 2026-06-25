using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// CI coverage for junction-deletion probing (Phase C, FST_FULL_GRAMMAR_PLAN.md): a prefix whose
/// insertion abuts a morpheme boundary, where a phonological rule then deletes the FOLLOWING root's
/// own leading segment (Indonesian meN- + a voiceless obstruent onset is the real-grammar case this
/// mirrors — <c>SurfacePhonology.DeletionJunctions</c> + <c>FstTemplateAnalyzer</c>'s root-chain
/// checkpoints). Deliberately requires a second segment of right context beyond the deleted one (a
/// following vowel) to satisfy the rule's own environment — the exact shape that broke the first
/// (single-neighbor-only) version of the probe.
/// </summary>
public class SurfacePhonologyJunctionTests : HermitCrabTestBase
{
    [OneTimeSetUp]
    public void AddBoundaryToTable1()
    {
        // language.SurfaceStratum (== Surface, Table1-based) is what SurfacePhonology/FstTemplateAnalyzer
        // actually segment strings with, regardless of which table an affix's InsertSegments cited when
        // the rule was defined — so the boundary character needed for this test must live on Table1.
        Table1.AddBoundary("+");
    }

    private AffixProcessRule AddMePrefix()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var prefix = new AffixProcessRule
        {
            Name = "me_prefix",
            Gloss = "AV",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        prefix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, int>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new InsertSegments(Table1, "m+"), new CopyFromInput("1") },
            }
        );
        Surface.MorphologicalRules.Add(prefix);
        return prefix;
    }

    private RewriteRule AddVoicelessDeletionAfterBoundary()
    {
        // p → ∅ / + _ a  (mirrors Indonesian's voiceless-obstruent deletion: needs BOTH a left boundary
        // AND a right-context vowel beyond the deleted segment itself to fire).
        var rule = new RewriteRule
        {
            Name = "voiceless_deletion",
            Lhs = Pattern<Word, int>.New().Annotation(Character(Table1, "p")).Value,
        };
        rule.Subrules.Add(
            new RewriteSubrule
            {
                LeftEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "+")).Value,
                RightEnvironment = Pattern<Word, int>.New().Annotation(Character(Table1, "a")).Value,
            }
        );
        Surface.PhonologicalRules.Add(rule);
        return rule;
    }

    [Test]
    public void Junction_RecoversRootOnsetDeletion_RequiringTwoSegmentProbe()
    {
        LexEntry root = AddEntry(
            "pat_root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Surface,
            "pat"
        );
        AffixProcessRule prefix = AddMePrefix();
        RewriteRule delRule = AddVoicelessDeletionAfterBoundary();
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(
                search.AnalyzeWord("mat").Any(),
                Is.True,
                "precondition: 'mat' = m+pat with p deleted after the boundary before a vowel"
            );
            Assert.That(
                search.AnalyzeWord("mpat"),
                Is.Empty,
                "precondition: the underlying (undeleted) form must not itself surface"
            );

            var fst = new FstTemplateAnalyzer(Language, new Morpher(new TraceManager(), Language));
            List<WordAnalysis> found = fst.AnalyzeWord("mat").ToList();
            Assert.That(found, Is.Not.Empty, "the junction-deletion arc must recover 'mat' directly");

            var verified = new VerifiedFstAnalyzer(TraceManager, Language);
            Assert.That(
                verified.AnalyzeWord("mat").Any(),
                Is.True,
                "the full propose-and-verify path must also recover 'mat'"
            );
            Assert.That(verified.AnalyzeWord("mpit"), Is.Empty, "a non-word must yield nothing");
        }
        finally
        {
            Surface.PhonologicalRules.Remove(delRule);
            Surface.MorphologicalRules.Remove(prefix);
            Surface.Entries.Remove(root);
            Entries.Remove("pat_root");
        }
    }

    [Test]
    public void Junction_DoesNotSkip_WhenRootOnsetIsNotTheDeletedClass()
    {
        // "dat" starts with a VOICED obstruent — outside the deletion rule's Lhs entirely — so the
        // build-time onset gate (WireDeletionSkips) must never offer the skip arc for this root: only
        // the normal, unskipped concatenation "mdat" should be recoverable.
        LexEntry root = AddEntry(
            "dat_root",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Surface,
            "dat"
        );
        AffixProcessRule prefix = AddMePrefix();
        RewriteRule delRule = AddVoicelessDeletionAfterBoundary();
        try
        {
            var search = new Morpher(TraceManager, Language);
            Assert.That(search.AnalyzeWord("mdat").Any(), Is.True, "precondition: 'mdat' = m+dat, unaltered");

            var verified = new VerifiedFstAnalyzer(TraceManager, Language);
            Assert.That(verified.AnalyzeWord("mdat").Any(), Is.True, "the unskipped path must still work");
            Assert.That(
                verified.AnalyzeWord("mat"),
                Is.Empty,
                "soundness: skipping 'd' would wrongly recover a word only 'pat' should produce"
            );
        }
        finally
        {
            Surface.PhonologicalRules.Remove(delRule);
            Surface.MorphologicalRules.Remove(prefix);
            Surface.Entries.Remove(root);
            Entries.Remove("dat_root");
        }
    }
}
