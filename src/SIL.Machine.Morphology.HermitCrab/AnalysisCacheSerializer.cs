using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Persists an <see cref="AnalysisCache"/> to/from text so a fixed corpus's complete analyses
    /// survive sessions (HERMITCRAB_FST_PLAN.md §13). Morphemes are written as
    /// <see cref="MorphemeRegistry"/> keys; a grammar-version line guards against loading a cache built
    /// against a different grammar (a stale cache could otherwise rehydrate wrong morphemes). One line
    /// per word: <c>word \t analysis | analysis | …</c> where an analysis is
    /// <c>key,key,…:rootIndex:category</c> (an empty word line records a confirmed non-word).
    /// </summary>
    public static class AnalysisCacheSerializer
    {
        private const string Magic = "hcfstcache/1";

        public static void Save(AnalysisCache cache, MorphemeRegistry registry, string grammarVersion, TextWriter writer)
        {
            writer.WriteLine(Magic + "\t" + (grammarVersion ?? string.Empty));
            foreach (KeyValuePair<string, IReadOnlyList<WordAnalysis>> entry in cache.Entries)
            {
                var analyses = new List<string>();
                foreach (WordAnalysis a in entry.Value)
                {
                    string keys = string.Join(",", a.Morphemes.Select(registry.Key));
                    analyses.Add($"{keys}:{a.RootMorphemeIndex}:{a.Category}");
                }
                writer.WriteLine(entry.Key + "\t" + string.Join(" | ", analyses));
            }
        }

        /// <summary>
        /// Load cached analyses into <paramref name="cache"/>. Returns false (loading nothing) if the
        /// file's grammar version does not match <paramref name="grammarVersion"/> — the caller should
        /// then re-warm. Skips any analysis referencing an unknown morpheme key (defensive).
        /// </summary>
        public static bool Load(AnalysisCache cache, MorphemeRegistry registry, string grammarVersion, TextReader reader)
        {
            string header = reader.ReadLine();
            if (header == null)
            {
                return false;
            }
            string[] head = header.Split('\t');
            if (head.Length < 1 || head[0] != Magic)
            {
                return false;
            }
            string fileVersion = head.Length > 1 ? head[1] : string.Empty;
            if (fileVersion != (grammarVersion ?? string.Empty))
            {
                return false; // stale cache for a different grammar version
            }

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int tab = line.IndexOf('\t');
                if (tab < 0)
                {
                    continue;
                }
                string word = line.Substring(0, tab);
                string rest = line.Substring(tab + 1);
                var analyses = new List<WordAnalysis>();
                if (rest.Length > 0)
                {
                    foreach (string a in rest.Split(new[] { " | " }, System.StringSplitOptions.None))
                    {
                        WordAnalysis parsed = ParseAnalysis(a, registry);
                        if (parsed != null)
                        {
                            analyses.Add(parsed);
                        }
                    }
                }
                cache.Set(word, analyses);
            }
            return true;
        }

        private static WordAnalysis ParseAnalysis(string s, MorphemeRegistry registry)
        {
            string[] parts = s.Split(':');
            if (parts.Length < 2)
            {
                return null;
            }
            var morphemes = new List<IMorpheme>();
            if (parts[0].Length > 0)
            {
                foreach (string k in parts[0].Split(','))
                {
                    if (!int.TryParse(k, out int key))
                    {
                        return null;
                    }
                    IMorpheme morpheme = registry.Resolve(key);
                    if (morpheme == null)
                    {
                        return null; // unknown key — grammar mismatch; drop this analysis
                    }
                    morphemes.Add(morpheme);
                }
            }
            if (!int.TryParse(parts[1], out int rootIndex))
            {
                return null;
            }
            string category = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null;
            return new WordAnalysis(morphemes, rootIndex, category);
        }
    }
}
