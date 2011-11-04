using System.Collections.Generic;
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
			var newNode = new ShapeNode(_constraint.Type, output.Shape.SpanFactory, _constraint.FeatureStruct.Clone());
			newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
			if (newNode.Annotation.FeatureStruct.HasVariables)
				throw new MorphException(MorphErrorCode.UninstantiatedFeature);
			output.Shape.Add(newNode);
			AddMorphAnnotation(output, newNode, newNode, allomorph);
		}
	}
}
