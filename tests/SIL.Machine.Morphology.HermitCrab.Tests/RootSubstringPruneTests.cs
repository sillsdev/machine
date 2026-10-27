using NUnit.Framework;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class RootSubstringPruneTests : HermitCrabTestBase
{
    private static readonly FeatureStruct Any = FeatureStruct.New().Symbol(HCFeatureSystem.Segment).Value;

    private int _attempts;

    private AffixProcessRule Suffix(string name, string insert, string? requiredPos = null)
    {
        var rule = new AffixProcessRule
        {
            Id = name,
            Name = name,
            Gloss = name,
        };
        if (requiredPos != null)
        {
            rule.RequiredSyntacticFeatureStruct = FeatureStruct
                .New(Language.SyntacticFeatureSystem)
                .Symbol(requiredPos)
                .Value;
        }
        rule.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, insert) },
            }
        );
        return rule;
    }

    private Morpher CreateMorpher(RootSubstringPruneMode mode)
    {
        var morpher = new Morpher(TraceManager, Language, maxDegreeOfParallelism: 1) { RootSubstringPrune = mode };
        morpher.RuleSelector = rule =>
        {
            if (rule is IMorphologicalRule)
                Interlocked.Increment(ref _attempts);
            return true;
        };
        return morpher;
    }

    private static string[] Signature(IEnumerable<Word> words) =>
        words
            .Select(w => string.Join(" ", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Gloss)))
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    private (string[] Parses, int Attempts) Parse(RootSubstringPruneMode mode, string word)
    {
        _attempts = 0;
        Morpher morpher = CreateMorpher(mode);
        string[] parses = Signature(morpher.ParseWord(word));
        return (parses, _attempts);
    }

    private void AssertSameParses(string word, bool expectParse, bool expectFewerAttempts = false)
    {
        (string[] off, int offAttempts) = Parse(RootSubstringPruneMode.Off, word);
        (string[] on, int onAttempts) = Parse(RootSubstringPruneMode.On, word);
        Assert.That(on, Is.EqualTo(off));
        Assert.That(off.Length > 0, Is.EqualTo(expectParse));
        if (expectFewerAttempts)
            Assert.That(onAttempts, Is.LessThan(offAttempts));
        else
            Assert.That(onAttempts, Is.EqualTo(offAttempts));
    }

    [Test]
    public void SuffixingGrammar_RootNotInLexicon_FewerAttemptsAndNoParse()
    {
        Morphophonemic.MorphologicalRules.Add(Suffix("dRule", "+d"));
        Morphophonemic.MorphologicalRules.Add(Suffix("zRule", "+z"));

        Assert.That(CreateMorpher(RootSubstringPruneMode.On).RootSubstringPruneStrata[Morphophonemic].Enabled, Is.True);
        AssertSameParses("kukdz", expectParse: false, expectFewerAttempts: true);
    }

    [Test]
    public void SuffixingGrammar_RootInLexicon_ParseSurvives()
    {
        Morphophonemic.MorphologicalRules.Add(Suffix("dRule", "+d", "V"));
        Morphophonemic.MorphologicalRules.Add(Suffix("zRule", "+z", "V"));

        AssertSameParses("sagd", expectParse: true);
        AssertSameParses("sagdz", expectParse: true);
    }

    [Test]
    public void Infix_FlagOffAndInfixedWordParses()
    {
        var cons = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons+")
            .Value;
        var infix = new AffixProcessRule
        {
            Id = "perf",
            Name = "perf",
            Gloss = "perf",
        };
        infix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(cons).Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(cons).Value,
                    Pattern<Word, ShapeNode>.New("3").Annotation(cons).Value,
                },
                Rhs =
                {
                    new CopyFromInput("1"),
                    new InsertSegments(Table3, "a"),
                    new CopyFromInput("2"),
                    new InsertSegments(Table3, "a"),
                    new CopyFromInput("3"),
                },
            }
        );
        Morphophonemic.MorphologicalRules.Add(infix);
        Morphophonemic.MorphologicalRules.Add(Suffix("zRule", "+z"));

        Assert.That(
            CreateMorpher(RootSubstringPruneMode.On).RootSubstringPruneStrata[Morphophonemic].Enabled,
            Is.False
        );
        AssertSameParses("katabz", expectParse: true);
    }

    [Test]
    public void DeeperStratumPhonologicalRule_FlagOffAndWordParses()
    {
        var p = Character(Table3, "p");
        var asp = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("asp+")
            .Value;
        var nonCons = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("cons-")
            .Value;
        var rule = new RewriteRule { Name = "asp", Lhs = Pattern<Word, ShapeNode>.New().Annotation(p).Value };
        Morphophonemic.PhonologicalRules.Add(rule);
        rule.Subrules.Add(
            new RewriteSubrule
            {
                Rhs = Pattern<Word, ShapeNode>.New().Annotation(asp).Value,
                RightEnvironment = Pattern<Word, ShapeNode>.New().Annotation(nonCons).Value,
            }
        );
        Allophonic.MorphologicalRules.Add(Suffix("dRule", "+d"));

        Morpher morpher = CreateMorpher(RootSubstringPruneMode.On);
        Assert.That(morpher.RootSubstringPruneStrata[Morphophonemic].Enabled, Is.True);
        Assert.That(morpher.RootSubstringPruneStrata[Allophonic].Enabled, Is.False);
        Assert.That(morpher.RootSubstringPruneStrata[Allophonic].DisabledReason, Does.Contain("phonological"));
        AssertSameParses("pʰud", expectParse: true);
    }

    [Test]
    public void ModifyFromInput_FlagOff()
    {
        var voiced = FeatureStruct
            .New(Language.PhonologicalFeatureSystem)
            .Symbol(HCFeatureSystem.Segment)
            .Symbol("vd+")
            .Value;
        var p = Character(Table1, "p");
        var simulfix = new AffixProcessRule
        {
            Id = "SIMUL",
            Name = "SIMUL",
            Gloss = "SIMUL",
        };
        simulfix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(p).Value,
                },
                Rhs = { new CopyFromInput("1"), new ModifyFromInput("2", voiced) },
            }
        );
        Allophonic.MorphologicalRules.Add(simulfix);

        Morpher morpher = CreateMorpher(RootSubstringPruneMode.On);
        Assert.That(morpher.RootSubstringPruneStrata[Allophonic].Enabled, Is.False);
        Assert.That(morpher.RootSubstringPruneStrata[Allophonic].DisabledReason, Does.Contain("modifies"));
        Assert.That(morpher.RootSubstringPruneStrata[Surface].Enabled, Is.False);
        Assert.That(morpher.RootSubstringPruneStrata[Morphophonemic].Enabled, Is.True);
        AssertSameParses("pib", expectParse: true);
    }

    [Test]
    public void TruncationRestoredLater_FlagOffAndWordParses()
    {
        var truncate = new AffixProcessRule
        {
            Id = "trunc",
            Name = "trunc",
            Gloss = "trunc",
        };
        truncate.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs =
                {
                    Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value,
                    Pattern<Word, ShapeNode>.New("2").Annotation(Character(Table3, "g")).Value,
                },
                Rhs = { new CopyFromInput("1"), new InsertSegments(Table3, "+d") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(truncate);
        Morphophonemic.MorphologicalRules.Add(Suffix("zRule", "+z"));

        Morpher morpher = CreateMorpher(RootSubstringPruneMode.On);
        Assert.That(morpher.RootSubstringPruneStrata[Morphophonemic].Enabled, Is.False);
        Assert.That(morpher.RootSubstringPruneStrata[Morphophonemic].DisabledReason, Does.Contain("drops the part"));
        AssertSameParses("sadz", expectParse: true);
    }

    [Test]
    public void Compounding_TwoLexiconRootsParse_UnknownHeadIsPruned()
    {
        Allophonic.MorphologicalRules.Add(
            new CompoundingRule
            {
                Name = "rule1",
                Subrules =
                {
                    new CompoundingSubrule
                    {
                        HeadLhs = { Pattern<Word, ShapeNode>.New("head").Annotation(Any).OneOrMore.Value },
                        NonHeadLhs = { Pattern<Word, ShapeNode>.New("nonHead").Annotation(Any).OneOrMore.Value },
                        Rhs =
                        {
                            new CopyFromInput("head"),
                            new InsertSegments(Table3, "+"),
                            new CopyFromInput("nonHead"),
                        },
                    },
                },
            }
        );

        Assert.That(CreateMorpher(RootSubstringPruneMode.On).RootSubstringPruneStrata[Allophonic].Enabled, Is.True);
        AssertSameParses("pʰutdat", expectParse: true);
        AssertSameParses("zazdat", expectParse: false, expectFewerAttempts: true);
    }

    [Test]
    public void LexicalPatternRoot_FlagOffAndGuessedRootsUnchanged()
    {
        Morphophonemic.MorphologicalRules.Add(Suffix("ed_suffix", "+d", "V"));
        Morphophonemic.CharacterDefinitionTable.AddNaturalClass(new NaturalClass(new FeatureStruct()) { Name = "Any" });
        LexEntry pattern = AddEntry("pattern", new FeatureStruct(), Morphophonemic, "[Any]*");
        try
        {
            var guesses = new List<string[]>();
            foreach (RootSubstringPruneMode mode in new[] { RootSubstringPruneMode.Off, RootSubstringPruneMode.On })
            {
                var morpher = new Morpher(TraceManager, Language) { RootSubstringPrune = mode };
                Assert.That(morpher.RootSubstringPruneStrata.Values, Has.None.Property("Enabled").True);
                Assert.That(morpher.AnalyzeWord("gagd"), Is.Empty);
                guesses.Add(morpher.AnalyzeWord("gagd", true).Select(a => a.ToString()).ToArray());
            }
            Assert.That(guesses[1], Is.EqualTo(guesses[0]));
            Assert.That(guesses[1], Does.Contain("[*gag ed_suffix]"));
        }
        finally
        {
            Morphophonemic.Entries.Remove(pattern);
        }
    }

    [Test]
    public void Shadow_ParsesUnchangedAndReportsFiredNodes()
    {
        Morphophonemic.MorphologicalRules.Add(Suffix("dRule", "+d"));
        Morphophonemic.MorphologicalRules.Add(Suffix("zRule", "+z"));

        (string[] off, int offAttempts) = Parse(RootSubstringPruneMode.Off, "kukdz");
        Morpher shadow = CreateMorpher(RootSubstringPruneMode.Shadow);
        int fired = 0;
        int probes = 0;
        shadow.RootSubstringProbed = () => probes++;
        shadow.RootSubstringShadow = (word, site) => fired++;
        _attempts = 0;
        string[] parses = Signature(shadow.ParseWord("kukdz"));

        Assert.That(parses, Is.EqualTo(off));
        Assert.That(_attempts, Is.EqualTo(offAttempts));
        Assert.That(fired, Is.GreaterThan(0));
        Assert.That(probes, Is.GreaterThan(0));
    }

    [Test]
    public void RuleThatRemovesNoSegments_IsNotProbed()
    {
        var nullAffix = new AffixProcessRule
        {
            Id = "nullRule",
            Name = "nullRule",
            Gloss = "nullRule",
        };
        nullAffix.Allomorphs.Add(
            new AffixProcessAllomorph
            {
                Lhs = { Pattern<Word, ShapeNode>.New("1").Annotation(Any).OneOrMore.Value },
                Rhs = { new CopyFromInput("1") },
            }
        );
        Morphophonemic.MorphologicalRules.Add(nullAffix);

        (string[] off, _) = Parse(RootSubstringPruneMode.Off, "kuk");
        Morpher on = CreateMorpher(RootSubstringPruneMode.On);
        int probes = 0;
        on.RootSubstringProbed = () => probes++;
        Assert.That(Signature(on.ParseWord("kuk")), Is.EqualTo(off));
        Assert.That(probes, Is.EqualTo(0));
    }
}
