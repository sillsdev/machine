using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.WhyNot;

/// <summary>
/// Maps HermitCrab's (morpheme, allomorph position) pairs to the allomorph and stem-name ids written in
/// grammar.xml, which the loaded object model does not keep. A morpheme is mapped only when the active
/// allomorph elements in the file equal the allomorphs the loader produced, so a skipped allomorph can never
/// shift an index; anything uncertain stays unmapped and the caller omits the id. Compounding subrules are not
/// mapped.
/// </summary>
public class GrammarIds
{
    private readonly Dictionary<string, List<string>> _allomorphIds = new Dictionary<string, List<string>>(
        StringComparer.Ordinal
    );
    private readonly Dictionary<string, string> _stemNameIds = new Dictionary<string, string>(StringComparer.Ordinal);

    public static GrammarIds Empty { get; } = new GrammarIds();

    public static GrammarIds Load(string grammarPath, Language language)
    {
        var ids = new GrammarIds();
        XDocument doc = XDocument.Load(grammarPath);

        Dictionary<string, int> loaded = LoadedAllomorphCounts(language);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        foreach (XElement morpheme in doc.Descendants().Where(e => e.Element("MorphemeId") != null && IsLive(e)))
        {
            var morphemeId = (string)morpheme.Element("MorphemeId");
            List<string> allomorphIds = morpheme
                .Elements("Allomorphs")
                .Elements("Allomorph")
                .Concat(morpheme.Elements("MorphologicalSubrules").Elements("MorphologicalSubrule"))
                .Where(IsActive)
                .Select(e => (string)e.Attribute("id"))
                .ToList();
            if (ids._allomorphIds.ContainsKey(morphemeId))
                ambiguous.Add(morphemeId);
            ids._allomorphIds[morphemeId] = allomorphIds;
        }
        foreach (string morphemeId in ids._allomorphIds.Keys.ToList())
        {
            bool countsAgree =
                loaded.TryGetValue(morphemeId, out int count) && count == ids._allomorphIds[morphemeId].Count;
            if (ambiguous.Contains(morphemeId) || !countsAgree)
                ids._allomorphIds.Remove(morphemeId);
        }

        var stemNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (XElement stemName in doc.Descendants().Where(e => e.Attribute("partsOfSpeech") != null && IsLive(e)))
        {
            var name = (string)stemName.Element("Name");
            if (name == null)
                continue;
            stemNameCounts[name] = stemNameCounts.TryGetValue(name, out int seen) ? seen + 1 : 1;
            ids._stemNameIds[name] = (string)stemName.Attribute("id");
        }
        foreach (string duplicate in stemNameCounts.Where(p => p.Value > 1).Select(p => p.Key))
            ids._stemNameIds.Remove(duplicate);
        return ids;
    }

    public string AllomorphId(string morphemeId, int index) =>
        _allomorphIds.TryGetValue(morphemeId, out List<string> list) && index >= 0 && index < list.Count
            ? list[index]
            : null;

    public int? AllomorphIndex(string morphemeId, string allomorphId)
    {
        if (!_allomorphIds.TryGetValue(morphemeId, out List<string> list))
            return null;
        int index = list.IndexOf(allomorphId);
        return index < 0 ? (int?)null : index;
    }

    public string StemNameId(StemName stemName) =>
        stemName.Name != null && _stemNameIds.TryGetValue(stemName.Name, out string id) ? id : null;

    private static Dictionary<string, int> LoadedAllomorphCounts(Language language)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Stratum stratum in language.Strata)
        {
            var morphemes = new List<Morpheme>(stratum.Entries);
            morphemes.AddRange(stratum.MorphologicalRules.OfType<Morpheme>());
            morphemes.AddRange(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules));
            foreach (Morpheme morpheme in morphemes.Distinct())
            {
                string id = MorphemeIds.Of(morpheme);
                counts[id] = counts.ContainsKey(id) ? -1 : morpheme.AllomorphCount;
            }
        }
        return counts;
    }

    private static bool IsActive(XElement element) => ((string)element.Attribute("isActive") ?? "yes") == "yes";

    private static bool IsLive(XElement element) => element.AncestorsAndSelf().All(IsActive);
}
