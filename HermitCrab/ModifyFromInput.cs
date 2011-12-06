using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public class ModifyFromInput : MorphologicalOutput
	{
		private readonly int _index;
		private readonly Constraint<Word, ShapeNode> _constraint;

		public ModifyFromInput(int index, Constraint<Word, ShapeNode> constraint)
		{
			_index = index;
			_constraint = constraint;
		}

		public int Index
		{
			get { return _index; }
		}

		public Constraint<Word, ShapeNode> Constraint
		{
			get { return _constraint; }
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Expression<Word, ShapeNode>> lhs)
		{
			Expression<Word, ShapeNode> expr = lhs[_index];
			var group = new Group<Word, ShapeNode>(_index.ToString(), expr.Children.Clone());
			foreach (Constraint<Word, ShapeNode> constraint in group.GetNodes().OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type == _constraint.Type))
				constraint.FeatureStruct.PriorityUnion(_constraint.FeatureStruct);
			analysisLhs.Children.Add(group);
		}

		public override void Apply(PatternMatch<ShapeNode> match, Word input, Word output, Allomorph allomorph)
		{
			Span<ShapeNode> inputSpan = match[_index.ToString()];
			Span<ShapeNode> outputSpan = input.CopyTo(inputSpan, output);
			foreach (ShapeNode outputNode in output.Shape.GetNodes(outputSpan))
			{
				if (outputNode.Annotation.Type == _constraint.Type)
					outputNode.Annotation.FeatureStruct.PriorityUnion(_constraint.FeatureStruct, match.VariableBindings);
			}
			if (allomorph != null)
				output.MarkMorph(outputSpan, allomorph);
		}
	}
}
