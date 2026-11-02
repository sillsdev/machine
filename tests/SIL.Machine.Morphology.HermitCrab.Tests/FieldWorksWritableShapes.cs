using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// The check behind <c>conformance/fieldworks-writable-shapes.tsv</c>, the census of what HCLoader can write.
/// Every element and attribute a flagged grammar puts on a rule, template, stratum, stem name, entry, allomorph
/// or phonological rule is looked up there by the kind it sits on. A member the table calls <c>never</c>, or
/// does not list, is an offence. The conditions a table row cannot say (an empty stem-name region, where an
/// environment may sit, which MPR feature group a feature belongs to, a cap on an exocompound) are checked here.
/// </summary>
internal static class FieldWorksWritableShapes
{
    private static readonly string[] EnumAttributes =
    [
        "isActive",
        "partial",
        "blockable",
        "final",
        "optional",
        "morphologicalRuleOrder",
        "cyclicity",
        "phonologicalRuleOrder",
        "multipleApplication",
        "multipleApplicationOrder",
        "isBound",
        "redupMorphType",
        "type",
        "adjacency",
    ];

    // LoadLanguage (HCLoader.cs 181-205) builds exactly these three groups, all with the default output.
    private static readonly Dictionary<string, string> LoaderGroups = new(StringComparer.Ordinal)
    {
        ["inflClasses"] = "any",
        ["exceptionFeatures"] = "all",
        ["lexEntryInflTypes"] = "all",
    };

    private static readonly string[] Verdicts = ["writes", "metadata", "never", "omitted", "inert", "shape"];

    public static string TablePath(string suiteRoot) => Path.Combine(suiteRoot, "fieldworks-writable-shapes.tsv");

    public static Dictionary<(string Kind, string Member), string> LoadTable(string path)
    {
        var table = new Dictionary<(string, string), string>();
        string[] lines = File.ReadAllLines(path);
        Assert.That(lines[0], Is.EqualTo("kind\tmember\tverdict\tloader\tnote"));
        foreach (string line in lines.Skip(1).Where(l => l.Length > 0))
        {
            string[] cells = line.Split('\t');
            Assert.That(cells, Has.Length.EqualTo(5), line);
            Assert.That(Verdicts, Does.Contain(cells[2]), line);
            Assert.That(table.TryAdd((cells[0], cells[1]), cells[2]), Is.True, $"duplicate row: {line}");
        }
        return table;
    }

    public static IEnumerable<(string Kind, string Member, string Verdict)> Rows(
        Dictionary<(string Kind, string Member), string> table
    ) => table.Select(pair => (pair.Key.Kind, pair.Key.Member, pair.Value));

    public static List<string> Check(XDocument doc, Dictionary<(string Kind, string Member), string> table)
    {
        var offences = new List<string>();
        var slotRuleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement slot in doc.Descendants("Slot"))
            slotRuleIds.UnionWith(Split((string?)slot.Attribute("morphologicalRules")));

