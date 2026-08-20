#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// How a surface was neutralized, so the counterfactual's delta can name what changed.
/// </summary>
public sealed record GrammarMutation(string SurfaceId, string Kind, string Detail, XDocument Mutated);

/// <summary>
/// Produces a grammar with one generated surface neutralized, so a counterfactual run can show whether
/// that surface influences any result. A mutation that leaves the document semantically identical is a
/// bug here, not evidence, which is why every mutation reports what it changed and refuses to return a
/// document it did not actually modify.
/// </summary>
public static class GrammarMutator
{
    public const string DeletedElements = "deleted-elements";
    public const string RewroteAttribute = "rewrote-attribute";
    public const string RemovedAttribute = "removed-attribute";
    public const string EmptiedChildren = "emptied-children";

    /// <summary>Only one of a joint pair was activated; on its own this is not evidence for either surface.</summary>
    public const string ActivatedPartnerAlone = "activated-partner-alone";

    /// <summary>Both halves of a joint pair were activated together; see <see cref="FindJointPartner"/>.</summary>
    public const string ActivatedJointly = "activated-jointly";

    /// <summary>
    /// Neutralizes <paramref name="surfaceId"/> in a copy of <paramref name="grammar"/>, or returns null
    /// when the document does not contain it.
    /// </summary>
    public static GrammarMutation? Mutate(XDocument grammar, string surfaceId, SemanticInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(surfaceId);
        ArgumentNullException.ThrowIfNull(inventory);

        var copy = new XDocument(grammar);
        if (surfaceId.StartsWith(GrammarFeatureUsage.ElementPrefix, StringComparison.Ordinal))
            return DeleteElement(copy, surfaceId);
        if (surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
            return NeutralizeEnumValue(copy, surfaceId, inventory);
        return null;
    }

    private static GrammarMutation? DeleteElement(XDocument copy, string surfaceId)
    {
        string name = Decode(surfaceId[GrammarFeatureUsage.ElementPrefix.Length..]);
        List<XElement> targets = copy.Descendants(name).ToList();
        if (targets.Count == 0)
            return null;
        foreach (XElement target in targets)
            target.Remove();
        return new GrammarMutation(surfaceId, DeletedElements, $"removed {targets.Count} <{name}> element(s)", copy);
    }

    /// <summary>One enum surface's parsed id components plus the attributes it targets in a document.</summary>
    private sealed record EnumTargets(string Element, string Attribute, string Value, XAttribute[] Attributes);

    private static EnumTargets? LocateEnumTargets(XDocument copy, string surfaceId)
    {
        string[] parts = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..].Split('/');
        if (parts.Length != 3)
            return null;
        string element = Decode(parts[0]);
        string attribute = Decode(parts[1]);
        string value = Decode(parts[2]);

        XAttribute[] targets = copy.Descendants(element)
            .Select(item => item.Attribute(attribute))
            .Where(attr => attr is not null && attr.Value == value)
            .Select(attr => attr!)
            .ToArray();
        return targets.Length == 0 ? null : new EnumTargets(element, attribute, value, targets);
    }

    private static GrammarMutation? NeutralizeEnumValue(XDocument copy, string surfaceId, SemanticInventory inventory)
    {
        EnumTargets? located = LocateEnumTargets(copy, surfaceId);
        if (located is null)
            return null;
        string[] parts = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..].Split('/');

        // Writing a value equal to the DTD default is indistinguishable from omitting it, so the only
        // available mutation is removal; the parser then supplies the same value, which is exactly what
        // makes such a surface unobservable and is the proof this mutation produces.
        string? sibling = Sibling(inventory, parts[0], parts[1], located.Value);
        if (sibling is null)
        {
            foreach (XAttribute target in located.Attributes)
                target.Remove();
            return new GrammarMutation(
                surfaceId,
                RemovedAttribute,
                $"removed {located.Attributes.Length} {located.Element}@{located.Attribute}=\"{located.Value}\" (no declared sibling value)",
                copy
            );
        }

