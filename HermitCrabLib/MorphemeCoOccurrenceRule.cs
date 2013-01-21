using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class MorphemeCoOccurrenceRule : MorphCoOccurrenceRule<Morpheme>
	{
		public MorphemeCoOccurrenceRule(IEnumerable<Morpheme> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override string GetMorphID(Allomorph morph)
		{
			return morph.Morpheme.ID;
		}
	}
}
