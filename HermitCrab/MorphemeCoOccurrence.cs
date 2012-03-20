using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public class MorphemeCoOccurrence : MorphCoOccurrence<Morpheme>
	{
		public MorphemeCoOccurrence(IEnumerable<Morpheme> others, MorphCoOccurrenceAdjacency adjacency)
			: base(others, adjacency)
		{
		}

		protected override string GetMorphID(Allomorph morph)
		{
			return morph.Morpheme.ID;
		}
	}
}
