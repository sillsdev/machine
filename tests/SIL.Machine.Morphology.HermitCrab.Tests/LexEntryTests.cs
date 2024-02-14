using NUnit.Framework;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

public class LexEntryTests : HermitCrabTestBase
{
    [Test]
    public void DisjunctiveAllomorphs()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+ɯd") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(_traceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("bazɯd"), "disj PAST");
        Assert.That(morpher.ParseWord("batɯd"), Is.Empty);
        Assert.That(morpher.ParseWord("badɯd"), Is.Empty);
        Assert.That(morpher.ParseWord("basɯd"), Is.Empty);
        AssertMorphsEqual(morpher.ParseWord("bas"), "disj");
    }

    [Test]
    public void FreeFluctuation()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var d = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("strident-")
            .Symbol("del_rel-")
            .Symbol("alveolar")
            .Symbol("nasal-")
            .Symbol("vd+")
            .Value;

        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+t") }
            }
        );
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+"), new InsertSimpleContext(d) }
            }
        );
        _morphophonemic.MorphologicalRules.Add(edSuffix);

        var morpher = new Morpher(_traceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("tazd"), "free PAST");
        AssertMorphsEqual(morpher.ParseWord("tast"), "free PAST");
        AssertMorphsEqual(morpher.ParseWord("tazt"), "free PAST");
        AssertMorphsEqual(morpher.ParseWord("tasd"), "free PAST");
    }

    [Test]
    public void StemNames()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            Gloss = "1",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("1"))
                .Value
        };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+ɯd") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(edSuffix);

        var tSuffix = new AffixProcessRule
        {
            Name = "t_suffix",
            Gloss = "2",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("2"))
                .Value
        };
        tSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+t") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(tSuffix);

        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "3",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("3"))
                .Value
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+s") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(sSuffix);

        var morpher = new Morpher(_traceManager, _language);

        AssertMorphsEqual(morpher.ParseWord("sanɯd"));
        AssertMorphsEqual(morpher.ParseWord("sant"));
        AssertMorphsEqual(morpher.ParseWord("sans"));
        AssertMorphsEqual(morpher.ParseWord("san"), "stemname");

        AssertMorphsEqual(morpher.ParseWord("sadɯd"), "stemname 1");
        AssertMorphsEqual(morpher.ParseWord("sadt"), "stemname 2");
        AssertMorphsEqual(morpher.ParseWord("sads"));
        AssertMorphsEqual(morpher.ParseWord("sad"));

        AssertMorphsEqual(morpher.ParseWord("sapɯd"), "stemname 1");
        AssertMorphsEqual(morpher.ParseWord("sapt"));
        AssertMorphsEqual(morpher.ParseWord("saps"), "stemname 3");
        AssertMorphsEqual(morpher.ParseWord("sap"));
    }

    [Test]
    public void BoundRootAllomorph()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        _morphophonemic.MorphologicalRules.Add(edSuffix);
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "+ɯd") }
            }
        );

        var morpher = new Morpher(_traceManager, _language);
        Assert.That(morpher.ParseWord("dag"), Is.Empty);
        AssertMorphsEqual(morpher.ParseWord("dagɯd"), "bound PAST");
    }

    [Test]
    public void AllomorphEnvironments()
    {
        var vowel = FeatureStruct.New(_language.PhonologicalFeatureSystem).Symbol("voc+").Value;

        LexEntry headEntry = _entries["32"];
        Pattern<Word, ShapeNode> envPattern = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value;
        var env = new AllomorphEnvironment(ConstraintType.Require, null, envPattern);
        headEntry.PrimaryAllomorph.Environments.Add(env);

        var word = new Word(headEntry.PrimaryAllomorph, FeatureStruct.New().Value);

        ShapeNode node = word.Shape.Last;
        LexEntry nonHeadEntry = _entries["40"];
        word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Segments.Shape.AsEnumerable().CloneItems());
        Annotation<ShapeNode> nonHeadMorph = word.MarkMorph(
            word.Shape.GetNodes(node.Next, word.Shape.Last),
            nonHeadEntry.PrimaryAllomorph,
            Word.RootMorphID
        );

        Assert.That(env.IsWordValid(word, word.GetMorphs(headEntry.PrimaryAllomorph).First()), Is.True);

        word.RemoveMorph(nonHeadMorph);

        nonHeadEntry = _entries["41"];
        word.Shape.AddRange(nonHeadEntry.PrimaryAllomorph.Segments.Shape.AsEnumerable().CloneItems());
        nonHeadMorph = word.MarkMorph(
            word.Shape.GetNodes(node.Next, word.Shape.Last),
            nonHeadEntry.PrimaryAllomorph,
            Word.RootMorphID
        );

        Assert.That(env.IsWordValid(word, word.GetMorphs(headEntry.PrimaryAllomorph).First()), Is.False);

        headEntry.PrimaryAllomorph.Environments.Clear();

        env = new AllomorphEnvironment(ConstraintType.Require, envPattern, null);
        headEntry.PrimaryAllomorph.Environments.Add(env);

        word.RemoveMorph(nonHeadMorph);

        node = word.Shape.First;
        nonHeadEntry = _entries["40"];
        word.Shape.AddRangeAfter(
            word.Shape.Begin,
            nonHeadEntry.PrimaryAllomorph.Segments.Shape.AsEnumerable().CloneItems()
        );
        nonHeadMorph = word.MarkMorph(
            word.Shape.GetNodes(word.Shape.First, node.Prev),
            nonHeadEntry.PrimaryAllomorph,
            Word.RootMorphID
        );

        Assert.That(env.IsWordValid(word, word.GetMorphs(headEntry.PrimaryAllomorph).First()), Is.True);

        word.RemoveMorph(nonHeadMorph);

        nonHeadEntry = _entries["41"];
        word.Shape.AddRangeAfter(
            word.Shape.Begin,
            nonHeadEntry.PrimaryAllomorph.Segments.Shape.AsEnumerable().CloneItems()
        );
        word.MarkMorph(
            word.Shape.GetNodes(word.Shape.First, node.Prev),
            nonHeadEntry.PrimaryAllomorph,
            Word.RootMorphID
        );

        Assert.That(env.IsWordValid(word, word.GetMorphs(headEntry.PrimaryAllomorph).First()), Is.False);
    }

    [Test]
    public void PartialEntry()
    {
        var any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        var nominalizer = new AffixProcessRule
        {
            Name = "nominalizer",
            Gloss = "NOM",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value
        };
        nominalizer.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "v") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(nominalizer);

        var morpher = new Morpher(_traceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("pi"), "54");
        AssertMorphsEqual(morpher.ParseWord("piv"), "54 NOM");

        _morphophonemic.MorphologicalRules.Clear();

        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "s") }
            }
        );

        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(sSuffix) { Optional = true });
        _morphophonemic.AffixTemplates.Add(verbTemplate);

        morpher = new Morpher(_traceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("pi"), "54");
        AssertMorphsEqual(morpher.ParseWord("pis"));
    }
}
