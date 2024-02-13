using System.Collections.Generic;
using System.Collections.Specialized;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
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
    public class MprFeatureGroup
    {
        private readonly ObservableHashSet<MprFeature> _mprFeatures;

        public MprFeatureGroup()
        {
            _mprFeatures = new ObservableHashSet<MprFeature>();
            _mprFeatures.CollectionChanged += MprFeaturesChanged;
        }

        private void MprFeaturesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (MprFeature mf in e.OldItems)
                    mf.Group = null;

            if (e.NewItems != null)
                foreach (MprFeature mf in e.NewItems)
                    mf.Group = this;
        }

        public string Name { get; set; }

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

        public ICollection<MprFeature> MprFeatures
        {
            get { return _mprFeatures; }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}
