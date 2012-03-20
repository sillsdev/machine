using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// The matching type
	/// </summary>
	public enum MprFeatureGroupMatchType
	{
		/// <summary>
		/// when any features match within the group
		/// </summary>
		Any,
		/// <summary>
		/// only if all features match within the group
		/// </summary>
		All
	}

	/// <summary>
	/// The outputting type
	/// </summary>
	public enum MprFeatureGroupOutput
	{
		/// <summary>
		/// overwrites all existing features in the same group
		/// </summary>
		Overwrite,
		/// <summary>
		/// appends features
		/// </summary>
		Append
	}

	/// <summary>
	/// This class represents a group of related MPR features.
	/// </summary>
	public class MprFeatureGroup : IDBearerSet<MprFeature>, IIDBearer
	{
		private readonly string _id;

		public MprFeatureGroup(string id)
		{
			_id = id;
			Description = id;
		}

		public string ID
		{
			get { return _id; }
		}

		public string Description { get; set; }

		/// <summary>
		/// Gets or sets the type of matching that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of matching.</value>
		public MprFeatureGroupMatchType MatchType { get; set; }

		/// <summary>
		/// Gets or sets the type of outputting that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of outputting.</value>
		public MprFeatureGroupOutput Output { get; set; }

		/// <summary>
		/// Adds the MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		public override bool Add(MprFeature mprFeature)
		{
			if (base.Add(mprFeature))
			{
				mprFeature.Group = this;
				return true;
			}
			return false;
		}

		public override bool Remove(string id)
		{
			MprFeature mprFeature;
			if (TryGetValue(id, out mprFeature))
				mprFeature.Group = null;
			return base.Remove(id);
		}

		public override void Clear()
		{
			foreach (MprFeature mprFeature in this)
				mprFeature.Group = null;
			base.Clear();
		}

		public override string ToString()
		{
			return Description;
		}
	}
}
