using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public static class RequiresDerivation
{
    public static List<string> Derive(string grammarPath)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        bool hasPhonology = doc.Descendants()
            .Any(e => e.Name.LocalName == "PhonologicalRule" || e.Name.LocalName == "MetathesisRule");

        return hasPhonology ? new List<string> { "phonology" } : new List<string>();
    }
}
