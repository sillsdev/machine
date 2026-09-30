#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Reads the generated surfaces a fixture's <c>grammar.xml</c> actually exercises. Only surfaces a
/// grammar author can control are observable this way: an element appears in the document, or an
/// enumerated attribute carries one of its declared values.
/// </summary>
public static class GrammarFeatureUsage
{
    public const string ElementPrefix = "dtd:element/";
    public const string EnumPrefix = "dtd:enum/";

    /// <summary>
    /// Surfaces observable from a grammar document, restricted to IDs the inventory actually
    /// declares so a typo in a fixture cannot invent coverage.
    /// </summary>
    public static IReadOnlyCollection<string> Read(XDocument grammar, SemanticInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(grammar);
        ArgumentNullException.ThrowIfNull(inventory);
        var declared = new HashSet<string>(inventory.Surfaces.Select(surface => surface.Id), StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        // Every ID component is percent-encoded by the inventory, so a Greek VariableFeature name
        // read raw here would never match its generated surface.
        foreach (XElement element in grammar.Descendants())
        {
            string encodedElement = CanonicalIdCodec.Encode(element.Name.LocalName);
            string elementId = ElementPrefix + encodedElement;
            if (declared.Contains(elementId))
                used.Add(elementId);
            foreach (XAttribute attribute in element.Attributes())
            {
                string enumId =
                    $"{EnumPrefix}{encodedElement}/{CanonicalIdCodec.Encode(attribute.Name.LocalName)}"
                    + $"/{CanonicalIdCodec.Encode(attribute.Value)}";
                if (declared.Contains(enumId))
                    used.Add(enumId);
            }
        }

        return used;
    }

    /// <summary>Every surface in the inventory that a grammar document could ever exercise.</summary>
    public static IReadOnlyCollection<string> Observable(SemanticInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return inventory
            .Surfaces.Where(surface => surface.Kind is "element" or "enum")
            .Select(surface => surface.Id)
            .ToHashSet(StringComparer.Ordinal);
    }
}
