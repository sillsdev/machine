using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab;

// Each grammar in TestData/PartialPrune puts partial material where a careless prune would miss it. The .expected
// files hold master's analyses, so a lost, extra or changed one fails.
public class PartialRulePruneTests
{
    private static readonly string[] Grammars =
    [
        "g6", // partial rules with no category
        "g6b", // partial rules in a deeper stratum than the template
        "g7", // a slot rule shared by a final and a non-final template
        "g8", // final templates a partial rule can reach and ones it cannot
        "g10", // plain prefixes and suffixes around the partial material
        "g11", // partial material wrapped by affixes and copied by reduplication
        "g12", // an infix splits the partial material
        "g13", // truncation replaces the partial rule's last segment
        "g13b", // truncation of the partial prefix's first segment
        "g13c", // truncation replaces the partial rule's last segment, minimal
        "g14", // a copied part that can be empty
        "g15", // two parts copied in swapped order move the material apart
        "g16", // compounding outside the partial rule
        "g17", // partial rules in a deeper stratum
        "g18", // partial rules in both strata
        "g19", // phonological deletion hides a partial segment
        "g19c", // phonological deletion hides a partial segment, minimal
        "g20", // three partial rules
        "g21", // compounding and a partial rule the look-ahead cannot reason about
        "cx1b", // a compound puts its non-head between two head parts, hiding the partial material
        "cx2", // a flagged and an unflagged word differ only in NeedsPartial and must both survive
        "cx3", // lexical family and a partial rule: master returns duplicates, see the pinning test below
        "cx4", // ModifyFromInput changes the partial material
        "cx10", // no final template, so no state is tracked
        "cx11", // the only partial rule sits in a template slot
        "cx12", // the only partial rule is a clitic rule
        "cx9b", // ModifyFromInput on an unbounded part widens the node the partial rule needs
        "cx5", // InsertSimpleContext splits the partial material
        "cx6", // an infix and a copy of a part (reduplication) rearrange the partial material
        "cx7", // a compound with the non-head before the head parts
        "cx8", // a compound copies one head part twice around the non-head
    ];

    // Grammars whose words need the deletion rules to be reapplied.
    private static readonly HashSet<string> DeletionReapplied = ["g19", "g19c"];

