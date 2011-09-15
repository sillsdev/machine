using System;
using System.Collections.Generic;
using SIL.APRE.Matching.Fluent;

namespace SIL.APRE.Matching
{
	public class Expression<TOffset> : PatternNode<TOffset>
	{
		public static IExpressionSyntax<TOffset> With
		{
			get
			{
				return new ExpressionBuilder<TOffset>();
			}
		}

		private readonly string _name;
		private readonly Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> _acceptable;

		public Expression()
		{
		}

		public Expression(params PatternNode<TOffset>[] nodes)
			: base(nodes)
		{
		}

		public Expression(IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
		}

		public Expression(string name)
		{
			_name = name;
			_acceptable = (input, match) => true;
		}

		public Expression(string name, params PatternNode<TOffset>[] nodes)
			: this(name, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Expression(string name, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
			_acceptable = (input, match) => true;
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable)
		{
			_name = name;
			_acceptable = acceptable;
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable, params PatternNode<TOffset>[] nodes)
			: this(name, acceptable, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> acceptable, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
			_acceptable = acceptable;
		}

		public Expression(Expression<TOffset> expr)
			: base(expr)
		{
			_name = expr._name;
			_acceptable = expr._acceptable;
		}

		public string Name
		{
			get { return _name; }
		}

		public Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Expression<TOffset>(this);
		}
	}
}
