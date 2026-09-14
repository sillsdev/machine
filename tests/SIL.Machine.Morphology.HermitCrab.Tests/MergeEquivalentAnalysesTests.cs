using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// MergeEquivalentAnalyses folds equivalent analyses of a stratum into one canonical word, and only that
// canonical word is unapplied against the strata below. Sharing a shape is therefore not enough to make
// two analyses interchangeable: they have to agree on everything an analysis-side rule reads, which is
// what AnalysisStateKey covers.
[TestFixture]
public class MergeEquivalentAnalysesTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    // Both Allophonic rules strip the same t from zudzt, so both reach the shape zudz: vRule leaves V
    // behind on the stem, anyRule leaves it unconstrained. Only the unconstrained stem lets the
    // Morphophonemic nRule (Out = N) unapply, so merging the two on shape alone loses the parse.
    [TestCase(true, TestName = "unconstrained path reaches the merge first")]
    [TestCase(false, TestName = "V-marked path reaches the merge first")]
    public void ParseWord_SameShapeDifferentAnalysisState_KeepsBothAnalyses(bool unconstrainedFirst)
    {
        FeatureStruct n = Pos("N");
        FeatureStruct v = Pos("V");

        AddEntry("zudRoot", n, Morphophonemic, "zud");
        Morphophonemic.MorphologicalRules.Add(Suffix("nRule", n, n, Table3, "+z"));

        AffixProcessRule vRule = Suffix("vRule", v, v, Table1, "t");
        AffixProcessRule anyRule = Suffix("anyRule", FeatureStruct.New().Value, v, Table1, "t");
        // The cascade runs the stratum's rules in reverse order of registration, and the first analysis to
        // reach a given key becomes the canonical word, so registration order decides which one that is.
        if (unconstrainedFirst)
            Allophonic.MorphologicalRules.Add(vRule);
        Allophonic.MorphologicalRules.Add(anyRule);
        if (!unconstrainedFirst)
            Allophonic.MorphologicalRules.Add(vRule);

        SetRuleOrder(MorphologicalRuleOrder.Linear);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        AssertMorphsEqual(morpher.ParseWord("zudzt"), "zudRoot nRule anyRule");
    }

    private static AffixProcessRule Suffix(
        string name,
        FeatureStruct required,
        FeatureStruct outFs,
        CharacterDefinitionTable table,
        string insert
    )
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
            RequiredSyntacticFeatureStruct = required,
            OutSyntacticFeatureStruct = outFs,
        };
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(table, insert) },
            }
        );
        return rule;
    }

    private FeatureStruct Pos(string symbol) => FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbol).Value;
}
