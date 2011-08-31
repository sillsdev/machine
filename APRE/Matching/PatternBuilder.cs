using System;
using System.Collections.Generic;

namespace SIL.APRE.Matching
{
	public class PatternBuilder<TOffset> : IPatternBuilder<TOffset>
	{
		private readonly SpanFactory<TOffset> _spanFactory;
		private Func<Annotation<TOffset>, bool> _filter;
		private Direction _dir;
		private readonly List<Expression<TOffset>> _expressions;

		public PatternBuilder(SpanFactory<TOffset> spanFactory)
		{
			_spanFactory = spanFactory;
			_dir = Direction.LeftToRight;
			_filter = ann => true;
			_expressions = new List<Expression<TOffset>>();
		}

		public IPatternBuilder<TOffset> MatchLeftToRight
		{
			get
			{
				_dir = Direction.LeftToRight;
				return this;
			}
		}

		public IPatternBuilder<TOffset> MatchRightToLeft
		{
			get
			{
				_dir = Direction.RightToLeft;
				return this;
			}
		}

		public IPatternBuilder<TOffset> AllowWhere(Func<Annotation<TOffset>, bool> filter)
		{
			_filter = filter;
			return this;
		}

		public IPatternBuilder<TOffset> Expression(Func<IExpressionBuilder<TOffset>, IExpressionBuilder<TOffset>> build)
		{
			var exprBuilder = new ExpressionBuilder<TOffset>();
			IExpressionBuilder<TOffset> result = build(exprBuilder);
			_expressions.Add(result.Value);
			return this;
		}

		public IPatternBuilder<TOffset> Expression(string name, Func<IExpressionBuilder<TOffset>, IExpressionBuilder<TOffset>> build)
		{
			var exprBuilder = new ExpressionBuilder<TOffset>(name);
			IExpressionBuilder<TOffset> result = build(exprBuilder);
			_expressions.Add(result.Value);
			return this;
		}

		public Pattern<TOffset> Value
		{
			get
			{
				return new Pattern<TOffset>(_spanFactory, _dir, _filter, _expressions);
			}
		}
	}
}
