using System.Collections.Generic;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This represents a set of MPR features.
	/// </summary>
	public class MprFeatureSet : HashSet<MprFeature>, IDeepCloneable<MprFeatureSet>
	{
		public MprFeatureSet()
		{
		}

		public MprFeatureSet(IEnumerable<MprFeature> mprFeats)
			: base(mprFeats)
		{
		}

		public IEnumerable<MprFeatureGroup> Groups
		{
			get
			{
				foreach (MprFeature feat in this)
				{
					if (feat.Group != null)
						yield return feat.Group;
				}
			}
		}

		public void AddOutput(MprFeatureSet mprFeats)
		{
			foreach (MprFeatureGroup group in mprFeats.Groups)
			{
				if (group.Output == MprFeatureGroupOutput.Overwrite)
				{
					foreach (MprFeature mprFeat in group.MprFeatures)
					{
						if (!mprFeats.Contains(mprFeat))
							Remove(mprFeat);
					}
				}
			}

			UnionWith(mprFeats);
		}

		public bool IsMatch(MprFeatureSet mprFeats)
		{
			MprFeatureGroup mismatchGroup;
			return IsMatch(mprFeats, out mismatchGroup);
		}

		public bool IsMatch(MprFeatureSet mprFeats, out MprFeatureGroup mismatchGroup)
		{
			foreach (MprFeatureGroup group in Groups)
			{
				bool match = true;
				foreach (MprFeature feat in group.MprFeatures)
				{
					if (Contains(feat))
					{
						if (group.MatchType == MprFeatureGroupMatchType.All)
						{
							if (!mprFeats.Contains(feat))
							{
								match = false;
								break;
							}
						}
						else
						{
							if (mprFeats.Contains(feat))
							{
								match = true;
								break;
							}
							match = false;
						}
					}
				}

				if (!match)
				{
					mismatchGroup = group;
					return false;
				}
			}

			foreach (MprFeature feat in this)
			{
				if (feat.Group == null && !mprFeats.Contains(feat))
				{
					mismatchGroup = null;
					return false;
				}
			}
			mismatchGroup = null;
			return true;
		}

		public MprFeatureSet DeepClone()
		{
			return new MprFeatureSet(this);
		}
	}
}
