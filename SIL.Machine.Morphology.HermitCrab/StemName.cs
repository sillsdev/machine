using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class StemName
	{
		private readonly FeatureStruct[] _regions;

		public StemName(params FeatureStruct[] regions)
			: this((IEnumerable<FeatureStruct>) regions)
		{
		}

		public StemName(IEnumerable<FeatureStruct> regions)
		{
			_regions = regions.ToArray();
		}

		public string Name { get; set; }

		public IEnumerable<FeatureStruct> Regions
		{
			get { return _regions; }
		}

		public bool IsMatch(FeatureStruct fs)
		{
			return _regions.Any(r => r.IsUnifiable(fs));
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
