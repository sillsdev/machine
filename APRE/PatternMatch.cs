using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	/// <summary>
	/// This class represents a match between a phonetic shape and a phonetic pattern.
	/// </summary>
	public class PatternMatch<TOffset> : Span<TOffset>, IComparable<PatternMatch<TOffset>>
	{
		private readonly Dictionary<string, Span<TOffset>> _groups;
		private readonly IDictionary<string, FeatureValue> _varBindings;

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternMatch{TOffset}"/> class.
		/// </summary>
		/// <param name="entire"></param>
		/// <param name="groups">The groups.</param>
		/// <param name="varBindings"></param>
		public PatternMatch(Span<TOffset> entire, IDictionary<string, Span<TOffset>> groups, IDictionary<string, FeatureValue> varBindings)
			: base(entire)
		{
			_groups = new Dictionary<string, Span<TOffset>>(groups);
			_varBindings = varBindings;
		}

		public IDictionary<string, FeatureValue> VariableBindings
		{
			get { return _varBindings; }
		}

		public Span<TOffset> this[string groupName]
		{
			get
			{
				Span<TOffset> group;
				if (_groups.TryGetValue(groupName, out group))
					return group;
				return null;
			}
		}

		public int CompareTo(PatternMatch<TOffset> other)
		{
			if (Length > other.Length)
				return -1;

			if (Length < other.Length)
				return 1;

			if (_groups.Count > other._groups.Count)
				return -1;

			if (_groups.Count < other._groups.Count)
				return 1;

			return 0;
		}
	}
}
