using System;
using System.Collections.Generic;
using System.Text;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// A candidate generator for <b>infixation</b> (an affix inserted inside the stem, e.g. Tagalog
    /// -um-: sulat → s·um·ulat) — a regular construct the FST proposer recognizes but does not build
    /// (FST_FULL_PLAN.md, Point 2). Handled here as a sibling generator feeding the same
    /// <see cref="VerifiedFstAnalyzer"/> gate.
    ///
    /// Mechanism (remove + recurse): for each infix and each interior position where the infix's surface
    /// segments occur, remove them and <b>recurse the residual through the FST proposer</b> (so an
    /// infixed form of an inflected stem is covered), then append the infix morpheme in HC application
    /// order (root·…·INF). Over-approximation: every interior occurrence is tried; verify prunes the
    /// wrong splits (a wrong removal won't re-synthesize to the surface). `O(surface-length × infixes)`
    /// candidates — bounded.
    ///
    /// Scope (first cut): the infix must be a single contiguous run of inserted segments, matched against
    /// its underlying representation. Templatic multi-slot infixes (separate insert runs) and infixes
    /// whose surface is phonologically altered are left to the engine (the parity gate keeps results
    /// correct — those words simply ride the slow path).
    /// </summary>
    public class InfixProposer : IConstructProposer
    {
        private static readonly MorphOp[] _ops = { MorphOp.Infix };
        private readonly IMorphologicalAnalyzer _baseProposer;
        private readonly List<KeyValuePair<MorphemicMorphologicalRule, string>> _infixes;

        public InfixProposer(Language language, IMorphologicalAnalyzer baseProposer)
        {
            _baseProposer = baseProposer;
            _infixes = new List<KeyValuePair<MorphemicMorphologicalRule, string>>();
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule mrule in stratum.MorphologicalRules)
                {
                    if (!(mrule is AffixProcessRule rule))
                    {
                        continue;
                    }
                    foreach (AffixProcessAllomorph allomorph in rule.Allomorphs)
                    {
                        if (MorphTokenCodec.ClassifyOp(allomorph, false) != MorphOp.Infix)
                        {
                            continue;
                        }
                        string infix = InfixString(allomorph);
                        if (!string.IsNullOrEmpty(infix))
                        {
                            _infixes.Add(new KeyValuePair<MorphemicMorphologicalRule, string>(rule, infix));
                        }
                    }
                }
            }
        }

        public IReadOnlyCollection<MorphOp> CoveredOps => _ops;

        public IEnumerable<WordAnalysis> AnalyzeWord(string word)
        {
            foreach (KeyValuePair<MorphemicMorphologicalRule, string> entry in _infixes)
            {
                string infix = entry.Value;
                // Interior occurrences only: stem material both before (i >= 1) and after the infix.
                int i = word.IndexOf(infix, 1, StringComparison.Ordinal);
                while (i >= 1 && i + infix.Length < word.Length)
                {
                    string residual = word.Remove(i, infix.Length);
                    foreach (WordAnalysis baseAnalysis in _baseProposer.AnalyzeWord(residual))
                    {
                        var morphemes = new List<IMorpheme>(baseAnalysis.Morphemes) { entry.Key };
                        yield return new WordAnalysis(morphemes, baseAnalysis.RootMorphemeIndex, null);
                    }
                    i = word.IndexOf(infix, i + 1, StringComparison.Ordinal);
                }
            }
        }

        /// <summary>The infix's inserted material iff it is a single contiguous run of inserted segments;
        /// null for templatic multi-slot infixes (left to the engine).</summary>
        private static string InfixString(AffixProcessAllomorph allomorph)
        {
            var runs = new List<string>();
            StringBuilder current = null;
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                if (action is InsertSegments insert)
                {
                    current = current ?? new StringBuilder();
                    current.Append(insert.Segments.Representation);
                }
                else if (current != null)
                {
                    runs.Add(current.ToString());
                    current = null;
                }
            }
            if (current != null)
            {
                runs.Add(current.ToString());
            }
            return runs.Count == 1 ? runs[0] : null;
        }
    }
}
