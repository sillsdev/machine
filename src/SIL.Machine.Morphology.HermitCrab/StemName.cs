using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class StemName
    {
        private readonly ReadOnlyCollection<FeatureStruct> _regions;

        public StemName(params FeatureStruct[] regions) : this((IEnumerable<FeatureStruct>)regions) { }

        public StemName(IEnumerable<FeatureStruct> regions)
        {
            FeatureStruct[] regionsArray = regions.ToArray();
            if (regionsArray.Length == 0)
                throw new ArgumentException("A region must be specified.", "regions");
            _regions = new ReadOnlyCollection<FeatureStruct>(regionsArray);
        }

        public string Name { get; set; }

        public ReadOnlyCollection<FeatureStruct> Regions
        {
            get { return _regions; }
        }

        public bool IsRequiredMatch(FeatureStruct fs)
        {
            return _regions.Any(r => r.Subsumes(fs));
        }

        public bool IsExcludedMatch(FeatureStruct fs, StemName stemName)
        {
            return _regions
                .Except(
                    stemName == null ? Enumerable.Empty<FeatureStruct>() : stemName.Regions,
                    FreezableEqualityComparer<FeatureStruct>.Default
                )
                .All(r => !r.Subsumes(fs));
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}
