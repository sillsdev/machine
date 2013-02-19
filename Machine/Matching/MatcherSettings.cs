using System;
using SIL.Collections;

namespace SIL.Machine.Matching
{
	public enum MatchingMethod
	{
		Subsumption,
		Unification
	}

	public sealed class MatcherSettings<TOffset>
	{
		private Direction _dir;
		private Func<Annotation<TOffset>, bool> _filter;
		private bool _useDefaults;
		private bool _nondeterministic;
		private bool _anchoredToStart;
		private bool _anchoredToEnd;
		private bool _allSubmatches;
		private MatchingMethod _matchingMethod;

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

		public bool UseDefaults
		{
			get { return _useDefaults; }
			set
			{
				CheckReadOnly();
				_useDefaults = value;
			}
		}

		public bool Nondeterministic
		{
			get { return _nondeterministic; }
			set
			{
				CheckReadOnly();
				_nondeterministic = value;
			}
		}

		public bool AnchoredToStart
		{
			get { return _anchoredToStart; }
			set
			{
				CheckReadOnly();
				_anchoredToStart = value;
			}
		}

		public bool AnchoredToEnd
		{
			get { return _anchoredToEnd; }
			set
			{
				CheckReadOnly();
				_anchoredToEnd = value;
			}
		}

		public bool AllSubmatches
		{
			get { return _allSubmatches; }
			set
			{
				CheckReadOnly();
				_allSubmatches = value;
			}
		}

		public MatchingMethod MatchingMethod
		{
			get { return _matchingMethod; }
			set
			{
				CheckReadOnly();
				_matchingMethod = value;
			}
		}

		private void CheckReadOnly()
		{
			if (ReadOnly)
				throw new InvalidOperationException("Settings cannot be changed after a Matcher object has been created.");
		}
	}
}
