using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// Independently re-parses a v2 fixture's <c>grammar.xml</c> (not via <see cref="XmlLanguageLoader"/>,
/// whose runtime rule objects lose the XML <c>id</c> attribute for several rule kinds -- see below)
/// to answer two questions the harness needs for the "rules:" coverage dimension: (1) every rule id
/// the grammar declares (for dead-rule detection), and (2) how to translate a *runtime* rule
/// identity (what <see cref="Word.MorphologicalRules"/> or a <see cref="Trace"/> node's
/// <c>Source</c> actually expose at parse time) back to that XML <c>id</c> attribute, so a traced
/// rule application can be compared against a <c>words.yaml</c> "rules:" entry.
///
/// <b>Why this indirection is needed</b> (see <c>XmlLanguageLoader.TryLoadAffixProcessRule</c> /
/// <c>TryLoadCompoundingRule</c> / <c>LoadRewriteRule</c> / <c>LoadMetathesisRule</c>): the XML
/// <c>id</c> attribute on a <c>MorphologicalRule</c>/<c>RealizationalRule</c> element is used only
/// as the loader's own lookup key while building the <see cref="Language"/> graph -- the *runtime*
/// object's <see cref="Morpheme.Id"/> is loaded from the separate <c>&lt;MorphemeId&gt;</c> child
/// element instead (the short code that appears in a parse's morph-chain signature, e.g. "PL").
/// A <c>CompoundingRule</c>, <c>PhonologicalRule</c>, and <c>MetathesisRule</c> element has no
/// <c>MorphemeId</c> at all; their runtime objects expose only <see cref="IHCRule.Name"/> (from the
/// element's <c>&lt;Name&gt;</c> child). None of the four runtime rule kinds retains the XML
/// <c>id</c> attribute itself anywhere the harness can read it back after loading. This class
/// rebuilds that lost mapping straight from the XML.
/// </summary>
public class GrammarRuleIndex
{
    /// <summary>Every rule id declared by the grammar (MorphologicalRule/RealizationalRule/CompoundingRule/PhonologicalRule/MetathesisRule ids, plus the co-occurrence-rule pseudo-ids below), in document order.</summary>
    public List<string> AllRuleIds { get; } = new();

    // MorphemeId text -> XML id attribute, for MorphologicalRule/RealizationalRule (AffixProcessRule
    // /RealizationalAffixProcessRule at runtime): these are the only two kinds whose runtime object
    // exposes a stable per-morpheme string (Morpheme.Id) that this class can translate back.
    private readonly Dictionary<string, string> _ruleIdByMorphemeId = new(StringComparer.Ordinal);

    // <Name> text -> XML id attribute, for CompoundingRule/PhonologicalRule/MetathesisRule: the only
    // identity these three retain at runtime is IHCRule.Name.
    private readonly Dictionary<string, string> _ruleIdByRuleName = new(StringComparer.Ordinal);

    /// <summary>
    /// MorphemeCoOccurrenceRule/AllomorphCoOccurrenceRule elements are EMPTY per the DTD -- they
    /// have no <c>id</c> attribute of their own at all, only <c>primaryMorpheme</c>/
    /// <c>primaryAllomorph</c> IDREFs. This harness's convention (documented here -- it is
    /// coverage/dead-rule bookkeeping, internal to the harness, not part of PROTOCOL.md's wire
    /// adapter contract) is: such a rule's "rule id", for both
    /// <c>blocked_by</c> and dead-rule detection purposes, IS the referenced primary morpheme/
    /// allomorph id. <see cref="CoOccurrencePseudoIds"/> is that set, so dead-rule detection can
    /// tell a real dead co-occurrence rule from an id that was never declared at all.
    /// </summary>
    public HashSet<string> CoOccurrencePseudoIds { get; } = new(StringComparer.Ordinal);

    public static GrammarRuleIndex Load(string grammarPath)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using XmlReader reader = XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);

        var index = new GrammarRuleIndex();
        foreach (XElement el in doc.Descendants())
        {
            switch (el.Name.LocalName)
            {
                case "MorphologicalRule":
                case "RealizationalRule":
                {
                    string id = (string)el.Attribute("id");
                    string morphemeId = (string)el.Element("MorphemeId");
                    if (id == null)
                        continue;
                    index.AllRuleIds.Add(id);
                    if (morphemeId != null)
                    {
                        index._ruleIdByMorphemeId[morphemeId] = id;
                    }
                    else
                    {
                        // Conservative fallback for a MorphologicalRule/RealizationalRule with no
                        // <MorphemeId> child at all (Morpheme.Id then null, so the morpheme-id path
                        // above can never resolve it) -- register its <Name> text the same way
                        // CompoundingRule/PhonologicalRule/MetathesisRule already are below, but only
                        // in this no-MorphemeId case, so every existing grammar (which assigns
                        // MorphemeId to every rule) is byte-for-byte unaffected: this dict only ever
                        // gains an entry when the morpheme-id path was never going to work anyway.
                        // Discovered authoring edge-cases/disjunctive-recheck + edge-cases/strrep-
                        // identity (both v1 fixtures assign no MorphemeId at all, a deliberate v1
                        // choice -- adding one would change the "+|wokta"-style signatures those
                        // fixtures pin) -- see those fixtures' words.yaml headers.
                        string name = (string)el.Element("Name");
                        if (name != null)
                            index._ruleIdByRuleName[name] = id;
                    }
                    break;
                }
                case "CompoundingRule":
                case "PhonologicalRule":
                case "MetathesisRule":
                {
                    string id = (string)el.Attribute("id");
                    string name = (string)el.Element("Name");
                    if (id == null)
                        continue;
                    index.AllRuleIds.Add(id);
                    if (name != null)
                        index._ruleIdByRuleName[name] = id;
                    break;
                }
                case "MorphemeCoOccurrenceRule":
                case "AllomorphCoOccurrenceRule":
                {
                    string primary = (string)(el.Attribute("primaryMorpheme") ?? el.Attribute("primaryAllomorph"));
                    if (primary == null)
                        continue;
                    index.AllRuleIds.Add(primary);
                    index.CoOccurrencePseudoIds.Add(primary);
                    break;
                }
            }
        }
        return index;
    }

    /// <summary>
    /// Translates a fired <see cref="IMorphologicalRule"/> (from <see cref="Word.MorphologicalRules"/>,
    /// or a RealizationalAffixProcessRule taken from a MorphologicalRuleSynthesis trace node -- see
    /// <see cref="TraceRuleAttributor"/>) back to its grammar.xml rule id. Returns null (rather than
    /// throwing) for a rule kind this index cannot attribute.
    /// </summary>
    public string ResolveMorphologicalRuleId(IMorphologicalRule rule)
    {
        if (
            rule is Morpheme morpheme
            && morpheme.Id != null
            && _ruleIdByMorphemeId.TryGetValue(morpheme.Id, out string byMorphemeId)
        )
        {
            return byMorphemeId;
        }
        if (rule.Name != null && _ruleIdByRuleName.TryGetValue(rule.Name, out string byName))
            return byName;
        return null;
    }

    /// <summary>Translates a fired phonological rule (from a trace node's Source) back to its grammar.xml rule id, by IHCRule.Name -- see the class doc comment.</summary>
    public string ResolvePhonologicalRuleId(IPhonologicalRule rule)
    {
        return rule.Name != null && _ruleIdByRuleName.TryGetValue(rule.Name, out string id) ? id : null;
    }
}
