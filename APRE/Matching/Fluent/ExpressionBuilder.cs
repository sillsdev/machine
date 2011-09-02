using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public class ExpressionBuilder<TOffset> : PatternNodeBuilder<TOffset>, IExpressionSyntax<TOffset>,
		IQuantifierExpressionSyntax<TOffset>
	{
		private readonly string _name;
		private Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> _acceptable;

		public ExpressionBuilder()
		{
		}

		public ExpressionBuilder(string name)
		{
			_name = name;
		}

		IInitialNodesExpressionSyntax<TOffset> IAlternationExpressionSyntax<TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifierExpressionSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierExpressionSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierExpressionSyntax<TOffset> Annotation(FeatureStruct fs)
		{
			AddAnnotation(fs);
			return this;
		}

		public INodesExpressionSyntax<TOffset> Expression(Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build)
		{
			AddExpression(null, build);
			return this;
		}

		public INodesExpressionSyntax<TOffset> Expression(string name, Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build)
		{
			AddExpression(name, build);
			return this;
		}

		public Expression<TOffset> Value
		{
			get
			{
				var expr = new Expression<TOffset>(_name, _acceptable);
				PopulateNode(expr);
				return expr;
			}
		}

		IAlternationExpressionSyntax<TOffset> IQuantifierExpressionSyntax<TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		IAlternationExpressionSyntax<TOffset> IQuantifierExpressionSyntax<TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		IAlternationExpressionSyntax<TOffset> IQuantifierExpressionSyntax<TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1);
				return this;
			}
		}

		IAlternationExpressionSyntax<TOffset> IQuantifierExpressionSyntax<TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max);
			return this;
		}

		public IExpressionSyntax<TOffset> MatchAcceptableWhere(Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable)
		{
			_acceptable = acceptable;
			return this;
		}
	}
}