        var cliticRuleIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement stratum in doc.Descendants("Stratum").Where(s => (string?)s.Element("Name") == "Clitics"))
            cliticRuleIds.UnionWith(Split((string?)stratum.Attribute("morphologicalRules")));

        foreach (XElement element in doc.Descendants())
        {
            string? kind = KindOf(element, slotRuleIds, cliticRuleIds);
            if (kind is null)
                continue;
            if (kind == "realizational-rule")
            {
                offences.Add($"{kind} {Label(element)} is never written by HCLoader (HCLoader.cs 1027)");
                continue;
            }

            foreach ((string specific, string generic) in Members(element))
            {
                if (!table.TryGetValue((kind, specific), out string? verdict))
                    table.TryGetValue((kind, generic), out verdict);
                if (verdict is null)
                    offences.Add($"{kind} {Label(element)} uses {specific}, which the census table does not list");
                else if (verdict == "never")
                    offences.Add($"{kind} {Label(element)} uses {specific}, which HCLoader never writes");
            }
        }

        if (doc.Descendants("FootFeatures").Any())
            offences.Add("the language defines FootFeatures, which HCLoader never writes");

        CheckStemNameRegions(doc, offences);
        CheckEnvironmentShapes(doc, offences);
        CheckSubruleHeadFeatureShapes(doc, offences);
        CheckExocompoundCaps(doc, offences);
        CheckMprFeatureGroups(doc, slotRuleIds, offences);
        return offences;
    }

    private static IEnumerable<(string Specific, string Generic)> Members(XElement element)
    {
        foreach (XAttribute attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
        {
            string name = attribute.Name.LocalName;
            yield return EnumAttributes.Contains(name)
                ? ($"@{name}={attribute.Value}", $"@{name}")
                : ($"@{name}", $"@{name}");
        }
        foreach (XElement child in element.Elements())
            yield return (child.Name.LocalName, child.Name.LocalName);
    }

    private static string Label(XElement element)
    {
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            string? label = (string?)current.Attribute("id") ?? (string?)current.Element("Name");
            if (label is not null)
                return label;
        }
        return "?";
    }

    private static string? KindOf(XElement element, HashSet<string> slotRuleIds, HashSet<string> cliticRuleIds)
    {
        switch (element.Name.LocalName)
        {
            case "MorphologicalRule":
                return AffixRuleKind(element, slotRuleIds, cliticRuleIds);
            case "CompoundingRule":
                return "compounding-rule";
            case "RealizationalRule":
                return "realizational-rule";
            case "MorphologicalSubrule":
                return AffixRuleKindOf(element, slotRuleIds, cliticRuleIds) + "/subrule";
            case "MorphologicalInput":
                return AffixRuleKindOf(element, slotRuleIds, cliticRuleIds) + "/input";
            case "MorphologicalOutput":
                return element.Parent?.Name.LocalName == "CompoundingSubrule"
                    ? "compounding-rule/output"
                    : AffixRuleKindOf(element, slotRuleIds, cliticRuleIds) + "/output";
            case "CompoundingSubrule":
                return "compounding-rule/subrule";
            case "HeadMorphologicalInput":
                return "compounding-rule/head-input";
            case "NonHeadMorphologicalInput":
                return "compounding-rule/nonhead-input";
            case "AffixTemplate":
                return "template";
            case "Slot":
                return "slot";
            case "Stratum":
                return "stratum";
            case "StemName":
                return "stem-name";
            case "Region":
                return "stem-region";
            case "LexicalEntry":
                return "lexical-entry";
            case "Allomorph":
                return "root-allomorph";
            case "PhonologicalRule":
                return "phonological-rule";
            case "PhonologicalSubrule":
                return "phonological-subrule";
            case "MetathesisRule":
                return "metathesis-rule";
            case "MorphemeCoOccurrenceRule":
            case "AllomorphCoOccurrenceRule":
                return "cooccurrence-rule";
            default:
                return null;
        }
    }

    private static string AffixRuleKindOf(XElement inner, HashSet<string> slotRuleIds, HashSet<string> cliticRuleIds)
    {
        XElement? rule = inner.Ancestors("MorphologicalRule").FirstOrDefault();
        return rule is null ? "unknown-rule" : AffixRuleKind(rule, slotRuleIds, cliticRuleIds);
    }

    // A rule in a slot is inflectional, a rule of the Clitics stratum is a clitic rule, a partial rule outside
    // a slot is unslotted, and the rest are derivational (HCLoader.cs 926-1093).
    private static string AffixRuleKind(XElement rule, HashSet<string> slotRuleIds, HashSet<string> cliticRuleIds)
    {
        string id = (string?)rule.Attribute("id") ?? "";
        if (slotRuleIds.Contains(id))
            return "slot-rule";
        if (cliticRuleIds.Contains(id))
            return "clitic-rule";
        return (string?)rule.Attribute("partial") == "true" ? "unslotted-rule" : "derivational-rule";
    }

    // HCLoader keeps a stem name only when it has a non-empty region (HCLoader.cs 223), so an empty region is a
    // grammar FieldWorks cannot write: it would leave no stem name at all.
    private static void CheckStemNameRegions(XDocument doc, List<string> offences)
    {
        foreach (XElement region in doc.Descendants("Region"))
        {
            XElement? head = region.Element("AssignedHeadFeatures");
            if (head is null || !head.Elements("FeatureValue").Any())
                offences.Add($"stem name {Label(region)} has an empty region, which HCLoader skips (HCLoader.cs 223)");
        }
    }

    // HCLoader writes an environment on a lone prefix (left), a lone suffix (right) and a circumfix (both).
    // An infix, a reduplication and an affix process add none (HCLoader.cs 1371, 1576-1654).
    private static void CheckEnvironmentShapes(XDocument doc, List<string> offences)
    {
        foreach (XElement subrule in doc.Descendants("MorphologicalSubrule"))
        {
            XElement? required = subrule.Element("RequiredEnvironments");
            if (required is null)
                continue;

            string[] output = (subrule.Element("MorphologicalOutput")?.Elements() ?? [])
                .Select(e => e.Name.LocalName)
                .ToArray();
            bool left = required.Descendants("LeftEnvironment").Any();
            bool right = required.Descendants("RightEnvironment").Any();
            string id = (string?)subrule.Attribute("id") ?? "?";
            if (output.SequenceEqual(["InsertSegments", "CopyFromInput"]))
            {
                if (right)
                {
                    offences.Add(
                        $"subrule {id} is a lone prefix with a right environment; HCLoader writes only a left one"
                    );
                }
            }
            else if (output.SequenceEqual(["CopyFromInput", "InsertSegments"]))
            {
                if (left)
                {
                    offences.Add(
                        $"subrule {id} is a lone suffix with a left environment; HCLoader writes only a right one"
                    );
                }
            }
            else if (!output.SequenceEqual(["InsertSegments", "CopyFromInput", "InsertSegments"]))
            {
                offences.Add(
                    $"subrule {id} has a RequiredEnvironments on an output of {string.Join(", ", output)}; "
                        + "HCLoader writes an environment only for a lone prefix, a lone suffix or a circumfix"
                );
            }
        }
    }

    // HCLoader sets a subrule's head features only on a lone affix form, not a circumfix (1100-1127) or an affix
    // process (ModifyFromInput, 1143, 1458-1466).
    private static void CheckSubruleHeadFeatureShapes(XDocument doc, List<string> offences)
    {
        foreach (XElement subrule in doc.Descendants("MorphologicalSubrule"))
        {
            if (subrule.Element("RequiredHeadFeatures") is null)
                continue;

            string[] output = (subrule.Element("MorphologicalOutput")?.Elements() ?? [])
                .Select(e => e.Name.LocalName)
                .ToArray();
            string id = (string?)subrule.Attribute("id") ?? "?";
            if (output.SequenceEqual(["InsertSegments", "CopyFromInput", "InsertSegments"]))
            {
                offences.Add(
                    $"subrule {id} is a circumfix with RequiredHeadFeatures; HCLoader writes it only on a lone affix form"
                );
            }
            else if (output.Contains("ModifyFromInput"))
            {
                offences.Add(
                    $"subrule {id} is an affix process with RequiredHeadFeatures; HCLoader writes it only on a lone affix form"
                );
            }
        }
    }

    // An exocompound becomes two mirror rules with the same name and no cap (HCLoader.cs 1971-2050).
    // Only an endocompound reads the cap, from the parser parameters (1943-1945).
    private static void CheckExocompoundCaps(XDocument doc, List<string> offences)
    {
        XElement[] compounds = doc.Descendants("CompoundingRule").ToArray();
        foreach (XElement rule in compounds)
        {
            string? cap = (string?)rule.Attribute("multipleApplication");
            if (cap is null or "1")
                continue;
            if (compounds.Any(other => !ReferenceEquals(other, rule) && IsMirror(rule, other)))
            {
                offences.Add(
                    $"compounding rule {Label(rule)} is half of an exocompound, which HCLoader never gives a cap; "
                        + $"it has multipleApplication=\"{cap}\""
                );
            }
        }
    }

    private static bool IsMirror(XElement a, XElement b) =>
        (string?)a.Element("Name") is not null
        && (string?)a.Element("Name") == (string?)b.Element("Name")
        && ((string?)a.Attribute("headPartsOfSpeech") ?? "") == ((string?)b.Attribute("nonHeadPartsOfSpeech") ?? "")
        && ((string?)a.Attribute("nonHeadPartsOfSpeech") ?? "") == ((string?)b.Attribute("headPartsOfSpeech") ?? "")
        && (string?)a.Attribute("outputPartOfSpeech") == (string?)b.Attribute("outputPartOfSpeech");

    // HCLoader puts each MPR feature in inflClasses, exceptionFeatures or lexEntryInflTypes (181-205).
    // A slot rule excludes only inflection types (1766); an entry has one inflection class (745-751).
    private static void CheckMprFeatureGroups(XDocument doc, HashSet<string> slotRuleIds, List<string> offences)
    {
        XElement? features = doc.Descendants("MorphologicalPhonologicalRuleFeatures").FirstOrDefault();
        if (features is null)
            return;

        var groupOf = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (XElement group in features.Elements("MorphologicalPhonologicalRuleFeatureGroup"))
        {
            string name = (string?)group.Element("Name") ?? "?";
            string matchType = (string?)group.Attribute("matchType") ?? "any";
            string outputType = (string?)group.Attribute("outputType") ?? "overwrite";
            if (!LoaderGroups.TryGetValue(name, out string? expected))
                offences.Add($"MPR feature group {name} is not one of the three groups HCLoader builds");
            else if (expected != matchType)
                offences.Add($"MPR feature group {name} has matchType=\"{matchType}\"; HCLoader builds \"{expected}\"");
            if (outputType != "overwrite")
                offences.Add($"MPR feature group {name} has outputType=\"{outputType}\", which HCLoader never sets");
            foreach (string feature in Split((string?)group.Attribute("features")))
                groupOf[feature] = name;
        }

        foreach (XElement feature in features.Elements("MorphologicalPhonologicalRuleFeature"))
        {
            string id = (string?)feature.Attribute("id") ?? "?";
            if (!groupOf.ContainsKey(id))
            {
                offences.Add(
                    $"MPR feature {id} is in no group; HCLoader puts every MPR feature in one of three groups"
                );
            }
        }

        string GroupOf(string feature) => groupOf.TryGetValue(feature, out string? g) ? g : "";

        foreach (XElement rule in doc.Descendants("MorphologicalRule"))
        {
            string id = (string?)rule.Attribute("id") ?? "?";
            bool inSlot = slotRuleIds.Contains(id);
            var requiredLists = new HashSet<string>(StringComparer.Ordinal);
            var outputLists = new HashSet<string>(StringComparer.Ordinal);
            foreach (XElement subrule in rule.Descendants("MorphologicalSubrule"))
            {
                string[] required = Split(
                    (string?)subrule.Element("MorphologicalInput")?.Attribute("requiredMPRFeatures")
                );
                string[] excluded = Split(
                    (string?)subrule.Element("MorphologicalInput")?.Attribute("excludedMPRFeatures")
                );
                string[] output = Split((string?)subrule.Element("MorphologicalOutput")?.Attribute("MPRFeatures"));
                requiredLists.Add(string.Join(" ", required.OrderBy(f => f, StringComparer.Ordinal)));
                outputLists.Add(string.Join(" ", output.OrderBy(f => f, StringComparer.Ordinal)));

                foreach (string feature in excluded.Where(f => !inSlot || GroupOf(f) != "lexEntryInflTypes"))
                {
                    offences.Add(
                        $"rule {id} excludes MPR feature {feature}; HCLoader excludes only the "
                            + "inflection-type features of a slot (HCLoader.cs 1766)"
                    );
                }
                foreach (string feature in required.Concat(output).Where(f => GroupOf(f) == "lexEntryInflTypes"))
                {
                    offences.Add(
                        $"rule {id} requires or writes inflection-type MPR feature {feature}; HCLoader never does"
                    );
                }
                if (output.Count(f => GroupOf(f) == "inflClasses") > 1)
                    offences.Add($"rule {id} writes more than one inflection class; a derivational affix writes one");
            }

            bool derivational = !inSlot && (string?)rule.Attribute("partial") != "true";
            if (derivational && (requiredLists.Count > 1 || outputLists.Count > 1))
            {
                offences.Add(
                    $"derivational rule {id} has different MPR lists on its subrules; "
                        + "HCLoader gives every allomorph the same list (HCLoader.cs 1015-1018)"
                );
            }
        }

        foreach (XElement entry in doc.Descendants("LexicalEntry"))
        {
            if (Split((string?)entry.Attribute("ruleFeatures")).Count(f => GroupOf(f) == "inflClasses") > 1)
                offences.Add($"entry {Label(entry)} has more than one inflection class");
        }

        foreach (XElement rule in doc.Descendants("CompoundingRule"))
        {
            foreach (
                XAttribute attribute in rule.Attributes()
                    .Where(a => a.Name.LocalName.EndsWith("ProdRestrictionsMprFeatures", StringComparison.Ordinal))
            )
            {
                foreach (string feature in Split(attribute.Value).Where(f => GroupOf(f) != "exceptionFeatures"))
                {
                    offences.Add(
                        $"compounding rule {Label(rule)} lists {feature} as a production restriction, "
                            + "which is not an exceptionFeatures feature"
                    );
                }
            }
            foreach (XElement output in rule.Descendants("MorphologicalOutput"))
            {
                string[] written = Split((string?)output.Attribute("MPRFeatures"));
                if (written.Length > 1 || written.Any(f => GroupOf(f) != "inflClasses"))
                {
                    offences.Add(
                        $"compounding rule {Label(rule)} writes {string.Join(" ", written)}; "
                            + "HCLoader writes one inflection class (HCLoader.cs 1950)"
                    );
                }
            }
        }
    }

    private static string[] Split(string? ids) =>
        string.IsNullOrWhiteSpace(ids) ? [] : ids.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
