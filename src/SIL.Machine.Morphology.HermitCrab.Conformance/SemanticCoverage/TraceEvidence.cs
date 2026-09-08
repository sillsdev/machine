#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// How strongly a fixture shows that it exercised a surface, rather than merely declaring it.
/// </summary>
public enum EvidenceStrength
{
    /// <summary>
    /// Declared in a grammar and nothing more. The construct may never influence any parse, so this
    /// is evidence context, never semantic credit.
    /// </summary>
    Presence,

    /// <summary>
    /// The declaration is deliberately deactivated, so it can never appear in a fired-rule list, and
    /// or is a rule capped at zero applications, so it can never appear in a fired-rule list. A word
    /// that fails BECAUSE it did not apply is the observation, and that word must name it.
    /// </summary>
    NegativeControl,

    /// <summary>
    /// The declaration is on the load path of a fixture that produces at least one verified parse,
    /// but no per-construct trace attributes the parse to it.
    /// </summary>
    Structural,

    /// <summary>
    /// A verified parse names the owning rule among the rules that fired, so the construct
    /// demonstrably took part in producing a result.
    /// </summary>
    Trace,
}

public sealed record SurfaceEvidence(string SurfaceId, EvidenceStrength Strength, string FixtureId, string Detail);

/// <summary>
/// Grades grammar-observable coverage by the evidence behind it. A rule-bearing construct is only
/// credited when some word's verified <c>rules:</c> list names the rule that owns it; the conformance
/// runner checks those lists against a real engine run, so they are trace data rather than a claim.
///
/// Known over-crediting, not yet fixed: attribution is per rule, so every attribute on a firing rule
/// is credited even when its value changed nothing. Deactivated declarations no longer inherit credit
/// from a neighbouring failing word: a word must name the declaration in its `neutralizes:` list. The
/// structural grade remains per fixture, and is reported separately from trace for that reason.
/// </summary>
public static class TraceEvidence
{
    /// <summary>Elements whose <c>id</c> can appear in a parse's fired-rule list.</summary>
    private static readonly HashSet<string> RuleElements = new(StringComparer.Ordinal)
    {
        "MorphologicalRule",
        "RealizationalRule",
        "CompoundingRule",
        "PhonologicalRule",
        "MetathesisRule",
    };

    /// <summary>
    /// Elements whose declarations can never reach <see cref="EvidenceStrength.Trace"/>, because their
    /// ids do not appear in a fired-rule list. AffixTemplate and Slot carry no id in the DTD at all,
    /// and the co-occurrence rules are blocking mechanisms the trace attributor does not resolve.
    /// </summary>
    public static readonly IReadOnlySet<string> TraceUnreachableElements = new HashSet<string>(StringComparer.Ordinal)
    {
        "AffixTemplate",
        "Slot",
        "AllomorphCoOccurrenceRule",
        "MorphemeCoOccurrenceRule",
    };

