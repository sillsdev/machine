using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public class NullPatternRuleAction<TOffset> : IPatternRuleAction<TOffset>
	{
		public bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			return true;
		}

		public Annotation<TOffset> Apply(Pattern<TOffset> lhs, IBidirList<Annotation<TOffset>> input, PatternMatch<TOffset> match)
		{
			return null;
		}
	}
}
