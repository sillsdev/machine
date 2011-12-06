using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Matching.Fluent;

namespace SIL.Machine.Matching
{
	public class Expression<TData, TOffset> : PatternNode<TData, TOffset> where TData : IData<TOffset>
	{
		public static IExpressionSyntax<TData, TOffset> New()
		{
			return new ExpressionBuilder<TData, TOffset>();
		}

		public static IExpressionSyntax<TData, TOffset> New(string name)
		{
			return new ExpressionBuilder<TData, TOffset>(name);
		}

		private readonly string _name;

		public Expression()
			: this(Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Expression(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			Acceptable = (input, match) => true;
		}

		public Expression(string name)
			: this(name, Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Expression(string name, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			Acceptable = (input, match) => true;
			_name = name;
		}

		public Expression(Expression<TData, TOffset> expr)
			: base(expr)
		{
			_name = expr._name;
			Acceptable = expr.Acceptable;
		}

		public string Name
		{
			get { return _name; }
		}

		public Func<TData, PatternMatch<TOffset>, bool> Acceptable { get; set; }

		public override PatternNode<TData, TOffset> Clone()
		{
			return new Expression<TData, TOffset>(this);
		}
	}
}
