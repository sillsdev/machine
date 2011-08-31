using System;
using System.Collections.Generic;
using SIL.APRE.Fsa;

namespace SIL.APRE.Matching
{
	public class Expression<TOffset> : PatternNode<TOffset>
	{
		private readonly string _name;
		private readonly Func<IBidirList<Annotation<TOffset>>, bool> _applicable;

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
		}

		public Expression(string name, params PatternNode<TOffset>[] nodes)
			: this(name, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Expression(string name, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, bool> applicable)
		{
			_name = name;
			_applicable = applicable;
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, bool> applicable, params PatternNode<TOffset>[] nodes)
			: this(name, applicable, (IEnumerable<PatternNode<TOffset>>) nodes)
		{
		}

		public Expression(string name, Func<IBidirList<Annotation<TOffset>>, bool> applicable, IEnumerable<PatternNode<TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
			_applicable = applicable;
		}

		public Expression(Expression<TOffset> expr)
			: base(expr)
		{
			_name = expr._name;
		}

		public override PatternNodeType Type
		{
			get { return PatternNodeType.Expression; }
		}

		public string Name
		{
			get { return _name; }
		}

		public Func<IBidirList<Annotation<TOffset>>, bool> Applicable
		{
			get { return _applicable; }
		}

		internal override State<TOffset> GenerateNfa(FiniteStateAutomaton<TOffset> fsa, State<TOffset> startState)
		{
			startState = base.GenerateNfa(fsa, startState);
			startState.AddArc(fsa.CreateTag(fsa.CreateAcceptingState(_name, _applicable), Pattern<TOffset>.EntireGroupName, false));
			return startState;
		}

		public override PatternNode<TOffset> Clone()
		{
			return new Expression<TOffset>(this);
		}
	}
}
