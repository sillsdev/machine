using System.Collections.Generic;
using System.Globalization;
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

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Pattern<Word, ShapeNode>> lhs)
		{
			Pattern<Word, ShapeNode> pattern = lhs[_index];
			var group = new Group<Word, ShapeNode>(_index.ToString(CultureInfo.InvariantCulture), pattern.Children.Clone());
			foreach (Constraint<Word, ShapeNode> constraint in group.GetNodesDepthFirst().OfType<Constraint<Word, ShapeNode>>().Where(c => c.Type() == _constraint.Type()))
				constraint.FeatureStruct.PriorityUnion(_constraint.FeatureStruct);
			analysisLhs.Children.Add(group);
		}

		public override void Apply(Match<Word, ShapeNode> match, Word output, Allomorph allomorph)
		{
			GroupCapture<ShapeNode> inputGroup = match[_index.ToString(CultureInfo.InvariantCulture)];
			Span<ShapeNode> outputSpan = match.Input.CopyTo(inputGroup.Span, output);
			foreach (ShapeNode outputNode in output.Shape.GetNodes(outputSpan))
			{
				if (outputNode.Annotation.Type() == _constraint.Type())
					outputNode.Annotation.FeatureStruct.PriorityUnion(_constraint.FeatureStruct, match.VariableBindings);
			}
			if (allomorph != null)
				output.MarkMorph(outputSpan, allomorph);
		}
	}
}
