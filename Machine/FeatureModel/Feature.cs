using System;
using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	/// <summary>
	/// This class represents a feature.
	/// </summary>
	public abstract class Feature : IDBearerBase, IFreezable
	{
		private FeatureValue _defaultValue;

		protected Feature(string id)
			: base(id)
		{
		}

		/// <summary>
		/// Gets all default values.
		/// </summary>
		/// <value>The default values.</value>
		public FeatureValue DefaultValue
		{
			get { return _defaultValue; }
			set
			{
				CheckFrozen();
				_defaultValue = value;
			}
		}

		protected void CheckFrozen()
		{
			if (IsFrozen)
				throw new InvalidOperationException("The feature is immutable.");
		}

		public bool IsFrozen { get; private set; }

		public virtual void Freeze()
		{
			IsFrozen = true;
		}
	}
}
