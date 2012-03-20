using System.Collections.Generic;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a slot in an affix template. It encapsulates a list of
	/// affixal morphological rules.
	/// </summary>
	public class AffixTemplateSlot : IDBearerBase
	{
		private readonly List<IMorphologicalRule> _rules;
		private bool _isOptional;

		/// <summary>
		/// Initializes a new instance of the <see cref="AffixTemplateSlot"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		public AffixTemplateSlot(string id)
			: base(id)
		{
			_rules = new List<IMorphologicalRule>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether this slot is optional.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is optional, otherwise <c>false</c>.
		/// </value>
		public bool Optional
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
		public ICollection<IMorphologicalRule> Rules
		{
			get { return _rules; }
		}
	}
}
