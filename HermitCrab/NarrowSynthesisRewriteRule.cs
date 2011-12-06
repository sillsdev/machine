using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public class NarrowSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<Word, ShapeNode> _rhs; 
		private readonly int _targetCount;

		public NarrowSynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs,
			Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv,
			FeatureStruct requiredSyntacticFS)
			: base(spanFactory, lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
			_targetCount = lhs.Children.Count;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			Span<ShapeNode> target = match["target"];
			ShapeNode curNode = target.GetEnd(Direction);
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(Direction))
			{
				var constraint = (Constraint<Word, ShapeNode>) node;
				if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Where(value => value.Feature.DefaultValue.Equals(value)).Any())
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
				FeatureStruct fs = constraint.FeatureStruct.Clone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = input.Shape.Insert(curNode, constraint.Type, fs);
			}

			ShapeNode matchStartNode = match.GetStart(Direction);
			ShapeNode resumeNode = matchStartNode == target.GetStart(Direction) ? target.GetEnd(Direction).GetNext(Direction) : matchStartNode;
			MarkSearchedNodes(resumeNode, curNode);

			ShapeNode[] nodes = input.Shape.GetNodes(target, Direction).ToArray();
			for (int i = 0; i < _targetCount; i++)
				nodes[i].Remove();

			output = input;
			return resumeNode.Annotation;
		}
	}
}
