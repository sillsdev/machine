using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

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
			FeatureStruct fs = _constraint.FeatureStruct.Clone();
			fs.ReplaceVariables(match.VariableBindings);
			ShapeNode newNode = output.Shape.Add(_constraint.Type, fs);
			if (allomorph != null)
				output.MarkMorph(newNode, newNode, allomorph);
		}
	}
}
