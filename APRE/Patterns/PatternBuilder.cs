using System;
using System.Collections.Generic;

namespace SIL.APRE.Patterns
{
	public class PatternBuilder<TOffset>
	{
		public static implicit operator Pattern<TOffset>(PatternBuilder<TOffset> builder)
		{
			return builder.ToPattern();
		}

		private readonly SpanFactory<TOffset> _spanFactory;
		private Func<Annotation<TOffset>, bool> _filter;
		private Direction _dir;
		private IEnumerable<PatternNode<TOffset>> _nodes;

		public PatternBuilder(SpanFactory<TOffset> spanFactory)
		{
			_spanFactory = spanFactory;
			_dir = Direction.LeftToRight;
			_filter = ann => true;
		}

		public PatternBuilder<TOffset> LeftToRight()
		{
			_dir = Direction.LeftToRight;
			return this;
		}

		public PatternBuilder<TOffset> RightToLeft()
		{
			_dir = Direction.RightToLeft;
			return this;
		}

		public PatternBuilder<TOffset> Filter(Func<Annotation<TOffset>, bool> filter)
		{
			_filter = filter;
			return this;
		}

		public PatternBuilder<TOffset> Expression(Action<PatternExpressionBuilder<TOffset>> build)
		{
			var exprBuilder = new PatternExpressionBuilder<TOffset>();
			build(exprBuilder);
			_nodes = exprBuilder.Build();
			return this;
		}

		public Pattern<TOffset> ToPattern()
		{
			return new Pattern<TOffset>(_spanFactory, _dir, _filter, _nodes);
		}
	}
}
