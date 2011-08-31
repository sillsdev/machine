using System;
using System.Collections.Generic;

namespace SIL.APRE.Transduction
{
	public class RuleCascade<TOffset> : RuleCascadeBase<TOffset>
	{
		private readonly Func<IBidirList<Annotation<TOffset>>, bool> _applicable;

		public RuleCascade(IEnumerable<IRule<TOffset>> rules, bool linear)
			: this(rules, linear, null)
		{
		}

		public RuleCascade(IEnumerable<IRule<TOffset>> rules, bool linear, Func<IBidirList<Annotation<TOffset>>, bool> applicable)
			: base(rules, linear)
		{
			_applicable = applicable;
		}

		public override bool IsApplicable(IBidirList<Annotation<TOffset>> input)
		{
			if (_applicable == null)
				return true;
			return _applicable(input);
		}
	}
}
