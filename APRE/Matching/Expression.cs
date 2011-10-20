using System;
using System.Collections.Generic;
using SIL.APRE.Matching.Fluent;

namespace SIL.APRE.Matching
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
		private readonly Func<TData, PatternMatch<TOffset>, bool> _acceptable;

		public Expression()
		{
		}

		public Expression(params PatternNode<TData, TOffset>[] nodes)
			: base(nodes)
		{
		}

		public Expression(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
		}

		public Expression(string name)
		{
			_name = name;
			_acceptable = (input, match) => true;
		}

		public Expression(string name, params PatternNode<TData, TOffset>[] nodes)
			: this(name, (IEnumerable<PatternNode<TData, TOffset>>)nodes)
		{
		}

		public Expression(string name, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
			_acceptable = (input, match) => true;
		}

		public Expression(string name, Func<TData, PatternMatch<TOffset>, bool> acceptable)
		{
			_name = name;
			_acceptable = acceptable;
		}

		public Expression(string name, Func<TData, PatternMatch<TOffset>, bool> acceptable, params PatternNode<TData, TOffset>[] nodes)
			: this(name, acceptable, (IEnumerable<PatternNode<TData, TOffset>>)nodes)
		{
		}

		public Expression(string name, Func<TData, PatternMatch<TOffset>, bool> acceptable, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
			_acceptable = acceptable;
		}

		public Expression(Expression<TData, TOffset> expr)
			: base(expr)
		{
			_name = expr._name;
			_acceptable = expr._acceptable;
		}

		public string Name
		{
			get { return _name; }
		}

		public Func<TData, PatternMatch<TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public override PatternNode<TData, TOffset> Clone()
		{
			return new Expression<TData, TOffset>(this);
		}
	}
}
