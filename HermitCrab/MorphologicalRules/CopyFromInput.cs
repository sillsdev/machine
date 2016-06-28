using System;
using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class CopyFromInput : MorphologicalOutputAction
	{
		public CopyFromInput(string partName)
			: base(partName)
		{
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup, IDictionary<string, int> capturedParts)
		{
			Pattern<Word, ShapeNode> pattern = partLookup[PartName];
			int count = capturedParts.GetValue(PartName, () => 0);
			string groupName = AnalysisMorphologicalTransform.GetGroupName(PartName, count);
			analysisLhs.Children.Add(new Group<Word, ShapeNode>(groupName, pattern.Children.DeepCloneExceptBoundaries()));
			capturedParts[PartName]++;
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
			GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[PartName];
			if (inputGroup.Success)
			{
				foreach (ShapeNode inputNode in match.Input.Shape.GetNodes(inputGroup.Span))
				{
					ShapeNode outputNode = inputNode.DeepClone();
					output.Shape.Add(outputNode);
					mappings.Add(Tuple.Create(inputNode, outputNode));
				}
			}
			return mappings;
		}

		public override string ToString()
		{
			return string.Format("<{0}>", PartName);
		}
	}
}
