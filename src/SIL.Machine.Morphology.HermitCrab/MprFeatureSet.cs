using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// This represents a set of MPR features.
    /// </summary>
    public class MprFeatureSet : HashSet<MprFeature>, ICloneable<MprFeatureSet>
    {
        public MprFeatureSet() { }

        public MprFeatureSet(IEnumerable<MprFeature> mprFeats)
            : base(mprFeats) { }

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

        public bool IsMatchRequired(MprFeatureSet mprFeats, out MprFeatureGroup mismatchGroup)
        {
            foreach (IGrouping<MprFeatureGroup, MprFeature> group in this.GroupBy(mf => mf.Group))
            {
                if (group.Key == null || group.Key.MatchType == MprFeatureGroupMatchType.All)
                {
                    if (group.Any(mf => !mprFeats.Contains(mf)))
                    {
                        mismatchGroup = group.Key;
                        return false;
                    }
                }
                else
                {
                    if (group.All(mf => !mprFeats.Contains(mf)))
                    {
                        mismatchGroup = group.Key;
                        return false;
                    }
                }
            }

            mismatchGroup = null;
            return true;
        }

        public bool IsMatchExcluded(MprFeatureSet mprFeats, out MprFeatureGroup mismatchGroup)
        {
            foreach (IGrouping<MprFeatureGroup, MprFeature> group in this.GroupBy(mf => mf.Group))
            {
                if (group.Key == null || group.Key.MatchType == MprFeatureGroupMatchType.All)
                {
                    if (group.Any(mf => mprFeats.Contains(mf)))
                    {
                        mismatchGroup = group.Key;
                        return false;
                    }
                }
                else
                {
                    if (group.All(mf => mprFeats.Contains(mf)))
                    {
                        mismatchGroup = group.Key;
                        return false;
                    }
                }
            }

            mismatchGroup = null;
            return true;
        }

        public MprFeatureSet Clone()
        {
            return new MprFeatureSet(this);
        }
    }
}
