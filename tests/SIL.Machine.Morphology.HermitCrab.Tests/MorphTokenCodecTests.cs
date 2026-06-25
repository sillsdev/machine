using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Proves the packed-token schema (HERMITCRAB_FST_PLAN.md §8) faithfully represents a real HC
/// analysis: encoding a parsed <see cref="Word"/> and decoding it reproduces the morphemes and
/// root that <c>WordAnalysis</c> carries, with the operation populated from the actual rule —
/// including the multi-stem (compound) case that the flat array must not lose.
/// </summary>
public class MorphTokenCodecTests : HermitCrabTestBase
{
    [Test]
    public void Encode_Suffix_RoundTripsToWordAnalysis()
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

        var morpher = new Morpher(TraceManager, Language);
        List<Word> words = morpher.ParseWord("sags").ToList();
        List<WordAnalysis> analyses = morpher.AnalyzeWord("sags").ToList();
        Assert.That(words, Has.Count.EqualTo(1));
        Assert.That(analyses, Has.Count.EqualTo(1));

        var codec = new MorphTokenCodec();
        uint[] tokens = codec.Encode(words[0]);
        WordAnalysis wa = analyses[0];

        // Morpheme channel: decoded indices reproduce WordAnalysis.Morphemes, in order.
        Assert.That(
            tokens.Select(t => codec.GetMorpheme(MorphToken.GetMorphemeId(t)).Id),
            Is.EqualTo(wa.Morphemes.Select(m => m.Id))
        );
        // Root recovered purely from the op codes == HC's RootMorphemeIndex (no separate field).
        Assert.That(MorphToken.RootIndex(tokens), Is.EqualTo(wa.RootMorphemeIndex));
        // Op channel is populated from the real rule: a root and a suffix.
        var ops = tokens.Select(MorphToken.GetOp).ToList();
        Assert.That(ops, Does.Contain(MorphOp.Root));
        Assert.That(ops, Does.Contain(MorphOp.Suffix));

        Morphophonemic.MorphologicalRules.Remove(sSuffix);
    }

    [Test]
    public void Encode_Compound_KeepsBothStems_OneRoot()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var rule1 = new CompoundingRule { Name = "rule1" };
        Allophonic.MorphologicalRules.Add(rule1);
        rule1.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(Table3, "+"), new CopyFromInput("nonHead") },
            }
        );

        var morpher = new Morpher(TraceManager, Language);
        List<Word> words = morpher.ParseWord("pʰutdat").ToList();
        List<WordAnalysis> analyses = morpher.AnalyzeWord("pʰutdat").ToList();
        Assert.That(words, Is.Not.Empty);

        // Match each encoded word to a WordAnalysis by morpheme sequence (decoupled from order).
        var codec = new MorphTokenCodec();
        foreach (Word w in words)
        {
            uint[] tokens = codec.Encode(w);

            // Two stems → two morphemes; exactly one tagged Root, the other Compound (not lost).
            Assert.That(tokens, Has.Length.EqualTo(2));
            Assert.That(tokens.Count(t => MorphToken.GetOp(t) == MorphOp.Root), Is.EqualTo(1));
            Assert.That(tokens.Select(MorphToken.GetOp), Does.Contain(MorphOp.Compound));

            string[] decoded = tokens.Select(t => codec.GetMorpheme(MorphToken.GetMorphemeId(t)).Id).ToArray();
            WordAnalysis? match = analyses.FirstOrDefault(a => a.Morphemes.Select(m => m.Id).SequenceEqual(decoded));
            Assert.That(match, Is.Not.Null, $"no WordAnalysis matches decoded morphemes [{string.Join(",", decoded)}]");
            Assert.That(MorphToken.RootIndex(tokens), Is.EqualTo(match!.RootMorphemeIndex));
        }

        Allophonic.MorphologicalRules.Remove(rule1);
    }

    [Test]
    public void ClassifyOp_PopulatesAffixRolesFromOutputActions()
    {
        Assert.That(RoleOf(new CopyFromInput("1"), new CopyFromInput("1")), Is.EqualTo(MorphOp.Reduplication));
        Assert.That(
            RoleOf(new CopyFromInput("1"), new InsertSegments(Table3, "a"), new CopyFromInput("2")),
            Is.EqualTo(MorphOp.Infix)
        );
        Assert.That(RoleOf(new InsertSegments(Table3, "di"), new CopyFromInput("1")), Is.EqualTo(MorphOp.Prefix));
        Assert.That(RoleOf(new CopyFromInput("1"), new InsertSegments(Table3, "s")), Is.EqualTo(MorphOp.Suffix));
    }

    private static MorphOp RoleOf(params MorphologicalOutputAction[] rhs)
    {
        var allo = new AffixProcessAllomorph();
        foreach (MorphologicalOutputAction action in rhs)
        {
            allo.Rhs.Add(action);
        }
        return MorphTokenCodec.ClassifyOp(allo, isHeadRoot: false);
    }
}
