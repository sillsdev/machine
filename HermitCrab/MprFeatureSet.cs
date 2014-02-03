using System.Collections.Generic;
using SIL.Collections;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This represents a set of MPR features.
	/// </summary>
	public class MprFeatureSet : IDBearerSet<MprFeature>, IDeepCloneable<MprFeatureSet>
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
					foreach (MprFeature mprFeat in group)
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
			foreach (MprFeatureGroup group in Groups)
			{
				bool match = true;
				foreach (MprFeature feat in group)
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
					return false;
			}

			foreach (MprFeature feat in this)
			{
				if (feat.Group == null && !mprFeats.Contains(feat))
					return false;
			}
			return true;
		}

		public MprFeatureSet DeepClone()
		{
			return new MprFeatureSet(this);
		}
	}
}
