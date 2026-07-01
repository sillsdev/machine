using System.Collections.Generic;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A stable, deterministic morpheme ↔ integer-key map for a grammar, used to persist
    /// <see cref="AnalysisCache"/> across sessions (HERMITCRAB_FST_PLAN.md §13). A cached
    /// <see cref="WordAnalysis"/> references grammar morpheme <i>objects</i>, which cannot be
    /// serialized directly; instead each morpheme is keyed by its position in a deterministic
    /// enumeration of the grammar (roots then affix rules, per stratum). Rebuilding the registry from
    /// the same grammar yields the same keys, so cached keys rehydrate to the right morphemes — and a
    /// grammar-version guard on the file rejects a cache built against a different grammar.
    /// </summary>
    public sealed class MorphemeRegistry
    {
        private readonly Dictionary<IMorpheme, int> _toKey = new Dictionary<IMorpheme, int>();
        private readonly List<IMorpheme> _byKey = new List<IMorpheme>();

        public MorphemeRegistry(Language language)
        {
            foreach (Stratum stratum in language.Strata)
            {
                foreach (LexEntry entry in stratum.Entries)
                {
                    Add(entry);
                }
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (mrule is MorphemicMorphologicalRule rule)
                    {
                        Add(rule);
                    }
                }
                foreach (AffixTemplate template in stratum.AffixTemplates)
                {
                    foreach (AffixTemplateSlot slot in template.Slots)
                    {
                        foreach (MorphemicMorphologicalRule rule in slot.Rules)
                        {
                            Add(rule);
                        }
                    }
                }
            }
        }

        private void Add(IMorpheme morpheme)
        {
            if (!_toKey.ContainsKey(morpheme))
            {
                _toKey[morpheme] = _byKey.Count;
                _byKey.Add(morpheme);
            }
        }

        /// <summary>The key for a morpheme; -1 if it is not a registered grammar morpheme.</summary>
        public int Key(IMorpheme morpheme)
        {
            return _toKey.TryGetValue(morpheme, out int key) ? key : -1;
        }

        /// <summary>The morpheme for a key, or null if out of range (a cache from a different grammar).</summary>
        public IMorpheme Resolve(int key)
        {
            return key >= 0 && key < _byKey.Count ? _byKey[key] : null;
        }
    }
}
