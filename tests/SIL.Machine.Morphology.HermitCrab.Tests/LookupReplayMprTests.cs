using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class LookupReplayMprTests
{
    private const string Letters = "aeioulmnrstkpd";

    private string _dir = default!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "hc-lookup-replay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [OneTimeTearDown]
    public void TearDown() => Directory.Delete(_dir, true);

    [Test]
    public void Default_IsOn()
    {
        Language language = Load(Grammar(rules: Rule("mrSuf", "SUF", Sub("a")), entries: Entry("eA", "tel", "")));
        var morpher = new Morpher(new TraceManager(), language);
        Assert.That(morpher.LookupReplayMode, Is.EqualTo(LookupReplayMode.On));
    }

    [Test]
    public void RequiredMpr_WrongClassRoot_IsPrunedAndParsesAreUnchanged()
    {
        Language language = Load(
            Grammar(
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprA"), Sub("o", required: "mprB")),
                entries: Entry("eA", "tel", "mprA") + Entry("eB", "tel", "mprB")
            )
        );

        (string[] on, int syntheses) = ParseCounting(language, "tela", prune: true);
        (string[] off, int synthesesOff) = ParseCounting(language, "tela", prune: false);

        Assert.That(on, Is.EqualTo(off));
        Assert.That(on, Is.EqualTo(new[] { "eA+SUF" }));
        Assert.That(synthesesOff, Is.GreaterThan(syntheses));
        Assert.That(syntheses, Is.EqualTo(1));
    }

    [Test]
    public void RequiredMpr_AnyGroup_IsSatisfiedByOneFeature()
    {
        Language language = Load(
            Grammar(
                groups: Group("mprA mprB", "any", "append"),
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprA mprB")),
                entries: Entry("eB", "tel", "mprB") + Entry("eC", "tel", "mprC")
            )
        );

        Assert.That(Verdicts(language, "tela"), Is.EqualTo(new[] { "eB:True", "eC:False" }));
    }

    [Test]
    public void ExcludedMpr_RootCarriesTheExcludedFeature_IsPruned()
    {
        Language language = Load(
            Grammar(
                rules: Rule("mrSuf", "SUF", Sub("a", excluded: "mprA")),
                entries: Entry("eA", "tel", "mprA") + Entry("eB", "tel", "mprB")
            )
        );

        Assert.That(Verdicts(language, "tela"), Is.EqualTo(new[] { "eA:False", "eB:True" }));
        Assert.That(ParseCounting(language, "tela", prune: true).Parses, Is.EqualTo(new[] { "eB+SUF" }));
    }

    [Test]
    public void ExcludedMpr_ReachableOverwriteGroup_IsNotUsedToPrune()
    {
        // mrTheme outputs mprB of an overwrite group, which can remove mprA, so the excluded test is unsafe.
        Language language = Load(
            Grammar(
                groups: Group("mprA mprB", "all", "overwrite"),
                rules: Rule("mrTheme", "THEME", Sub("u", outMpr: "mprB"))
                    + Rule("mrSuf", "SUF", Sub("a", excluded: "mprA")),
                entries: Entry("eA", "tel", "mprA"),
                order: "mrTheme mrSuf"
            )
        );

        Assert.That(Verdicts(language, "telua"), Is.EqualTo(new[] { "eA:True" }));
        Assert.That(ParseCounting(language, "telua", prune: true).Parses, Is.EqualTo(new[] { "eA+THEME+SUF" }));
        Assert.That(ParseCounting(language, "telua", prune: false).Parses, Is.EqualTo(new[] { "eA+THEME+SUF" }));
    }

    [Test]
    public void RequiredMpr_OutputOfAnEarlierRule_IsSeenByALaterRule()
    {
        Language language = Load(
            Grammar(
                rules: Rule("mrTheme", "THEME", Sub("u", outMpr: "mprB"))
                    + Rule("mrSuf", "SUF", Sub("a", required: "mprB")),
                entries: Entry("eA", "tel", "mprA"),
                order: "mrTheme mrSuf"
            )
        );

        Assert.That(Verdicts(language, "telua"), Is.EqualTo(new[] { "eA:True" }));
        Assert.That(ParseCounting(language, "telua", prune: true).Parses, Is.EqualTo(new[] { "eA+THEME+SUF" }));
    }

    [Test]
    public void RequiredStemName_RootWithAnotherStemName_IsPruned()
    {
        Language language = Load(
            Grammar(
                header: HeadFeatures,
                stemNames: StemNames,
                rules: StemRule("mrPers", "PERS", null, PersOutput, Sub("m"))
                    + StemRule("mrSuf", "SUF", "snP1", "", Sub("o")),
                entries: Entry("eX", "kap", "") + Entry("eY", "kap", "", stemName: "snP1"),
                order: "mrPers mrSuf"
            )
        );

        Assert.That(Verdicts(language, "kapmo"), Is.EqualTo(new[] { "eX:False", "eY:True" }));
        Assert.That(ParseCounting(language, "kapmo", prune: true).Parses, Is.EqualTo(new[] { "eY+PERS+SUF" }));
        Assert.That(ParseCounting(language, "kapmo", prune: false).Parses, Is.EqualTo(new[] { "eY+PERS+SUF" }));
    }

    [Test]
    public void FamilyRoot_IsNeverPruned()
    {
        Language language = Load(
            Grammar(
                families: "<Families><Family id=\"fam\">f</Family></Families>",
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprA")),
                entries: Entry("eB", "tel", "mprB", family: "fam") + Entry("eC", "tel", "mprB", family: "fam")
            )
        );

        Assert.That(Verdicts(language, "tela"), Is.EqualTo(new[] { "eB:True", "eC:True" }));
    }

    [Test]
    public void MergedAlternatives_AreJudgedPerAlternativeAndTheFeasibleOneSurvives()
    {
        // Both allomorphs insert "a", so with merging on the two analyses of "tela" are merged and later
        // expanded. Only the alternative that records the class-A allomorph is feasible for the class-A root.
        Language language = Load(
            Grammar(
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprB"), Sub("a", required: "mprA")),
                entries: Entry("eA", "tel", "mprA")
            )
        );

        foreach (bool merge in new[] { true, false })
        {
            var verdicts = new List<string>();
            var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
            {
                MergeEquivalentAnalyses = merge,
                LookupReplayMode = LookupReplayMode.Shadow,
                LookupReplayVerdict = (w, ok) => verdicts.Add(w.AllomorphApplications[0].Index + ":" + ok),
            };
            string[] parses = Signatures(morpher.ParseWord("tela"));

            Assert.That(parses, Is.EqualTo(new[] { "eA+SUF" }), "merge=" + merge);
            Assert.That(
                verdicts.OrderBy(v => v, StringComparer.Ordinal),
                Is.EqualTo(new[] { "0:False", "1:True" }),
                "merge=" + merge
            );

            var pruning = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
            {
                MergeEquivalentAnalyses = merge,
            };
            Assert.That(Signatures(pruning.ParseWord("tela")), Is.EqualTo(parses), "merge=" + merge);
        }
    }

    [Test]
    public void NullRecordedAllomorph_IsPrunedOnlyWhenEveryAllomorphFails()
    {
        // Guessed (null) allomorphs happen when the analysis came from a rule whose allomorph is unconstrained;
        // the predicate must keep the candidate if any allomorph of the rule could still apply.
        Language language = Load(
            Grammar(
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprA"), Sub("a", required: "mprB")),
                entries: Entry("eA", "tel", "mprA") + Entry("eC", "tel", "mprC")
            )
        );
        var rule = (AffixProcessRule)language.Strata[0].MorphologicalRules[0];

        Assert.That(IsFeasible(language, "eA", rule, null), Is.True);
        Assert.That(IsFeasible(language, "eC", rule, null), Is.False);
        Assert.That(IsFeasible(language, "eA", rule, rule.Allomorphs[1]), Is.False);
    }

    [Test]
    public void ExcludedMpr_AnyGroup_RejectsOnlyWhenEveryFeatureIsPresent()
    {
        Language language = Load(
            Grammar(
                groups: Group("mprA mprB", "any", "append"),
                rules: Rule("mrSuf", "SUF", Sub("a", excluded: "mprA mprB")),
                entries: Entry("eA", "tel", "mprA") + Entry("eAB", "tel", "mprA mprB")
            )
        );

        Assert.That(Verdicts(language, "tela"), Is.EqualTo(new[] { "eA:True", "eAB:False" }));
        Assert.That(ParseCounting(language, "tela", prune: true).Parses, Is.EqualTo(new[] { "eA+SUF" }));
    }

    [Test]
    public void RequiredMpr_OverwriteRemovesTheFeature_IsNotPrunedAndParsesAreUnchanged()
    {
        // mrTheme outputs mprB of an overwrite group, which removes mprA, so mrSuf can never apply. The replay
        // ignores removals for required checks and keeps the candidate; synthesis then fails the same way.
        Language language = Load(
            Grammar(
                groups: Group("mprA mprB", "all", "overwrite"),
                rules: Rule("mrTheme", "THEME", Sub("u", outMpr: "mprB"))
                    + Rule("mrSuf", "SUF", Sub("a", required: "mprA")),
                entries: Entry("eA", "tel", "mprA"),
                order: "mrTheme mrSuf"
            )
        );

        Assert.That(Verdicts(language, "telua"), Is.EqualTo(new[] { "eA:True" }));
        Assert.That(ParseCounting(language, "telua", prune: true).Parses, Is.Empty);
        Assert.That(ParseCounting(language, "telua", prune: false).Parses, Is.Empty);
    }

    [Test]
    public void ParallelSynthesis_PrunesTheSameCandidatesAndParsesAreUnchanged()
    {
        Language language = Load(
            Grammar(
                rules: Rule("mrSuf", "SUF", Sub("a", required: "mprA"), Sub("o", required: "mprB")),
                entries: Entry("eA", "tel", "mprA") + Entry("eB", "tel", "mprB")
            )
        );
        var verdicts = new System.Collections.Concurrent.ConcurrentBag<string>();
        var shadow = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 4)
        {
            LookupReplayMode = LookupReplayMode.Shadow,
            LookupReplayVerdict = (w, ok) => verdicts.Add(((LexEntry)w.RootAllomorph.Morpheme).Id + ":" + ok),
        };
        shadow.ParseWord("tela").ToList();
        var off = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 4)
        {
            LookupReplayMode = LookupReplayMode.Off,
        };
        var on = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 4);

        Assert.That(verdicts.OrderBy(v => v, StringComparer.Ordinal), Is.EqualTo(new[] { "eA:True", "eB:False" }));
        Assert.That(Signatures(on.ParseWord("tela")), Is.EqualTo(Signatures(off.ParseWord("tela"))));
        Assert.That(Signatures(on.ParseWord("tela")), Is.EqualTo(new[] { "eA+SUF" }));
    }

    [Test]
    public void ClassGatedSlots_PruneMostWrongClassCandidatesAndParsesAreUnchanged()
    {
        // Three agreement slots, each with an allomorph per noun class, and one homophonous root per class: only
        // the root whose class matches every slot survives, so the other candidates are doomed.
        string[] classes = { "mprA", "mprB", "mprC" };
        string Slot(string id, string name, string insert) =>
            Rule(id, name, classes.Select((c, i) => Sub(insert + new string('a', i), required: c)).ToArray());
        Language language = Load(
            Grammar(
                rules: Slot("mrOne", "ONE", "t") + Slot("mrTwo", "TWO", "k") + Slot("mrThree", "THR", "p"),
                entries: Entry("eA", "tel", "mprA") + Entry("eB", "tel", "mprB") + Entry("eC", "tel", "mprC")
            )
        );

        (string[] on, int syntheses) = ParseCounting(language, "teltkp", prune: true);
        (string[] off, int synthesesOff) = ParseCounting(language, "teltkp", prune: false);

        Assert.That(on, Is.EqualTo(off));
        Assert.That(on, Is.Not.Empty);
        Assert.That(synthesesOff, Is.GreaterThan(syntheses));
    }

    [Test]
    public void FirstViolatedEntry_StopsTheReplay()
    {
        Language language = Load(
            Grammar(
                rules: Rule("mrX", "X", Sub("a", required: "mprA")) + Rule("mrY", "Y", Sub("o", required: "mprB")),
                entries: Entry("eC", "tel", "mprC")
            )
        );
        LexEntry entry = language.Strata[0].Entries.Single();
        var word = new Word(entry.PrimaryAllomorph, FeatureStruct.New().Value);
        word.MorphologicalRuleUnapplied((AffixProcessRule)language.Strata[0].MorphologicalRules[0], null);
        word.MorphologicalRuleUnapplied((AffixProcessRule)language.Strata[0].MorphologicalRules[1], null);
        var replay = new LookupReplay(language);

        Assert.That(replay.IsFeasible(word), Is.False);
        // One superset built and one required match, then the replay stops before the other rule.
        Assert.That(replay.Work, Is.EqualTo(2));
    }

    [Test]
    public void GrammarWithoutConstraints_DoesNoReplayWork()
    {
        Language language = Load(Grammar(rules: Rule("mrSuf", "SUF", Sub("a")), entries: Entry("eA", "tel", "mprA")));
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1);

        Assert.That(Signatures(morpher.ParseWord("tela")), Is.EqualTo(new[] { "eA+SUF" }));
        Assert.That(morpher.LookupReplayWork, Is.EqualTo(0));
    }

    private static bool IsFeasible(Language language, string entryId, AffixProcessRule rule, Allomorph? recorded)
    {
        LexEntry entry = language.Strata[0].Entries.Single(e => e.Id == entryId);
        var word = new Word(entry.PrimaryAllomorph, FeatureStruct.New().Value);
        word.MorphologicalRuleUnapplied(rule, recorded);
        return new LookupReplay(language).IsFeasible(word);
    }

    private static (string[] Parses, int Syntheses) ParseCounting(Language language, string word, bool prune)
    {
        var tm = new TraceManager { IsTracing = true };
        var morpher = new Morpher(tm, language, maxDegreeOfParallelism: 1)
        {
            LookupReplayMode = prune ? LookupReplayMode.On : LookupReplayMode.Off,
        };
        string[] parses = Signatures(morpher.ParseWord(word, out object trace));
        return (parses, CountSynthesisRuleEvents((Trace)trace));
    }

    private static int CountSynthesisRuleEvents(Trace trace) =>
        (trace.Type == TraceType.MorphologicalRuleSynthesis ? 1 : 0) + trace.Children.Sum(CountSynthesisRuleEvents);

    private static List<string> Verdicts(Language language, string word)
    {
        var verdicts = new List<string>();
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism: 1)
        {
            LookupReplayMode = LookupReplayMode.Shadow,
            LookupReplayVerdict = (w, ok) => verdicts.Add(((LexEntry)w.RootAllomorph.Morpheme).Id + ":" + ok),
        };
        morpher.ParseWord(word).ToList();
        verdicts.Sort(StringComparer.Ordinal);
        return verdicts;
    }

    private static string[] Signatures(IEnumerable<Word> words) =>
        words
            .Select(w => string.Join("+", w.AllomorphsInMorphOrder.Select(a => a.Morpheme.Id)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    private Language Load(string xml)
    {
        string path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".xml");
        File.WriteAllText(path, xml);
        return XmlLanguageLoader.Load(path);
    }

    private const string HeadFeatures =
        "<HeadFeatures><SymbolicFeature id=\"featPers\"><Name>pers</Name><Symbols><Symbol id=\"symP1\">1</Symbol></Symbols></SymbolicFeature></HeadFeatures>";

    private const string StemNames =
        "<StemNames><StemName id=\"snP1\" partsOfSpeech=\"posN\"><Name>P1</Name><Regions><Region><AssignedHeadFeatures>"
        + "<FeatureValue feature=\"featPers\" symbolValues=\"symP1\" /></AssignedHeadFeatures></Region></Regions></StemName></StemNames>";

    private static string Group(string features, string matchType, string output) =>
        $"<MorphologicalPhonologicalRuleFeatureGroup outputType=\"{output}\" matchType=\"{matchType}\" features=\"{features}\"><Name>g{features.Length}</Name></MorphologicalPhonologicalRuleFeatureGroup>";

    private static string Sub(string shape, string? required = null, string? excluded = null, string? outMpr = null)
    {
        string id = "s" + Guid.NewGuid().ToString("N");
        string input =
            (required != null ? $" requiredMPRFeatures=\"{required}\"" : "")
            + (excluded != null ? $" excludedMPRFeatures=\"{excluded}\"" : "");
        string output = outMpr != null ? $" MPRFeatures=\"{outMpr}\"" : "";
        return $"<MorphologicalSubrule id=\"{id}\"><MorphologicalInput{input}><PhoneticSequence id=\"{id}p\"><OptionalSegmentSequence min=\"1\" max=\"-1\"><SimpleContext naturalClass=\"ncAny\" /></OptionalSegmentSequence></PhoneticSequence></MorphologicalInput><MorphologicalOutput{output}><CopyFromInput index=\"{id}p\" /><InsertSegments><PhoneticShape>{shape}</PhoneticShape></InsertSegments></MorphologicalOutput></MorphologicalSubrule>";
    }

    private const string PersOutput =
        "<OutputHeadFeatures><FeatureValue feature=\"featPers\" symbolValues=\"symP1\" /></OutputHeadFeatures>";

    private static string Rule(string id, string morphemeId, params string[] subs) =>
        StemRule(id, morphemeId, null, "", subs);

    private static string StemRule(
        string id,
        string morphemeId,
        string? stemName,
        string extra,
        params string[] subs
    ) =>
        $"<MorphologicalRule id=\"{id}\" requiredPartsOfSpeech=\"posN\" outputPartOfSpeech=\"posN\"{(stemName != null ? $" requiredStemName=\"{stemName}\"" : "")}><Name>{id}</Name><MorphologicalSubrules>{string.Concat(subs)}</MorphologicalSubrules>{extra}<MorphemeId>{morphemeId}</MorphemeId></MorphologicalRule>";

    private static string Entry(
        string id,
        string shape,
        string features,
        string? stemName = null,
        string? family = null
    ) =>
        $"<LexicalEntry id=\"{id}\" partOfSpeech=\"posN\"{(features.Length > 0 ? $" ruleFeatures=\"{features}\"" : "")}{(family != null ? $" family=\"{family}\"" : "")}><Allomorphs><Allomorph id=\"a{id}\"{(stemName != null ? $" stemName=\"{stemName}\"" : "")}><PhoneticShape>{shape}</PhoneticShape></Allomorph></Allomorphs><MorphemeId>{id}</MorphemeId><Gloss>{id}</Gloss></LexicalEntry>";

    private static string Grammar(
        string rules,
        string entries,
        string groups = "",
        string header = "",
        string stemNames = "",
        string families = "",
        string? order = null
    )
    {
        string segments = string.Concat(
            Letters.Select(c =>
                $"<SegmentDefinition id=\"c{c}\"><Representations><Representation>{c}</Representation></Representations></SegmentDefinition>"
            )
        );
        var ruleIds = System
            .Text.RegularExpressions.Regex.Matches(rules, "<MorphologicalRule id=\"(\\w+)\"")
            .Select(m => m.Groups[1].Value);
        string ruleOrder = order != null ? "linear" : "unordered";
        string ruleList = order ?? string.Join(" ", ruleIds);
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?><!DOCTYPE HermitCrabInput SYSTEM \"HermitCrabInput.dtd\"><HermitCrabInput><Language><Name>T</Name>"
            + "<PartsOfSpeech><PartOfSpeech id=\"posN\"><Name>n</Name></PartOfSpeech></PartsOfSpeech>"
            + header
            + "<MorphologicalPhonologicalRuleFeatures>"
            + string.Concat(
                new[] { "mprA", "mprB", "mprC" }.Select(f =>
                    $"<MorphologicalPhonologicalRuleFeature id=\"{f}\">{f}</MorphologicalPhonologicalRuleFeature>"
                )
            )
            + (groups.Length > 0 ? groups : "")
            + "</MorphologicalPhonologicalRuleFeatures>"
            + stemNames
            + $"<CharacterDefinitionTable id=\"t1\"><Name>Main</Name><SegmentDefinitions>{segments}</SegmentDefinitions></CharacterDefinitionTable>"
            + "<NaturalClasses><FeatureNaturalClass id=\"ncAny\"><Name>Any</Name></FeatureNaturalClass></NaturalClasses>"
            + families
            + $"<Strata><Stratum characterDefinitionTable=\"t1\" morphologicalRuleOrder=\"{ruleOrder}\" morphologicalRules=\"{ruleList}\"><Name>S</Name>"
            + $"<MorphologicalRuleDefinitions>{rules}</MorphologicalRuleDefinitions><LexicalEntries>{entries}</LexicalEntries></Stratum></Strata></Language></HermitCrabInput>";
    }
}
