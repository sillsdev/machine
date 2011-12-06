using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	public class EpenthesisSynthesisRewriteRule : SynthesisRewriteRule
	{
		private readonly Expression<Word, ShapeNode> _rhs;

		public EpenthesisSynthesisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs,
			Expression<Word, ShapeNode> rhs, Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv, FeatureStruct requiredSyntacticFS)
			: base(spanFactory, lhs, leftEnv, rightEnv, requiredSyntacticFS)
		{
			_rhs = rhs;
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			if (Direction == Direction.LeftToRight)
			{
				Span<ShapeNode> leftEnv;
				if (match.TryGetGroup("leftEnv", out leftEnv))
				{
					startNode = leftEnv.End;
				}
				else
				{
					Span<ShapeNode> rightEnv = match["rightEnv"];
					startNode = rightEnv.Start.Prev;
				}
			}
			else
			{
				Span<ShapeNode> rightEnv;
				if (match.TryGetGroup("rightEnv", out rightEnv))
				{
					startNode = rightEnv.Start;
				}
				else
				{
					Span<ShapeNode> leftEnv = match["leftEnv"];
					startNode = leftEnv.End.Next;
				}
			}

			ShapeNode curNode = startNode;
			foreach (PatternNode<Word, ShapeNode> node in _rhs.Children.GetNodes(Direction))
			{
				if (input.Shape.Count == 256)
					throw new MorphException(MorphErrorCode.TooManySegs);
				var constraint = (Constraint<Word, ShapeNode>) node;
				if (match.VariableBindings.Values.OfType<SymbolicFeatureValue>().Where(value => value.Feature.DefaultValue.Equals(value)).Any())
					throw new MorphException(MorphErrorCode.UninstantiatedFeature);
				FeatureStruct fs = constraint.FeatureStruct.Clone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = input.Shape.Insert(curNode, constraint.Type, fs);
			}

			MarkSearchedNodes(startNode, curNode);
			output = input;
			return startNode.GetNext(Direction).Annotation;
		}
	}
}
