using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class ModifyFromInput : MorphologicalOutputAction
	{
		private readonly FeatureStruct _fs;

		public ModifyFromInput(string partName, FeatureStruct fs)
			: base(partName)
		{
			_fs = fs;
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup, IDictionary<string, int> capturedParts)
		{
			Pattern<Word, ShapeNode> pattern = partLookup[PartName];
			int count = capturedParts.GetValue(PartName, () => 0);
			string groupName = AnalysisMorphologicalTransform.GetGroupName(PartName, count);
			var group = new Group<Word, ShapeNode>(groupName, pattern.Children.DeepCloneExceptBoundaries());
			foreach (Constraint<Word, ShapeNode> constraint in group.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type() == (FeatureSymbol) _fs.GetValue(HCFeatureSystem.Type)))
				constraint.FeatureStruct.PriorityUnion(_fs);
			analysisLhs.Children.Add(group);
			capturedParts[PartName]++;
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
			GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[PartName];
			foreach (ShapeNode inputNode in match.Input.Shape.GetNodes(inputGroup.Span))
			{
				ShapeNode outputNode = inputNode.DeepClone();
				if (outputNode.Annotation.Type() == (FeatureSymbol) _fs.GetValue(HCFeatureSystem.Type))
					outputNode.Annotation.FeatureStruct.PriorityUnion(_fs, match.VariableBindings);
				output.Shape.Add(outputNode);
				mappings.Add(Tuple.Create(inputNode, outputNode));
			}
			return mappings;
		}

		public override string ToString()
		{
			return string.Format("<{0}> -> {1}", PartName, _fs);
		}
	}
}
