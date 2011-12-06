using System.Collections.Generic;
using SIL.Machine;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a group of related MPR features.
	/// </summary>
	public class MprFeatureGroup : IDBearerBase
	{
		/// <summary>
		/// The matching type
		/// </summary>
		public enum GroupMatchType
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
		public enum GroupOutputType
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

		private GroupMatchType _matchType = GroupMatchType.Any;
		private GroupOutputType _outputType = GroupOutputType.Overwrite;
		private readonly IDBearerSet<MprFeature> _mprFeatures;

		public MprFeatureGroup(string id)
			: base(id)
		{
			_mprFeatures = new IDBearerSet<MprFeature>();
		}

		/// <summary>
		/// Gets or sets the type of matching that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of matching.</value>
		public GroupMatchType MatchType
		{
			get
			{
				return _matchType;
			}

			set
			{
				_matchType = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of outputting that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of outputting.</value>
		public GroupOutputType OutputType
		{
			get
			{
				return _outputType;
			}

			set
			{
				_outputType = value;
			}
		}

		/// <summary>
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public IEnumerable<MprFeature> Features
		{
			get
			{
				return _mprFeatures;
			}
		}

		/// <summary>
		/// Adds the MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		public void Add(MprFeature mprFeature)
		{
			mprFeature.Group = this;
			_mprFeatures.Add(mprFeature);
		}

		/// <summary>
		/// Determines whether this group contains the specified MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		/// <returns>
		/// 	<c>true</c> if this group contains the feature, otherwise <c>false</c>.
		/// </returns>
		public bool Contains(MprFeature mprFeature)
		{
			return _mprFeatures.Contains(mprFeature);
		}
	}
}
