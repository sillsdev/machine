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
			{
				var target = new Group<PhoneticShapeNode>("target");
				foreach (Constraint<PhoneticShapeNode> constraint in rhs.Children)
				{
					var newConstraint = (Constraint<PhoneticShapeNode>)constraint.Clone();
					newConstraint.FeatureStruct.AddValue(HCFeatureSystem.Backtrack, HCFeatureSystem.NotSearched);
					target.Children.Add(newConstraint);
				}
				Lhs.Children.Add(target);
			}
			AddEnvironment("rightEnv", rightEnv);
		}

		public override AnalysisReapplyType AnalysisReapplyType
		{
			get { return AnalysisReapplyType.Deletion; }
		}

		public override Annotation<PhoneticShapeNode> ApplyRhs(IBidirList<Annotation<PhoneticShapeNode>> input, PatternMatch<PhoneticShapeNode> match)
		{
			PhoneticShape shape;
			PhoneticShapeNode curNode;
			Span<PhoneticShapeNode> target;
			if (match.TryGetGroup("target", out target))
			{
				shape = (PhoneticShape) target.Start.List;
				curNode = target.End;
			}
			else
			{
				Span<PhoneticShapeNode> leftEnv;
				if (match.TryGetGroup("leftEnv", out leftEnv))
				{
					shape = (PhoneticShape)leftEnv.Start.List;
					curNode = leftEnv.End;
				}
				else
				{
					Span<PhoneticShapeNode> rightEnv = match["rightEnv"];
					shape = (PhoneticShape)rightEnv.Start.List;
					curNode = rightEnv.Start.Prev;
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

			MarkSearchedNodes(match.Start, curNode);

			return match.GetStart(Lhs.Direction).Annotation;
		}

	}
}
