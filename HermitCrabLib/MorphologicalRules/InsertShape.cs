using System;
using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class InsertShape : MorphologicalOutputAction
	{
		private readonly Shape _shape;

		public InsertShape(Shape shape)
			: base(null)
		{
			_shape = shape;
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup)
		{
			foreach (ShapeNode node in _shape)
			{
				if (node.Annotation.Type() != HCFeatureSystem.Boundary)
					analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(node.Annotation.FeatureStruct));
			}
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
			Span<ShapeNode> outputSpan = _shape.CopyTo(_shape.SpanFactory.Create(_shape.First, _shape.Last), output.Shape);
			foreach (ShapeNode outputNode in output.Shape.GetNodes(outputSpan))
				mappings.Add(Tuple.Create((ShapeNode) null, outputNode));
			return mappings;
		}
	}
}
