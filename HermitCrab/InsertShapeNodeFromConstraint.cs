using System.Collections.Generic;
using System.Linq;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class InsertShapeNodeFromConstraint : MorphologicalOutput
	{
		private readonly Constraint<Word, ShapeNode> _constraint; 

		public InsertShapeNodeFromConstraint(Constraint<Word, ShapeNode> constraint)
		{
			_constraint = constraint;
		}

		public override void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Expression<Word, ShapeNode>> lhs)
		{
			analysisLhs.Children.Add(_constraint.Clone());
		}

		public override void Apply(PatternMatch<ShapeNode> match, Word input, Word output, Allomorph allomorph)
		{
			if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Where(value => value.Feature.DefaultValue.Equals(value)).Any())
				throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			var newNode = new ShapeNode(_constraint.Type, output.Shape.SpanFactory, _constraint.FeatureStruct.Clone());
			newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
			output.Shape.Add(newNode);
			if (allomorph != null)
				output.MarkMorph(newNode, newNode, allomorph);
		}
	}
}
