using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// A fixture flagged <c>fieldworks_producible: true</c> must not use a grammar shape HCLoader never
/// writes. The flag is authored, so these checks can only prove it wrong. They
/// cover the shapes sillsdev/machine#612 found in flagged fixtures: a slot rule with an
/// <c>outputPartOfSpeech</c>, a <c>linear</c> stratum that holds morphological rules or affix
/// templates, a partial stem entry with a part of speech, and a partial rule that sits in a slot or has
/// an <c>outputPartOfSpeech</c>.
/// </summary>
[TestFixture]
public sealed class FieldWorksProducibleShapeTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    // conformance-stress exists only on the branches that carry it.
    private static List<Fixture> DiscoverProducible()
    {
        string root = RepositoryRoot();
        var fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        fixtures.AddRange(Fixture.DiscoverAll(Path.Combine(root, "conformance-stress")));
        return fixtures.Where(fixture => fixture.Words.FieldworksProducible == true).ToList();
    }

    [Test]
    public void FlaggedFixturesUseNoShapeHcLoaderCannotWrite()
    {
        List<Fixture> fixtures = DiscoverProducible();
        Assert.That(fixtures, Has.Count.GreaterThanOrEqualTo(25));

        string[] offences = fixtures
            .SelectMany(fixture => FindUnproducibleShapes(fixture.GrammarPath).Select(o => $"{fixture.Id}: {o}"))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            offences,
            Is.Empty,
            "these fixtures are flagged fieldworks_producible but use a shape HCLoader never writes; "
                + "rewrite the grammar and re-run the oracle, or delete the fixture:\n  "
                + string.Join("\n  ", offences)
        );
    }

    [Test]
    public void TheCheckFindsASlotRuleWithAnOutputPartOfSpeech()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot" outputPartOfSpeech="posV" />
                <MorphologicalRule id="mrLoose" outputPartOfSpeech="posV" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(found, Is.EqualTo(new[] { "slot rule mrSlot has an outputPartOfSpeech" }));
    }

    [Test]
    public void TheCheckFindsALinearStratumThatHoldsAMorphologicalRuleOrATemplate()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRules="mrA">
              <MorphologicalRuleDefinitions><MorphologicalRule id="mrA" /></MorphologicalRuleDefinitions>
            </Stratum>
            <Stratum morphologicalRuleOrder="linear" morphologicalRules="mrB">
              <MorphologicalRuleDefinitions><CompoundingRule id="mrB" /></MorphologicalRuleDefinitions>
            </Stratum>
            <Stratum morphologicalRuleOrder="linear">
              <MorphologicalRuleDefinitions><MorphologicalRule id="mrSlotOnly" /></MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlotOnly" /></AffixTemplate></AffixTemplates>
            </Stratum>
            <Stratum morphologicalRuleOrder="unordered">
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlotOnly" /></AffixTemplate></AffixTemplates>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "stratum #1 is linear (the DTD default) but holds morphological rules mrA",
                    "stratum #2 is linear but holds morphological rules mrB",
                    "stratum #3 is linear but holds 1 affix template(s)",
                }
            )
        );
    }

    [Test]
    public void TheCheckAllowsLinearStrataThatHoldNoActiveRuleOrTemplate()
    {
        string[] found = Scan(
            """
            <Stratum>
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrInactive" isActive="no" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates>
                <AffixTemplate isActive="no"><Slot morphologicalRules="mrInactive" /></AffixTemplate>
              </AffixTemplates>
            </Stratum>
            <Stratum morphologicalRules="mrInactive">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrInactive" isActive="no" />
              </MorphologicalRuleDefinitions>
            </Stratum>
            """
        );

        Assert.That(found, Is.Empty);
    }

    [Test]
    public void TheCheckFindsPartialShapesHcLoaderCannotWrite()
    {
        string[] found = Scan(
            """
            <Stratum morphologicalRuleOrder="unordered" morphologicalRules="mrLoose">
              <MorphologicalRuleDefinitions>
                <MorphologicalRule id="mrSlot" partial="true" />
                <MorphologicalRule id="mrLoose" partial="true" outputPartOfSpeech="posP" />
                <MorphologicalRule id="mrOk" partial="true" />
              </MorphologicalRuleDefinitions>
              <AffixTemplates><AffixTemplate><Slot morphologicalRules="mrSlot" /></AffixTemplate></AffixTemplates>
              <LexicalEntries>
                <LexicalEntry id="eBad" partOfSpeech="posN" partial="true" />
                <LexicalEntry id="eOk" partial="true" />
              </LexicalEntries>
            </Stratum>
            """
        );

        Assert.That(
            found,
            Is.EqualTo(
                new[]
                {
                    "partial rule mrLoose has an outputPartOfSpeech",
                    "partial rule mrSlot is in a slot",
                    "partial stem entry eBad has a partOfSpeech",
                }
            )
        );
    }

    private static string[] Scan(string strata)
    {
        string path = Path.Combine(Path.GetTempPath(), $"hc-shape-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, $"<HermitCrabInput><Language><Strata>{strata}</Strata></Language></HermitCrabInput>");
        try
        {
            return FindUnproducibleShapes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static readonly string[] RuleElements = ["MorphologicalRule", "RealizationalRule", "CompoundingRule"];

    private static string[] FindUnproducibleShapes(string grammarPath)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        var offences = new List<string>();
        var slotRuleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement slot in doc.Descendants("Slot"))
            slotRuleIds.UnionWith(Split((string?)slot.Attribute("morphologicalRules")));

        var rulesById = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (XElement rule in doc.Descendants().Where(e => RuleElements.Contains(e.Name.LocalName)))
        {
            string? id = (string?)rule.Attribute("id");
            if (id is not null)
                rulesById[id] = rule;
        }

        foreach (string id in slotRuleIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (rulesById.TryGetValue(id, out XElement? rule) && rule.Attribute("outputPartOfSpeech") is not null)
                offences.Add($"slot rule {id} has an outputPartOfSpeech");
        }

        foreach (KeyValuePair<string, XElement> pair in rulesById.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if ((string?)pair.Value.Attribute("partial") != "true")
                continue;
            if (pair.Value.Attribute("outputPartOfSpeech") is not null)
                offences.Add($"partial rule {pair.Key} has an outputPartOfSpeech");
            if (slotRuleIds.Contains(pair.Key))
                offences.Add($"partial rule {pair.Key} is in a slot");
        }

        foreach (XElement entry in doc.Descendants("LexicalEntry"))
        {
            if ((string?)entry.Attribute("partial") == "true" && entry.Attribute("partOfSpeech") is not null)
                offences.Add($"partial stem entry {(string?)entry.Attribute("id")} has a partOfSpeech");
        }

        // HCLoader builds every stratum that holds rules or templates as Unordered, and the DTD default is
        // linear. The engine reads the order for the listed rules and when it applies templates.
        int number = 0;
        foreach (XElement stratum in doc.Descendants("Stratum"))
        {
            number++;
            string? order = (string?)stratum.Attribute("morphologicalRuleOrder");
            if (order == "unordered")
                continue;

            var active = new HashSet<string>(StringComparer.Ordinal);
            foreach (
                XElement rule in stratum
                    .Elements("MorphologicalRuleDefinitions")
                    .Elements()
                    .Where(e => RuleElements.Contains(e.Name.LocalName))
            )
            {
                string? id = (string?)rule.Attribute("id");
                if (id is not null && (string?)rule.Attribute("isActive") != "no")
                    active.Add(id);
            }

            string how = order is null ? "linear (the DTD default)" : "linear";
            string[] held = Split((string?)stratum.Attribute("morphologicalRules")).Where(active.Contains).ToArray();
            if (held.Length > 0)
                offences.Add($"stratum #{number} is {how} but holds morphological rules {string.Join(" ", held)}");

            int templates = stratum
                .Elements("AffixTemplates")
                .Elements("AffixTemplate")
                .Count(template => (string?)template.Attribute("isActive") != "no");
            if (templates > 0)
                offences.Add($"stratum #{number} is {how} but holds {templates} affix template(s)");
        }

        return offences.ToArray();
    }

    private static string[] Split(string? ids) =>
        string.IsNullOrWhiteSpace(ids) ? [] : ids.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
