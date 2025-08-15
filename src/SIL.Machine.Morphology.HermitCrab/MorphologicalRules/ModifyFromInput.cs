using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public class ModifyFromInput : MorphologicalOutputAction
    {
        private readonly SimpleContext _simpleCtxt;

        internal ModifyFromInput(string partName, FeatureStruct fs, params SymbolicFeatureValue[] variables)
            : this(partName, new SimpleContext(new NaturalClass(fs), variables)) { }

        public ModifyFromInput(string partName, NaturalClass nc, IEnumerable<SymbolicFeatureValue> variables)
            : this(partName, new SimpleContext(nc, variables)) { }

        public ModifyFromInput(string partName, SimpleContext simpleCtxt)
            : base(partName)
        {
            _simpleCtxt = simpleCtxt;
        }

        public SimpleContext SimpleContext
        {
            get { return _simpleCtxt; }
        }

        public override void GenerateAnalysisLhs(
            Pattern<Word, ShapeNode> analysisLhs,
            IDictionary<string, Pattern<Word, ShapeNode>> partLookup,
            IDictionary<string, int> capturedParts
        )
        {
            Pattern<Word, ShapeNode> pattern = partLookup[PartName];
            int count = capturedParts.GetOrCreate(PartName, () => 0);
            string groupName = AnalysisMorphologicalTransform.GetGroupName(PartName, count);
            var group = new Group<Word, ShapeNode>(groupName, pattern.Children.DeepCloneExceptBoundaries());
            foreach (
                Constraint<Word, ShapeNode> constraint in group
                    .GetNodesDepthFirst()
                    .OfType<Constraint<Word, ShapeNode>>()
                    .Where(c =>
                        c.Type()
                        == SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                            _simpleCtxt.FeatureStruct,
                            HCFeatureSystem.Type
                        )
                    )
            )
            {
                constraint.FeatureStruct.PriorityUnion(_simpleCtxt.FeatureStruct);
            }
            analysisLhs.Children.Add(group);
            capturedParts[PartName]++;
        }

        public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
        {
            var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
            GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[PartName];
            foreach (
                ShapeNode inputNode in GetSkippedOptionalNodes(match.Input.Shape, inputGroup.Range)
                    .Concat(match.Input.Shape.GetNodes(inputGroup.Range))
            )
            {
                ShapeNode outputNode = inputNode.Clone();
                if (
                    outputNode.Annotation.Type()
                    == SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                        _simpleCtxt.FeatureStruct,
                        HCFeatureSystem.Type
                    )
                )
                {
                    outputNode.Annotation.FeatureStruct.PriorityUnion(
                        _simpleCtxt.FeatureStruct,
                        match.VariableBindings
                    );
                }
                output.Shape.Add(outputNode);
                mappings.Add(Tuple.Create(inputNode, outputNode));
            }
            return mappings;
        }

        public override string ToString()
        {
            return string.Format("<{0}> -> {1}", PartName, _simpleCtxt.FeatureStruct);
        }
    }
}
