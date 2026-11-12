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
    ];

    // Grammars whose words need the deletion rules to be reapplied.
    private static readonly HashSet<string> DeletionReapplied = ["g19", "g19c"];

    private static string DataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "PartialPrune", file);

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
        Func<IHCRule, bool>? selector = null
    )
    {
        Language language = XmlLanguageLoader.Load(DataPath(grammar + ".xml"));
        var morpher = new Morpher(new TraceManager(), language, maxDegreeOfParallelism);
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
}
