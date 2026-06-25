using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Forward phonology for the surface-allomorph precompile (FST_FULL_PLAN.md, Point 1). Applies the
    /// grammar's <b>synthesis</b> phonological rules to a morpheme's underlying segment string and
    /// returns the distinct surface realizations. Reuses HC's own compiled synthesis rules — no
    /// reimplemented phonology — exactly the rules <see cref="SynthesisStratumRule"/> runs.
    ///
    /// Two tiers, both precompiled into the proposer's arcs:
    /// <list type="bullet">
    /// <item><b>C-internal (1a):</b> apply rules to the morpheme <i>in isolation</i> (word-edge context)
    /// — catches edge-conditioned and morpheme-internal alternations.</item>
    /// <item><b>C-boundary (1b):</b> apply rules to the morpheme with each single neighbor segment of the
    /// surface alphabet on each side, and (when the rule is length-preserving) read back the morpheme's
    /// own surface portion — catches an affix whose <i>own</i> surface is conditioned by a neighbor across
    /// the seam. Bounded by alphabet size × 2; a length-changing context is skipped (no reliable
    /// portion), so it stays a sound superset.</item>
    /// </list>
    /// What remains — a neighbor's surface changing (e.g. a root devoicing before an affix), and any
    /// longer-distance interaction — is covered completely by <see cref="ComposedPhonologyProposer"/>
    /// (Point 4), which un-applies phonology on the assembled surface. So this helper is the cheap
    /// fast-path; the composition proposer is the complete backstop.
    /// </summary>
    internal sealed class SurfacePhonology
    {
        private readonly CharacterDefinitionTable _table;
        private readonly Stratum _surfaceStratum;
        private readonly List<LinearRuleCascade<Word, int>> _strataPrules;
        private readonly List<string> _alphabet;

        // Variants(underlying) is a pure function of the fixed strata/alphabet above, but the FST builder
        // calls it once per build SITE (per allomorph x slot x template x derivation depth/side) rather
        // than once per distinct affix string - the same underlying segment string is re-cascaded many
        // times over. Memoize so build cost scales with the affix inventory, not the template structure.
        private readonly Dictionary<string, IReadOnlyCollection<string>> _variantsCache =
            new Dictionary<string, IReadOnlyCollection<string>>();

        // DeletionJunctions(underlying) is likewise a pure function of the fixed strata/alphabet, but
        // FstTemplateAnalyzer.BuildDeletionJunctionArcs calls it per prefix-affix allomorph PER
        // derivation-layer build PER depth level (Phase H, FST_FULL_GRAMMAR_PLAN.md) - the same handful
        // of distinct affix strings get re-probed dozens of times over on a grammar with many templates.
        // Memoize for the same reason Variants is memoized.
        private readonly Dictionary<string, IReadOnlyCollection<(string, FeatureStruct)>> _deletionJunctionsCache =
            new Dictionary<string, IReadOnlyCollection<(string, FeatureStruct)>>();

        // Capability gates (Phase H): computed once from the grammar's own rule shapes, not the
        // alphabet, so they're free to check on every call. A grammar with no phonological rules at
        // all (e.g. Sena) can never alter a surface form, so Variants degenerates to identity with zero
        // probing; one with no deletion-shaped subrule (empty Rhs) can never delete a neighbor, so
        // DeletionJunctions' alphabet/alphabet^2 probing - which previously ran to exhaustion finding
        // nothing on exactly such a grammar - is skipped entirely.
        private readonly bool _anyPhonologicalRules;
        private readonly bool _anyDeletionSubrule;

        public SurfacePhonology(Language language, Morpher morpher)
        {
            _table = language.SurfaceStratum.CharacterDefinitionTable;
            _surfaceStratum = language.SurfaceStratum;
            _strataPrules = new List<LinearRuleCascade<Word, int>>();
            foreach (Stratum stratum in language.Strata)
            {
                _strataPrules.Add(
                    new LinearRuleCascade<Word, int>(
                        stratum.PhonologicalRules.Select(p => p.CompileSynthesisRule(morpher))
                    )
                );
                foreach (IPhonologicalRule prule in stratum.PhonologicalRules)
                {
                    _anyPhonologicalRules = true;
                    if (prule is RewriteRule rewrite)
                    {
                        foreach (RewriteSubrule subrule in rewrite.Subrules)
                        {
                            if (!subrule.Rhs.Children.Any())
                            {
                                _anyDeletionSubrule = true;
                            }
                        }
                    }
                }
            }
            // The surface alphabet: one representative per segment character definition (the neighbor
            // segments used to probe boundary-conditioned alternations).
            _alphabet = new List<string>();
            foreach (CharacterDefinition cd in _table)
            {
                if (cd.Type == HCFeatureSystem.Segment)
                {
                    string rep = cd.Representations.FirstOrDefault();
                    if (!string.IsNullOrEmpty(rep))
                    {
                        _alphabet.Add(rep);
                    }
                }
            }
        }

        /// <summary>The distinct surface realizations of <paramref name="underlying"/> — its isolation
        /// form (always included, so the 0-phonology path is unchanged) plus each boundary-context
        /// realization recovered when the rule is length-preserving.</summary>
        public IReadOnlyCollection<string> Variants(string underlying)
        {
            if (_variantsCache.TryGetValue(underlying, out IReadOnlyCollection<string> cached))
            {
                return cached;
            }
            IReadOnlyCollection<string> computed = ComputeVariants(underlying);
            _variantsCache[underlying] = computed;
            return computed;
        }

        private IReadOnlyCollection<string> ComputeVariants(string underlying)
        {
            if (!_anyPhonologicalRules)
            {
                return new[] { underlying }; // no rule exists ⇒ identity is exact, not an approximation
            }
            var result = new HashSet<string> { underlying };
            int underlyingLen = NodeCount(underlying);
            if (underlyingLen < 0)
            {
                return new[] { underlying }; // unsegmentable
            }

            // C-internal: the morpheme in isolation.
            string isolation = SurfaceOf(underlying);
            if (isolation != null)
            {
                result.Add(isolation);
            }

            // C-boundary: the morpheme with one neighbor segment on each side. When the context is
            // length-preserving, read back just the morpheme's own surface nodes.
            foreach (string c in _alphabet)
            {
                AddBoundaryVariant(c + underlying, underlyingLen, fromEnd: true, result); // left neighbor
                AddBoundaryVariant(underlying + c, underlyingLen, fromEnd: false, result); // right neighbor
            }
            return result.ToList();
        }

        private void AddBoundaryVariant(string context, int underlyingLen, bool fromEnd, HashSet<string> result)
        {
            List<ShapeNode> outNodes = SurfaceNodes(context);
            if (outNodes == null || outNodes.Count != underlyingLen + 1)
            {
                return; // unsegmentable, or a length-changing rule fired ⇒ no reliable morpheme portion
            }
            // The neighbor is one node; the morpheme is the remaining contiguous nodes.
            IEnumerable<ShapeNode> morphemeNodes = fromEnd
                ? outNodes.Skip(1) // left neighbor consumed the first node
                : outNodes.Take(underlyingLen); // right neighbor is the last node
            string rendered = RenderNodes(morphemeNodes);
            if (rendered != null)
            {
                result.Add(rendered);
            }
        }

        /// <summary>Render nodes to their surface string, OMITTING any <c>IsDeleted()</c> node (HC marks a
        /// deletion rather than physically removing the node — see <c>PhonologyRuleCompiler</c>'s remarks
        /// — so a naive render would still print the pre-deletion segment). This is what lets a
        /// deletion-shortened morpheme (e.g. Indonesian's meN- nasal deleting before a sonorant root) show
        /// up as a genuinely shorter variant instead of silently reproducing the underlying form. Returns
        /// null if some surviving node has no single representation.</summary>
        private string RenderNodes(IEnumerable<ShapeNode> nodes)
        {
            var sb = new System.Text.StringBuilder();
            foreach (ShapeNode node in nodes)
            {
                if (node.IsDeleted())
                {
                    continue;
                }
                string rep = _table.GetMatchingStrReps(node).FirstOrDefault();
                if (string.IsNullOrEmpty(rep))
                {
                    return null; // an under-specified node has no single representation — skip this context
                }
                sb.Append(rep);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Junction probe (Phase C, FST_FULL_GRAMMAR_PLAN.md): for each alphabet representative as the
        /// RIGHT neighbor of <paramref name="underlying"/>, reports every case where the real synthesis
        /// cascade deletes that NEIGHBOR itself (not just changes the morpheme's own segments) — e.g.
        /// Indonesian's meN- causing a following voiceless obstruent to delete after nasal assimilation.
        /// <see cref="Variants"/> already handles a neighbor that survives unchanged or whose OWN
        /// substitution is length-preserving; this method is the complementary case where the neighbor
        /// disappears. Each result pairs the morpheme's own resulting surface (skipping any deleted node
        /// within its own span too, so a length-preserving deletion inside the morpheme itself — e.g. the
        /// nasal deleting before a sonorant root — is also captured here without double-counting against
        /// <see cref="Variants"/>) with the neighbor's own underlying <see cref="FeatureStruct"/>, so the
        /// caller can build-time-gate a "the next real segment was deleted" arc to roots whose own leading
        /// segment actually unifies with it — never a general runtime mechanism, just a bounded lookup
        /// table keyed by the alphabet (size ~dozens), not by the lexicon.
        ///
        /// Tries a single trailing neighbor first (enough for a rule whose own environment needs nothing
        /// beyond the deleted segment); if that finds nothing for a given candidate, falls back to probing
        /// WITH A SECOND trailing segment too — some rules (Indonesian's voiceless-obstruent deletion)
        /// require a further segment of right context (e.g. "and then a vowel") to satisfy their own
        /// environment, which a single neighbor can never supply. Bounded by alphabet² in the worst case
        /// (dozens², not lexicon-sized).
        /// </summary>
        public IReadOnlyCollection<(string AffixSurface, FeatureStruct DeletedNeighbor)> DeletionJunctions(
            string underlying
        )
        {
            if (_deletionJunctionsCache.TryGetValue(underlying, out var cached))
            {
                return cached;
            }
            IReadOnlyCollection<(string, FeatureStruct)> computed = ComputeDeletionJunctions(underlying);
            _deletionJunctionsCache[underlying] = computed;
            return computed;
        }

        private IReadOnlyCollection<(string AffixSurface, FeatureStruct DeletedNeighbor)> ComputeDeletionJunctions(
            string underlying
        )
        {
            var result = new List<(string, FeatureStruct)>();
            if (!_anyDeletionSubrule)
            {
                return result; // no rule can ever delete a segment ⇒ nothing to find, by construction
            }
            int underlyingLen = NodeCount(underlying);
            if (underlyingLen < 0)
            {
                return result;
            }
            foreach (string c1 in _alphabet)
            {
                if (TryProbeDeletion(underlying, c1, null, underlyingLen, out var hit))
                {
                    result.Add(hit);
                    continue;
                }
                foreach (string c2 in _alphabet)
                {
                    if (TryProbeDeletion(underlying, c1, c2, underlyingLen, out var hit2))
                    {
                        result.Add(hit2);
                        break; // one confirming c2 is enough to know c1's class deletes in SOME context
                    }
                }
            }
            return result;
        }

        private bool TryProbeDeletion(
            string underlying,
            string c1,
            string c2,
            int underlyingLen,
            out (string AffixSurface, FeatureStruct DeletedNeighbor) hit
        )
        {
            hit = default;
            int extra = c2 == null ? 1 : 2;
            List<ShapeNode> outNodes = SurfaceNodes(underlying + c1 + c2);
            if (outNodes == null || outNodes.Count != underlyingLen + extra)
            {
                return false; // unsegmentable, or a length-changing rule fired elsewhere in the window
            }
            if (!outNodes[underlyingLen].IsDeleted())
            {
                return false; // c1 survived — Variants() already covers that case
            }
            string affixSurface = RenderNodes(outNodes.Take(underlyingLen));
            if (affixSurface == null)
            {
                return false;
            }
            CharacterDefinition cd = _table.FirstOrDefault(d =>
                d.Type == HCFeatureSystem.Segment && d.Representations.Contains(c1)
            );
            if (cd == null)
            {
                return false;
            }
            hit = (affixSurface, cd.FeatureStruct);
            return true;
        }

        /// <summary>Apply forward phonology to a segment string and return the surface string, or null if
        /// it cannot be segmented.</summary>
        private string SurfaceOf(string underlying)
        {
            List<ShapeNode> nodes = SurfaceNodes(underlying);
            return nodes == null ? null : RenderNodes(nodes);
        }

        /// <summary>Apply forward phonology to a segment string and return the surface segment nodes, or
        /// null if it cannot be segmented.</summary>
        private List<ShapeNode> SurfaceNodes(string str)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(str);
            }
            catch (InvalidShapeException)
            {
                return null;
            }
            var word = new Word(_surfaceStratum, shape);
            foreach (LinearRuleCascade<Word, int> cascade in _strataPrules)
            {
                word = cascade.Apply(word).DefaultIfEmpty(word).First();
            }
            return word.Shape.Where(n => n.Annotation.Type() == HCFeatureSystem.Segment).ToList();
        }

        /// <summary>The number of segment nodes after segmentation (before any phonology), or -1 if the
        /// string cannot be segmented. This is the reference length for boundary extraction: a neighbor
        /// adds exactly one node, so a length-preserving context yields <c>underlyingLen + 1</c> nodes.</summary>
        private int NodeCount(string str)
        {
            Shape shape;
            try
            {
                shape = _table.Segment(str);
            }
            catch (InvalidShapeException)
            {
                return -1;
            }
            return shape.Count(n => n.Annotation.Type() == HCFeatureSystem.Segment);
        }
    }
}
