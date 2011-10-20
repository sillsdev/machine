using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public class ExpressionBuilder<TData, TOffset> : PatternNodeBuilder<TData, TOffset>, IExpressionSyntax<TData, TOffset>, IQuantifierExpressionSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly string _name;
		private Func<TData, PatternMatch<TOffset>, bool> _acceptable;

		public ExpressionBuilder()
		{
		}

		public ExpressionBuilder(string name)
		{
			_name = name;
		}

		IInitialNodesExpressionSyntax<TData, TOffset> IAlternationExpressionSyntax<TData, TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifierExpressionSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierExpressionSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierExpressionSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs)
		{
			AddAnnotation(type, fs);
			return this;
		}

		public INodesExpressionSyntax<TData, TOffset> Expression(Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build)
		{
			AddExpression(null, build);
			return this;
		}

		public INodesExpressionSyntax<TData, TOffset> Expression(string name, Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build)
		{
			AddExpression(name, build);
			return this;
		}

		public Expression<TData, TOffset> Value
		{
			get
			{
				var expr = new Expression<TData, TOffset>(_name, _acceptable);
				PopulateNode(expr);
				return expr;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.LazyZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.LazyOneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1, true);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.LazyOptional
		{
			get
			{
				AddQuantifier(0, 1, false);
				return this;
			}
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max, true);
			return this;
		}

		IAlternationExpressionSyntax<TData, TOffset> IQuantifierExpressionSyntax<TData, TOffset>.LazyRange(int min, int max)
		{
			AddQuantifier(min, max, false);
			return this;
		}

		public IExpressionSyntax<TData, TOffset> MatchAcceptableWhere(Func<TData, PatternMatch<TOffset>, bool> acceptable)
		{
			_acceptable = acceptable;
			return this;
		}
	}
}
