#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Structural (XML-static) detectors for the three <see cref="ObligationMutatorClass"/> members
/// <see cref="DataflowObligationLedger"/> declared but never checked against corpus content:
/// <see cref="ObligationMutatorClass.Blocking"/>, <see cref="ObligationMutatorClass.PosPriorityUnion"/>,
/// <see cref="ObligationMutatorClass.CompoundingNonHeadDrop"/> (<see cref="ObligationMutatorClass.Overwrite"/>
/// already has one -- <see cref="MutatingConstructs"/>, reused unmodified from
/// <see cref="InteractionChainLedger"/>). Each detector answers a NECESSARY-condition question about an
/// exercising fixture's grammar.xml -- "does this fixture even contain the construct the engine mechanism
/// needs to fire at all" -- never a sufficient/temporal-ordering proof. That is the same rigor
/// <see cref="MutatingConstructs"/> already applies to <see cref="ObligationMutatorClass.Overwrite"/>:
/// presence of the construct is a HAZARD signal, not a proof that some specific derivation actually routes
/// through it at the point where it would matter. A finding of "absent" IS definitive here (the cited
/// engine guard literally cannot fire without the construct), so <see cref="DataflowObligationLedger"/>
/// reports that as such; a finding of "present" only ever raises a cell to the same
/// <see cref="ObligationStatus.Unknown"/> Overwrite's own <c>Hazardous</c> rows already carry -- never
/// <see cref="ObligationStatus.Satisfied"/>, which stays reserved for a same-word pair witness.
/// </summary>
internal static class MutatorClassDetectors
{
    private static XDocument? TryLoadGrammar(string repositoryRoot, string fixtureId)
    {
        string path = Path.Combine(repositoryRoot, "conformance", fixtureId.Replace('/', Path.DirectorySeparatorChar), "grammar.xml");
        return File.Exists(path) ? XDocument.Load(path) : null;
    }

    /// <summary>
    /// Scans <paramref name="exercisingFixtures"/> in order and returns the first one whose grammar
    /// satisfies <paramref name="predicate"/>, or null if none does.
    /// </summary>
    public static (bool Found, string? FixtureId) ScanForAny(
        string repositoryRoot,
        IReadOnlyList<string> exercisingFixtures,
        Func<XDocument, bool> predicate
    )
    {
        foreach (string fixtureId in exercisingFixtures)
        {
            XDocument? grammar = TryLoadGrammar(repositoryRoot, fixtureId);
            if (grammar != null && predicate(grammar))
                return (true, fixtureId);
        }
        return (false, null);
    }

    /// <summary>Scans every exercising fixture and returns the largest count <paramref name="counter"/>
    /// finds, and which fixture realizes it (ties keep the first, matching the chain's own sorted
    /// exercising-fixture order).</summary>
    public static (int MaxCount, string? FixtureId) ScanForMax(
        string repositoryRoot,
        IReadOnlyList<string> exercisingFixtures,
        Func<XDocument, int> counter
    )
    {
        int best = 0;
        string? bestFixture = null;
        foreach (string fixtureId in exercisingFixtures)
        {
            XDocument? grammar = TryLoadGrammar(repositoryRoot, fixtureId);
            if (grammar == null)
                continue;
            int count = counter(grammar);
            if (count > best)
            {
                best = count;
                bestFixture = fixtureId;
            }
        }
        return (best, bestFixture);
    }

