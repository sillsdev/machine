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
        /// to a valid analysis. A copy containing an optional node (an unapplied deletion), or a part the
        /// rule modifies, cannot be judged and never counts as disagreeing.
        /// </summary>
        internal bool HasDisagreeingCopies(Match<Word, ShapeNode> match)
        {
            foreach (KeyValuePair<string, int> capturedPart in _capturedParts)
            {
                string partName = capturedPart.Key;
                int copyCount = capturedPart.Value;
                if (copyCount < 2)
                    continue;

                if (_modifyFromInfos.ContainsKey(partName))
                    continue;

                var copies = new List<List<ShapeNode>>(copyCount);
                bool partUndecidable = false;
                for (int i = 0; i < copyCount; i++)
                {
                    GroupCapture<ShapeNode> capture = match.GroupCaptures[GetGroupName(partName, i)];
                    if (!capture.Success)
                    {
                        partUndecidable = true;
                        break;
                    }

                    List<ShapeNode> nodes = GetCapturedNodes(match.Input.Shape, capture.Range);
                    if (nodes.Any(node => node.Annotation.Optional))
                    {
                        partUndecidable = true;
                        break;
                    }

                    copies.Add(nodes.Where(node => node.Annotation.Type() == HCFeatureSystem.Segment).ToList());
                }

                if (partUndecidable)
                    continue;

                List<ShapeNode> firstCopy = copies[0];
                for (int i = 1; i < copies.Count; i++)
                {
                    List<ShapeNode> otherCopy = copies[i];
                    if (firstCopy.Count != otherCopy.Count)
                        return true;

                    for (int j = 0; j < firstCopy.Count; j++)
                    {
                        if (!firstCopy[j].Annotation.FeatureStruct.IsUnifiable(otherCopy[j].Annotation.FeatureStruct))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Includes optional nodes the matcher skipped before the capture when they reach the start of the
        /// shape, so an unapplied word-initial deletion is still seen.
        /// </summary>
        private static List<ShapeNode> GetCapturedNodes(Shape shape, Range<ShapeNode> range)
        {
            var nodes = new List<ShapeNode>(MorphologicalOutputAction.SkippedOptionalNodes(shape, range));
            nodes.AddRange(shape.GetNodes(range));
            return nodes;
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