    private static string DataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "PartialPrune", file);

    private static IEnumerable<Trace> Flatten(Trace trace)
    {
        yield return trace;
        foreach (Trace child in trace.Children)
        {
            foreach (Trace descendant in Flatten(child))
                yield return descendant;
        }
    }

    private static IEnumerable<TestCaseData> GrammarCases()
    {
        foreach (string grammar in Grammars)
        {
            yield return new TestCaseData(grammar, 1).SetName($"{grammar}_Degree1");
            yield return new TestCaseData(grammar, 0).SetName($"{grammar}_DefaultDegree");
        }
    }

    private static Morpher CreateMorpher(
        string grammar,
        int maxDegreeOfParallelism,
        Func<IHCRule, bool>? selector = null,
        bool tracing = false
    )
    {
        Language language = XmlLanguageLoader.Load(DataPath(grammar + ".xml"));
        var morpher = new Morpher(new TraceManager { IsTracing = tracing }, language, maxDegreeOfParallelism);
        if (DeletionReapplied.Contains(grammar))
            morpher.DeletionReapplications = 1;
        if (selector != null)
            morpher.RuleSelector = selector;
        return morpher;
    }

    // Allomorphs, not AllomorphsInMorphOrder: morph order comes from shape annotations, which a truncation can drop.
    // Distinct: the number of identical analyses a parse returns varies between runs.
    private static string Signature(Morpher morpher, string word)
    {
        try
        {
            List<string> analyses = morpher
                .ParseWord(word)
                .Select(w =>
                    string.Join(
                        "+",
                        w.Allomorphs.Select(a => $"{a.Morpheme.Id}.{a.Index}").OrderBy(n => n, StringComparer.Ordinal)
                    )
                    + "|"
                    + w.Shape.ToRegexString(w.Stratum.CharacterDefinitionTable, true)
                    + "|"
                    + w.SyntacticFeatureStruct
                )
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            return analyses.Count == 0 ? "-" : string.Join(";", analyses);
        }
        catch (InvalidShapeException)
        {
            return "SKIPPED";
        }
    }

    [TestCaseSource(nameof(GrammarCases))]
    public void ParseWord_PartialRuleGrammar_KeepsEveryAnalysis(string grammar, int maxDegreeOfParallelism)
    {
        Morpher morpher = CreateMorpher(grammar, maxDegreeOfParallelism);
        Assert.That(morpher.HasPartialRules, Is.True);

        var differences = new List<string>();

        foreach (string line in File.ReadAllLines(DataPath(grammar + ".expected")))
        {
            string[] fields = line.Split('\t');
            string actual = Signature(morpher, fields[0]);
            if (actual != fields[1])
                differences.Add($"{fields[0]}: expected {fields[1]} but was {actual}");
        }
        Assert.That(differences, Is.Empty);
    }

    // Master returns each word twice in a family grammar with partial rules; the prune drops only duplicates.
    [TestCase("kamdtr")]
    [TestCase("kamdtrt")]
    public void ParseWord_FamilyAndPartialRuleGrammar_ReturnsEachAnalysisOnce(string word)
    {
        Morpher morpher = CreateMorpher("cx3", maxDegreeOfParallelism: 1);
        Assert.That(morpher.ParseWord(word).Count(), Is.EqualTo(1));
    }

    // Rule-selector calls count work. At one thread the count is deterministic, so a bound tells a prune that
    // fires from one that does not.
    [TestCase("g8", "akamdt", 130, TestName = "TemplateNoPartialWordCanReach")]
    [TestCase("g6", "loptr", 18, TestName = "ShapeHasNoRoomForPartialMaterialContiguous")]
    [TestCase("g12", "loptr", 16, TestName = "ShapeHasNoRoomForPartialMaterialExistence")]
    public void ParseWord_PartialRuleGrammar_PrunesWhatNoPartialWordCanUse(string grammar, string word, int maxCalls)
    {
        int calls = 0;
        Morpher morpher = CreateMorpher(
            grammar,
            maxDegreeOfParallelism: 1,
            selector: _ =>
            {
                calls++;
                return true;
            }
        );
        Signature(morpher, word);
        Assert.That(calls, Is.LessThanOrEqualTo(maxCalls));
    }

    // The look-ahead assumes unapplying a rule copies input nodes and adds only inserted or rebuilt ones. Weigh a
    // new output action type there before listing it here.
    [Test]
    public void MorphologicalOutputAction_Subtypes_AreTheOnesPartialRuleLookAheadKnows()
    {
        string[] subtypes = typeof(MorphologicalOutputAction)
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(MorphologicalOutputAction)))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.That(
            subtypes,
            Is.EqualTo(
                new[]
                {
                    nameof(CopyFromInput),
                    nameof(InsertSegments),
                    nameof(InsertSimpleContext),
                    nameof(ModifyFromInput),
                }
            )
        );
    }

    // A pruned candidate must show in a trace, not vanish: the final template that owes a partial rule, with the
    // partial rules as the failure object.
    [TestCase("g6", "lopqtr")]
    [TestCase("g6b", "loptr")]
    [TestCase("cx1b", "lopkotr")]
    public void ParseWord_OwedPartialDropped_IsTraced(string grammar, string word)
    {
        foreach (int degree in new[] { 1, 0 })
        {
            Morpher morpher = CreateMorpher(grammar, degree, tracing: true);
            morpher.ParseWord(word, out object root);
            Trace[] dropped = Flatten((Trace)root)
                .Where(t => t.FailureReason == FailureReason.OwedPartialCannotUnapply)
                .ToArray();
            Assert.That(dropped, Is.Not.Empty);
            foreach (Trace t in dropped)
            {
                Assert.That(t.Type, Is.EqualTo(TraceType.TemplateAnalysisOutput));
                Assert.That(t.Source, Is.InstanceOf<AffixTemplate>().With.Property("IsFinal").True);
                Assert.That(t.Output, Is.Null);
                Assert.That(t.FailureObject, Is.InstanceOf<IReadOnlyList<AffixProcessRule>>());
                Assert.That((IReadOnlyList<AffixProcessRule>)t.FailureObject, Is.Not.Empty);
            }
        }
    }

    // Nothing arms the prune without a final template, so the analysis state must not be tracked.
    [TestCase("cx10", false)]
    [TestCase("cx1b", true)]
    [TestCase("cx11", true)]
    [TestCase("cx12", true)]
    public void TracksFinalTemplateState_PartialRuleGrammar_OnlyWhenAFinalTemplateCanBePruned(
        string grammar,
        bool tracked
    )
    {
        Assert.That(CreateMorpher(grammar, 1).TracksFinalTemplateState, Is.EqualTo(tracked));
    }

    // Partial rules in a template slot or clitic rules never make a word partial in synthesis, so they do not
    // keep the final-template prune off.
    [TestCase("cx11", "lopkmootr", 15, TestName = "SlotPartialRuleDoesNotKeepPruneOff")]
    [TestCase("cx12", "lopkotr", 13, TestName = "CliticPartialRuleDoesNotKeepPruneOff")]
    public void ParseWord_OnlyNonPartialWordsReachTheTemplate_Prunes(string grammar, string word, int maxCalls)
    {
        int calls = 0;
        Morpher morpher = CreateMorpher(
            grammar,
            maxDegreeOfParallelism: 1,
            selector: _ =>
            {
                calls++;
                return true;
            }
        );
        Signature(morpher, word);
        Assert.That(calls, Is.LessThanOrEqualTo(maxCalls));
    }
}
