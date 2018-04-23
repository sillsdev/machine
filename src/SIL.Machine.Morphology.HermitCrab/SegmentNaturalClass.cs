using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class SegmentNaturalClass : NaturalClass
	{
		private readonly ReadOnlyCollection<CharacterDefinition> _segments;

		public SegmentNaturalClass(IEnumerable<CharacterDefinition> segments)
		{
			_segments = new ReadOnlyCollection<CharacterDefinition>(segments.ToArray());
			FeatureStruct fs = null;
			foreach (CharacterDefinition segment in _segments)
			{
				if (fs == null)
					fs = segment.FeatureStruct.Clone();
				else
					fs.Union(segment.FeatureStruct);
			}
			if (fs == null)
				fs = new FeatureStruct();
			fs.Freeze();
			FeatureStruct = fs;
		}

		public ReadOnlyCollection<CharacterDefinition> Segments
		{
			get { return _segments; }
		}
	}
}
