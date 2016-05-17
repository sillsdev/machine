using System.Collections.Generic;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class MorphemeCoOccurrenceRule : MorphCoOccurrenceRule<Morpheme>
	{
		public MorphemeCoOccurrenceRule(IEnumerable<Morpheme> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override Morpheme GetMorphObject(Allomorph morph)
		{
			return morph.Morpheme;
		}
	}
}
