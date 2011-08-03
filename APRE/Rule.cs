using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class Rule<TOffset>
	{
		private readonly Pattern<TOffset> _lhs;
		private readonly Action<IDictionary<string, IBidirList<Annotation<TOffset>>>> _rhs;

		public Rule(Pattern<TOffset> lhs, Action<IDictionary<string, IBidirList<Annotation<TOffset>>>> rhs)
		{
			_lhs = lhs;
			_rhs = rhs;
		}

		public Pattern<TOffset> Lhs
		{
			get { return _lhs; }
		}

		public Action<IDictionary<string, IBidirList<Annotation<TOffset>>>> Rhs
		{
			get { return _rhs; }
		}

		public void Compile()
		{
			_lhs.Compile();
		}

		public bool Apply(IBidirList<Annotation<TOffset>> input)
		{
			return false;
		}
	}
}
