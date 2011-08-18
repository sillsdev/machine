using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class Rule<TOffset>
	{
		private readonly Pattern<TOffset> _lhs;
		private readonly Action<PatternMatch<TOffset>> _rhs;
		private readonly bool _simultaneous;

		public Rule(Pattern<TOffset> lhs, Action<PatternMatch<TOffset>> rhs)
			: this(lhs, rhs, false)
		{
		}

		public Rule(Pattern<TOffset> lhs, Action<PatternMatch<TOffset>> rhs, bool simultaneous)
		{
			_lhs = lhs;
			_rhs = rhs;
			_simultaneous = simultaneous;
		}

		public Pattern<TOffset> Lhs
		{
			get { return _lhs; }
		}

		public Action<PatternMatch<TOffset>> Rhs
		{
			get { return _rhs; }
		}

		public bool Simultaneous
		{
			get { return _simultaneous; }
		}

		public void Compile()
		{
			_lhs.Compile();
		}

		public bool Apply(IBidirList<Annotation<TOffset>> input)
		{
			IEnumerable<PatternMatch<TOffset>> matches;
			if (_lhs.IsMatch(input, out matches))
			{
				foreach (PatternMatch<TOffset> match in matches)
					_rhs(match);
				return true;
			}
			return false;
		}
	}
}
