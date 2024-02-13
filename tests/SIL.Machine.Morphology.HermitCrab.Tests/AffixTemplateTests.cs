using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

public class AffixTemplateTests : HermitCrabTestBase
{
    [Test]
    public void RealizationalRule()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        FeatureStruct alvStop = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("strident-")
            .Symbol("del_rel-")
            .Symbol("alveolar")
            .Value;
        FeatureStruct voicelessCons = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("vd-")
            .Value;
        FeatureStruct labiodental = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("labiodental")
            .Value;
        FeatureStruct voiced = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        FeatureStruct strident = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("strident+")
            .Value;

        var edSuffix = new RealizationalAffixProcessRule
        {
            Name = "ed_suffix",
            RealizationalFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value,
            Gloss = "PAST"
        };

        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
                },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertSegments(_table3, "ɯd") }
            }
        );
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "t") }
            }
        );
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "d") }
            }
        );

        var sSuffix = new RealizationalAffixProcessRule
        {
            Name = "s_suffix",
            RealizationalFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("pers").EqualTo("3").Feature("tense").EqualTo("pres"))
                .Value,
            Gloss = "3SG"
        };

        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(labiodental).Value
                },
                Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced), new InsertSegments(_table3, "z") }
            }
        );
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(strident).Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "ɯz") }
            }
        );
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(voicelessCons).Value
                },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertSegments(_table3, "s") }
            }
        );
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "z") }
            }
        );

        var evidential = new RealizationalAffixProcessRule
        {
            Name = "evidential",
            RealizationalFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Feature(_head)
                .EqualTo(head => head.Feature("evidential").EqualTo("witnessed"))
                .Value,
            Gloss = "WIT"
        };

        evidential.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "v") }
            }
        );

        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(sSuffix, edSuffix) { Optional = true });
        verbTemplate.Slots.Add(new AffixTemplateSlot(evidential) { Optional = true });
        _morphophonemic.AffixTemplates.Add(verbTemplate);

        var morpher = new Morpher(_ttraceManager, _language);
        Word[] output = morpher.ParseWord("sagd").ToArray();
        AssertMorphsEqual(output, "32 PAST");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("V")
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value
        );
        output = morpher.ParseWord("sagdv").ToArray();
        AssertMorphsEqual(output, "32 PAST WIT");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("V")
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("past").Feature("evidential").EqualTo("witnessed"))
                .Value
        );
        Assert.That(morpher.ParseWord("sid"), Is.Empty);
        output = morpher.ParseWord("sau").ToArray();
        AssertMorphsEqual(output, "bl2");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("V")
                .Feature(_head)
                .EqualTo(head => head.Feature("tense").EqualTo("past"))
                .Value
        );

        evidential.RealizationalFeatureStruct = FeatureStruct
            .New(_language.SyntacticFeatureSystem)
            .Feature(_head)
            .EqualTo(head => head.Feature("evidential").EqualTo("witnessed").Feature("tense").EqualTo("pres"))
            .Value;

        morpher = new Morpher(_ttraceManager, _language);
        output = morpher.ParseWord("sagzv").ToArray();
        AssertMorphsEqual(output, "32 3SG WIT");
        AssertSyntacticFeatureStructsEqual(
            output,
            FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("V")
                .Feature(_head)
                .EqualTo(head =>
                    head.Feature("pers")
                        .EqualTo("3")
                        .Feature("tense")
                        .EqualTo("pres")
                        .Feature("evidential")
                        .EqualTo("witnessed")
                )
                .Value
        );
    }

    [Test]
    public void NonFinalTemplate()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
        FeatureStruct alvStop = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("strident-")
            .Symbol("del_rel-")
            .Symbol("alveolar")
            .Value;
        FeatureStruct voicelessCons = FeatureStruct
            .New(_language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Symbol("vd-")
            .Value;

        var edSuffix = new AffixProcessRule { Name = "ed_suffix", Gloss = "PAST", };
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(alvStop).Value
                },
                Rhs = { new CopyFromInput("1"), new CopyFromInput("2"), new InsertSegments(_table3, "ɯd") }
            }
        );
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Annotation(voicelessCons).Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "t") }
            }
        );
        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "d") }
            }
        );

        var verbTemplate = new AffixTemplate
        {
            Name = "verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value
        };
        verbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix));
        _morphophonemic.AffixTemplates.Add(verbTemplate);

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

        var crule = new CompoundingRule
        {
            Name = "rule1",
            HeadRequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("V").Value,
            NonHeadRequiredSyntacticFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("N")
                .Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value
        };
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(any).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new InsertSegments(_table3, "+"), new CopyFromInput("nonHead") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(crule);

        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "PL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value,
        };
        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "s") }
            }
        );

        var nounTemplate = new AffixTemplate
        {
            Name = "noun",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value
        };
        nounTemplate.Slots.Add(new AffixTemplateSlot(sSuffix) { Optional = true });
        _morphophonemic.AffixTemplates.Add(nounTemplate);

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("sagd"), "32 PAST");
        AssertMorphsEqual(morpher.ParseWord("sagdv"));
        AssertMorphsEqual(morpher.ParseWord("sagdvs"));
        AssertMorphsEqual(morpher.ParseWord("sagdmi"));
        AssertMorphsEqual(morpher.ParseWord("sagdmis"));

        verbTemplate.IsFinal = false;
        morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("sagd"));
        AssertMorphsEqual(morpher.ParseWord("sagdv"), "32 PAST NOM");
        AssertMorphsEqual(morpher.ParseWord("sagdvs"), "32 PAST NOM PL");
        AssertMorphsEqual(morpher.ParseWord("sagdmi"), "32 PAST 53");
        AssertMorphsEqual(morpher.ParseWord("sagdmis"), "32 PAST 53 PL");
    }

    [Test]
    public void AffixTemplateAppliedAfterMorphologicalRule()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;
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

        var sSuffix = new AffixProcessRule
        {
            Name = "s_suffix",
            Gloss = "PL",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value,
        };

        sSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "s") }
            }
        );

        var nounTemplate = new AffixTemplate
        {
            Name = "noun",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value
        };
        nounTemplate.Slots.Add(new AffixTemplateSlot(sSuffix) { Optional = true });
        _morphophonemic.AffixTemplates.Add(nounTemplate);

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("sagv"), "32 NOM");
        AssertMorphsEqual(morpher.ParseWord("sagvs"), "32 NOM PL");
    }

    [Test]
    public void SameRuleUsedInMultipleTemplates()
    {
        FeatureStruct any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

        var edSuffix = new AffixProcessRule
        {
            Name = "ed_suffix",
            Gloss = "PAST",
            RequiredSyntacticFeatureStruct = FeatureStruct
                .New(_language.SyntacticFeatureSystem)
                .Symbol("V", "IV", "TV")
                .Value,
        };

        edSuffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "d") }
            }
        );

        var transitiveVerbTemplate = new AffixTemplate
        {
            Name = "Transitive Verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("TV").Value
        };
        transitiveVerbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix));
        _morphophonemic.AffixTemplates.Add(transitiveVerbTemplate);

        var intransitiveVerbTemplate = new AffixTemplate
        {
            Name = "Intransitive Verb",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("IV").Value
        };
        intransitiveVerbTemplate.Slots.Add(new AffixTemplateSlot(edSuffix));
        _morphophonemic.AffixTemplates.Add(intransitiveVerbTemplate);

        var nominalizer = new AffixProcessRule
        {
            Name = "intransitive verbalizer",
            Gloss = "IVERB",
            RequiredSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("N").Value,
            OutSyntacticFeatureStruct = FeatureStruct.New(_language.SyntacticFeatureSystem).Symbol("IV").Value
        };

        nominalizer.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(_table3, "v") }
            }
        );
        _morphophonemic.MorphologicalRules.Add(nominalizer);

        var morpher = new Morpher(_ttraceManager, _language);
        AssertMorphsEqual(morpher.ParseWord("mivd"), "53 IVERB PAST");
    }
}
