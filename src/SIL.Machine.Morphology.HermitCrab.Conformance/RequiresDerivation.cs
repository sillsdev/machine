using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Mechanically derives a fixture's "requires" capability list from its grammar.xml, per
/// conformance/PROTOCOL.md section 5: today the only defined capability is "phonology", meaning
/// the grammar contains at least one <c>PhonologicalRule</c> or <c>MetathesisRule</c> element.
/// <see cref="Runner"/> validates every fixture's <c>words.yaml</c> "requires" (and
/// <see cref="MaterializedRunner"/> the materialized <c>manifest.json</c> equivalent) against this
/// derivation, so the field can never silently drift from what the grammar actually contains
/// (conformance/README.md's own growth-policy instructions already say to determine this by
/// inspecting the grammar, not by hand-guessing -- this makes that mechanical, not just advisory).
/// </summary>
public static class RequiresDerivation
{
    public static List<string> Derive(string grammarPath)
    {
        // DtdProcessing.Ignore: this only needs element names, not the HermitCrabInput.dtd itself
        // (which XmlLanguageLoader resolves via an embedded-resource resolver elsewhere) -- ignoring
        // it here avoids taking on that dependency for a check this narrow.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        bool hasPhonology = doc.Descendants()
            .Any(e => e.Name.LocalName == "PhonologicalRule" || e.Name.LocalName == "MetathesisRule");

        return hasPhonology ? new List<string> { "phonology" } : new List<string>();
    }
}
