using System.Collections.Generic;
using SIL.Machine;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a slot in an affix template. It encapsulates a list of
	/// affixal morphological rules.
	/// </summary>
	public class Slot : IDBearerBase
	{
		private readonly IDBearerSet<MorphologicalRule> _rules;
		private bool _isOptional;

		/// <summary>
		/// Initializes a new instance of the <see cref="Slot"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		public Slot(string id)
			: base(id)
		{
			_rules = new IDBearerSet<MorphologicalRule>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether this slot is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is optional, otherwise <c>false</c>.
		/// </value>
		public bool IsOptional
		{
			get
			{
				if (_rules.Count == 0)
					return true;

				return _isOptional;
			}

			set
			{
				_isOptional = value;
			}
		}

		/// <summary>
		/// Gets the morphological rules.
		/// </summary>
		/// <value>The morphological rules.</value>
		public IEnumerable<MorphologicalRule> MorphologicalRules
		{
			get
			{
				return _rules;
			}
		}

		/// <summary>
		/// Adds the morphological rule.
		/// </summary>
		/// <param name="rule">The morphological rule.</param>
		public void AddRule(MorphologicalRule rule)
		{
			_rules.Add(rule);
		}
	}
}
