using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class GlossMorphemeMapper : IMorphemeMapper
	{
		private readonly Dictionary<string, MorphemeInfo> _morphemes; 

		public GlossMorphemeMapper(ITargetGenerator targetGenerator)
		{
			_morphemes = targetGenerator.Morphemes.ToDictionary(m => m.Gloss, StringComparer.InvariantCultureIgnoreCase);
		}

		public bool TryGetTargetMorpheme(MorphemeInfo sourceMorpheme, out MorphemeInfo targetMorpheme)
		{
			return _morphemes.TryGetValue(sourceMorpheme.Gloss, out targetMorpheme);
		}
	}
}