    /// <summary>
    /// <see cref="ObligationMutatorClass.Blocking"/>'s operative precondition: <c>Word.CheckBlocking</c>
    /// (Word.cs:472-497) returns false immediately unless the current root's <c>LexEntry.Family</c> is
    /// non-null, which <c>XmlLanguageLoader</c> (line 460-464) populates only when this entry's
    /// <c>family</c> IDREF names a <c>&lt;Family&gt;</c> shared by at least one OTHER
    /// <c>&lt;LexicalEntry&gt;</c>. <c>CheckBlocking</c> also requires the sibling to share this word's
    /// current <c>Stratum</c> (Word.cs:483), and <c>LexicalEntry</c> is only ever declared directly under
    /// one <c>&lt;Stratum&gt;</c> (DTD: <c>Stratum -&gt; LexicalEntries -&gt; LexicalEntry</c>), so two
    /// siblings sharing a family AND a nearest <c>&lt;Stratum&gt;</c> ancestor is exactly this
    /// precondition, minus the one fact static XML cannot resolve: whether the current word's
    /// <c>SyntacticFeatureStruct</c> actually <c>Subsumes</c> the sibling's (Word.cs:484) -- a runtime
    /// fact over resolved feature structures, not decidable from the raw attribute strings alone.
    /// </summary>
    public static bool HasEligibleFamily(XDocument grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);

        var byFamily = new Dictionary<string, List<XElement>>(StringComparer.Ordinal);
        foreach (XElement entry in grammar.Descendants("LexicalEntry"))
        {
            string? family = (string?)entry.Attribute("family");
            if (string.IsNullOrEmpty(family))
                continue;

            if (!byFamily.TryGetValue(family, out List<XElement>? entries))
            {
                entries = new List<XElement>();
                byFamily[family] = entries;
            }
            entries.Add(entry);
        }

        foreach (List<XElement> entries in byFamily.Values)
        {
            var byStratum = entries
                .Select(e => e.Ancestors("Stratum").FirstOrDefault())
                .Where(s => s != null)
                .GroupBy(s => s);
            if (byStratum.Any(g => g.Count() >= 2))
                return true;
        }

        return false;
    }

    /// <summary>
    /// <see cref="ObligationMutatorClass.PosPriorityUnion"/>'s operative precondition: the
    /// <c>PriorityUnion</c> call (SynthesisAffixProcessRule.cs:181-182,
    /// SynthesisCompoundingRule.cs:181-182) runs unconditionally on every <c>MorphologicalRule</c>/
    /// <c>CompoundingRule</c> application, but is a no-op whenever that rule's own
    /// <c>outputPartOfSpeech</c> is empty -- so the mutator only ever has an effect where the fixture
    /// contains a POS-writing rule to DO the clobbering. Counts every <c>&lt;MorphologicalRule&gt;</c>/
    /// <c>&lt;CompoundingRule&gt;</c> element with a non-empty <c>outputPartOfSpeech</c> in the fixture
    /// (not just the chain's own writer/reader rule, which this static scan cannot single out by
    /// instance identity).
    /// </summary>
    public static int CountPosWritingRuleElements(XDocument grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);

        int count = 0;
        foreach (string elementName in new[] { "MorphologicalRule", "CompoundingRule" })
        {
            foreach (XElement rule in grammar.Descendants(elementName))
            {
                if (!string.IsNullOrEmpty((string?)rule.Attribute("outputPartOfSpeech")))
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// <see cref="ObligationMutatorClass.CompoundingNonHeadDrop"/>'s entire operative precondition:
    /// <c>SynthesisCompoundingRule.ApplySubrule</c> (SynthesisCompoundingRule.cs:236) builds its output
    /// from <c>headMatch.Input.Clone()</c> alone, so the non-head's whole <c>MprFeatureSet</c> -- however
    /// it was written (<c>LexicalEntry.ruleFeatures</c>, a prior <c>MorphologicalOutput.MPRFeatures</c>
    /// write, or a prior <c>CompoundingRule.outputProdRestrictionsMprFeatures</c> write) -- is dropped in
    /// one step, unconditionally, the moment that word is consumed as a non-head. No overwrite group, no
    /// feature match, nothing else is needed for the drop itself to fire, so the presence of at least one
    /// <c>&lt;CompoundingRule&gt;</c> (DTD-mandatory &gt;=1 <c>CompoundingSubrule</c>, hence &gt;=1
    /// <c>HeadMorphologicalInput</c>/<c>NonHeadMorphologicalInput</c> pair) is the whole precondition.
    /// </summary>
    public static bool HasCompoundingRule(XDocument grammar)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        return grammar.Descendants("CompoundingRule").Any();
    }
}
