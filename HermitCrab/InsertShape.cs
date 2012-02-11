using System.Collections.Generic;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public class InsertShape : MorphologicalOutput
	{
		private readonly Shape _shape;

		public InsertShape(Shape shape)
		{
			_shape = shape;
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Pattern<Word, ShapeNode>> lhs)
		{
			foreach (ShapeNode node in _shape)
			{
				if (node.Annotation.Type() != HCFeatureSystem.BoundaryType)
					analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(node.Annotation.FeatureStruct));
			}
		}

		public override void Apply(Match<Word, ShapeNode> match, Word output, Allomorph allomorph)
		{
			Span<ShapeNode> outputSpan = _shape.CopyTo(_shape.SpanFactory.Create(_shape.First, _shape.Last), output.Shape);
			if (allomorph != null)
				output.MarkMorph(outputSpan, allomorph);
		}
	}
}
