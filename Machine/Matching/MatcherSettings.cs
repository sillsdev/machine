using System;

namespace SIL.Machine.Matching
{
	public sealed class MatcherSettings<TOffset>
	{
		private Direction _dir;
		private Func<Annotation<TOffset>, bool> _filter;
		private bool _useDefaultsForMatching;
		private bool _quasideterministic;

		public MatcherSettings()
		{
			_filter = ann => true;
		}

		internal bool ReadOnly { get; set; }

		public Direction Direction
		{
			get { return _dir; }
			set
			{
				CheckReadOnly();
				_dir = value;
			}
		}

		public Func<Annotation<TOffset>, bool> Filter
		{
			get { return _filter; }
			set
			{
				CheckReadOnly();
				_filter = value;
			}
		}

		public bool UseDefaultsForMatching
		{
			get { return _useDefaultsForMatching; }
			set
			{
				CheckReadOnly();
				_useDefaultsForMatching = value;
			}
		}

		public bool Quasideterministic
		{
			get { return _quasideterministic; }
			set
			{
				CheckReadOnly();
				_quasideterministic = value;
			}
		}

		private void CheckReadOnly()
		{
			if (ReadOnly)
				throw new InvalidOperationException("Settings cannot be changed after a Matcher object has been created.");
		}
	}
}
