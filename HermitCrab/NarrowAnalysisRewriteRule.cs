using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	public class NarrowAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly SpanFactory<PhoneticShapeNode> _spanFactory;
		private readonly Expression<PhoneticShapeNode> _analysisRhs;
		private readonly int _targetCount;

		public NarrowAnalysisRewriteRule(SpanFactory<PhoneticShapeNode> spanFactory, Expression<PhoneticShapeNode> lhs, Expression<PhoneticShapeNode> rhs,
			Expression<PhoneticShapeNode> leftEnv, Expression<PhoneticShapeNode> rightEnv)
			: base(spanFactory, Direction.LeftToRight, true, (input, match) => true)
		{
			_spanFactory = spanFactory;
			_analysisRhs = lhs;
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			if (rhs.Children.Count > 0)
				Lhs.Children.Add(new Group<PhoneticShapeNode>("target", rhs.Children.Clone()));
			AddEnvironment("rightEnv", rightEnv);
		}

		public override AnalysisReapplyType AnalysisReapplyType
		{
			get { return AnalysisReapplyType.Deletion; }
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			var shape = (PhoneticShape) match.Start.List;
			PhoneticShapeNode curNode;
			Span<PhoneticShapeNode> target;
			if (match.TryGetGroup("target", out target))
			{
				curNode = target.End;
			}
			else
			{
				Span<PhoneticShapeNode> leftEnv;
				if (match.TryGetGroup("leftEnv", out leftEnv))
				{
					curNode = leftEnv.End;
				}
				else
				{
					Span<PhoneticShapeNode> rightEnv;
					if (match.TryGetGroup("rightEnv", out rightEnv))
						curNode = rightEnv.Start.Prev;
					else
						curNode = match.Start;
				}
			}
			foreach (Constraint<PhoneticShapeNode> constraint in _analysisRhs.Children)
			{
				var newNode = new PhoneticShapeNode(constraint.Type, _spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
				newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
				newNode.Annotation.Optional = true;
				shape.Insert(newNode, curNode, Direction.LeftToRight);
				curNode = newNode;
			}

			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.Next;
			}

			return null;
		}

	}
}
