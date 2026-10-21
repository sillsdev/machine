using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public static class RequiresDerivation
{
    /// <summary>
    /// <c>phonology</c> when the grammar has a phonological rule, plus
    /// <see cref="MorpherSettings.Capability"/> when the fixture declares a <c>morpher:</c> block, in that order.
    /// </summary>
    public static List<string> Derive(string grammarPath, MorpherSettings morpher)
    {
        List<string> requires = Derive(grammarPath);
        if (!morpher.IsEmpty)
            requires.Add(MorpherSettings.Capability);
        return requires;
    }

    private static List<string> Derive(string grammarPath)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        bool hasPhonology = doc.Descendants()
            .Any(e => e.Name.LocalName == "PhonologicalRule" || e.Name.LocalName == "MetathesisRule");

        return hasPhonology ? new List<string> { "phonology" } : new List<string>();
    }
}
