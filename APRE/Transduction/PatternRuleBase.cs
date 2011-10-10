using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBase<TOffset> : IPatternRule<TOffset>
	{
		private readonly Pattern<TOffset> _lhs;
		private readonly bool _simult;

		protected PatternRuleBase(Pattern<TOffset> lhs, bool simult)
		{
			_lhs = lhs;
			_simult = simult;
		}

		public Pattern<TOffset> Lhs
		{
			get { return _lhs; }
		}

		public bool Simultaneous
		{
			get { return _simult; }
		}

		public void Compile()
		{
			_lhs.Compile();
		}

		public abstract bool IsApplicable(IBidirList<Annotation<TOffset>> input);

		public virtual bool Apply(IBidirList<Annotation<TOffset>> input)
		{
			if (!IsApplicable(input))
				return false;

			if (_simult)
			{
				IEnumerable<PatternMatch<TOffset>> matches;
				if (_lhs.IsMatch(input, out matches))
				{
					foreach (PatternMatch<TOffset> match in matches)
						ApplyRhs(input, match);
					return true;
				}
				return false;
			}

			bool applied = false;
			Annotation<TOffset> first = input.GetFirst(_lhs.Direction, _lhs.Filter);
			PatternMatch<TOffset> curMatch;
			while (_lhs.IsMatch(input, first, out curMatch))
			{
				first = ApplyRhs(input, curMatch);
				first = first == null ? input.GetFirst(_lhs.Direction, _lhs.Filter)
					: first.GetNext(_lhs.Direction, (cur, next) => !cur.Span.Overlaps(next.Span) && _lhs.Filter(next));
				applied = true;
			}
			return applied;
		}

		public abstract Annotation<TOffset> ApplyRhs(IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match);
	}
}
