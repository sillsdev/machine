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
            Pattern<Word, int> analysisLhs,
            IDictionary<string, Pattern<Word, int>> partLookup,
            IDictionary<string, int> capturedParts
        )
        {
            Pattern<Word, int> pattern = partLookup[PartName];
            int count = capturedParts.GetOrCreate(PartName, () => 0);
            string groupName = AnalysisMorphologicalTransform.GetGroupName(PartName, count);
            var group = new Group<Word, int>(groupName, pattern.Children.DeepCloneExceptBoundaries());
            foreach (
                Constraint<Word, int> constraint in group
                    .GetNodesDepthFirst()
                    .OfType<Constraint<Word, int>>()
                    .Where(c => c.Type() == (FeatureSymbol)_simpleCtxt.FeatureStruct.GetValue(HCFeatureSystem.Type))
            )
            {
                constraint.FeatureStruct.PriorityUnion(_simpleCtxt.FeatureStruct);
            }
            analysisLhs.Children.Add(group);
            capturedParts[PartName]++;
        }

        public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, int> match, Word output)
        {
            var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
            GroupCapture<int> inputGroup = match.GroupCaptures[PartName];
            foreach (
                ShapeNode inputNode in GetSkippedOptionalNodes(match.Input.Shape, inputGroup.Range)
                    .Concat(match.Input.Shape.GetNodes(inputGroup.Range))
            )
            {
                ShapeNode outputNode = inputNode.Clone();
                if (
                    outputNode.Annotation.Type()
                    == (FeatureSymbol)_simpleCtxt.FeatureStruct.GetValue(HCFeatureSystem.Type)
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