    /// <summary>
    /// Grades every observable surface in one fixture. <paramref name="firedRuleIds"/> is the union of
    /// every verified parse's fired rules plus any blocked-by attribution, since a rule that blocked
    /// an analysis also ran.
    /// </summary>
    public static IReadOnlyList<SurfaceEvidence> Grade(
        string fixtureId,
        XDocument grammar,
        SemanticInventory inventory,
        IReadOnlyCollection<string> firedRuleIds,
        bool hasVerifiedParse,
        IReadOnlyCollection<string> neutralizedIds
    )
    {
        ArgumentNullException.ThrowIfNull(neutralizedIds);
        var neutralized = new HashSet<string>(neutralizedIds, StringComparer.Ordinal);
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(firedRuleIds);
        var declared = new HashSet<string>(inventory.Surfaces.Select(surface => surface.Id), StringComparer.Ordinal);
        var fired = new HashSet<string>(firedRuleIds, StringComparer.Ordinal);
        var best = new Dictionary<string, SurfaceEvidence>(StringComparer.Ordinal);

        foreach (XElement element in grammar.Descendants())
        {
            (EvidenceStrength strength, string detail) = GradeElement(element, fired, hasVerifiedParse, neutralized);
            foreach (string surfaceId in ObservableIds(element, declared))
            {
                if (best.TryGetValue(surfaceId, out SurfaceEvidence? existing) && existing.Strength >= strength)
                    continue;
                best[surfaceId] = new SurfaceEvidence(surfaceId, strength, fixtureId, detail);
            }
        }

        return best.Values.OrderBy(item => item.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> ObservableIds(XElement element, HashSet<string> declared)
    {
        string encodedElement = CanonicalIdCodec.Encode(element.Name.LocalName);
        string elementId = GrammarFeatureUsage.ElementPrefix + encodedElement;
        if (declared.Contains(elementId))
            yield return elementId;
        foreach (XAttribute attribute in element.Attributes())
        {
            string enumId =
                $"{GrammarFeatureUsage.EnumPrefix}{encodedElement}/{CanonicalIdCodec.Encode(attribute.Name.LocalName)}"
                + $"/{CanonicalIdCodec.Encode(attribute.Value)}";
            if (declared.Contains(enumId))
                yield return enumId;
        }
    }

    /// <summary>
    /// A construct inside a rule is credited only when that rule fired. Everything else can reach at
    /// most structural strength, because nothing in a fixture attributes a parse to it.
    /// </summary>
    private static (EvidenceStrength Strength, string Detail) GradeElement(
        XElement element,
        HashSet<string> fired,
        bool hasVerifiedParse,
        HashSet<string> neutralized
    )
    {
        // A deactivated declaration is never loaded, so it can never reach a fired-rule list; the
        // observation is a word that fails BECAUSE it is absent. That word must NAME the declaration:
        // a fixture-wide flag would let a decoy nothing targets inherit credit from its neighbours.
        if (DeactivatedAncestor(element) is string deactivated)
        {
            // Some deactivatable elements (Stratum) carry no id, so a word may name any id inside the
            // deactivated subtree instead; either way it has targeted that subtree.
            string? named = NeutralizedName(element, neutralized);
            return named is not null
                ? (
                    EvidenceStrength.NegativeControl,
                    $"deactivated at '{deactivated}'; a verified failing word neutralizes '{named}'"
                )
                : (EvidenceStrength.Presence, $"deactivated at '{deactivated}', which no word names in neutralizes:");
        }

        string? owningRule = OwningRuleId(element);
        if (owningRule is not null)
        {
            if (fired.Contains(owningRule))
                return (EvidenceStrength.Trace, $"rule '{owningRule}' fired in a verified parse");

            // A rule can be DESIGNED never to fire: multipleApplication="0" caps it at zero
            // applications. Its evidence is the same shape as a deactivated decoy's, a word that fails
            // because the rule did not apply, so it is credited the same way and only when named.
            return neutralized.Contains(owningRule)
                ? (
                    EvidenceStrength.NegativeControl,
                    $"rule '{owningRule}' never fires and a verified failing word neutralizes it"
                )
                : (EvidenceStrength.Presence, $"declared under rule '{owningRule}', which no verified parse fired");
        }

        if (!hasVerifiedParse)
            return (EvidenceStrength.Presence, "fixture produces no verified parse");
        string? unreachable = UnreachableAncestor(element);
        return unreachable is not null
            ? (EvidenceStrength.Structural, $"trace-unreachable: '{unreachable}' ids never appear in a fired-rule list")
            : (EvidenceStrength.Structural, "on the load path of a fixture with a verified parse");
    }

    /// <summary>
    /// The first identifier a word names, searching this element and its ancestors up to and including
    /// the deactivated one, then the ids declared beneath that deactivated element.
    /// </summary>
    private static string? NeutralizedName(XElement element, HashSet<string> neutralized)
    {
        XElement? deactivated = null;
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            if (Identifier(current) is string own && neutralized.Contains(own))
                return own;
            if (neutralized.Contains(current.Name.LocalName))
                return current.Name.LocalName;
            if ((string?)current.Attribute("isActive") == "no")
            {
                deactivated = current;
                break;
            }
        }

        if (deactivated is null)
            return null;
        return deactivated
            .Descendants()
            .Select(Identifier)
            .FirstOrDefault(id => id is not null && neutralized.Contains(id));
    }

    /// <summary>
    /// How a word names a declaration: its id, else its Name child's text (Slot, AffixTemplate and
    /// Stratum carry no id), else the element name.
    /// </summary>
    private static string? Identifier(XElement element) =>
        (string?)element.Attribute("id")
        ?? element.Elements("Name").FirstOrDefault()?.Value.Trim()
        ?? element.Name.LocalName;

    /// <summary>The nearest self-or-ancestor element whose kind trace attribution cannot reach.</summary>
    public static string? UnreachableAncestor(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            if (TraceUnreachableElements.Contains(current.Name.LocalName))
                return current.Name.LocalName;
        }

        return null;
    }

    /// <summary>The nearest self-or-ancestor element carrying <c>isActive="no"</c>.</summary>
    public static string? DeactivatedAncestor(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            if ((string?)current.Attribute("isActive") == "no")
                return (string?)current.Attribute("id") ?? current.Name.LocalName;
        }

        return null;
    }

    /// <summary>The id of the nearest rule-or-self ancestor whose id can appear in a fired-rule list.</summary>
    public static string? OwningRuleId(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        for (XElement? current = element; current is not null; current = current.Parent)
        {
            if (!RuleElements.Contains(current.Name.LocalName))
                continue;
            string? id = (string?)current.Attribute("id");
            if (!string.IsNullOrEmpty(id))
                return id;
        }

        return null;
    }
}
