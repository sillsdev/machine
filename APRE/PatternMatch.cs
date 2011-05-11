using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	/// <summary>
	/// This class represents a match between a phonetic shape and a phonetic pattern.
	/// </summary>
	public class PatternMatch<TOffset> : IComparable<PatternMatch<TOffset>>
	{
		private readonly Dictionary<int, Span<TOffset>> _groups;

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternMatch{TOffset}"/> class.
		/// </summary>
		/// <param name="groups">The groups.</param>
		/// <param name="vars">The vars.</param>
		public PatternMatch(IDictionary<int, Span<TOffset>> groups, FeatureStructure vars)
		{
			_groups = new Dictionary<int, Span<TOffset>>(groups);
			VariableValues = vars;
		}

		public PatternMatch(Span<TOffset> entireMatch, FeatureStructure vars)
		{
			_groups = new Dictionary<int, Span<TOffset>> {{0, entireMatch}};
			VariableValues = vars;
		}

		/// <summary>
		/// Gets the entire match.
		/// </summary>
		/// <value>The entire match.</value>
		public Span<TOffset> EntireMatch
		{
			get
			{
				return _groups[0];
			}
		}

		/// <summary>
		/// Gets or sets the data associated with this match.
		/// </summary>
		/// <value>The data.</value>
		public FeatureStructure VariableValues { get; internal set; }

		public Span<TOffset> this[int groupNum]
		{
			get
			{
				Span<TOffset> group;
				if (_groups.TryGetValue(groupNum, out group))
					return group;
				return null;
			}
		}

		public int CompareTo(PatternMatch<TOffset> other)
		{
			if (_groups[0].Length > other._groups[0].Length)
				return -1;

			if (_groups[0].Length < other._groups[0].Length)
				return 1;

			if (_groups.Count > other._groups.Count)
				return -1;

			if (_groups.Count < other._groups.Count)
				return 1;

			return 0;
		}

		public override string ToString()
		{
			return _groups[0].ToString();
		}
	}
}
