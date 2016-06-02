using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public class CopyFromInput : MorphologicalOutputAction
	{
		public CopyFromInput(string partName)
			: base(partName)
		{
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup)
		{
			Pattern<Word, ShapeNode> pattern = partLookup[PartName];
			analysisLhs.Children.Add(new Group<Word, ShapeNode>(PartName, pattern.Children.CloneItems()));
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
			GroupCapture<ShapeNode> inputGroup = match.GroupCaptures[PartName];
			if (inputGroup.Success)
			{
				foreach (ShapeNode inputNode in match.Input.Shape.GetNodes(inputGroup.Span))
				{
					ShapeNode outputNode = inputNode.Clone();
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
