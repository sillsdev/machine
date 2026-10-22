using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

public static class RequiresDerivation
{
    /// <summary>
    /// <c>phonology</c> when the grammar has a phonological rule, plus
    /// <see cref="MorpherSettings.Capability"/> when any fixture run declares a <c>morpher:</c> block, in that order.
    /// </summary>
    public static List<string> Derive(string grammarPath, MorpherSettings morpher)
    {
        return Derive(grammarPath, hasMorpherSettings: !morpher.IsEmpty);
    }

    public static List<string> Derive(string grammarPath, IEnumerable<MorpherSettings> runs)
    {
        return Derive(grammarPath, hasMorpherSettings: runs.Any(settings => !settings.IsEmpty));
    }

    /// <summary>
    /// Derive grammar capabilities and add the fixture-level settings capability when any run has
    /// settings. A selected run's settings may be empty while another named run requires support.
    /// </summary>
    public static List<string> Derive(string grammarPath, bool hasMorpherSettings)
    {
        List<string> requires = Derive(grammarPath);
        if (hasMorpherSettings)
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
