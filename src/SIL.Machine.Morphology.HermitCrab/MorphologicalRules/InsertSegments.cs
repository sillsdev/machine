using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public class InsertSegments : MorphologicalOutputAction
	{
		private readonly Segments _segments;

		public InsertSegments(CharacterDefinitionTable table, string representation, Shape shape)
			: this(new Segments(table, representation, shape))
		{
		}

		public InsertSegments(CharacterDefinitionTable table, string representation)
			: this(new Segments(table, representation))
		{
		}

		public InsertSegments(Segments segments)
			: base(null)
		{
			_segments = segments;
		}

		public Segments Segments
		{
			get { return _segments; }
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IDictionary<string, Pattern<Word, ShapeNode>> partLookup, IDictionary<string, int> capturedParts)
		{
			foreach (ShapeNode node in _segments.Shape)
			{
				if (node.Annotation.Type() != HCFeatureSystem.Boundary)
					analysisLhs.Children.Add(new Constraint<Word, ShapeNode>(node.Annotation.FeatureStruct));
			}
		}

		public override IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output)
		{
			Shape shape = _segments.Shape;
			var mappings = new List<Tuple<ShapeNode, ShapeNode>>();
			Span<ShapeNode> outputSpan = shape.CopyTo(shape.SpanFactory.Create(shape.First, shape.Last), output.Shape);
			foreach (ShapeNode outputNode in output.Shape.GetNodes(outputSpan))
				mappings.Add(Tuple.Create((ShapeNode) null, outputNode));
			return mappings;
		}

		public override string ToString()
		{
			return _segments.ToString();
		}
	}
}
