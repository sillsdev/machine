using System;
using System.Collections.Generic;
using SIL.APRE.Patterns;

namespace SIL.APRE.Rules
{
	public class Rule<TOffset>
	{
		private readonly Pattern<TOffset> _lhs;
		private readonly IRuleAction<TOffset> _rhs;
		private readonly bool _simultaneous;

		public Rule(Pattern<TOffset> lhs, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs)
			: this(lhs, rhs, false)
		{
		}

		public Rule(Pattern<TOffset> lhs, Func<Pattern<TOffset>, IBidirList<Annotation<TOffset>>, PatternMatch<TOffset>, Annotation<TOffset>> rhs,
			bool simultaneous)
			: this(lhs, new DelegateRuleAction<TOffset>(rhs), simultaneous)
		{
		}

		public Rule(Pattern<TOffset> lhs, IRuleAction<TOffset> rhs)
			: this(lhs, rhs, false)
		{
		}

		public Rule(Pattern<TOffset> lhs, IRuleAction<TOffset> rhs, bool simultaneous)
		{
			_lhs = lhs;
			_rhs = rhs;
			_simultaneous = simultaneous;
		}

		public Pattern<TOffset> Lhs
		{
			get { return _lhs; }
		}

		public IRuleAction<TOffset> Rhs
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
			if (_simultaneous)
			{
				IEnumerable<PatternMatch<TOffset>> matches;
				if (_lhs.IsMatch(input, out matches))
				{
					foreach (PatternMatch<TOffset> match in matches)
						_rhs.Run(_lhs, input, match);
					return true;
				}
				return false;
			}
			else
			{
				bool applied = false;
				Annotation<TOffset> first = input.GetFirst(_lhs.Direction, _lhs.Filter);
				while (first != null)
				{
					PatternMatch<TOffset> match;
					if (_lhs.IsMatch(input.GetView(first, _lhs.Direction), out match))
					{
						first = _rhs.Run(_lhs, input, match);
						applied = true;

					}
					first = first.GetNext(_lhs.Direction, (cur, next) => !cur.Span.Overlaps(next.Span) && _lhs.Filter(next));
				}
				return applied;
			}
		}
	}
}
