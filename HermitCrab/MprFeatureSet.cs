using System;
using System.Collections.Generic;
using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This represents a set of MPR features.
	/// </summary>
	public class MprFeatureSet : IDBearerSet<MprFeature>, ICloneable
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
				var groups = new IDBearerSet<MprFeatureGroup>();
				foreach (MprFeature feat in this)
				{
					if (feat.Group != null)
						groups.Add(feat.Group);
				}
				return groups;
			}
		}

		public void AddOutput(MprFeatureSet mprFeats)
		{
			foreach (MprFeatureGroup group in mprFeats.Groups)
			{
				if (group.OutputType == MprFeatureGroup.GroupOutputType.Overwrite)
				{
					foreach (MprFeature mprFeat in group.Features)
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
				foreach (MprFeature feat in group.Features)
				{
					if (Contains(feat))
					{
						if (group.MatchType == MprFeatureGroup.GroupMatchType.All)
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
							else
							{
								match = false;
							}
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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MprFeatureSet Clone()
		{
			return new MprFeatureSet(this);
		}
	}
}
