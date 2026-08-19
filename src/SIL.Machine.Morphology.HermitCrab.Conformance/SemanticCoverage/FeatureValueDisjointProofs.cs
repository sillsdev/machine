#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: two <c>PhonologicalRule</c>s in
/// a Stratum <c>phonologicalRules</c> adjacent pair whose active subrules are gated by disjoint
/// <c>Environment</c> natural classes, so at most one of them can ever match a given site regardless of
/// which one runs first, AND neither rule's output segment could ever create a NEW site for the other (it
/// is not a member of the other's resolved environment or its shared input class). Both conditions are
/// recomputed fresh, never read from a file:
/// <list type="bullet">
/// <item>(a) the union of each rule's active-subrule <c>LeftEnvironment</c>/<c>RightEnvironment</c> classes
/// does not intersect the other's;</item>
/// <item>(b) neither rule's <c>PhoneticOutput</c> segment is a member of the other's environment class OR
/// its <c>PhoneticInput</c> class -- the input half closes a feeding channel that environment-disjointness
/// alone misses (one rule's output creating a brand-new instance of the other's input class), resolved by
/// the same natural-class machinery for free, so it only ever rejects a proof that would otherwise be
/// wrongly certified.</item>
/// </list>
/// A rule with an active subrule that has NO Environment at all fires unconditionally wherever its input
/// matches; that removes the mutual-exclusion argument entirely (there is no "at most one can match a given
/// site" race to reason about), so such a pair is never modeled by this check.
/// </summary>
public static class FeatureValueDisjointProofs
{
    public const string Kind = "feature-value-disjoint";

    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        Check check = Evaluate(grammar, item);
        return check.Relation == Relation.Disjoint ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND the check
    /// recomputes <see cref="Relation.Disjoint"/> for it. A stale id, a recomputed environment overlap,
    /// and a recomputed output-feeds-the-other overlap all fail closed to false.
    /// </summary>
    public static bool Verify(XDocument grammar, string fixtureId, Proof proof)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(fixtureId);
        ArgumentNullException.ThrowIfNull(proof);
        OrderingItem? item = OrderingGenerator
            .EnumerateAdjacentPairs(grammar, fixtureId)
            .FirstOrDefault(candidate => candidate.Id == proof.ItemId);
        if (item is null)
            return false;
        return Evaluate(grammar, item).Relation == Relation.Disjoint;
    }

    private enum Relation
    {
        Disjoint,
        Overlaps,
        Undetermined,
    }

    private sealed record Check(string ItemId, Relation Relation, string Reason);

    private static Check Evaluate(XDocument grammar, OrderingItem item)
    {
        if (item.Kind != OrderingListKind.StratumPhonologicalRules)
        {
            return new Check(
                item.Id,
                Relation.Undetermined,
                $"{item.Kind} pairs are not modeled -- this check only resolves a PhonologicalRule's PhoneticInput/Environment classes"
            );
        }

        XElement? ruleA = FindElementById(grammar, item.MemberA);
        XElement? ruleB = FindElementById(grammar, item.MemberB);
        if (ruleA is null || ruleB is null)
        {
            string missing = ruleA is null ? item.MemberA : item.MemberB;
            return new Check(item.Id, Relation.Undetermined, $"'{missing}' was not found as an element with a matching id attribute");
        }
        if (ruleA.Name.LocalName != "PhonologicalRule" || ruleB.Name.LocalName != "PhonologicalRule")
        {
            return new Check(
                item.Id,
                Relation.Undetermined,
                $"<{ruleA.Name.LocalName}>/<{ruleB.Name.LocalName}> is not modeled -- only a PhonologicalRule/PhonologicalRule pair "
                    + "has PhoneticInput/Environment natural classes to compare"
            );
        }

        RuleProfile? profileA = BuildProfile(ruleA, grammar);
        if (profileA is null)
        {
            return new Check(
                item.Id,
                Relation.Undetermined,
                $"'{item.MemberA}' has no active subrule with an Environment, or its natural classes could not be resolved"
            );
        }
        RuleProfile? profileB = BuildProfile(ruleB, grammar);
        if (profileB is null)
        {
            return new Check(
                item.Id,
                Relation.Undetermined,
                $"'{item.MemberB}' has no active subrule with an Environment, or its natural classes could not be resolved"
            );
        }

        string[] sharedEnvironment = profileA
            .Environment.Intersect(profileB.Environment, StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        if (sharedEnvironment.Length > 0)
            return new Check(item.Id, Relation.Overlaps, $"environment classes share segment(s) {{{string.Join(",", sharedEnvironment)}}}");

        HashSet<string> triggerB = profileB.Environment.Union(profileB.Input, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        string[] aFeedsB = profileA.Output.Intersect(triggerB, StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        if (aFeedsB.Length > 0)
        {
            return new Check(
                item.Id,
                Relation.Overlaps,
                $"'{item.MemberA}''s output {{{string.Join(",", aFeedsB)}}} would create a new site for '{item.MemberB}'"
            );
        }

        HashSet<string> triggerA = profileA.Environment.Union(profileA.Input, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        string[] bFeedsA = profileB.Output.Intersect(triggerA, StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        if (bFeedsA.Length > 0)
        {
            return new Check(
                item.Id,
                Relation.Overlaps,
                $"'{item.MemberB}''s output {{{string.Join(",", bFeedsA)}}} would create a new site for '{item.MemberA}'"
            );
        }

        return new Check(
            item.Id,
            Relation.Disjoint,
            $"'{item.MemberA}' environment {{{string.Join(",", profileA.Environment.OrderBy(s => s, StringComparer.Ordinal))}}} and "
                + $"'{item.MemberB}' environment {{{string.Join(",", profileB.Environment.OrderBy(s => s, StringComparer.Ordinal))}}} are "
                + "disjoint, and neither rule's output could ever create a new site for the other"
        );
    }

    private sealed record RuleProfile(HashSet<string> Input, HashSet<string> Output, HashSet<string> Environment);

    // Null means: no active subrule declares any Environment (so the rule fires unconditionally and the
    // mutual-exclusion argument this kind depends on does not apply), or some referenced natural class
    // could not be fully resolved. Both fail closed to "not modeled" rather than guessing.
    private static RuleProfile? BuildProfile(XElement rule, XDocument grammar)
    {
        SegmentResolution input = ResolveSequenceSegments(rule.Element("PhoneticInput")?.Element("PhoneticSequence"), grammar);
        if (!input.IsResolved)
            return null;

        List<XElement> subrules =
            rule.Element("PhonologicalSubrules")?.Elements("PhonologicalSubrule").Where(IsActiveElement).ToList() ?? new List<XElement>();
        if (subrules.Count == 0)
            return null;

        SegmentResolution output = SegmentResolution.Ok(Array.Empty<string>());
        SegmentResolution environment = SegmentResolution.Ok(Array.Empty<string>());
        bool anyEnvironment = false;
        foreach (XElement subrule in subrules)
        {
            output = Union(output, ResolveSequenceSegments(subrule.Element("PhoneticOutput")?.Element("PhoneticSequence"), grammar));
            if (!output.IsResolved)
                return null;

            XElement? env = subrule.Element("Environment");
            if (env is null)
                continue;

            XElement? left = env.Element("LeftEnvironment")?.Element("PhoneticTemplate")?.Element("PhoneticSequence");
            XElement? right = env.Element("RightEnvironment")?.Element("PhoneticTemplate")?.Element("PhoneticSequence");
            if (left is not null || right is not null)
                anyEnvironment = true;

            environment = Union(environment, ResolveSequenceSegments(left, grammar));
            if (!environment.IsResolved)
                return null;
            environment = Union(environment, ResolveSequenceSegments(right, grammar));
            if (!environment.IsResolved)
                return null;
        }

        if (!anyEnvironment)
            return null;

        return new RuleProfile(input.Segments.ToHashSet(StringComparer.Ordinal), output.Segments.ToHashSet(StringComparer.Ordinal), environment.Segments.ToHashSet(StringComparer.Ordinal));
    }

    private readonly record struct SegmentResolution(bool IsResolved, IReadOnlySet<string> Segments)
    {
        public static SegmentResolution Ok(IEnumerable<string> segments) => new(true, segments.ToHashSet(StringComparer.Ordinal));

        public static SegmentResolution No() => new(false, new HashSet<string>());
    }

    private static SegmentResolution Union(SegmentResolution a, SegmentResolution b)
    {
        if (!a.IsResolved)
            return a;
        if (!b.IsResolved)
            return b;
        return SegmentResolution.Ok(a.Segments.Concat(b.Segments));
    }

    // Over-approximates on purpose (recursing into OptionalSegmentSequence too): the result is later used
    // ONLY to test for intersection, and a superset can only ADD a spurious intersection, never hide a
    // real one -- so it can turn a real Disjoint into a false Overlaps but never the reverse, which is the
    // safe direction given this check's one forbidden mistake is a false Disjoint.
    private static SegmentResolution ResolveSequenceSegments(XElement? sequence, XDocument grammar)
    {
        if (sequence is null)
            return SegmentResolution.Ok(Array.Empty<string>());

        SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
        foreach (XElement child in sequence.Elements())
        {
            acc = Union(acc, ResolveElementSegments(child, grammar));
            if (!acc.IsResolved)
                return acc;
        }
        return acc;
    }

    private static SegmentResolution ResolveElementSegments(XElement element, XDocument grammar)
    {
        switch (element.Name.LocalName)
        {
            case "Segment":
            {
                string? segmentId = (string?)element.Attribute("segment");
                if (segmentId is null)
                    return SegmentResolution.No();
                XElement? segmentDef = FindSegmentDefinition(grammar, segmentId);
                if (segmentDef is null)
                    return SegmentResolution.No();
                return IsActiveElement(segmentDef) ? SegmentResolution.Ok(new[] { segmentId }) : SegmentResolution.Ok(Array.Empty<string>());
            }
            case "SimpleContext":
            {
                string? naturalClassId = (string?)element.Attribute("naturalClass");
                return naturalClassId is null ? SegmentResolution.No() : ResolveNaturalClass(naturalClassId, grammar);
            }
            case "OptionalSegmentSequence":
            {
                SegmentResolution acc = SegmentResolution.Ok(Array.Empty<string>());
                foreach (XElement child in element.Elements())
                {
                    acc = Union(acc, ResolveElementSegments(child, grammar));
                    if (!acc.IsResolved)
                        return acc;
                }
                return acc;
            }
            case "BoundaryMarker":
                return SegmentResolution.Ok(Array.Empty<string>());
            default:
                return SegmentResolution.No();
        }
    }

    private static SegmentResolution ResolveNaturalClass(string naturalClassId, XDocument grammar)
    {
        XElement? segmentClass = grammar.Descendants("SegmentNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
        if (segmentClass is not null)
            return ResolveSegmentNaturalClass(segmentClass, grammar);

        XElement? featureClass = grammar.Descendants("FeatureNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
        return featureClass is not null ? ResolveFeatureNaturalClass(featureClass, grammar) : SegmentResolution.No();
    }

    private static SegmentResolution ResolveSegmentNaturalClass(XElement segmentClass, XDocument grammar)
    {
        var ids = new List<string>();
        foreach (XElement segment in segmentClass.Elements("Segment"))
        {
            string? segmentId = (string?)segment.Attribute("segment");
            if (segmentId is null)
                return SegmentResolution.No();
            XElement? segmentDef = FindSegmentDefinition(grammar, segmentId);
            if (segmentDef is null)
                return SegmentResolution.No();
            if (IsActiveElement(segmentDef))
                ids.Add(segmentId);
        }
        return SegmentResolution.Ok(ids);
    }

    private static SegmentResolution ResolveFeatureNaturalClass(XElement featureClass, XDocument grammar)
    {
        var constraints = new List<(string Feature, HashSet<string> Symbols)>();
        foreach (XElement featureValue in featureClass.Elements("FeatureValue"))
        {
            if (!IsActiveElement(featureValue))
                continue;
            if (featureValue.Elements("FeatureValue").Any())
                return SegmentResolution.No();

            string? feature = (string?)featureValue.Attribute("feature");
            string? symbolValues = (string?)featureValue.Attribute("symbolValues");
            if (feature is null || symbolValues is null)
                return SegmentResolution.No();
            constraints.Add((feature, SplitIdrefs(symbolValues).ToHashSet(StringComparer.Ordinal)));
        }

        var members = new List<string>();
        foreach (XElement segmentDef in grammar.Descendants("SegmentDefinition"))
        {
            if (!IsActiveElement(segmentDef))
                continue;
            string? segmentId = (string?)segmentDef.Attribute("id");
            if (segmentId is null)
                continue;

            bool? matches = MatchesEveryConstraint(segmentDef, constraints);
            if (matches is null)
                return SegmentResolution.No();
            if (matches.Value)
                members.Add(segmentId);
        }
        return SegmentResolution.Ok(members);
    }

    private static bool? MatchesEveryConstraint(XElement segmentDef, List<(string Feature, HashSet<string> Symbols)> constraints)
    {
        foreach ((string feature, HashSet<string> symbols) in constraints)
        {
            XElement? declared = segmentDef.Elements("FeatureValue").FirstOrDefault(fv => IsActiveElement(fv) && (string?)fv.Attribute("feature") == feature);
            if (declared is null)
                return null;

            string? declaredSymbols = (string?)declared.Attribute("symbolValues");
            HashSet<string> declaredSet =
                declaredSymbols is null ? new HashSet<string>(StringComparer.Ordinal) : SplitIdrefs(declaredSymbols).ToHashSet(StringComparer.Ordinal);
            if (!declaredSet.Overlaps(symbols))
                return false;
        }
        return true;
    }

    private static string[] SplitIdrefs(string value) => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static XElement? FindSegmentDefinition(XDocument grammar, string id) =>
        grammar.Descendants("SegmentDefinition").FirstOrDefault(e => (string?)e.Attribute("id") == id);

    private static XElement? FindElementById(XDocument grammar, string id) =>
        grammar.Descendants().FirstOrDefault(e => (string?)e.Attribute("id") == id);

    private static bool IsActiveElement(XElement element) => (string?)element.Attribute("isActive") != "no";
}
