using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

public class PruneDisagreeingCopiesTests : HermitCrabTestBase
{
    private static readonly FeatureStruct AnySegment = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    [Test]
    public void PruningIsOnByDefault()
    {
        Assert.That(new Morpher(TraceManager, Language).PruneDisagreeingCopies, Is.True);
    }

    [TestCase(false, 3)]
    [TestCase(true, 1)]
    public void FullCopyKeepsOnlyTheSplitWhoseCopiesAgree(bool pruneCopies, int expectedOutputs)
    {
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());

        List<Word> outputs = new AnalysisAffixProcessRule(CreateMorpher(pruneCopies), rule)
            .Apply(CreateInput(first + second + first + second))
            .ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
        if (pruneCopies)
            Assert.That(outputs.Single().Shape.Count, Is.EqualTo(2));
    }

    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public void DisagreeingCopiesAreRemovedOnlyWhenPruning(bool pruneCopies, int expectedOutputs)
    {
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(SingleSegmentPart());

        List<Word> outputs = new AnalysisAffixProcessRule(CreateMorpher(pruneCopies), rule)
            .Apply(CreateInput(first + second))
            .ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
    }

    [Test]
    public void TracingStillPrunesDisagreeingCopies()
    {
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(SingleSegmentPart());
        var input = new Word(Surface, Table3.Segment(first + second));
        TraceManager.AnalyzeWord(Language, input);
        input.Freeze();
        TraceManager.IsTracing = true;
        try
        {
            List<Word> outputs = new AnalysisAffixProcessRule(CreateMorpher(true), rule).Apply(input).ToList();

            Assert.That(outputs, Is.Empty);
        }
        finally
        {
            TraceManager.IsTracing = false;
        }
    }

    [TestCase(false, 1)]
    [TestCase(true, 0)]
    public void EveryPairOfCopiesIsCompared(bool pruneCopies, int expectedOutputs)
    {
        // the first copy is underspecified, as unapplied phonology can leave it, so it unifies with both others
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(SingleSegmentPart(), copyCount: 3);
        var input = new Word(Surface, Table3.Segment(first + first + second));
        input.Shape.First.Annotation.FeatureStruct = AnySegment.Clone();
        input.Freeze();

        List<Word> outputs = new AnalysisAffixProcessRule(CreateMorpher(pruneCopies), rule).Apply(input).ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
    }

    [Test]
    public void OptionalSegmentThatCannotLineUpIsPruned()
    {
        // skipping or keeping the restored segment, every split still pairs the two incompatible segments
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());
        Word input = CreateInputWithOptionalSecondSegment(first + second + second);

        Assert.That(new AnalysisAffixProcessRule(CreateMorpher(false), rule).Apply(input), Is.Not.Empty);
        Assert.That(new AnalysisAffixProcessRule(CreateMorpher(true), rule).Apply(input), Is.Empty);
    }

    [Test]
    public void OptionalSegmentThatLinesUpWhenSkippedIsKept()
    {
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());
        Word input = CreateInputWithOptionalSecondSegment(first + second + first);

        Assert.That(
            new AnalysisAffixProcessRule(CreateMorpher(true), rule).Apply(input).Count(),
            Is.EqualTo(new AnalysisAffixProcessRule(CreateMorpher(false), rule).Apply(input).Count())
        );
    }

    [Test]
    public void WordInitialOptionalSegmentThatLinesUpWhenRestoredIsKept()
    {
        // "(a)bab" for stem "ab": the optional first segment can be restored into the first copy so [a,b] meets [a,b]
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());
        Word input = CreateInputWithOptionalFirstSegment(first + second + first + second);

        string[] unpruned = AnalysisForms(false, rule, input);
        Assert.That(unpruned, Is.EquivalentTo(new[] { first, first + second, first + second + first }));
        // only the split that pairs "(a)b" with "ab" lines up; the other splits pair incompatible segments
        Assert.That(AnalysisForms(true, rule, input), Is.EqualTo(new[] { first + second }));
    }

    [Test]
    public void OptionalNonSegmentInACopyIsNeverPruned()
    {
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());
        var input = new Word(Surface, Table3.Segment(first + second + second));
        input.Shape.AddAfter(input.Shape.First.Next, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Value, true);
        input.Freeze();

        // a copy holding an optional boundary is never reported as disagreeing, so its split survives pruning;
        // the split of two plain, incompatible copies is the only one removed
        Assert.That(AnalysisForms(false, rule, input), Is.EquivalentTo(new[] { first, first + second }));
        Assert.That(AnalysisForms(true, rule, input), Is.EqualTo(new[] { first }));
    }

    [Test]
    public void WordInitialOptionalNonSegmentIsNeverPruned()
    {
        // the matcher skips the leading boundary, so it sits before the first copy rather than inside it
        (string first, string second) = FindIncompatibleSegments();
        AffixProcessRule rule = CreateCopyRule(FullPart());
        var input = new Word(Surface, Table3.Segment(first + second + first + second));
        input.Shape.AddAfter(input.Shape.Begin, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Value, true);
        input.Freeze();

        // nothing is pruned: with the boundary ignored, pruning keeps exactly what the unpruned rule produces
        string[] expected = { first, first + second, first + second + first };
        Assert.That(AnalysisForms(false, rule, input), Is.EquivalentTo(expected));
        Assert.That(AnalysisForms(true, rule, input), Is.EquivalentTo(expected));
    }

    [Test]
    public void EmptyCopyIsNeverPruned()
    {
        (string first, string second) = FindIncompatibleSegments();
        Pattern<Word, ShapeNode> part = Pattern<Word, ShapeNode>.New("copy").Annotation(AnySegment).ZeroOrMore.Value;
        AffixProcessRule rule = CreateCopyRule(part);
        Word input = CreateInput(first + second);

        // the split with an empty copy has nothing to compare, so only the split of two incompatible copies is removed
        Assert.That(AnalysisForms(false, rule, input), Is.EquivalentTo(new[] { first, first + second }));
        Assert.That(AnalysisForms(true, rule, input), Is.EqualTo(new[] { first + second }));
    }

    [TestCase(false, 3)]
    [TestCase(true, 1)]
    public void RealizationalFullCopyKeepsOnlyTheSplitWhoseCopiesAgree(bool pruneCopies, int expectedOutputs)
    {
        (string first, string second) = FindIncompatibleSegments();
        var rule = new RealizationalAffixProcessRule { Name = "real_copy", Gloss = "RED" };
        rule.Allomorphs.Add(CreateCopyAllomorph(FullPart(), copyCount: 2));

        List<Word> outputs = new AnalysisRealizationalAffixProcessRule(CreateMorpher(pruneCopies), rule)
            .Apply(CreateInput(first + second + first + second))
            .ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
    }

    [TestCase(false, 3)]
    [TestCase(true, 1)]
    public void CompoundHeadCopiedTwiceKeepsOnlyTheSplitWhoseCopiesAgree(bool pruneCopies, int expectedOutputs)
    {
        var crule = new CompoundingRule { Name = "head_copy_compound" };
        crule.Subrules.Add(
            new CompoundingSubrule
            {
                HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(AnySegment).OneOrMore.Value },
                NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(AnySegment).OneOrMore.Value },
                Rhs = { new CopyFromInput("head"), new CopyFromInput("head"), new CopyFromInput("nonHead") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(crule);

        // "pu" is the only root the input can end in, so every analysis differs only in how "tata" splits
        List<Word> outputs = new AnalysisCompoundingRule(CreateMorpher(pruneCopies), crule)
            .Apply(CreateInput("tatapu"))
            .ToList();

        Assert.That(outputs.Count, Is.EqualTo(expectedOutputs));
        Assert.That(outputs.Select(w => w.CurrentNonHead.RootAllomorph.Morpheme.Gloss), Is.All.EqualTo("52"));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void ParsesTheSameUnderEveryParallelismSetting(int maxDegreeOfParallelism)
    {
        AffixProcessRule redup = CreateCopyRule(FullPart());
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        Morphophonemic.MorphologicalRules.Add(redup);

        string[] unpruned = ParseGlosses(CreateMorpher(false, maxDegreeOfParallelism: 1), "sagsag");
        Assert.That(unpruned, Is.EqualTo(new[] { "32 RED" }));
        Assert.That(ParseGlosses(CreateMorpher(true, maxDegreeOfParallelism), "sagsag"), Is.EqualTo(unpruned));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PartialMorphemesParseTheSameWithAndWithoutPruning(bool alwaysEnforceFinalTemplates)
    {
        Morphophonemic.MorphologicalRules.Add(CreateCopyRule(FullPart()));

        string[] Parse(bool pruneCopies, string word)
        {
            Morpher morpher = CreateMorpher(pruneCopies);
            Assert.That(morpher.IsPartial, Is.True, "entry 54 has an empty syntactic feature structure");
            morpher.AlwaysEnforceFinalTemplates = alwaysEnforceFinalTemplates;
            return ParseGlosses(morpher, word);
        }

        Assert.That(Parse(false, "pipi"), Does.Contain("54 RED"));
        foreach (string word in new[] { "pipi", "sagsag", "sagsa" })
            Assert.That(Parse(true, word), Is.EqualTo(Parse(false, word)), word);
    }

    /// <summary>
    /// The prune's weakest point: a self-feeding deletion strips two segments from the second copy only,
    /// and <see cref="Morpher.DeletionReapplications"/> = 0 lets analysis restore just one of them.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void CapLimitedDeletionInOneCopyParsesTheSameWithAndWithoutPruning(int deletionReapplications)
    {
        var vowel = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("voc+")
            .Value;
        AffixProcessRule redup = CreateCopyRule(FullPart());
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        Morphophonemic.MorphologicalRules.Add(redup);

        var gDelete = new RewriteRule
        {
            Name = "g_delete",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "g")).Value,
        };
        gDelete.Subrules.Add(
            new RewriteSubrule { LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(vowel).Value }
        );
        Allophonic.PhonologicalRules.Add(gDelete);

        LexEntry root = AddEntry(
            "GG",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "ggasa"
        );
        try
        {
            var generator = new Morpher(TraceManager, Language);
            Assert.That(
                generator.GenerateWords(root, new Morpheme[] { redup }, FeatureStruct.New().Value),
                Is.EquivalentTo(new[] { "ggasaasa" }),
                "forward derivation defines the expected parse"
            );

            string[] Parse(bool pruneCopies)
            {
                Morpher morpher = CreateMorpher(pruneCopies);
                morpher.DeletionReapplications = deletionReapplications;
                return ParseGlosses(morpher, "ggasaasa");
            }

            string[] unpruned = Parse(false);
            Assert.That(unpruned, Does.Contain("GG RED"), "the unpruned engine finds the generated analysis");
            Assert.That(Parse(true), Is.EqualTo(unpruned));
        }
        finally
        {
            Morphophonemic.Entries.Remove(root);
        }
    }

    /// <summary>
    /// Counterbleeding opacity: voicing changes only the second copy, then raising destroys the vowel that
    /// conditioned it, so analysis must restore the copy through an environment that is no longer on the surface.
    /// </summary>
    [Test]
    public void OpaqueChangeToOneCopyParsesTheSameWithAndWithoutPruning()
    {
        AffixProcessRule redup = CreateCopyRule(FullPart());
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        Morphophonemic.MorphologicalRules.Add(redup);

        var voicing = new RewriteRule
        {
            Name = "voicing",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "s")).Value,
        };
        voicing.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>
                    .New()
                    .Annotation(
                        FeatureStruct
                            .New(Language.PhonologicalFeatureSystem)
                            .Symbol(HCFeatureSystem.Segment)
                            .Symbol("vd+")
                            .Value
                    )
                    .Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "a")).Value,
                RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "a")).Value,
            }
        );
        Allophonic.PhonologicalRules.Add(voicing);

        var raising = new RewriteRule
        {
            Name = "raising",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "a")).Value,
        };
        raising.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>
                    .New()
                    .Annotation(
                        FeatureStruct
                            .New(Language.PhonologicalFeatureSystem)
                            .Symbol(HCFeatureSystem.Segment)
                            .Symbol("high+")
                            .Symbol("low-")
                            .Symbol("back-")
                            .Value
                    )
                    .Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(Table1, "z")).Value,
            }
        );
        Allophonic.PhonologicalRules.Add(raising);

        LexEntry root = AddEntry(
            "SA",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "sa"
        );
        try
        {
            var generator = new Morpher(TraceManager, Language);
            Assert.That(
                generator.GenerateWords(root, new Morpheme[] { redup }, FeatureStruct.New().Value),
                Is.EquivalentTo(new[] { "sazi" }),
                "forward derivation defines the expected parse"
            );

            string[] unpruned = ParseGlosses(CreateMorpher(false), "sazi");
            Assert.That(unpruned, Does.Contain("SA RED"), "the unpruned engine finds the generated analysis");
            Assert.That(ParseGlosses(CreateMorpher(true), "sazi"), Is.EqualTo(unpruned));
        }
        finally
        {
            Morphophonemic.Entries.Remove(root);
        }
    }

    /// <summary>
    /// Feature-bearing rewrite (s -> z between vowels, voicing): analysis unapplies it, so pruning keeps the parse.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void FeatureRewriteOfOneCopyParsesTheSameWithAndWithoutPruning(bool pruneCopies)
    {
        AffixProcessRule redup = CreateCopyRule(FullPart());
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value;
        Morphophonemic.MorphologicalRules.Add(redup);
        Allophonic.PhonologicalRules.Add(CreateBetweenVowelsRewrite(Table1, "s", Character(Table1, "z")));
        LexEntry root = AddEntry(
            "ROOT",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "sa"
        );
        try
        {
            Assert.That(
                new Morpher(TraceManager, Language).GenerateWords(
                    root,
                    new Morpheme[] { redup },
                    FeatureStruct.New().Value
                ),
                Is.EquivalentTo(new[] { "saza" })
            );
            Assert.That(ParseGlosses(CreateMorpher(pruneCopies), "saza"), Does.Contain("ROOT RED"));
        }
        finally
        {
            Morphophonemic.Entries.Remove(root);
        }
    }

    /// <summary>
    /// A rewrite between segments that differ only by their string representation (no symbolic features) must
    /// be unapplied by analysis so the changed copy can agree with the other; otherwise pruning would drop the parse.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void StrRepOnlyRewriteOfOneCopyParsesTheSameWithAndWithoutPruning(bool pruneCopies)
    {
        var table = new CharacterDefinitionTable { Name = "strrep" };
        table.AddSegment("a");
        table.AddSegment("s");
        table.AddSegment("z");
        var syntactic = new SyntacticFeatureSystem();
        syntactic.AddPartsOfSpeech(new FeatureSymbol("V", "Verb"));
        syntactic.Freeze();
        var phonological = new FeatureSystem();
        phonological.Freeze();
        var stratum = new Stratum(table) { Name = "S", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
        var language = new Language
        {
            Name = "StrRepOnly",
            PhonologicalFeatureSystem = phonological,
            SyntacticFeatureSystem = syntactic,
            Strata = { stratum },
        };
        AffixProcessRule redup = CreateCopyRule(FullPart());
        redup.RequiredSyntacticFeatureStruct = FeatureStruct.New(syntactic).Symbol("V").Value;
        stratum.MorphologicalRules.Add(redup);
        stratum.PhonologicalRules.Add(CreateBetweenVowelsRewrite(table, "s", table["z"].FeatureStruct));
        var root = new LexEntry
        {
            Id = "ROOT",
            Gloss = "ROOT",
            SyntacticFeatureStruct = FeatureStruct.New(syntactic).Symbol("V").Value,
        };
        root.Allomorphs.Add(new RootAllomorph(new Segments(table, "sa", true)));
        stratum.Entries.Add(root);

        var morpher = new Morpher(TraceManager, language) { PruneDisagreeingCopies = pruneCopies };
        Assert.That(
            morpher.GenerateWords(root, new Morpheme[] { redup }, FeatureStruct.New().Value),
            Is.EquivalentTo(new[] { "saza" })
        );
        Assert.That(ParseGlosses(morpher, "saza"), Does.Contain("ROOT RED"));
    }

    [Test]
    public void CopyAndModifyOfTheSamePartDoesNotInstallTheCopyAgreementPredicate()
    {
        AffixProcessAllomorph allomorph = CreateCopyAndModifyAllomorph();

        Assert.That(new AnalysisAffixProcessAllomorphRuleSpec(allomorph).CopyAgreementCheck, Is.Null);
    }

    [Test]
    public void TwoCopiesOfAPartStillInstallTheCopyAgreementPredicate()
    {
        AffixProcessAllomorph allomorph = CreateCopyAllomorph(FullPart(), 2);

        Assert.That(new AnalysisAffixProcessAllomorphRuleSpec(allomorph).CopyAgreementCheck, Is.Not.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CopyAndModifyOfTheSamePartParsesTheSameWithAndWithoutPruning(bool pruneCopies)
    {
        var rule = new AffixProcessRule
        {
            Name = "copy_modify",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        rule.Allomorphs.Add(CreateCopyAndModifyAllomorph());
        Morphophonemic.MorphologicalRules.Add(rule);
        LexEntry root = AddEntry(
            "ROOT",
            FeatureStruct.New(Language.SyntacticFeatureSystem).Symbol("V").Value,
            Morphophonemic,
            "pi"
        );
        try
        {
            string[] generated = new Morpher(TraceManager, Language)
                .GenerateWords(root, new Morpheme[] { rule }, FeatureStruct.New().Value)
                .ToArray();
            Assert.That(generated, Is.Not.Empty);
            Assert.That(ParseGlosses(CreateMorpher(pruneCopies), generated[0]), Does.Contain("ROOT RED"));
        }
        finally
        {
            Morphophonemic.Entries.Remove(root);
            Morphophonemic.MorphologicalRules.Remove(rule);
        }
    }

    private AffixProcessAllomorph CreateCopyAndModifyAllomorph()
    {
        FeatureStruct voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        return new AffixProcessAllomorph
        {
            Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(AnySegment).OneOrMore.Value },
            Rhs = { new CopyFromInput("1"), new ModifyFromInput("1", voiced) },
        };
    }

    private RewriteRule CreateBetweenVowelsRewrite(CharacterDefinitionTable table, string from, FeatureStruct rhs)
    {
        var rule = new RewriteRule
        {
            Name = "rewrite",
            Lhs = Pattern<Word, ShapeNode>.New().Annotation(Character(table, from)).Value,
        };
        rule.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(rhs).Value,
                LeftEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(table, "a")).Value,
                RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(Character(table, "a")).Value,
            }
        );
        return rule;
    }

    private Morpher CreateMorpher(bool pruneCopies, int maxDegreeOfParallelism = 0)
    {
        return new Morpher(TraceManager, Language, maxDegreeOfParallelism)
        {
            PruneDisagreeingCopies = pruneCopies,
            RootSubstringPrune = RootSubstringPruneMode.Off,
        };
    }

    private static string[] ParseGlosses(Morpher morpher, string word)
    {
        return morpher
            .ParseWord(word)
            .Select(w => string.Join(" ", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }

    private static Pattern<Word, ShapeNode> FullPart()
    {
        return Pattern<Word, ShapeNode>.New("copy").Annotation(AnySegment).OneOrMore.Value;
    }

    private static Pattern<Word, ShapeNode> SingleSegmentPart()
    {
        return Pattern<Word, ShapeNode>.New("copy").Annotation(AnySegment).Value;
    }

    private static AffixProcessRule CreateCopyRule(Pattern<Word, ShapeNode> part, int copyCount = 2)
    {
        var rule = new AffixProcessRule
        {
            Name = "full_copy",
            Gloss = "RED",
            RequiredSyntacticFeatureStruct = FeatureStruct.New().Value,
            OutSyntacticFeatureStruct = FeatureStruct.New().Value,
        };
        rule.Allomorphs.Add(CreateCopyAllomorph(part, copyCount));
        return rule;
    }

    private static AffixProcessAllomorph CreateCopyAllomorph(Pattern<Word, ShapeNode> part, int copyCount)
    {
        var allomorph = new AffixProcessAllomorph { Lhs = { part } };
        for (int i = 0; i < copyCount; i++)
            allomorph.Rhs.Add(new CopyFromInput("copy"));
        return allomorph;
    }

    private (string First, string Second) FindIncompatibleSegments()
    {
        CharacterDefinition[] segments = Table3
            .Where(definition => definition.Type == HCFeatureSystem.Segment)
            .ToArray();
        var pair = segments
            .SelectMany(
                (first, index) => segments.Skip(index + 1).Select(second => new { First = first, Second = second })
            )
            .First(candidate => !candidate.First.FeatureStruct.IsUnifiable(candidate.Second.FeatureStruct));
        return (pair.First.Representations.First(), pair.Second.Representations.First());
    }

    private string[] AnalysisForms(bool pruneCopies, AffixProcessRule rule, Word input)
    {
        return new AnalysisAffixProcessRule(CreateMorpher(pruneCopies), rule)
            .Apply(input)
            .Select(output => output.Shape.ToString(Table3, false))
            .OrderBy(form => form, StringComparer.Ordinal)
            .ToArray();
    }

    private Word CreateInputWithOptionalSecondSegment(string representation)
    {
        var input = new Word(Surface, Table3.Segment(representation));
        input.Shape.First.Next.Annotation.Optional = true;
        input.Freeze();
        return input;
    }

    private Word CreateInputWithOptionalFirstSegment(string representation)
    {
        var input = new Word(Surface, Table3.Segment(representation));
        input.Shape.First.Annotation.Optional = true;
        input.Freeze();
        return input;
    }

    private Word CreateInput(string representation)
    {
        var input = new Word(Surface, Table3.Segment(representation));
        input.Freeze();
        return input;
    }
}
