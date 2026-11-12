using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Decides from a word's shape whether analysis can still unapply a partial rule below it. Within a stratum
    /// analysis unapplies the phonological rules once, on entry, and afterwards only morphological rules. When every
    /// morphological rule of the stratum uses only <see cref="CopyFromInput"/> and <see cref="InsertSegments"/>,
    /// each node of every later shape is a clone of a node of the current shape, or a node
    /// <c>AnalysisMorphologicalTransform</c> rebuilds from the pattern of a part that was deleted or can be empty.
    /// A partial rule unapplies only if each segment it inserts unifies with a node of the shape at that time. The
    /// look-ahead is off for a stratum when a rule can edit copied nodes (<see cref="ModifyFromInput"/>,
    /// <see cref="InsertSimpleContext"/>, an unknown rule type), when a partial rule lives in a deeper stratum
    /// whose phonological rules would change the shape, or when a partial rule inserts no segment.
    /// </summary>
    internal sealed class PartialRuleLookAhead
    {
        private sealed class StratumMaterial
        {
            // Every rule of the stratum leaves a contiguous run of its input and rebuilds no node that a partial
            // rule's inserted segment unifies with, so the inserted segments must already stand side by side.
            public bool Contiguous;

            // Per partial-rule allomorph, the runs of segments it inserts.
            public FeatureStruct[][][] Runs;

            // Per segment of those runs, whether a rule of the stratum can rebuild a node that it unifies with.
            public bool[][][] Rebuildable;
        }

        private sealed class Transform
        {
            // The parts whose nodes make up the unapplied shape.
            public IList<Pattern<Word, ShapeNode>> Parts;
            public IList<MorphologicalOutputAction> Rhs;
        }

        private readonly Dictionary<Stratum, StratumMaterial> _material = new Dictionary<Stratum, StratumMaterial>();

        public PartialRuleLookAhead(Language lang)
        {
            bool partialInDeeperStratum = false;
            foreach (Stratum stratum in lang.Strata)
            {
                AffixProcessRule[] partial = stratum
                    .MorphologicalRules.OfType<AffixProcessRule>()
                    .Where(r => r.IsPartial && !r.IsTemplateRule && !r.GetIsCliticRule())
                    .ToArray();
                if (!partialInDeeperStratum && partial.Length > 0)
                    _material[stratum] = Inspect(stratum, partial);
                partialInDeeperStratum |= partial.Length > 0;
            }
        }

        /// <summary>
        /// False when no partial rule can be unapplied to this word or to anything analysis derives from it in
        /// its stratum.
        /// </summary>
        public bool PartialRuleMayUnapply(Word input)
        {
            if (!_material.TryGetValue(input.Stratum, out StratumMaterial material) || material == null)
                return true;
            ShapeNode[] nodes = input.Shape.Where(n => n.Annotation.Type() == HCFeatureSystem.Segment).ToArray();
            if (nodes.Length != input.Shape.Count)
                return true;
            for (int a = 0; a < material.Runs.Length; a++)
            {
                bool possible = true;
                for (int r = 0; possible && r < material.Runs[a].Length; r++)
                {
                    FeatureStruct[] run = material.Runs[a][r];
                    if (material.Contiguous)
                    {
                        possible = RunOccurs(nodes, run);
                        continue;
                    }
                    for (int i = 0; possible && i < run.Length; i++)
                    {
                        possible =
                            material.Rebuildable[a][r][i]
                            || nodes.Any(n => n.Annotation.FeatureStruct.IsUnifiable(run[i], false, null));
                    }
                }
                if (possible)
                    return true;
            }
            return false;
        }

        public bool IsActive => _material.Values.Any(m => m != null);

        private static StratumMaterial Inspect(Stratum stratum, AffixProcessRule[] partial)
        {
            IEnumerable<IMorphologicalRule> rules = stratum
                .MorphologicalRules.Concat(stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules))
                .Distinct();
            var rebuilt = new List<FeatureStruct>();
            int notContiguous = 0;
            foreach (IMorphologicalRule rule in rules)
            {
                IEnumerable<Transform> transforms = Transforms(rule);
                if (transforms == null)
                    return null;
                foreach (Transform t in transforms)
                {
                    if (!Inspect(t, rebuilt, out bool contiguous))
                        return null;
                    if (!contiguous)
                        notContiguous++;
                }
            }

            FeatureStruct[][][] runs = partial
                .SelectMany(r => r.Allomorphs)
                .Select(a =>
                    a.Rhs.OfType<InsertSegments>()
                        .Select(ins =>
                            ins.Segments.Shape.Where(n => n.Annotation.Type() == HCFeatureSystem.Segment)
                                .Select(n => n.Annotation.FeatureStruct)
                                .ToArray()
                        )
                        .Where(run => run.Length > 0)
                        .ToArray()
                )
                .ToArray();
            // An allomorph that inserts nothing can be unapplied to any shape.
            if (runs.Any(allomorphRuns => allomorphRuns.Length == 0))
                return null;

            bool[][][] rebuildable = runs.Select(allomorphRuns =>
                    allomorphRuns
                        .Select(run =>
                            run.Select(fs => rebuilt.Any(c => c.HasVariables || c.IsUnifiable(fs, false, null)))
                                .ToArray()
                        )
                        .ToArray()
                )
                .ToArray();
            return new StratumMaterial
            {
                Runs = runs,
                Rebuildable = rebuildable,
                Contiguous = notContiguous == 0 && !rebuildable.Any(a => a.Any(r => r.Any(x => x))),
            };
        }

        private static IEnumerable<Transform> Transforms(IMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule ap:
                    return ap.Allomorphs.Select(a => new Transform { Parts = a.Lhs, Rhs = a.Rhs });
                case RealizationalAffixProcessRule rp:
                    return rp.Allomorphs.Select(a => new Transform { Parts = a.Lhs, Rhs = a.Rhs });
                case CompoundingRule cr:
                    return cr.Subrules.Select(sr => new Transform { Parts = sr.HeadLhs, Rhs = sr.Rhs });
                default:
                    return null;
            }
        }

        // Unapplying the transform keeps a contiguous run of the input's nodes, in order, and rebuilds only the
        // nodes added to "rebuilt". Returns false if it can do anything else.
        private static bool Inspect(Transform t, List<FeatureStruct> rebuilt, out bool contiguous)
        {
            contiguous = false;
            if (t.Rhs.Any(a => !(a is CopyFromInput) && !(a is InsertSegments)))
                return false;
            List<string> names = t.Parts.Select(p => p.Name).ToList();
            var copies = new List<int>();
            bool insertedAfterCopy = false;
            bool insertedBetween = false;
            foreach (MorphologicalOutputAction action in t.Rhs)
            {
                if (action is CopyFromInput)
                {
                    int index = names.IndexOf(action.PartName);
                    if (index < 0)
                        continue; // a non-head part of a compound
                    if (insertedAfterCopy)
                        insertedBetween = true;
                    copies.Add(index);
                }
                else if (copies.Count > 0)
                {
                    insertedAfterCopy = true;
                }
            }
            for (int i = 0; i < names.Count; i++)
            {
                if (!copies.Contains(i) || !NeverEmpty(t.Parts[i]))
                    CollectSegments(t.Parts[i], rebuilt);
            }
            bool increasing = true;
            for (int i = 1; i < copies.Count; i++)
                increasing &= copies[i] > copies[i - 1];
            contiguous = copies.Distinct().Count() <= 1 || (increasing && !insertedBetween);
            return true;
        }

        // A group over this part always captures at least one node.
        private static bool NeverEmpty(PatternNode<Word, ShapeNode> node)
        {
            foreach (PatternNode<Word, ShapeNode> child in node.Children)
            {
                if (child is Constraint<Word, ShapeNode> c && c.Type() == HCFeatureSystem.Segment)
                    return true;
                if (child is Quantifier<Word, ShapeNode> q)
                {
                    if (q.MinOccur >= 1 && NeverEmpty(q))
                        return true;
                }
                else if (child is Group<Word, ShapeNode> g && NeverEmpty(g))
                {
                    return true;
                }
            }
            return false;
        }

        // Mirrors AnalysisMorphologicalTransform.Untruncate: an unbounded quantifier rebuilds nothing.
        private static void CollectSegments(PatternNode<Word, ShapeNode> node, List<FeatureStruct> into)
        {
            if (node is Constraint<Word, ShapeNode> c && c.Type() == HCFeatureSystem.Segment)
                into.Add(c.FeatureStruct);
            if (node is Quantifier<Word, ShapeNode> q && q.MaxOccur <= 0)
                return;
            foreach (PatternNode<Word, ShapeNode> child in node.Children)
                CollectSegments(child, into);
        }

        private static bool RunOccurs(ShapeNode[] nodes, FeatureStruct[] run)
        {
            for (int start = 0; start < nodes.Length; start++)
            {
                if (RunMatches(nodes, start, run, 0))
                    return true;
            }
            return false;
        }

        // Optional nodes may be passed over once the run has started.
        private static bool RunMatches(ShapeNode[] nodes, int n, FeatureStruct[] run, int r)
        {
            if (r == run.Length)
                return true;
            if (n == nodes.Length)
                return false;
            if (
                nodes[n].Annotation.FeatureStruct.IsUnifiable(run[r], false, null)
                && RunMatches(nodes, n + 1, run, r + 1)
            )
                return true;
            return r > 0 && nodes[n].Annotation.Optional && RunMatches(nodes, n + 1, run, r);
        }
    }
}
