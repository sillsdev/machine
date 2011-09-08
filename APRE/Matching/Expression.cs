using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.Fsa;
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

		private IEnumerable<Expression<TOffset>> Expressions
		{
			get
			{
				PatternNode<TOffset> node = this;
				while (node != null)
				{
					var expr = node as Expression<TOffset>;
					if (expr != null)
						yield return expr;
					node = node.Parent;
				}
			}
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			State<TOffset> endState = base.GenerateNfa(fsa, startState);
			if (!(Children.GetLast(fsa.Direction) is Expression<TOffset>))
			{
				Pattern<TOffset> pattern = Pattern;
				var acceptables = new List<Func<IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, bool>>();
				var sb = new StringBuilder();
				bool first = true;
				foreach (Expression<TOffset> expr in Expressions.Reverse())
				{
					if (expr != pattern)
					{
						if (!first)
							sb.Append('*');
						sb.Append(expr.Name);
						first = false;
					}
					if (expr.Acceptable != null)
						acceptables.Add(expr.Acceptable);
				}
				State<TOffset> acceptingState = fsa.CreateAcceptingState(sb.ToString(),
					(input, match) => acceptables.All(acceptable => acceptable(input, pattern.CreatePatternMatch(match))));
				endState.AddArc(fsa.CreateTag(acceptingState, pattern.Name, false));
			}
			return startState;
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Expression<TOffset>(this);
		}
	}
}
