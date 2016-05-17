using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology;

namespace SIL.Machine.Translation
{
	public class GlossMorphemeMapper : IMorphemeMapper
	{
		private readonly Dictionary<string, Morpheme> _morphemes; 

		public GlossMorphemeMapper(IMorphologicalGenerator targetGenerator)
		{
			_morphemes = targetGenerator.Morphemes.ToDictionary(m => m.Gloss, StringComparer.InvariantCultureIgnoreCase);
		}

		public bool TryGetTargetMorpheme(Morpheme sourceMorpheme, out Morpheme targetMorpheme)
		{
			return _morphemes.TryGetValue(sourceMorpheme.Gloss, out targetMorpheme);
		}
	}
}
