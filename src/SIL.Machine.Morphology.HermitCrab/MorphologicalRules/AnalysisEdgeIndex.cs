using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    /// <summary>
    /// Numbers the distinct segment feature structures of a language's character definitions, so that an analysis
    /// affix rule can precompute, per word edge, the segments on which every one of its allomorphs fails the edge
    /// prefilter, and skip the whole rule with one lookup.
    /// </summary>
    /// <remarks>
    /// An edge node gets a number only when it is a mandatory segment whose frozen feature structure equals a
    /// character definition's, apart from the Modified flag shape nodes carry. Such a node is at least as specific as
    /// the definition, so it fails every edge constraint the definition fails, and a rule skipped by the index is a
    /// rule whose every allomorph the edge prefilter rejects. Nodes left underspecified by phonological rule
    /// unapplication, natural-class nodes and optional nodes get no number and are never skipped.
    /// </remarks>
    internal sealed class AnalysisEdgeIndex
    {
        internal const int NoSegment = -1;

        private readonly Dictionary<FeatureStruct, int> _ids;
        private readonly List<FeatureStruct> _segments;

        public AnalysisEdgeIndex(Language language)
        {
            _ids = new Dictionary<FeatureStruct, int>(FreezableEqualityComparer<FeatureStruct>.Default);
            _segments = new List<FeatureStruct>();
            foreach (Stratum stratum in language.Strata)
            {
                if (stratum.CharacterDefinitionTable == null)
                    continue;
                foreach (CharacterDefinition cd in stratum.CharacterDefinitionTable)
                {
                    if (cd.Type != HCFeatureSystem.Segment)
                        continue;
                    FeatureStruct fs = Frozen(cd.FeatureStruct, null);
                    if (_ids.ContainsKey(fs))
                        continue;
                    int id = _segments.Count;
                    _segments.Add(fs);
                    _ids[fs] = id;
                    if (fs.ContainsFeature(HCFeatureSystem.Modified))
                        continue;
                    _ids[Frozen(fs, HCFeatureSystem.Clean)] = id;
                    _ids[Frozen(fs, HCFeatureSystem.Dirty)] = id;
                }
            }
        }

        public IReadOnlyList<FeatureStruct> Segments => _segments;

        private static FeatureStruct Frozen(FeatureStruct fs, FeatureSymbol modified)
        {
            FeatureStruct clone = fs.Clone();
            if (modified != null)
                clone.AddValue(HCFeatureSystem.Modified, modified);
            clone.Freeze();
            return clone;
        }

        internal static ShapeNode FindEdge(Shape shape, bool first)
        {
            ShapeNode node = first ? shape.First : shape.Last;
            ShapeNode end = first ? shape.End : shape.Begin;
            while (node != null && node != end && node.Type() != HCFeatureSystem.Segment)
                node = first ? node.Next : node.Prev;
            return node == end ? null : node;
        }

        /// <summary>
        /// Returns the segment numbers of the word's first and last segments, or <see cref="NoSegment"/> for an
        /// edge that the index cannot vouch for. The result is cached on a frozen word.
        /// </summary>
        public (int Left, int Right) GetEdgeSegments(Word word)
        {
            EdgeSegments cached = word.EdgeSegments;
            if (cached != null && cached.Index == this)
                return (cached.Left, cached.Right);
            var edges = new EdgeSegments(this, IdOf(FindEdge(word.Shape, true)), IdOf(FindEdge(word.Shape, false)));
            if (word.IsFrozen)
                word.EdgeSegments = edges;
            return (edges.Left, edges.Right);
        }

        private int IdOf(ShapeNode node)
        {
            if (node == null || node.Annotation.Optional || !node.Annotation.FeatureStruct.IsFrozen)
                return NoSegment;
            return _ids.TryGetValue(node.Annotation.FeatureStruct, out int id) ? id : NoSegment;
        }

        /// <summary>
        /// Immutable, so frozen words shared across analysis threads can publish it without locking.
        /// </summary>
        internal sealed class EdgeSegments
        {
            public EdgeSegments(AnalysisEdgeIndex index, int left, int right)
            {
                Index = index;
                Left = left;
                Right = right;
            }

            public AnalysisEdgeIndex Index { get; }
            public int Left { get; }
            public int Right { get; }
        }
    }
}
