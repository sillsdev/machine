#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Builds and re-verifies a <see cref="Proof"/> of kind <see cref="Kind"/>: one member of a Stratum
/// <c>phonologicalRules</c> adjacent pair is a <c>PhonologicalRule</c> that can never fire in any
/// derivation, so its position in the rule list is a no-op regardless of which sibling it sits next to.
/// A rule can never fire when either its shared <c>PhoneticInput</c>, or every one of its active
/// <c>PhonologicalSubrule</c>s' <c>LeftEnvironment</c>/<c>RightEnvironment</c>, requires a natural class
/// that resolves to zero active segments -- counting only active <c>FeatureValue</c>/<c>SegmentDefinition</c>
/// declarations, mirroring <c>XmlLanguageLoader</c>'s own filtering. Only a DIRECT <c>SimpleContext</c> child
/// of a <c>PhoneticSequence</c> is checked; one nested inside an <c>OptionalSegmentSequence</c> is never
/// required (the option can simply not match), so an empty class there does not make the sequence
/// unmatchable and is deliberately ignored rather than risking a false "can never fire".
/// </summary>
public static class NeverFiresProofs
{
    public const string Kind = "never-fires";

    /// <summary>Builds a <see cref="Kind"/> proof for <paramref name="item"/>, or null when neither
    /// member is provably dead.</summary>
    public static Proof? TryBuild(XDocument grammar, OrderingItem item)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(item);
        NeverFiresCheck check = Check(grammar, item);
        return check.CertifiesDead ? new Proof(item.Id, Kind, check.Reason) : null;
    }

    /// <summary>
    /// Re-verifies <paramref name="proof"/>: true only when <paramref name="fixtureId"/>'s freshly
    /// generated adjacent pairs still contain an item with <paramref name="proof"/>'s id AND at least one
    /// current member still resolves to a permanently dead rule. A stale id, a reactivated
    /// <c>FeatureValue</c>, and a newly-added qualifying segment all fail closed to false.
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
        return Check(grammar, item).CertifiesDead;
    }

    private sealed record NeverFiresCheck(string ItemId, bool CertifiesDead, string Reason);

    private static NeverFiresCheck Check(XDocument grammar, OrderingItem item)
    {
        if (item.Kind != OrderingListKind.StratumPhonologicalRules)
        {
            return new NeverFiresCheck(
                item.Id,
                false,
                $"{item.Kind} pairs are not modeled -- this check only resolves a PhonologicalRule's "
                    + "PhoneticInput/Environment natural classes"
            );
        }

        (bool deadA, string reasonA) = RuleNeverFires(grammar, item.MemberA);
        if (deadA)
            return new NeverFiresCheck(item.Id, true, $"'{item.MemberA}' can never fire: {reasonA}");

        (bool deadB, string reasonB) = RuleNeverFires(grammar, item.MemberB);
        if (deadB)
            return new NeverFiresCheck(item.Id, true, $"'{item.MemberB}' can never fire: {reasonB}");

        return new NeverFiresCheck(
            item.Id,
            false,
            $"neither '{item.MemberA}' nor '{item.MemberB}' resolves to a PhoneticInput or active-subrule "
                + "Environment naming a natural class with zero active qualifying segments"
        );
    }

    // A rule never fires when its shared input can never match, or every one of its active subrules is
    // individually dead (an environment side names an empty class). A rule with an active subrule that
    // has NO Environment at all fires unconditionally wherever the input matches, so it is never dead by
    // this mechanism.
    private static (bool IsDead, string Reason) RuleNeverFires(XDocument grammar, string ruleId)
    {
        XElement? rule = FindElementById(grammar, ruleId);
        if (rule is null || rule.Name.LocalName != "PhonologicalRule")
            return (false, $"'{ruleId}' is not a <PhonologicalRule> in the current document");

        XElement? inputSequence = rule.Element("PhoneticInput")?.Element("PhoneticSequence");
        if (TryFindEmptyDirectClass(inputSequence, grammar, out string? inputReason))
            return (true, $"PhoneticInput requires {inputReason}, which resolves to zero active segments -- no site can ever match");

        List<XElement> subrules =
            rule.Element("PhonologicalSubrules")?.Elements("PhonologicalSubrule").Where(IsActiveElement).ToList() ?? new List<XElement>();
        if (subrules.Count == 0)
            return (false, "has no active PhonologicalSubrule -- not modeled by this check");

        var deadReasons = new List<string>();
        foreach (XElement subrule in subrules)
        {
            XElement? environment = subrule.Element("Environment");
            if (environment is null)
                return (false, "an active subrule has no Environment at all, so it fires unconditionally wherever the input matches");

            XElement? left = environment.Element("LeftEnvironment")?.Element("PhoneticTemplate")?.Element("PhoneticSequence");
            if (TryFindEmptyDirectClass(left, grammar, out string? leftReason))
            {
                deadReasons.Add($"LeftEnvironment requires {leftReason}");
                continue;
            }

            XElement? right = environment.Element("RightEnvironment")?.Element("PhoneticTemplate")?.Element("PhoneticSequence");
            if (TryFindEmptyDirectClass(right, grammar, out string? rightReason))
            {
                deadReasons.Add($"RightEnvironment requires {rightReason}");
                continue;
            }

            return (false, "an active subrule's Environment does not name a natural class with zero active segments on either side");
        }

        return (true, string.Join("; ", deadReasons) + ", each of which resolves to zero active segments");
    }

    /// <summary>
    /// True when a DIRECT (non-optional) <c>SimpleContext</c> child of <paramref name="sequence"/> names a
    /// natural class that fully resolves to zero active segments. Never descends into
    /// <c>OptionalSegmentSequence</c> -- an empty class there is never required, so it cannot make the
    /// sequence itself unmatchable. An absent <paramref name="sequence"/> (e.g. an epenthesis rule's empty
    /// input) is never empty-by-class -- it is unconditioned, not impossible.
    /// </summary>
    private static bool TryFindEmptyDirectClass(XElement? sequence, XDocument grammar, out string? reason)
    {
        reason = null;
        if (sequence is null)
            return false;

        foreach (XElement child in sequence.Elements())
        {
            if (child.Name.LocalName != "SimpleContext")
                continue;

            string? naturalClassId = (string?)child.Attribute("naturalClass");
            if (naturalClassId is null)
                continue;

            ClassResolution resolution = ResolveNaturalClass(naturalClassId, grammar);
            if (resolution.IsResolved && resolution.Members.Count == 0)
            {
                reason = $"natural class '{naturalClassId}'";
                return true;
            }
        }
        return false;
    }

    private readonly record struct ClassResolution(bool IsResolved, IReadOnlySet<string> Members)
    {
        public static ClassResolution Ok(IEnumerable<string> members) => new(true, members.ToHashSet(StringComparer.Ordinal));

        public static ClassResolution No() => new(false, new HashSet<string>());
    }

    private static ClassResolution ResolveNaturalClass(string naturalClassId, XDocument grammar)
    {
        XElement? segmentClass = grammar.Descendants("SegmentNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
        if (segmentClass is not null)
            return ResolveSegmentNaturalClass(segmentClass, grammar);

        XElement? featureClass = grammar.Descendants("FeatureNaturalClass").FirstOrDefault(e => (string?)e.Attribute("id") == naturalClassId);
        return featureClass is not null ? ResolveFeatureNaturalClass(featureClass, grammar) : ClassResolution.No();
    }

    private static ClassResolution ResolveSegmentNaturalClass(XElement segmentClass, XDocument grammar)
    {
        var ids = new List<string>();
        foreach (XElement segment in segmentClass.Elements("Segment"))
        {
            string? segmentId = (string?)segment.Attribute("segment");
            if (segmentId is null)
                return ClassResolution.No();
            XElement? segmentDef = FindSegmentDefinition(grammar, segmentId);
            if (segmentDef is null)
                return ClassResolution.No();
            if (IsActiveElement(segmentDef))
                ids.Add(segmentId);
        }
        return ClassResolution.Ok(ids);
    }

    // Nested FeatureValue (ComplexFeature shape) and a missing feature/symbolValues are refused rather
    // than guessed at; a SegmentDefinition that does not declare a value for a constrained feature is
    // unresolved too, exactly matching OrderingGenerator's own FeatureNaturalClass resolution.
    private static ClassResolution ResolveFeatureNaturalClass(XElement featureClass, XDocument grammar)
    {
        var constraints = new List<(string Feature, HashSet<string> Symbols)>();
        foreach (XElement featureValue in featureClass.Elements("FeatureValue"))
        {
            if (!IsActiveElement(featureValue))
                continue;
            if (featureValue.Elements("FeatureValue").Any())
                return ClassResolution.No();

            string? feature = (string?)featureValue.Attribute("feature");
            string? symbolValues = (string?)featureValue.Attribute("symbolValues");
            if (feature is null || symbolValues is null)
                return ClassResolution.No();
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
                return ClassResolution.No();
            if (matches.Value)
                members.Add(segmentId);
        }
        return ClassResolution.Ok(members);
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
