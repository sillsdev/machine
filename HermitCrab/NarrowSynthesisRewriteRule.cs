using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

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
				ShapeNode newNode = CreateNodeFromConstraint(constraint, match.VariableBindings);
				input.Shape.Insert(newNode, curNode, Direction);
				curNode = newNode;
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
