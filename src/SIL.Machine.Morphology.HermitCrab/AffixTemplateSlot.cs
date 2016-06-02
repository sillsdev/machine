using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
	/// <summary>
	/// This class represents a slot in an affix template. It encapsulates a list of
	/// affixal morphological rules.
	/// </summary>
	public class AffixTemplateSlot
	{
		private readonly List<IMorphologicalRule> _rules;
		private bool _isOptional;

		/// <summary>
		/// Initializes a new instance of the <see cref="AffixTemplateSlot"/> class.
		/// </summary>
		public AffixTemplateSlot()
		{
			_rules = new List<IMorphologicalRule>();
		}

		public string Name { get; set; }

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

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
