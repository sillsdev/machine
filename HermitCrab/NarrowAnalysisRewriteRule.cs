using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Transduction;

namespace SIL.HermitCrab
{
	public class NarrowAnalysisRewriteRule : AnalysisRewriteRule
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly Expression<Word, ShapeNode> _analysisRhs;
		private readonly int _targetCount;

		public NarrowAnalysisRewriteRule(SpanFactory<ShapeNode> spanFactory, Expression<Word, ShapeNode> lhs, Expression<Word, ShapeNode> rhs,
			Expression<Word, ShapeNode> leftEnv, Expression<Word, ShapeNode> rightEnv)
			: base(spanFactory)
		{
			ApplicationMode = ApplicationMode.Simultaneous;

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
				FeatureStruct fs = constraint.FeatureStruct.Clone();
				fs.ReplaceVariables(match.VariableBindings);
				curNode = input.Shape.Insert(curNode, constraint.Type, fs, true);
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
