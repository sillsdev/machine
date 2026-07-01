using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The static feeding-closure pass (HERMITCRAB_FST_PLAN.md §9.5): a non-regular escape is CLOSED
/// only if no FST-able rule could apply before it and feed it. Closed ⇒ the FST's "no path" is a
/// proof; fed ⇒ those words need the search backstop.
/// </summary>
public class GrammarFstClosureTests : HermitCrabTestBase
{
    private static AffixProcessRule MakeReduplication(string name)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var redup = new AffixProcessRule { Name = name, Gloss = "INTENS" };
        redup.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("1") },
            }
        );
        return redup;
    }

    private AffixProcessRule MakeSuffix(string name)
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var suffix = new AffixProcessRule { Name = name, Gloss = "PL" };
        suffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "s") },
            }
        );
        return suffix;
    }

    [Test]
    public void Analyze_NoEscapes_IsClosedVacuously()
    {
        ClosureReport report = GrammarFstClosure.Analyze(Language);
        Assert.That(report.Escapes, Is.Empty);
        Assert.That(report.FstClosed, Is.True);
    }

    [Test]
    public void Analyze_InnermostReduplication_NothingPrecedes_IsClosed()
    {
        // Reduplication as the only rule at the innermost stratum: nothing FST-able precedes it,
        // so no derivation can feed it — closed, and the FST's silence is a proof.
        AffixProcessRule redup = MakeReduplication("redup");
        Morphophonemic.MorphologicalRules.Add(redup);

        ClosureReport report = GrammarFstClosure.Analyze(Language);

        Assert.That(report.Escapes, Has.Count.EqualTo(1));
        Assert.That(report.Escapes[0].Rule, Is.EqualTo("redup"));
        Assert.That(report.Escapes[0].Closed, Is.True, report.Format());
        Assert.That(report.FstClosed, Is.True);

        Morphophonemic.MorphologicalRules.Remove(redup);
    }

    [Test]
    public void Analyze_FeederBeforeReduplication_IsPotentiallyFed()
    {
        // A concatenative suffix in the same (unordered) stratum could apply before the
        // reduplication and feed it: the pass conservatively reports it not closed.
        AffixProcessRule redup = MakeReduplication("redup");
        AffixProcessRule suffix = MakeSuffix("pl");
        Morphophonemic.MorphologicalRules.Add(suffix);
        Morphophonemic.MorphologicalRules.Add(redup);

        ClosureReport report = GrammarFstClosure.Analyze(Language);

        Assert.That(report.Escapes, Has.Count.EqualTo(1));
        Assert.That(report.Escapes[0].Closed, Is.False, report.Format());
        Assert.That(report.FstClosed, Is.False);

        Morphophonemic.MorphologicalRules.Remove(redup);
        Morphophonemic.MorphologicalRules.Remove(suffix);
    }
}
