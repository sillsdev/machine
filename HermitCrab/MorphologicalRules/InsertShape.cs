using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public class InsertShape : MorphologicalOutputAction
	{
		private readonly Shape _shape;
		private readonly SymbolTable _table;

		public InsertShape(SymbolTable table, string shape)
			: this(table, table.Segment(shape))
		{
		}

		public InsertShape(SymbolTable table, Shape shape)
			: base(null)
		{
			_shape = shape;
			_table = table;
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

		public override string ToString()
		{
			return _shape.ToString(_table, true);
		}
	}
}
