using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class AnalysisMorphologicalTransform
    {
        private readonly Pattern<Word, ShapeNode> _pattern;
        private readonly Dictionary<string, Tuple<int, FeatureStruct>> _modifyFromInfos;
        private readonly Dictionary<string, int> _capturedParts;

        public AnalysisMorphologicalTransform(
            IEnumerable<Pattern<Word, ShapeNode>> lhs,
            IList<MorphologicalOutputAction> rhs
        )
        {
            Dictionary<string, Pattern<Word, ShapeNode>> partLookup = lhs.ToDictionary(p => p.Name);
            _modifyFromInfos = new Dictionary<string, Tuple<int, FeatureStruct>>();
            _pattern = new Pattern<Word, ShapeNode>();
            _capturedParts = new Dictionary<string, int>();
            foreach (MorphologicalOutputAction outputAction in rhs)
            {
                outputAction.GenerateAnalysisLhs(_pattern, partLookup, _capturedParts);

                if (outputAction is ModifyFromInput modifyFromInput)
                {
                    _modifyFromInfos[modifyFromInput.PartName] = Tuple.Create(
                        _capturedParts[modifyFromInput.PartName] - 1,
                        modifyFromInput.SimpleContext.FeatureStruct.AntiFeatureStruct()
                    );
                }
            }
        }

        internal static string GetGroupName(string partName, int index)
        {
            return string.Format("{0}_{1}", partName, index);
        }

        protected IDictionary<string, int> CapturedParts
        {
            get { return _capturedParts; }
        }

        internal bool HasRepeatedParts
        {
            get { return _capturedParts.Values.Any(count => count >= 2); }
        }

        /// <summary>
        /// Synthesis writes every copy of a part from the same input, and anything that later changes one
        /// copy is unapplied before this rule, so copies proven to disagree segment by segment cannot lead
        /// to a valid analysis. An optional segment (an unapplied deletion) stands for any number of segments
        /// it unifies with, because <see cref="Morpher.DeletionReapplications"/> can restore fewer segments
        /// than were deleted, so copies disagree only if no such choice lines them up. A part the rule
        /// modifies, or a copy with an optional node that is not a segment, is never reported as disagreeing.
        /// </summary>
        internal bool HasDisagreeingCopies(Match<Word, ShapeNode> match)
        {
            foreach (KeyValuePair<string, int> capturedPart in _capturedParts)
            {
                if (capturedPart.Value < 2 || _modifyFromInfos.ContainsKey(capturedPart.Key))
                    continue;

                if (
                    TryGetSegmentCopies(match, capturedPart.Key, capturedPart.Value, out List<List<ShapeNode>> copies)
                    && AnyCopiesDisagree(copies)
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetSegmentCopies(
            Match<Word, ShapeNode> match,
            string partName,
            int copyCount,
            out List<List<ShapeNode>> copies
        )
        {
            copies = new List<List<ShapeNode>>(copyCount);
            for (int i = 0; i < copyCount; i++)
            {
                GroupCapture<ShapeNode> capture = match.GroupCaptures[GetGroupName(partName, i)];
                if (!capture.Success)
                    return false;

                // an unapplied word-initial deletion sits before the capture, not inside it
                List<ShapeNode> nodes = MorphologicalOutputAction
                    .GetSkippedOptionalNodes(match.Input.Shape, capture.Range)
                    .Concat(match.Input.Shape.GetNodes(capture.Range))
                    .ToList();
                if (nodes.Any(node => node.Annotation.Optional && node.Annotation.Type() != HCFeatureSystem.Segment))
                    return false;

                copies.Add(nodes.Where(node => node.Annotation.Type() == HCFeatureSystem.Segment).ToList());
            }
            return true;
        }

        // unifiability is not transitive, so every pair is compared, not just each copy against the first
        private static bool AnyCopiesDisagree(List<List<ShapeNode>> copies)
        {
            for (int i = 0; i < copies.Count; i++)
            {
                for (int j = i + 1; j < copies.Count; j++)
                {
                    if (!CanAlign(copies[i], copies[j]))
                        return true;
                }
            }
            return false;
        }

        private static bool CanAlign(List<ShapeNode> first, List<ShapeNode> second)
        {
            if (!first.Any(node => node.Annotation.Optional) && !second.Any(node => node.Annotation.Optional))
            {
                if (first.Count != second.Count)
                    return false;
                for (int k = 0; k < first.Count; k++)
                {
                    if (!first[k].Annotation.FeatureStruct.IsUnifiable(second[k].Annotation.FeatureStruct))
                        return false;
                }
                return true;
            }

            // reachable[i, j]: the first i nodes of one copy can be lined up with the first j of the other;
            // an optional node is passed over, or absorbs a node of the other copy and stays available
            var reachable = new bool[first.Count + 1, second.Count + 1];
            reachable[0, 0] = true;
            for (int i = 0; i <= first.Count; i++)
            {
                for (int j = 0; j <= second.Count; j++)
                {
                    if (!reachable[i, j])
                        continue;
                    bool firstOptional = i < first.Count && first[i].Annotation.Optional;
                    bool secondOptional = j < second.Count && second[j].Annotation.Optional;
                    if (firstOptional)
                        reachable[i + 1, j] = true;
                    if (secondOptional)
                        reachable[i, j + 1] = true;
                    if (
                        i < first.Count
                        && j < second.Count
                        && first[i].Annotation.FeatureStruct.IsUnifiable(second[j].Annotation.FeatureStruct)
                    )
                    {
                        reachable[i + 1, j + 1] = true;
                        if (firstOptional)
                            reachable[i, j + 1] = true;
                        if (secondOptional)
                            reachable[i + 1, j] = true;
                    }
                }
            }
            return reachable[first.Count, second.Count];
        }

        public Pattern<Word, ShapeNode> Pattern
        {
            get { return _pattern; }
        }

        public void GenerateShape(IList<Pattern<Word, ShapeNode>> lhs, Shape shape, Match<Word, ShapeNode> match)
        {
            shape.Clear();
            foreach (Pattern<Word, ShapeNode> part in lhs)
                AddPartNodes(part, match, shape);
        }

        private void AddPartNodes(Pattern<Word, ShapeNode> part, Match<Word, ShapeNode> match, Shape output)
        {
            int count;
            if (_capturedParts.TryGetValue(part.Name, out count))
            {
                Tuple<int, FeatureStruct> modifyFromInfo;
                if (_modifyFromInfos.TryGetValue(part.Name, out modifyFromInfo))
                {
                    if (AddCapturedPartNodes(part.Name, modifyFromInfo.Item1, match, modifyFromInfo.Item2, output))
                        return;
                }

                for (int i = 0; i < count; i++)
                {
                    if (AddCapturedPartNodes(part.Name, i, match, null, output))
                        return;
                }
            }

            Untruncate(part, output, false, match.VariableBindings);
        }

        private bool AddCapturedPartNodes(
            string partName,
            int index,
            Match<Word, ShapeNode> match,
            FeatureStruct modifyFromFS,
            Shape output
        )
        {
            GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[GetGroupName(partName, index)];
            if (inputGroup.Success)
            {
                Range<ShapeNode> outputRange = match.Input.Shape.CopyTo(inputGroup.Range, output);
                if (modifyFromFS != null)
                {
                    foreach (ShapeNode node in output.GetNodes(outputRange))
                    {
                        if ((FeatureSymbol)modifyFromFS.GetValue(HCFeatureSystem.Type) == node.Annotation.Type())
                            node.Annotation.FeatureStruct.Add(modifyFromFS, match.VariableBindings);
                    }
                }
                return true;
            }
            return false;
        }

        private void Untruncate(
            PatternNode<Word, ShapeNode> patternNode,
            Shape output,
            bool optional,
            VariableBindings varBindings
        )
        {
            foreach (PatternNode<Word, ShapeNode> node in patternNode.Children)
            {
                if (node is Constraint<Word, ShapeNode> constraint && constraint.Type() == HCFeatureSystem.Segment)
                {
                    FeatureStruct fs = constraint.FeatureStruct.Clone();
                    fs.ReplaceVariables(varBindings);
                    output.Add(fs, optional);
                }
                else
                {
                    if (node is Quantifier<Word, ShapeNode> quantifier)
                    {
                        for (int i = 0; i < quantifier.MaxOccur; i++)
                            Untruncate(quantifier, output, i >= quantifier.MinOccur, varBindings);
                    }
                    else
                    {
                        Untruncate(node, output, optional, varBindings);
                    }
                }
            }
        }
    }
}
