using System;
using System.Collections.Generic;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching
{
	/// <summary>
	/// This class represents a match between a phonetic shape and a phonetic pattern.
	/// </summary>
	public class PatternMatch<TOffset> : Span<TOffset>, IComparable<PatternMatch<TOffset>>
	{
		private readonly Dictionary<string, Span<TOffset>> _groups;
		private readonly VariableBindings _varBindings;
		private readonly IEnumerable<string> _exprPath;

		/// <summary>
		/// Initializes a new instance of the <see cref="PatternMatch{TOffset}"/> class.
		/// </summary>
		/// <param name="entire"></param>
		/// <param name="groups">The groups.</param>
		/// <param name="exprPath"></param>
		/// <param name="varBindings"></param>
		public PatternMatch(Span<TOffset> entire, IDictionary<string, Span<TOffset>> groups, IEnumerable<string> exprPath,
			VariableBindings varBindings)
			: base(entire)
		{
			_groups = new Dictionary<string, Span<TOffset>>(groups);
			_exprPath = exprPath;
			_varBindings = varBindings;
		}

		public IEnumerable<string> ExpressionPath
		{
			get { return _exprPath; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}

		public IEnumerable<string> Groups
		{
			get { return _groups.Keys; }
		}

		public Span<TOffset> this[string groupName]
		{
			get { return _groups[groupName]; }
		}

		public bool TryGetGroup(string groupName, out Span<TOffset> span)
		{
			return _groups.TryGetValue(groupName, out span);
		}

		public bool ContainsGroup(string groupName)
		{
			return _groups.ContainsKey(groupName);
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
