using System;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.ObjectModel;

namespace SIL.Machine.Matching
{
	public enum MatchingMethod
	{
		Subsumption,
		Unification
	}

	public sealed class MatcherSettings<TOffset> : Freezable<MatcherSettings<TOffset>>, ICloneable<MatcherSettings<TOffset>>
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

		private MatcherSettings(MatcherSettings<TOffset> other)
		{
			_dir = other._dir;
			_filter = other._filter;
			_useDefaults = other._useDefaults;
			_nondeterministic = other._nondeterministic;
			_anchoredToStart = other._anchoredToStart;
			_anchoredToEnd = other._anchoredToEnd;
			_allSubmatches = other._allSubmatches;
			_matchingMethod = other._matchingMethod;
		}

		public Direction Direction
		{
			get { return _dir; }
			set
			{
				CheckFrozen();
				_dir = value;
			}
		}

		public Func<Annotation<TOffset>, bool> Filter
		{
			get { return _filter; }
			set
			{
				CheckFrozen();
				_filter = value;
			}
		}

		public bool UseDefaults
		{
			get { return _useDefaults; }
			set
			{
				CheckFrozen();
				_useDefaults = value;
			}
		}

		public bool Nondeterministic
		{
			get { return _nondeterministic; }
			set
			{
				CheckFrozen();
				_nondeterministic = value;
			}
		}

		public bool AnchoredToStart
		{
			get { return _anchoredToStart; }
			set
			{
				CheckFrozen();
				_anchoredToStart = value;
			}
		}

		public bool AnchoredToEnd
		{
			get { return _anchoredToEnd; }
			set
			{
				CheckFrozen();
				_anchoredToEnd = value;
			}
		}

		public bool AllSubmatches
		{
			get { return _allSubmatches; }
			set
			{
				CheckFrozen();
				_allSubmatches = value;
			}
		}

		public MatchingMethod MatchingMethod
		{
			get { return _matchingMethod; }
			set
			{
				CheckFrozen();
				_matchingMethod = value;
			}
		}

		public override bool ValueEquals(MatcherSettings<TOffset> other)
		{
			return other != null && _dir == other._dir && _filter == other._filter && _useDefaults == other._useDefaults
			    && _nondeterministic == other._nondeterministic && _anchoredToStart == other._anchoredToStart && _anchoredToEnd == other._anchoredToEnd
			    && _allSubmatches == other._allSubmatches && _matchingMethod == other._matchingMethod;
		}

		protected override int FreezeImpl()
		{
			int code = 23;
			code = code * 31 + _dir.GetHashCode();
			code = code * 31 + _filter.GetHashCode();
			code = code * 31 + _useDefaults.GetHashCode();
			code = code * 31 + _nondeterministic.GetHashCode();
			code = code * 31 + _anchoredToStart.GetHashCode();
			code = code * 31 + _anchoredToEnd.GetHashCode();
			code = code * 31 + _allSubmatches.GetHashCode();
			code = code * 31 + _matchingMethod.GetHashCode();
			return code;
		}

		public MatcherSettings<TOffset> Clone()
		{
			return new MatcherSettings<TOffset>(this);
		}
	}
}
