using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class StemName
	{
		private readonly ReadOnlyCollection<FeatureStruct> _regions;

		public StemName(params FeatureStruct[] regions)
			: this((IEnumerable<FeatureStruct>) regions)
		{
		}

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
