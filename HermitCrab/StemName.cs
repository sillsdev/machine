using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class StemName : IDBearerBase
	{
		private readonly FeatureStruct[] _regions;

		public StemName(string id, params FeatureStruct[] regions)
			: this(id, (IEnumerable<FeatureStruct>) regions)
		{
		}

		public StemName(string id, IEnumerable<FeatureStruct> regions)
			: base(id)
		{
			_regions = regions.ToArray();
		}

		public IEnumerable<FeatureStruct> Regions
		{
			get { return _regions; }
		}

		public bool IsMatch(FeatureStruct fs)
		{
			return _regions.Any(r => r.Subsumes(fs));
		}
	}
}
