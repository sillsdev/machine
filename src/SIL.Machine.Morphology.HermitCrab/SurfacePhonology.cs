using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
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
        private readonly List<LinearRuleCascade<Word, ShapeNode>> _strataPrules;
        private readonly List<string> _alphabet;

        public SurfacePhonology(Language language, Morpher morpher)
        {
            _table = language.SurfaceStratum.CharacterDefinitionTable;
            _surfaceStratum = language.SurfaceStratum;
            _strataPrules = new List<LinearRuleCascade<Word, ShapeNode>>();
            foreach (Stratum stratum in language.Strata)
            {
                _strataPrules.Add(
                    new LinearRuleCascade<Word, ShapeNode>(
                        stratum.PhonologicalRules.Select(p => p.CompileSynthesisRule(morpher))
                    )
                );
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
            var sb = new System.Text.StringBuilder();
            foreach (ShapeNode node in morphemeNodes)
            {
                string rep = _table.GetMatchingStrReps(node).FirstOrDefault();
                if (string.IsNullOrEmpty(rep))
                {
                    return; // an under-specified node has no single representation — skip this context
                }
                sb.Append(rep);
            }
            result.Add(sb.ToString());
        }

        /// <summary>Apply forward phonology to a segment string and return the surface string, or null if
        /// it cannot be segmented.</summary>
        private string SurfaceOf(string underlying)
        {
            List<ShapeNode> nodes = SurfaceNodes(underlying);
            if (nodes == null)
            {
                return null;
            }
            var sb = new System.Text.StringBuilder();
            foreach (ShapeNode node in nodes)
            {
                string rep = _table.GetMatchingStrReps(node).FirstOrDefault();
                if (string.IsNullOrEmpty(rep))
                {
                    return null;
                }
                sb.Append(rep);
            }
            return sb.ToString();
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
            foreach (LinearRuleCascade<Word, ShapeNode> cascade in _strataPrules)
            {
                word = cascade.Apply(word).DefaultIfEmpty(word).First();
            }
            return word
                .Shape.Where(n => n.Annotation.Type() == HCFeatureSystem.Segment)
                .ToList();
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