        return RewriteEnumTargets(copy, surfaceId, located, sibling);
    }

    private static GrammarMutation RewriteEnumTargets(
        XDocument copy,
        string surfaceId,
        EnumTargets located,
        string sibling
    )
    {
        foreach (XAttribute target in located.Attributes)
            target.Value = sibling;
        return new GrammarMutation(
            surfaceId,
            RewroteAttribute,
            $"rewrote {located.Attributes.Length} {located.Element}@{located.Attribute} from \"{located.Value}\" to \"{sibling}\"",
            copy
        );
    }

    /// <summary>One candidate neutralization of an enum surface against a specific declared sibling.</summary>
    public sealed record EnumSiblingCandidate(string Sibling, GrammarMutation Mutation);

    /// <summary>
    /// Builds one neutralizing mutation per declared sibling value of <paramref name="surfaceId"/>'s
    /// enumerated attribute, in the same deterministic (ordinal) order <see cref="Sibling"/> uses --
    /// a caller that tries only <see cref="Sibling"/>'s pick can miss a sibling that discriminates
    /// where the first one happens not to. Empty when <paramref name="surfaceId"/> is not an enum
    /// surface, the document does not contain it, or it has no declared sibling at all (that last
    /// case has only <see cref="Mutate"/>'s removal fallback to offer).
    /// </summary>
    public static IReadOnlyList<EnumSiblingCandidate> MutateEnumAgainstEverySibling(
        XDocument grammar,
        string surfaceId,
        SemanticInventory inventory
    )
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(surfaceId);
        ArgumentNullException.ThrowIfNull(inventory);
        if (!surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
            return Array.Empty<EnumSiblingCandidate>();
        string[] parts = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..].Split('/');
        if (parts.Length != 3)
            return Array.Empty<EnumSiblingCandidate>();
        string value = Decode(parts[2]);

        var candidates = new List<EnumSiblingCandidate>();
        foreach (string sibling in Siblings(inventory, parts[0], parts[1], value))
        {
            var copy = new XDocument(grammar);
            EnumTargets? located = LocateEnumTargets(copy, surfaceId);
            if (located is null)
                continue;
            candidates.Add(new EnumSiblingCandidate(sibling, RewriteEnumTargets(copy, surfaceId, located, sibling)));
        }
        return candidates;
    }

    /// <summary>
    /// Empties every occurrence of <paramref name="surfaceId"/>'s element instead of removing it, for a
    /// caller whose full deletion left the search space unconstrained rather than genuinely neutralized.
    /// Returns null when the surface is not an element surface, the document lacks it, or it is already
    /// empty (emptying it again would not be a real mutation).
    /// </summary>
    public static GrammarMutation? MutateByEmptyingElementChildren(XDocument grammar, string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(surfaceId);
        if (!surfaceId.StartsWith(GrammarFeatureUsage.ElementPrefix, StringComparison.Ordinal))
            return null;

        var copy = new XDocument(grammar);
        string name = Decode(surfaceId[GrammarFeatureUsage.ElementPrefix.Length..]);
        List<XElement> targets = copy.Descendants(name).ToList();
        if (targets.Count == 0)
            return null;

        int removedNodes = targets.Sum(target => target.Nodes().Count());
        if (removedNodes == 0)
            return null;

        foreach (XElement target in targets)
            target.RemoveNodes();
        return new GrammarMutation(
            surfaceId,
            EmptiedChildren,
            $"emptied {targets.Count} <{name}> element(s) ({removedNodes} child node(s) removed) rather than deleting them",
            copy
        );
    }

    /// <summary>
    /// Identifies a joint-mutation pair for an <c>isActive="no"</c> surface that a single flip cannot
    /// evidence: <paramref name="PartnerElement"/>/<paramref name="PartnerAttribute"/> is an
    /// independently inactive declaration elsewhere in the same document whose IDREF names
    /// <paramref name="TargetId"/>. Carries only names and ids, not <see cref="XElement"/> references,
    /// so the same descriptor can be replayed against any number of fresh document clones.
    /// </summary>
    public sealed record JointPartner(
        string TargetElement,
        string TargetId,
        string PartnerElement,
        string PartnerAttribute
    );

    /// <summary>
    /// Finds a declaration that references an inactive instance of <paramref name="surfaceId"/>'s
    /// element while itself staying inactive -- the pair a joint mutation needs. Only applies to
    /// <c>isActive="no"</c> enum surfaces, since that is the only shape with a "dangling reference if
    /// activated alone" hazard for something else to guard against; returns null for anything else, for
    /// a target the document does not contain, or when no such partner exists in the document.
    /// </summary>
    public static JointPartner? FindJointPartner(XDocument grammar, string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(surfaceId);
        if (!surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
            return null;
        string[] parts = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..].Split('/');
        if (parts.Length != 3)
            return null;
        string element = Decode(parts[0]);
        string attribute = Decode(parts[1]);
        string value = Decode(parts[2]);
        if (attribute != "isActive" || value != "no")
            return null;

        XElement[] targets = grammar
            .Descendants(element)
            .Where(e => (string?)e.Attribute(attribute) == value && e.Attribute("id") is not null)
            .ToArray();

        foreach (XElement target in targets)
        {
            var targetId = (string)target.Attribute("id")!;
            foreach (XElement candidate in grammar.Descendants())
            {
                if (ReferenceEquals(candidate, target) || (string?)candidate.Attribute("isActive") != "no")
                    continue;

                // The reference may sit on a nested element rather than on the deactivated one itself,
                // as a rule's pattern names a natural class from inside its own subrule.
                XAttribute? reference = candidate
                    .DescendantsAndSelf()
                    .Attributes()
                    .FirstOrDefault(a =>
                        a.Name.LocalName is not ("id" or "isActive")
                        && a.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(targetId)
                    );
                if (reference is not null)
                    return new JointPartner(element, targetId, candidate.Name.LocalName, reference.Name.LocalName);
            }
        }

        return null;
    }

    private static XElement? LocatePartner(XDocument document, JointPartner partner) =>
        document
            .Descendants(partner.PartnerElement)
            .FirstOrDefault(e =>
                (string?)e.Attribute("isActive") == "no"
                // The reference may sit on a nested element rather than on the deactivated one
                // itself, matching FindJointPartner's own search.
                && e.DescendantsAndSelf()
                    .Attributes()
                    .Any(a =>
                        a.Name.LocalName == partner.PartnerAttribute
                        && a.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(partner.TargetId)
                    )
            );

    private static string DescribePartner(XElement partnerElement) =>
        (string?)partnerElement.Attribute("id") ?? (string?)partnerElement.Element("Name") ?? "(unidentified)";

    /// <summary>
    /// Activates only <paramref name="partner"/>'s referencing declaration, leaving the target it
    /// names still <c>isActive="no"</c>. Run alone -- never as evidence for the target -- to confirm
    /// the delta a joint mutation produces is not already fully explained by the partner by itself.
    /// </summary>
    public static GrammarMutation? MutatePartnerAlone(XDocument grammar, JointPartner partner)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(partner);

        var copy = new XDocument(grammar);
        XElement? partnerElement = LocatePartner(copy, partner);
        if (partnerElement is null)
            return null;
        XAttribute isActive = partnerElement.Attribute("isActive")!;
        isActive.Value = "yes";
        return new GrammarMutation(
            $"{GrammarFeatureUsage.EnumPrefix}{Encode(partner.TargetElement)}/isActive/no",
            ActivatedPartnerAlone,
            $"activated only its referencing <{partner.PartnerElement}> ({DescribePartner(partnerElement)}) via "
                + $"{partner.PartnerAttribute}=\"{partner.TargetId}\", while {partner.TargetElement}@isActive stayed \"no\"",
            copy
        );
    }

    /// <summary>
    /// Activates <paramref name="surfaceId"/>'s target(s) exactly as <see cref="Mutate"/> would, then
    /// additionally activates <paramref name="partner"/>'s referencing declaration in the same
    /// document, so the reference resolves instead of dangling. This is weaker evidence than a single
    /// flip: see <see cref="CounterfactualVerdict.EvidencedJointly"/> for why both flips must be shown
    /// necessary before the delta counts for the target.
    /// </summary>
    public static GrammarMutation? MutateJointly(
        XDocument grammar,
        string surfaceId,
        JointPartner partner,
        SemanticInventory inventory
    )
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(surfaceId);
        ArgumentNullException.ThrowIfNull(partner);
        ArgumentNullException.ThrowIfNull(inventory);

        GrammarMutation? targetMutation = Mutate(grammar, surfaceId, inventory);
        if (targetMutation is null)
            return null;

        XElement? partnerElement = LocatePartner(targetMutation.Mutated, partner);
        if (partnerElement is null)
            return null;
        XAttribute isActive = partnerElement.Attribute("isActive")!;
        isActive.Value = "yes";

        return new GrammarMutation(
            surfaceId,
            ActivatedJointly,
            $"{targetMutation.Detail}; jointly activated its sole referencing <{partner.PartnerElement}> "
                + $"({DescribePartner(partnerElement)}) via {partner.PartnerAttribute}=\"{partner.TargetId}\", "
                + "itself rewritten from isActive=\"no\" to \"yes\"",
            targetMutation.Mutated
        );
    }

    /// <summary>Every other declared value of the same enumerated attribute, in deterministic order.</summary>
    public static IReadOnlyList<string> Siblings(
        SemanticInventory inventory,
        string encodedElement,
        string encodedAttribute,
        string value
    )
    {
        ArgumentNullException.ThrowIfNull(inventory);
        string prefix = $"{GrammarFeatureUsage.EnumPrefix}{encodedElement}/{encodedAttribute}/";
        return inventory
            .Surfaces.Where(surface => surface.Id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(surface => Decode(surface.Id[prefix.Length..]))
            .Where(other => other != value)
            .Distinct()
            .OrderBy(other => other, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>The deterministically first other declared value of the same enumerated attribute.</summary>
    public static string? Sibling(
        SemanticInventory inventory,
        string encodedElement,
        string encodedAttribute,
        string value
    ) => Siblings(inventory, encodedElement, encodedAttribute, value).FirstOrDefault();

    private static string Decode(string encoded) => Uri.UnescapeDataString(encoded);

    private static string Encode(string authoredValue) => CanonicalIdCodec.Encode(authoredValue);
}
