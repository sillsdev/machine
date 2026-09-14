using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

/// <summary>
/// How analysis (un-application) tracks the syntactic feature structure of the stem it is looking for.
/// Synthesis computes PriorityUnion(Unify(stem, rule.Required), rule.Out), so after un-applying a rule the
/// stem must unify with rule.Required: the analysis FS is narrowed with PriorityUnion, not widened with Add.
/// </summary>
public class AnalysisSyntacticFeatureStructTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    /// <summary>
    /// Three category-changing derivational rules: N -> V, V -> N and a third rule N -> V. After un-applying
    /// the first two, the stem must be N. With Add the FS would be {N, V} and the third rule (Out = V) would
    /// be attempted; with PriorityUnion it is N and the third rule is filtered out before pattern matching.
    /// </summary>
    [Test]
    public void AnalysisAffixProcessRule_CategoryChangeChain_RequiredOverridesAccumulatedPos()
    {
        FeatureStruct n = Pos("N");
        FeatureStruct v = Pos("V");
        AffixProcessRule n2v = MakeSuffixRule("n2v", n, v, Table3, "u");
        AffixProcessRule v2n = MakeSuffixRule("v2n", v, n, Table3, "i");
        AffixProcessRule third = MakeSuffixRule("third", n, v, Table3, "a");

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        var analysisV2N = new AnalysisAffixProcessRule(morpher, v2n);
        var analysisN2V = new AnalysisAffixProcessRule(morpher, n2v);
        var analysisThird = new AnalysisAffixProcessRule(morpher, third);

        // synthesis: root(N) --n2v--> V --v2n--> N; un-apply in reverse
        Word surface = MakeWord("zimui", n);
        Word afterV2N = analysisV2N.Apply(surface).Single();
        Assert.That(afterV2N.SyntacticFeatureStruct.ValueEquals(v), Is.True);
        Word afterN2V = analysisN2V.Apply(afterV2N).Single();
        Assert.That(
            afterN2V.SyntacticFeatureStruct.ValueEquals(n),
            Is.True,
            "the stem must be N; Add would have produced {N, V}"
        );

        Assert.That(analysisThird.Apply(afterN2V), Is.Empty, "Out = V is not unifiable with the stem's N");
    }

    [Test]
    public void ParseWord_CategoryChangeChain_FullChainStillFound()
    {
        FeatureStruct n = Pos("N");
        FeatureStruct v = Pos("V");
        AddEntry("flipRoot", n, Morphophonemic, "zim");
        var template = new AffixTemplate { Name = "flipTemplate" };
        template.Slots.Add(new AffixTemplateSlot(MakeSuffixRule("n2v", n, v, Table3, "u")));
        template.Slots.Add(new AffixTemplateSlot(MakeSuffixRule("v2n", v, n, Table3, "i")));
        template.Slots.Add(new AffixTemplateSlot(MakeSuffixRule("third", n, v, Table3, "a")));
        Morphophonemic.AffixTemplates.Add(template);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        AssertMorphsEqual(morpher.ParseWord("zimuia"), "flipRoot n2v v2n third");
    }

    /// <summary>
    /// Morpher.MergeEquivalentAnalyses folds same-shape analyses of a stratum into one canonical word, and the
    /// strata below are un-applied against the canonical word's FS only. Two upper-stratum paths reach the
    /// same shape here: r1 (V -> N) then r0 (N -> V) gives a stem FS of V... N, and r2 (V -> N) gives V. Only
    /// root + r3 + r2 is a valid word. Whichever path is merged first, the canonical FS must stay general
    /// enough for r3 (Out = V) in the lower stratum, otherwise the valid parse is lost.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void ParseWord_MergedEquivalentAnalyses_CanonicalFsCoversEveryAlternative(bool oneRulePathFirst)
    {
        FeatureStruct v = Pos("V");
        FeatureStruct n = Pos("N");
        AddEntry("smRoot", v, Morphophonemic, "zud");
        Morphophonemic.MorphologicalRules.Add(MakeSuffixRule("r3", v, v, Table3, "z"));
        AffixProcessRule r0 = MakeSuffixRule("r0", n, v, Table1, "i");
        AffixProcessRule r1 = MakeSuffixRule("r1", v, n, Table1, "t");
        AffixProcessRule r2 = MakeSuffixRule("r2", v, n, Table1, "it");
        if (oneRulePathFirst)
            Allophonic.MorphologicalRules.Add(r2);
        Allophonic.MorphologicalRules.Add(r0);
        Allophonic.MorphologicalRules.Add(r1);
        if (!oneRulePathFirst)
            Allophonic.MorphologicalRules.Add(r2);

        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1);
        AssertMorphsEqual(morpher.ParseWord("zudzit"), "smRoot r3 r2");
    }

    private Word MakeWord(string shape, FeatureStruct syntacticFS)
    {
        var word = new Word(Morphophonemic, Morphophonemic.CharacterDefinitionTable.Segment(shape))
        {
            SyntacticFeatureStruct = syntacticFS,
        };
        word.Freeze();
        return word;
    }

    private static AffixProcessRule MakeSuffixRule(
        string name,
        FeatureStruct required,
        FeatureStruct outFs,
        CharacterDefinitionTable table,
        string insert
    )
    {
        var rule = new AffixProcessRule
        {
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

    private FeatureStruct Pos(params string[] symbols) =>
        FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol(symbols).Value;
}
