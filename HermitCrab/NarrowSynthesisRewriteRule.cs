using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class NarrowSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<Word, ShapeNode> _rhs; 
		private readonly int _targetCount;

		public NarrowSynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Direction dir, ApplicationMode appMode, Expression<Word, ShapeNode> lhs,
			Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv,
			FeatureStruct applicableFS)
			: base(spanFactory, dir, appMode, lhs, leftEnv, rightEnv, applicableFS)
		{
			_rhs = rhs;
			_targetCount = lhs.Children.Count;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			Span<ShapeNode> target = match["target"];
			ShapeNode curNode = target.GetEnd(Lhs.Direction);
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(Lhs.Direction))
			{
				var constraint = (Constraint<Word, ShapeNode>) node;
				ShapeNode newNode = CreateNodeFromConstraint(constraint, match.VariableBindings);
				input.Shape.Insert(newNode, curNode, Lhs.Direction);
				curNode = newNode;
			}

			ShapeNode matchStartNode = match.GetStart(Lhs.Direction);
			ShapeNode resumeNode = matchStartNode == target.GetStart(Lhs.Direction) ? target.GetEnd(Lhs.Direction).GetNext(Lhs.Direction) : matchStartNode;
			MarkSearchedNodes(resumeNode, curNode);

			ShapeNode[] nodes = input.Shape.GetNodes(target, Lhs.Direction).ToArray();
			for (int i = 0; i < _targetCount; i++)
				nodes[i].Remove();

			output = input;
			return resumeNode.Annotation;
		}
	}
}
