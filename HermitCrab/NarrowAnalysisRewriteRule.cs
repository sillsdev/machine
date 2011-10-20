using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class NarrowAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Expression<Word, ShapeNode> _analysisRhs;
		private readonly int _targetCount;

		public NarrowAnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs, Expression<Word, ShapeNode> rhs,
			Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
			: base(spanFactory, Direction.LeftToRight, ApplicationMode.Simultaneous, (input, match) => true)
		{
			_spanFactory = spanFactory;
			_analysisRhs = lhs;
			_targetCount = rhs.Children.Count;

			AddEnvironment("leftEnv", leftEnv);
			if (rhs.Children.Count > 0)
				Lhs.Children.Add(new Group<Word, ShapeNode>("target", rhs.Children.Clone()));
			AddEnvironment("rightEnv", rightEnv);
		}

		public override AnalysisReapplyType AnalysisReapplyType
		{
			get { return AnalysisReapplyType.Deletion; }
		}

		public override Annotation<ShapeNode> ApplyRhs(Word input, PatternMatch<ShapeNode> match, out Word output)
		{
			ShapeNode startNode;
			Span<ShapeNode> target;
			if (match.TryGetGroup("target", out target))
			{
				startNode = target.End;
			}
			else
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

			ShapeNode curNode = startNode;
			foreach (Constraint<Word, ShapeNode> constraint in _analysisRhs.Children)
			{
				var newNode = new ShapeNode(constraint.Type, _spanFactory, (FeatureStruct) constraint.FeatureStruct.Clone());
				newNode.Annotation.FeatureStruct.ReplaceVariables(match.VariableBindings);
				newNode.Annotation.Optional = true;
				input.Shape.Insert(newNode, curNode, Direction.LeftToRight);
				curNode = newNode;
			}

			for (int i = 0; i < _targetCount; i++)
			{
				curNode.Annotation.Optional = true;
				curNode = curNode.Next;
			}

			output = input;
			return null;
		}

	}
}
