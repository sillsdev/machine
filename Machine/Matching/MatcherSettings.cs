using System;

namespace SIL.Machine.Matching
{
	public class MatcherSettings<TOffset>
	{
		public MatcherSettings()
		{
			Filter = ann => true;
		}

		public Direction Direction { get; set; }

		public Func<Annotation<TOffset>, bool> Filter { get; set; }

		public bool UseDefaultsForMatching { get; set; }

		public bool Quasideterministic { get; set; }
	}
}
